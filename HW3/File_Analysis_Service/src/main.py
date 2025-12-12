import os
import re
import httpx
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from collections import Counter
from urllib.parse import quote

app = FastAPI(title="🔍 File Analysis Service", version="1.0")


class AnalyzeRequest(BaseModel):
    submission_id: int
    file_path: str
    task_id: str
    student_name: str


FILE_SERVICE_URL = os.getenv("FILE_SERVICE_URL", "http://file-storing-service:8001")

# Стоп-слова для русского и английского
STOP_WORDS = {
    # Русские
    'и', 'в', 'на', 'не', 'что', 'с', 'как', 'а', 'то', 'все', 'она', 'так',
    'его', 'но', 'да', 'ты', 'к', 'у', 'же', 'вы', 'за', 'бы', 'по', 'только',
    'её', 'мне', 'было', 'вот', 'от', 'меня', 'ещё', 'нет', 'о', 'из', 'ему',
    'теперь', 'когда', 'уже', 'для', 'вас', 'ни', 'раз', 'если', 'или', 'это',
    # Английские
    'the', 'a', 'an', 'and', 'or', 'but', 'in', 'on', 'at', 'to', 'for', 'of',
    'is', 'are', 'was', 'were', 'be', 'been', 'being', 'have', 'has', 'had',
    'do', 'does', 'did', 'will', 'would', 'could', 'should', 'may', 'might',
    'it', 'this', 'that', 'these', 'those', 'i', 'you', 'he', 'she', 'we', 'they',
    # Код
    'def', 'class', 'import', 'from', 'return', 'if', 'else', 'elif', 'for',
    'while', 'try', 'except', 'with', 'as', 'pass', 'break', 'continue', 'self'
}


def read_file_safely(file_path: str) -> tuple[str, bool]:
    """
    Безопасно читает файл с несколькими попытками кодировки.
    Возвращает (текст, успех)
    """
    encodings = ['utf-8', 'cp1251', 'latin-1', 'utf-16']

    for encoding in encodings:
        try:
            with open(file_path, "r", encoding=encoding) as f:
                return f.read(), True
        except (UnicodeDecodeError, UnicodeError):
            continue
        except FileNotFoundError:
            return "", False
        except Exception:
            continue

    # Попытка прочитать как бинарный и декодировать
    try:
        with open(file_path, "rb") as f:
            content = f.read()
            # Пробуем определить текстовый контент
            try:
                return content.decode('utf-8', errors='ignore'), True
            except:
                return "", False
    except:
        return "", False


@app.post("/analyze/")
async def analyze(req: AnalyzeRequest):
    """
    🔍 Анализирует работу на плагиат.

    Логика:
    - Если сходство > 80% с более ранней работой другого студента → PLAGIARISM DETECTED
    - Если сходство <= 80% → Clean, но показываем ближайшего
    - Если это первая работа → Clean (No previous submissions)
    """

    # 1. Читаем текущий файл
    current_text, success = read_file_safely(req.file_path)

    if not success or not current_text:
        return {"verdict": "⚠️ Read Error:  Could not read file"}

    if len(current_text.strip()) < 10:
        return {"verdict": "✅ Clean (Too short to analyze)"}

    # 2. Получаем историю (отсортирована по времени:  Старые -> Новые)
    history = []
    async with httpx.AsyncClient() as client:
        try:
            resp = await client.get(f"{FILE_SERVICE_URL}/history/{req.task_id}", timeout=10.0)
            if resp.status_code == 200:
                history = resp.json()
        except Exception as e:
            return {"verdict": f"⚠️ Service Error: {e}"}

    # 3. Подготовка документов для сравнения
    documents = [current_text]
    valid_entries = []  # [{student, uploaded_at}, ...]

    for item in history:
        # Пропускаем саму работу
        if str(item["id"]) == str(req.submission_id):
            continue
        # Пропускаем пересдачи того же студента
        if item["student_name"] == req.student_name:
            continue

        text, success = read_file_safely(item["file_path"])
        if success and len(text.strip()) > 10:
            documents.append(text)
            valid_entries.append({
                "student": item["student_name"],
                "uploaded_at": item["uploaded_at"]
            })

    # 4. Если нет с чем сравнивать
    if len(documents) == 1:
        return {"verdict": "✅ Clean (No previous submissions to compare)"}

    # 5. Вычисляем сходство
    try:
        vec = TfidfVectorizer(
            min_df=1,
            stop_words=list(STOP_WORDS),
            ngram_range=(1, 3)  # Учитываем фразы для лучшего обнаружения
        )
        tfidf = vec.fit_transform(documents)

        # Сравниваем текущую работу (индекс 0) со всеми остальными
        cosine_sim = cosine_similarity(tfidf[0:1], tfidf[1:])

        # Находим максимальное сходство
        max_index = cosine_sim[0].argmax()
        max_similarity = cosine_sim[0][max_index]
        max_percent = int(max_similarity * 100)
        closest_student = valid_entries[max_index]["student"]

        # Решение о плагиате
        if max_percent > 80:
            verdict = f"🚨 PLAGIARISM DETECTED (Copied from {closest_student}, similarity: {max_percent}%)"
        elif max_percent > 50:
            verdict = f"⚠️ Suspicious (Similar to {closest_student}, similarity: {max_percent}%)"
        else:
            verdict = f"✅ Clean (Closest:  {closest_student}, similarity: {max_percent}%)"

        return {"verdict": verdict, "similarity": max_percent, "compared_with": closest_student}

    except Exception as e:
        return {"verdict": f"⚠️ Analysis Error: {e}"}


class WordCloudRequest(BaseModel):
    file_path: str


@app.post("/wordcloud/")
async def generate_wordcloud(req: WordCloudRequest):
    """
    ☁️ Генерирует облако слов через QuickChart API.

    Возвращает:
    - URL изображения облака слов
    - Топ-10 самых частых слов
    - Статистику
    """

    # Читаем файл
    text, success = read_file_safely(req.file_path)

    if not success:
        return {"error": "❌ Cannot read file"}

    if not text.strip():
        return {"error": "❌ File is empty"}

    # Обработка текста - находим слова (буквы латиницы и кириллицы)
    words = re.findall(r'\b[а-яёa-z]{3,}\b', text.lower())

    if not words:
        return {"error": "❌ No valid words found in file"}

    # Фильтруем стоп-слова
    words = [w for w in words if w not in STOP_WORDS]

    if not words:
        return {"error": "❌ Only stop-words found in file"}

    # Считаем частоты
    word_freq = Counter(words)
    top_words = word_freq.most_common(50)  # Топ-50 для облака

    # Формируем данные для QuickChart
    # Формат: "слово: частота,слово:частота,..."
    word_data = ",".join([f"{word}:{count}" for word, count in top_words])

    # URL-кодирование для безопасной передачи
    encoded_data = quote(word_data)

    # URL для QuickChart Word Cloud API
    quickchart_url = f"https://quickchart.io/wordcloud?text={encoded_data}&width=800&height=400&fontScale=15&backgroundColor=white"

    # Топ-10 с эмодзи
    top_10_with_emoji = [
        {"word": word, "count": count, "rank": f"#{i + 1}"}
        for i, (word, count) in enumerate(word_freq.most_common(10))
    ]

    return {
        "wordcloud_url": quickchart_url,
        "top_10_words": top_10_with_emoji,
        "statistics": {
            "total_words": len(words),
            "unique_words": len(word_freq),
            "most_common_word": word_freq.most_common(1)[0] if word_freq else None
        }
    }


@app.get("/")
async def root():
    return {
        "service": "🔍 File Analysis Service",
        "status": "✅ running",
        "features": [
            "📊 Plagiarism detection (TF-IDF + Cosine Similarity)",
            "☁️ Word cloud generation (QuickChart API)",
            "🔤 Stop-words filtering (RU/EN)"
        ]
    }