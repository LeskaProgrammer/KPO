using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Persistence.InMemory
{
    /// <summary>
    /// Простая in-memory реализация <see cref="ICategoryRepository"/>.
    /// </summary>
    /// <remarks>
    /// Хранение осуществляется в <see cref="Dictionary{TKey, TValue}"/> внутри процесса:
    /// <list type="bullet">
    /// <item><description>Данные не персистятся между запусками приложения.</description></item>
    /// <item><description>Потокобезопасность не гарантируется (нет синхронизации доступа).</description></item>
    /// <item><description>Операции выборки выполняются линейно по количеству записей словаря.</description></item>
    /// </list>
    /// Подходит для демо/тестов и как «фейковая» инфраструктура.
    /// </remarks>
    public sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        /// <summary>
        /// Основное хранилище категорий.
        /// Ключ — <see cref="Category.Id"/>.
        /// </summary>
        private readonly Dictionary<string, Category> _store = new();

        /// <summary>
        /// Возвращает категорию по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <returns>
        /// Экземпляр <see cref="Category"/>, если найден, иначе <c>null</c>.
        /// </returns>
        public Category? Get(string id) => _store.TryGetValue(id, out var c) ? c : null;

        /// <summary>
        /// Возвращает последовательность всех категорий.
        /// </summary>
        /// <remarks>
        /// Порядок перечисления не определён (зависит от внутреннего состояния словаря).
        /// </remarks>
        public IEnumerable<Category> GetAll() => _store.Values;

        /// <summary>
        /// Добавляет новую категорию.
        /// </summary>
        /// <param name="category">Добавляемая категория.</param>
        /// <remarks>
        /// Используется <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>.
        /// Бросит <see cref="ArgumentException"/>, если запись с таким <see cref="Category.Id"/> уже существует.
        /// </remarks>
        public void Add(Category category) => _store.Add(category.Id, category);

        /// <summary>
        /// Обновляет существующую категорию (или добавляет, если ещё не было).
        /// </summary>
        /// <param name="category">Категория для сохранения.</param>
        /// <remarks>
        /// Индексатор словаря выполняет upsert: при наличии ключа — перезапишет,
        /// при отсутствии — добавит новую запись.
        /// </remarks>
        public void Update(Category category) => _store[category.Id] = category;

        /// <summary>
        /// Удаляет категорию по идентификатору (если существует).
        /// </summary>
        /// <param name="id">Идентификатор удаляемой категории.</param>
        /// <remarks>
        /// Если записи нет — метод тихо вернёт <c>false</c> внутри <see cref="Dictionary{TKey, TValue}.Remove(TKey)"/> и никаких исключений не будет.
        /// </remarks>
        public void Remove(string id) => _store.Remove(id);
    }
}
