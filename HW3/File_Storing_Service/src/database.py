import os
from sqlalchemy.ext.asyncio import AsyncSession, create_async_engine
from sqlalchemy.orm import sessionmaker, declarative_base
from sqlalchemy import Column, Integer, String, DateTime, Text
from sqlalchemy.sql import func

DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql+asyncpg://postgres:secretpassword@postgres:5432/files_db",
)

engine = create_async_engine(DATABASE_URL, echo=False)
AsyncSessionLocal = sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)
Base = declarative_base()


class Submission(Base):
    """📄 Модель для хранения информации о сданных работах"""

    __tablename__ = "submissions"

    id = Column(Integer, primary_key=True, index=True)
    filename = Column(String(255), nullable=False)
    file_path = Column(String(512), nullable=False)
    student_name = Column(String(255), nullable=False, index=True)
    task_id = Column(String(100), nullable=False, index=True)
    uploaded_at = Column(DateTime(timezone=True), server_default=func.now())

    # === Поля для анализа ===
    verdict = Column(Text, default="⏳ Pending")
    similarity_score = Column(Integer, default=0)
    compared_with = Column(String(255), nullable=True)  # С кем сравнивали


async def get_db():
    async with AsyncSessionLocal() as session:
        yield session