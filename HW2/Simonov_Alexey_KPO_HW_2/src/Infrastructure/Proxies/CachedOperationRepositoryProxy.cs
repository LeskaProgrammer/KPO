using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Proxies
{
    /// <summary>
    /// Кэширующий прокси над <see cref="IOperationRepository"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Proxy</b>: прозрачно подменяет репозиторий, добавляя in-memory кэширование.
    /// <para/>
    /// Поведение:
    /// <list type="bullet">
    /// <item><description><see cref="Get(string)"/> — кэш по ключу операции.</description></item>
    /// <item><description><see cref="GetAll"/> — лениво загружает все операции один раз и помечает <see cref="_allLoaded"/>.</description></item>
    /// <item><description><see cref="GetByAccount(string)"/> / <see cref="GetByAccountAndPeriod(string, System.DateTime, System.DateTime)"/> — работают по кэшу.</description></item>
    /// <item><description><see cref="Add(Operation)"/>, <see cref="Update(Operation)"/>, <see cref="Remove(string)"/> — синхронизируют состояние кэша.</description></item>
    /// </list>
    /// Потокобезопасность не обеспечивается (коллекции без синхронизации).
    /// Кэш не «протухает»: предполагается, что все изменения идут через этот прокси, иначе возможна рассинхронизация.
    /// </remarks>
    public sealed class CachedOperationRepositoryProxy : IOperationRepository
    {
        // Внутренний (реальный) репозиторий
        private readonly IOperationRepository _inner;

        // Простейший кэш операций по Id
        private readonly Dictionary<string, Operation> _cache = new();

        // Флаг «все операции уже были загружены в кэш»
        private bool _allLoaded;

        /// <summary>
        /// Создаёт прокси поверх конкретной реализации <see cref="IOperationRepository"/>.
        /// </summary>
        /// <param name="inner">Делегатный (базовый) репозиторий.</param>
        public CachedOperationRepositoryProxy(IOperationRepository inner) => _inner = inner;

        /// <summary>
        /// Возвращает операцию по идентификатору, используя кэш (при наличии).
        /// </summary>
        /// <param name="id">Идентификатор операции.</param>
        /// <returns>Экземпляр <see cref="Operation"/> или <c>null</c>, если не найдено.</returns>
        public Operation? Get(string id)
        {
            // Сначала пытаемся отдать из кэша
            if (_cache.TryGetValue(id, out var o)) return o;

            // Иначе — тянем из базового репозитория и кладу в кэш
            var fromInner = _inner.Get(id);
            if (fromInner != null) _cache[id] = fromInner;
            return fromInner;
        }

        /// <summary>
        /// Возвращает все операции.
        /// </summary>
        /// <remarks>
        /// При первом вызове материализует все записи из <see cref="_inner"/> и наполняет кэш.
        /// Последующие вызовы читают только из кэша, без обращений к <see cref="_inner"/>.
        /// </remarks>
        public IEnumerable<Operation> GetAll()
        {
            // Ленивое наполнение кэша всеми операциями
            if (!_allLoaded)
            {
                foreach (var o in _inner.GetAll()) _cache[o.Id] = o;
                _allLoaded = true;
            }
            // Возвращаем представление кэша
            return _cache.Values;
        }

        /// <summary>
        /// Возвращает операции по счёту.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        public IEnumerable<Operation> GetByAccount(string accountId)
            // Фильтрация по кэшу; если кэш ещё не прогрет, вызовется GetAll() через другой путь.
            => GetAll().Where(o => o.BankAccountId == accountId);

        /// <summary>
        /// Возвращает операции по счёту за период [from; to].
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начало периода (включительно).</param>
        /// <param name="to">Конец периода (включительно).</param>
        public IEnumerable<Operation> GetByAccountAndPeriod(string accountId, System.DateTime from, System.DateTime to)
            // Фильтрация по кэшу по счёту и дате
            => GetAll().Where(o => o.BankAccountId == accountId && o.Date >= from && o.Date <= to);

        /// <summary>
        /// Добавляет операцию в хранилище и кэш.
        /// </summary>
        /// <param name="o">Операция для добавления.</param>
        public void Add(Operation o) { _inner.Add(o); _cache[o.Id] = o; }

        /// <summary>
        /// Обновляет операцию в хранилище и кэше.
        /// </summary>
        /// <param name="o">Операция для обновления.</param>
        public void Update(Operation o) { _inner.Update(o); _cache[o.Id] = o; }

        /// <summary>
        /// Удаляет операцию из хранилища и кэша.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой операции.</param>
        public void Remove(string id) { _inner.Remove(id); _cache.Remove(id); }
    }
}
