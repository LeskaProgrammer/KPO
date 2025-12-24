import asyncio
import json
import logging
import os
from contextlib import asynccontextmanager

import aio_pika
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sqlalchemy import Column, Integer, String, Float, Boolean, select, update
from sqlalchemy.ext.asyncio import create_async_engine, async_sessionmaker, AsyncSession
from sqlalchemy.orm import declarative_base

# Настройки
DATABASE_URL = os.getenv("DATABASE_URL")
RABBITMQ_URL = os.getenv("RABBITMQ_URL")
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("PaymentService")

Base = declarative_base()


# --- Модели БД ---
class Account(Base):
    __tablename__ = "accounts"
    user_id = Column(Integer, primary_key=True, index=True)
    balance = Column(Float, default=0.0)


class Inbox(Base):
    """Inbox Pattern: храним ID обработанных сообщений (идемпотентность)"""
    __tablename__ = "inbox"
    message_id = Column(String, primary_key=True)
    processed = Column(Boolean, default=True)


class Outbox(Base):
    """Outbox Pattern: события для отправки в RabbitMQ"""
    __tablename__ = "outbox"
    id = Column(Integer, primary_key=True, autoincrement=True)
    routing_key = Column(String)
    payload = Column(String)
    processed = Column(Boolean, default=False)


engine = create_async_engine(DATABASE_URL, echo=False)
AsyncSessionLocal = async_sessionmaker(engine, expire_on_commit=False)


# --- Фоновые процессы ---

async def rabbitmq_consumer():
    """Слушает очередь задач на оплату"""
    connection = None
    while connection is None:
        try:
            connection = await aio_pika.connect_robust(RABBITMQ_URL)
        except Exception as e:
            logger.warning(f"RabbitMQ not ready yet, retrying in 5s... Error: {e}")
            await asyncio.sleep(5)
    channel = await connection.channel()
    exchange = await channel.declare_exchange("shop_events", type="topic")
    queue = await channel.declare_queue("payment_tasks", durable=True)
    await queue.bind(exchange, routing_key="order.created")

    async with queue.iterator() as queue_iter:
        async for message in queue_iter:
            async with message.process():
                # Уникальный ID сообщения для дедупликации
                msg_id = message.message_id or str(hash(message.body))
                data = json.loads(message.body.decode())

                async with AsyncSessionLocal() as session:
                    async with session.begin():
                        # 1. Проверка Inbox (Идемпотентность)
                        # Если сообщение уже есть в Inbox, значит мы его обрабатывали
                        exists = await session.execute(select(Inbox).where(Inbox.message_id == msg_id))
                        if exists.scalar():
                            logger.info(f"Message {msg_id} duplicate. Skipping.")
                            continue

                        # 2. Бизнес-логика: Атомарное списание
                        # Пытаемся уменьшить баланс, если денег хватает
                        user_id = data['user_id']
                        amount = data['amount']
                        order_id = data['order_id']

                        result = await session.execute(
                            update(Account)
                            .where(Account.user_id == user_id, Account.balance >= amount)
                            .values(balance=Account.balance - amount)
                        )

                        success = result.rowcount > 0
                        status = "FINISHED" if success else "CANCELLED"

                        # 3. Запись в Inbox (мы обработали это сообщение)
                        session.add(Inbox(message_id=msg_id))

                        # 4. Запись в Outbox (Transactional Outbox)
                        # Ответное сообщение также пишется в той же транзакции
                        response_payload = json.dumps({
                            "order_id": order_id,
                            "status": status,
                            "user_id": user_id
                        })
                        session.add(Outbox(routing_key="payment.processed", payload=response_payload))

                        logger.info(f"Order {order_id} processed: {status}")




async def outbox_processor():

    connection = None
    while connection is None:
        try:
            connection = await aio_pika.connect_robust(RABBITMQ_URL)
        except Exception:
            await asyncio.sleep(5)

    channel = await connection.channel()
    exchange = await channel.declare_exchange("shop_events", type="topic")

    while True:
        try:
            async with AsyncSessionLocal() as session:
                async with session.begin():
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
        await asyncio.sleep(0.5)

# --- FastAPI App ---
@asynccontextmanager
async def lifespan(app: FastAPI):
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    asyncio.create_task(rabbitmq_consumer())
    asyncio.create_task(outbox_processor())
    yield


app = FastAPI(lifespan=lifespan)


class BalanceReq(BaseModel):
    user_id: int
    amount: float = 0


@app.post("/accounts")
async def create_account(req: BalanceReq):
    async with AsyncSessionLocal() as session:
        async with session.begin():
            session.add(Account(user_id=req.user_id, balance=0.0))
    return {"status": "created"}


@app.post("/accounts/topup")
async def topup(req: BalanceReq):
    async with AsyncSessionLocal() as session:
        async with session.begin():
            await session.execute(
                update(Account).where(Account.user_id == req.user_id)
                .values(balance=Account.balance + req.amount)
            )
    return {"status": "ok"}


@app.get("/accounts/{user_id}")
async def get_balance(user_id: int):
    async with AsyncSessionLocal() as session:
        acc = await session.get(Account, user_id)
        if not acc: raise HTTPException(404, "Not found")
        return {"balance": acc.balance}