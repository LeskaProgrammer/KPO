using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Proxies
{
    /// <summary>
    /// Кэширующий прокси над <see cref="IBankAccountRepository"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Proxy</b>: прозрачно добавляет in-memory кэш поверх реального репозитория.
    /// <list type="bullet">
    /// <item><description><see cref="Get(string)"/> — быстрый доступ по Id с использованием кэша.</description></item>
    /// <item><description><see cref="GetAll"/> — лениво один раз загружает все счета и помечает <see cref="_allLoaded"/>.</description></item>
    /// <item><description><see cref="Add(BankAccount)"/>, <see cref="Update(BankAccount)"/>, <see cref="Remove(string)"/> — синхронизируют базовый источник и кэш.</description></item>
    /// </list>
    /// <para/>
    /// ⚠️ Потокобезопасность не обеспечивается (используются обычные коллекции без синхронизации).
    /// Предполагается, что все изменения проходят через этот прокси; иначе возможна рассинхронизация кэша с источником.
    /// Кэш не «протухает» сам по себе.
    /// </remarks>
    public sealed class CachedBankAccountRepositoryProxy : IBankAccountRepository
    {
        // Реальный (делегируемый) репозиторий
        private readonly IBankAccountRepository _inner;

        // Простейший кэш счетов по их идентификатору
        private readonly Dictionary<string, BankAccount> _cache = new();

        // Признак, что мы уже загрузили весь набор из _inner в кэш
        private bool _allLoaded;

        /// <summary>
        /// Создаёт прокси поверх конкретной реализации <see cref="IBankAccountRepository"/>.
        /// </summary>
        /// <param name="inner">Базовый репозиторий, к которому делегируются вызовы.</param>
        public CachedBankAccountRepositoryProxy(IBankAccountRepository inner) => _inner = inner;

        /// <summary>
        /// Возвращает счёт по <paramref name="id"/>, используя кэш при наличии.
        /// </summary>
        /// <param name="id">Идентификатор счёта.</param>
        /// <returns>Экземпляр <see cref="BankAccount"/> или <c>null</c>, если не найден.</returns>
        public BankAccount? Get(string id)
        {
            // Сначала пытаемся отдать из кэша
            if (_cache.TryGetValue(id, out var a)) return a;

            // При промахе идём в базовый репозиторий и, если нашли, кладём в кэш
            var fromInner = _inner.Get(id);
            if (fromInner != null) _cache[id] = fromInner;
            return fromInner;
        }

        /// <summary>
        /// Возвращает все счета.
        /// </summary>
        /// <remarks>
        /// При первом вызове наполняет кэш всеми сущностями из <see cref="_inner"/> и выставляет флаг <see cref="_allLoaded"/>.
        /// Последующие вызовы работают только с кэшем без обращений к источнику.
        /// </remarks>
        public IEnumerable<BankAccount> GetAll()
        {
            // Лениво прогреваем кэш полным снимком
            if (!_allLoaded)
            {
                foreach (var a in _inner.GetAll()) _cache[a.Id] = a;
                _allLoaded = true;
            }
            return _cache.Values;
        }

        /// <summary>
        /// Добавляет счёт в хранилище и синхронизирует кэш.
        /// </summary>
        /// <param name="account">Добавляемый счёт.</param>
        public void Add(BankAccount account) { _inner.Add(account); _cache[account.Id] = account; }

        /// <summary>
        /// Обновляет счёт в хранилище и кэше.
        /// </summary>
        /// <param name="account">Обновляемый счёт.</param>
        public void Update(BankAccount account) { _inner.Update(account); _cache[account.Id] = account; }

        /// <summary>
        /// Удаляет счёт из хранилища и удаляет его из кэша.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого счёта.</param>
        public void Remove(string id) { _inner.Remove(id); _cache.Remove(id); }
    }
}
