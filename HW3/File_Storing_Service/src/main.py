import os
from pathlib import Path
from fastapi import FastAPI, UploadFile, File, Form, Depends, HTTPException
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from .database import get_db, Submission, engine, Base
from pydantic import BaseModel
from datetime import datetime

app = FastAPI(title="📁 File Storing Service", version="1.0")

# Папка для загрузки
UPLOAD_DIR = Path("/app/uploaded_files")
UPLOAD_DIR.mkdir(parents=True, exist_ok=True)


@app.on_event("startup")
async def startup():
    """Создаем таблицы при старте"""
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    print("🚀 File Storing Service started!")


@app.post("/upload/")
async def upload_file(
        student_name: str = Form(...),
        task_id: str = Form(...),
        file: UploadFile = File(...),
        db: AsyncSession = Depends(get_db)
):
    """📤 Загружает файл и сохраняет запись в БД"""

    # Генерируем уникальное имя файла с timestamp
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    safe_filename = file.filename.replace(" ", "_")
    file_path = UPLOAD_DIR / f"{task_id}_{student_name}_{timestamp}_{safe_filename}"

    # Сохраняем файл
    content = await file.read()
    with open(file_path, "wb") as f:
        f.write(content)

    # Создаем запись в БД
    new_submission = Submission(
        filename=file.filename,
        file_path=str(file_path),
        student_name=student_name,
        task_id=task_id,
        verdict="⏳ Pending"
    )

    db.add(new_submission)
    await db.commit()
    await db.refresh(new_submission)

    return {
        "id": new_submission.id,
        "filename": new_submission.filename,
        "file_path": str(file_path),
        "student_name": student_name,
        "task_id": task_id,
        "uploaded_at": new_submission.uploaded_at.isoformat()
    }


@app.get("/submission/{submission_id}")
async def get_submission(submission_id: int, db: AsyncSession = Depends(get_db)):
    """
    📄 Возвращает информацию об одной работе по ID
    """
    result = await db.execute(
        select(Submission).where(Submission.id == submission_id)
    )
    submission = result.scalar_one_or_none()

    if not submission:
        raise HTTPException(404, detail="❌ Submission not found")

    return {
        "id": submission.id,
        "student_name": submission.student_name,
        "filename": submission.filename,
        "file_path": submission.file_path,
        "task_id": submission.task_id,
        "uploaded_at": submission.uploaded_at.isoformat(),
        "verdict": submission.verdict
    }


@app.get("/history/{task_id}")
async def get_history(task_id: str, db: AsyncSession = Depends(get_db)):
    """
    📜 Возвращает историю всех сдач по заданию,
    отсортированную по времени (от старых к новым)
    """
    result = await db.execute(
        select(Submission)
        .where(Submission.task_id == task_id)
        .order_by(Submission.uploaded_at.asc())  # От старых к новым
    )
    submissions = result.scalars().all()

    return [
        {
            "id": sub.id,
            "student_name": sub.student_name,
            "filename": sub.filename,
            "file_path": sub.file_path,
            "uploaded_at": sub.uploaded_at.isoformat(),
            "verdict": sub.verdict
        }
        for sub in submissions
    ]


class VerdictUpdate(BaseModel):
    verdict: str


@app.patch("/submission/{submission_id}/verdict")
async def update_verdict(
        submission_id: int,
        update: VerdictUpdate,
        db: AsyncSession = Depends(get_db)
):
    """✏️ Обновляет вердикт проверки для работы"""
    result = await db.execute(
        select(Submission).where(Submission.id == submission_id)
    )
    submission = result.scalar_one_or_none()

    if not submission:
        raise HTTPException(404, detail="❌ Submission not found")

    submission.verdict = update.verdict
    await db.commit()

    return {"message": "✅ Verdict updated", "verdict": update.verdict}


@app.get("/reports/{task_id}")
async def get_reports(task_id: str, db: AsyncSession = Depends(get_db)):
    """
    📊 Возвращает отчет для преподавателя:
    все работы по заданию с вердиктами,
    отсортированные по времени сдачи
    """
    result = await db.execute(
        select(Submission)
        .where(Submission.task_id == task_id)
        .order_by(Submission.uploaded_at.asc())  # От старых к новым
    )
    submissions = result.scalars().all()

    if not submissions:
        return {
            "task_id": task_id,
            "total_submissions": 0,
            "plagiarism_detected": 0,
            "message": "📭 Работ по данному заданию пока нет",
            "submissions": []
        }

    # Формируем отчет с анализом
    report_data = []
    plagiarism_count = 0
    clean_count = 0
    pending_count = 0

    for sub in submissions:
        verdict_lower = sub.verdict.lower()
        has_plagiarism = "plagiarism" in verdict_lower
        is_clean = "clean" in verdict_lower
        is_pending = "pending" in verdict_lower

        if has_plagiarism:
            plagiarism_count += 1
            status_emoji = "🚨"
        elif is_clean:
            clean_count += 1
            status_emoji = "✅"
        else:
            pending_count += 1
            status_emoji = "⏳"

        report_data.append({
            "submission_id": sub.id,
            "student_name": sub.student_name,
            "filename": sub.filename,
            "uploaded_at": sub.uploaded_at.isoformat(),
            "verdict": sub.verdict,
            "status": status_emoji,
            "has_plagiarism": has_plagiarism
        })

    return {
        "task_id": task_id,
        "total_submissions": len(submissions),
        "statistics": {
            "plagiarism_detected": plagiarism_count,
            "clean": clean_count,
            "pending": pending_count
        },
        "summary": f"📊 Всего работ: {len(submissions)} | ✅ Чистых: {clean_count} | 🚨 Плагиат: {plagiarism_count} | ⏳ Ожидают:  {pending_count}",
        "submissions": report_data
    }


@app.get("/stats")
async def get_stats(db: AsyncSession = Depends(get_db)):
    """📈 Возвращает общую статистику системы"""

    result = await db.execute(select(Submission))
    all_submissions = result.scalars().all()

    total = len(all_submissions)
    plagiarism = sum(1 for s in all_submissions if "plagiarism" in s.verdict.lower())
    clean = sum(1 for s in all_submissions if "clean" in s.verdict.lower())

    # Уникальные студенты и задания
    students = set(s.student_name for s in all_submissions)
    tasks = set(s.task_id for s in all_submissions)

    return {
        "total_submissions": total,
        "unique_students": len(students),
        "unique_tasks": len(tasks),
        "plagiarism_cases": plagiarism,
        "clean_submissions": clean,
        "plagiarism_rate": f"{(plagiarism / total * 100):.1f}%" if total > 0 else "0%"
    }


@app.get("/")
async def root():
    return {
        "service": "📁 File Storing Service",
        "status": "✅ running",
        "endpoints": [
            "POST /upload/ - загрузить файл",
            "GET /submission/{id} - получить работу",
            "GET /history/{task_id} - история задания",
            "GET /reports/{task_id} - отчет преподавателя",
            "PATCH /submission/{id}/verdict - обновить вердикт"
        ]
    }