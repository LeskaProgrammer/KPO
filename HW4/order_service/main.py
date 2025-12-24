import asyncio
import json
import logging
import os
import uuid
from typing import Dict, List
from contextlib import asynccontextmanager

import aio_pika
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from pydantic import BaseModel
from sqlalchemy import Column, Integer, String, Float, Boolean, select, update
from sqlalchemy.ext.asyncio import create_async_engine, async_sessionmaker
from sqlalchemy.orm import declarative_base

DATABASE_URL = os.getenv("DATABASE_URL")
RABBITMQ_URL = os.getenv("RABBITMQ_URL")
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("OrderService")

Base = declarative_base()


# --- Модели ---
class Order(Base):
    __tablename__ = "orders"
    id = Column(Integer, primary_key=True, autoincrement=True)
    user_id = Column(Integer)
    description = Column(String)
    amount = Column(Float)
    status = Column(String, default="NEW")


class Outbox(Base):
    __tablename__ = "outbox"
    id = Column(Integer, primary_key=True, autoincrement=True)
    routing_key = Column(String)
    payload = Column(String)
    processed = Column(Boolean, default=False)


engine = create_async_engine(DATABASE_URL, echo=False)
AsyncSessionLocal = async_sessionmaker(engine, expire_on_commit=False)


# --- WebSocket Manager ---
class ConnectionManager:
    def __init__(self):
        self.active_connections: Dict[int, List[WebSocket]] = {}

    async def connect(self, websocket: WebSocket, user_id: int):
        await websocket.accept()
        if user_id not in self.active_connections:
            self.active_connections[user_id] = []
        self.active_connections[user_id].append(websocket)

    def disconnect(self, websocket: WebSocket, user_id: int):
        if user_id in self.active_connections:
            if websocket in self.active_connections[user_id]:
                self.active_connections[user_id].remove(websocket)

    async def send_to_user(self, message: str, user_id: int):
        if user_id in self.active_connections:
            for connection in self.active_connections[user_id]:
                await connection.send_text(message)


manager = ConnectionManager()



async def outbox_processor():
    """Вычитывает Outbox таблицу и отправляет события в брокер"""


    connection = None
    while connection is None:
        try:
            connection = await aio_pika.connect_robust(RABBITMQ_URL)
        except Exception:
            await asyncio.sleep(5)  # Ждем, если кролик еще не встал

    channel = await connection.channel()
    exchange = await channel.declare_exchange("shop_events", type="topic")

    while True:
        try:
            async with AsyncSessionLocal() as session:
                async with session.begin():
                    # Выбираем необработанные записи
                    stmt = select(Outbox).where(Outbox.processed == False).limit(10).with_for_update(skip_locked=True)
                    messages = (await session.execute(stmt)).scalars().all()

                    for msg in messages:

                        await exchange.publish(
                            aio_pika.Message(
                                body=msg.payload.encode(),
                                message_id=str(msg.id)
                            ),
                            routing_key=msg.routing_key
                        )
                        msg.processed = True

        except Exception as e:
            logger.error(f"Outbox error: {e}")
            # Если соединение отвалилось, connect_robust сам переподключится,
            # но можно добавить небольшую паузу
            await asyncio.sleep(1)

        await asyncio.sleep(0.5)







async def payment_result_consumer():
    """Получает результаты оплаты от PaymentService"""
    connection = None
    while connection is None:
        try:
            connection = await aio_pika.connect_robust(RABBITMQ_URL)
        except Exception as e:
            logger.warning(f"RabbitMQ not ready yet, retrying in 5s... Error: {e}")
            await asyncio.sleep(5)
    channel = await connection.channel()
    exchange = await channel.declare_exchange("shop_events", type="topic")
    queue = await channel.declare_queue("order_updates", durable=True)
    await queue.bind(exchange, routing_key="payment.processed")

    async with queue.iterator() as queue_iter:
        async for message in queue_iter:
            async with message.process():
                data = json.loads(message.body.decode())
                order_id = data['order_id']
                status = data['status']
                user_id = data['user_id']

                logger.info(f"Update Order {order_id} -> {status}")

                async with AsyncSessionLocal() as session:
                    async with session.begin():
                        await session.execute(update(Order).where(Order.id == order_id).values(status=status))

                # Push уведомление через WebSocket
                await manager.send_to_user(json.dumps(data), user_id)


# --- API ---
@asynccontextmanager
async def lifespan(app: FastAPI):
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    asyncio.create_task(outbox_processor())
    asyncio.create_task(payment_result_consumer())
    yield


app = FastAPI(lifespan=lifespan)


class OrderReq(BaseModel):
    user_id: int
    description: str
    amount: float


@app.post("/orders")
async def create_order(req: OrderReq):
    async with AsyncSessionLocal() as session:
        async with session.begin():
            # 1. Сохраняем заказ
            order = Order(user_id=req.user_id, description=req.description, amount=req.amount, status="NEW")
            session.add(order)
            await session.flush()

            # 2. Сохраняем задачу в Outbox
            payload = json.dumps({"order_id": order.id, "user_id": req.user_id, "amount": req.amount})
            session.add(Outbox(routing_key="order.created", payload=payload))

            return {"id": order.id, "status": "NEW"}


@app.get("/orders")
async def list_orders():
    async with AsyncSessionLocal() as session:
        res = await session.execute(select(Order).order_by(Order.id.desc()))
        return res.scalars().all()


@app.websocket("/ws/{user_id}")
async def ws_endpoint(websocket: WebSocket, user_id: int):
    await manager.connect(websocket, user_id)
    try:
        while True: await websocket.receive_text()  # Keep connection alive
    except WebSocketDisconnect:
        manager.disconnect(websocket, user_id)