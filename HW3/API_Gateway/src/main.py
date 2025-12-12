import os
import httpx
from fastapi import FastAPI, UploadFile, File, Form, HTTPException

app = FastAPI(
    title="Anti-Plagiarism System API Gateway",
    version="1.0",
    description="🎓 Центральный шлюз для системы проверки на плагиат"
)

FILE_URL = os.getenv("FILE_SERVICE_URL", "http://file-storing-service:8001")
ANALYSIS_URL = os.getenv("ANALYSIS_SERVICE_URL", "http://file-analysis-service: 8002")

# Разрешённые расширения файлов
ALLOWED_EXTENSIONS = {'. txt', '.py', '.java', '.cpp', '. c', '.js', '.html', '. css', '.md', '.json'}


def is_allowed_file(filename: str) -> bool:
    """Проверяет, что файл имеет разрешённое расширение"""
    ext = os.path.splitext(filename)[1].lower()
    return ext in ALLOWED_EXTENSIONS


@app.post("/submit", summary="📤 Отправить работу на проверку")
async def submit_homework(
        student_name: str = Form(..., description="ФИО студента"),
        task_id: str = Form(..., description="ID задания"),
        file: UploadFile = File(..., description="Файл работы (только текстовые форматы)")
):
    """
    Студент отправляет работу на проверку.

    ⚠️ **Поддерживаемые форматы:** .txt, .py, .java, . cpp, .c, .js, .html, .css, . md, .json

    Процесс:
    1. ✅ Валидация файла
    2. 📁 Файл загружается в File Storing Service
    3. 🔍 Запускается анализ в File Analysis Service
    4. 💾 Результат сохраняется обратно в БД
    """

    # Проверка расширения файла
    if not is_allowed_file(file.filename):
        raise HTTPException(
            400,
            detail=f"❌ Неподдерживаемый формат файла.  Разрешены: {', '.join(ALLOWED_EXTENSIONS)}"
        )

    async with httpx.AsyncClient() as client:
        # 1. Загружаем файл в Хранилище
        try:
            file_bytes = await file.read()

            # Проверяем, что файл можно прочитать как текст
            try:
                file_bytes.decode('utf-8')
            except UnicodeDecodeError:
                raise HTTPException(
                    400,
                    detail="❌ Файл содержит бинарные данные. Загружайте только текстовые файлы."
                )

            files = {'file': (file.filename, file_bytes, file.content_type or 'text/plain')}
            data = {'student_name': student_name, 'task_id': task_id}

            resp_storage = await client.post(
                f"{FILE_URL}/upload/",
                data=data,
                files=files,
                timeout=30.0
            )
            resp_storage.raise_for_status()
            storage_data = resp_storage.json()
        except HTTPException:
            raise
        except httpx.HTTPStatusError as e:
            raise HTTPException(503, detail=f"Storage service error: {e}")
        except Exception as e:
            raise HTTPException(503, detail=f"Storage service unavailable: {e}")

        # 2. Отправляем на Анализ
        analysis_res = {"verdict": "Analysis Failed"}
        try:
            payload = {
                "submission_id": storage_data["id"],
                "file_path": storage_data["file_path"],
                "task_id": task_id,
                "student_name": student_name
            }
            resp = await client.post(
                f"{ANALYSIS_URL}/analyze/",
                json=payload,
                timeout=60.0
            )
            if resp.status_code == 200:
                analysis_res = resp.json()
        except Exception as e:
            analysis_res = {"verdict": f"Analysis Error: {e}"}

        # 3. Сохраняем вердикт обратно в БД
        try:
            await client.patch(
                f"{FILE_URL}/submission/{storage_data['id']}/verdict",
                json={"verdict": analysis_res.get("verdict", "Error")},
                timeout=10.0
            )
        except:
            pass

    # Определяем эмодзи для результата
    verdict = analysis_res.get("verdict", "")
    if "PLAGIARISM" in verdict:
        emoji = "🚨"
    elif "Clean" in verdict:
        emoji = "✅"
    else:
        emoji = "⚠️"

    return {
        "message": f"{emoji} Работа получена и проанализирована",
        "submission_id": storage_data["id"],
        "student": student_name,
        "task_id": task_id,
        "filename": file.filename,
        "result": analysis_res
    }


@app.get("/reports/{task_id}", summary="📊 Получить отчет по заданию (для преподавателя)")
async def get_teacher_report(task_id: str):
    """
    Преподаватель запрашивает отчет по всем работам для данного задания.

    Возвращает:
    - 📋 Список всех работ с вердиктами
    - 📈 Статистику по плагиату
    - ⏰ Отсортировано по времени сдачи
    """

    async with httpx.AsyncClient() as client:
        try:
            resp = await client.get(
                f"{FILE_URL}/reports/{task_id}",
                timeout=15.0
            )
            resp.raise_for_status()
            return resp.json()
        except httpx.HTTPStatusError as e:
            raise HTTPException(503, detail=f"Report service error: {e}")
        except Exception as e:
            raise HTTPException(503, detail=f"Report service unavailable: {e}")


@app.get("/wordcloud/{submission_id}", summary="☁️ Получить облако слов для работы")
async def get_wordcloud(submission_id: int):
    """
    Генерирует облако слов для указанной работы.

    Возвращает:
    - 🖼️ URL изображения облака слов (QuickChart)
    - 🔝 Топ-10 самых частых слов
    - 📊 Статистику по словам
    """

    async with httpx.AsyncClient() as client:
        # 1. Получаем информацию о работе по ID
        try:
            resp = await client.get(
                f"{FILE_URL}/submission/{submission_id}",
                timeout=10.0
            )
            resp.raise_for_status()
            submission = resp.json()
        except httpx.HTTPStatusError as e:
            if e.response.status_code == 404:
                raise HTTPException(404, detail="❌ Работа не найдена")
            raise HTTPException(503, detail=f"Storage service error: {e}")
        except Exception as e:
            raise HTTPException(503, detail=f"Storage service unavailable:  {e}")

        # 2. Генерируем облако слов
        try:
            resp = await client.post(
                f"{ANALYSIS_URL}/wordcloud/",
                json={"file_path": submission["file_path"]},
                timeout=30.0
            )
            resp.raise_for_status()
            result = resp.json()

            if "error" in result:
                raise HTTPException(400, detail=result["error"])

            return result

        except HTTPException:
            raise
        except httpx.HTTPStatusError as e:
            raise HTTPException(503, detail=f"Analysis service error: {e}")
        except Exception as e:
            raise HTTPException(503, detail=f"WordCloud generation failed: {e}")


@app.get("/submissions/{task_id}", summary="📁 Список работ по заданию")
async def list_submissions(task_id: str):
    """
    Возвращает список всех работ по заданию.

    Полезно для:
    - 📋 Просмотра всех сданных работ
    - 🔗 Получения ID для генерации облака слов
    """

    async with httpx.AsyncClient() as client:
        try:
            resp = await client.get(
                f"{FILE_URL}/history/{task_id}",
                timeout=10.0
            )
            resp.raise_for_status()
            return resp.json()
        except httpx.HTTPStatusError as e:
            raise HTTPException(503, detail=f"Storage service error: {e}")
        except Exception as e:
            raise HTTPException(503, detail=f"Storage service unavailable:  {e}")


@app.get("/", summary="🏠 Проверка работоспособности")
async def root():
    """Проверка статуса API Gateway"""
    return {
        "service": "🎓 Anti-Plagiarism API Gateway",
        "status": "✅ running",
        "version": "1.0",
        "endpoints": {
            "submit": "POST /submit - отправить работу",
            "reports": "GET /reports/{task_id} - отчет преподавателя",
            "wordcloud": "GET /wordcloud/{submission_id} - облако слов",
            "submissions": "GET /submissions/{task_id} - список работ",
            "health": "GET /health - проверка сервисов"
        }
    }


@app.get("/health", summary="🏥 Health check всех сервисов")
async def health_check():
    """Проверяет доступность всех микросервисов"""

    services = {
        "api_gateway": "✅ ok",
        "file_storing": "❓ unknown",
        "file_analysis": "❓ unknown"
    }

    async with httpx.AsyncClient() as client:
        # Проверяем File Storing
        try:
            resp = await client.get(f"{FILE_URL}/", timeout=5.0)
            services["file_storing"] = "✅ ok" if resp.status_code == 200 else "⚠️ error"
        except:
            services["file_storing"] = "❌ down"

        # Проверяем File Analysis
        try:
            resp = await client.get(f"{ANALYSIS_URL}/", timeout=5.0)
            services["file_analysis"] = "✅ ok" if resp.status_code == 200 else "⚠️ error"
        except:
            services["file_analysis"] = "❌ down"

    all_ok = all("ok" in status for status in services.values())

    return {
        "status": "✅ healthy" if all_ok else "⚠️ degraded",
        "services": services
    }