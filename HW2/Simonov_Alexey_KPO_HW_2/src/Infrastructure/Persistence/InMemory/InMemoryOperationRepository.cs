using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Persistence.InMemory
{
    /// <summary>
    /// Простая in-memory реализация <see cref="IOperationRepository"/> для хранения операций в процессе выполнения.
    /// </summary>
    /// <remarks>
    /// Данные хранятся в <see cref="Dictionary{TKey, TValue}"/> в адресном пространстве процесса,
    /// поэтому:
    /// <list type="bullet">
    /// <item><description>Состояние не персистится между запусками.</description></item>
    /// <item><description>Потокобезопасность не гарантируется (без синхронизации).</description></item>
    /// <item><description>Операции выборки по предикатам — линейные по количеству записей.</description></item>
    /// </list>
    /// Подходит для демо/тестов и как «фейковая» инфраструктура.
    /// </remarks>
    public sealed class InMemoryOperationRepository : IOperationRepository
    {
        /// <summary>
        /// Основное хранилище: ключ — <see cref="Operation.Id"/>.
        /// </summary>
        private readonly Dictionary<string, Operation> _store = new();

        /// <summary>
        /// Возвращает операцию по её идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор операции.</param>
        /// <returns>
        /// Экземпляр <see cref="Operation"/>, если найден, иначе <c>null</c>.
        /// </returns>
        public Operation? Get(string id) => _store.TryGetValue(id, out var o) ? o : null;

        /// <summary>
        /// Возвращает последовательность всех операций.
        /// </summary>
        /// <remarks>
        /// Порядок перечисления не определён (зависит от внутреннего порядка словаря).
        /// </remarks>
        public IEnumerable<Operation> GetAll() => _store.Values;

        /// <summary>
        /// Возвращает операции по указанному счёту.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <returns>
        /// Последовательность операций, у которых <see cref="Operation.BankAccountId"/> совпадает с <paramref name="accountId"/>.
        /// </returns>
        public IEnumerable<Operation> GetByAccount(string accountId)
            // Линейная фильтрация по всем значениям словаря.
            => _store.Values.Where(o => o.BankAccountId == accountId);

        /// <summary>
        /// Возвращает операции по счёту за интервал дат (включительно).
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начальная дата (включительно).</param>
        /// <param name="to">Конечная дата (включительно).</param>
        /// <returns>
        /// Последовательность операций, удовлетворяющих счёту и диапазону дат.
        /// </returns>
        public IEnumerable<Operation> GetByAccountAndPeriod(string accountId, System.DateTime from, System.DateTime to)
            // Обе границы включены: o.Date >= from && o.Date <= to
            => _store.Values.Where(o => o.BankAccountId == accountId && o.Date >= from && o.Date <= to);

        /// <summary>
        /// Добавляет новую операцию.
        /// </summary>
        /// <param name="op">Добавляемая операция.</param>
        /// <remarks>
        /// Бросит <see cref="ArgumentException"/>, если запись с таким <see cref="Operation.Id"/> уже существует
        /// (используется <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>).
        /// </remarks>
        public void Add(Operation op) => _store.Add(op.Id, op);

        /// <summary>
        /// Обновляет существующую операцию (или добавляет, если не было).
        /// </summary>
        /// <param name="op">Операция для сохранения.</param>
        /// <remarks>
        /// Индексатор словаря выполняет upsert: при наличии ключа — перезапишет,
        /// при отсутствии — добавит новую запись.
        /// </remarks>
        public void Update(Operation op) => _store[op.Id] = op;

        /// <summary>
        /// Удаляет операцию по идентификатору (если существует).
        /// </summary>
        /// <param name="id">Идентификатор операции.</param>
        /// <remarks>
        /// Если записи нет — метод тихо ничего не делает (см. <see cref="Dictionary{TKey, TValue}.Remove(TKey)"/>).
        /// </remarks>
        public void Remove(string id) => _store.Remove(id);
    }
}
