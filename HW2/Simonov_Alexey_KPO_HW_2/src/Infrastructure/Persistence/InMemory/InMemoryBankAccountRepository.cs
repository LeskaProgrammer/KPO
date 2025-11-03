using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Persistence.InMemory
{
    /// <summary>
    /// Простая in-memory реализация <see cref="IBankAccountRepository"/>.
    /// </summary>
    /// <remarks>
    /// Данные хранятся в <see cref="Dictionary{TKey, TValue}"/> внутри процесса приложения:
    /// <list type="bullet">
    /// <item><description>Состояние не сохраняется между запусками.</description></item>
    /// <item><description>Потокобезопасность не гарантируется (нет синхронизации доступа).</description></item>
    /// <item><description>Выборки выполняются линейно по количеству записей словаря.</description></item>
    /// </list>
    /// Подходит для демо/тестов и как «фейковая» инфраструктура.
    /// </remarks>
    public sealed class InMemoryBankAccountRepository : IBankAccountRepository
    {
        /// <summary>
        /// Хранилище счетов по ключу <see cref="BankAccount.Id"/>.
        /// </summary>
        private readonly Dictionary<string, BankAccount> _store = new();

        /// <summary>
        /// Возвращает счёт по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор счёта.</param>
        /// <returns>
        /// Экземпляр <see cref="BankAccount"/>, если найден, иначе <c>null</c>.
        /// </returns>
        public BankAccount? Get(string id) => _store.TryGetValue(id, out var a) ? a : null;

        /// <summary>
        /// Возвращает последовательность всех счетов.
        /// </summary>
        /// <remarks>
        /// Порядок перечисления не определён (зависит от внутреннего состояния словаря).
        /// </remarks>
        public IEnumerable<BankAccount> GetAll() => _store.Values;

        /// <summary>
        /// Добавляет новый счёт.
        /// </summary>
        /// <param name="account">Добавляемый счёт.</param>
        /// <remarks>
        /// Используется <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>.
        /// Бросит <see cref="ArgumentException"/>, если запись с таким <see cref="BankAccount.Id"/> уже существует.
        /// </remarks>
        public void Add(BankAccount account) => _store.Add(account.Id, account);

        /// <summary>
        /// Обновляет существующий счёт (или добавляет, если не было).
        /// </summary>
        /// <param name="account">Счёт для сохранения.</param>
        /// <remarks>
        /// Индексатор словаря выполняет upsert: при наличии ключа перезапишет запись,
        /// при отсутствии — создаст новую.
        /// </remarks>
        public void Update(BankAccount account) => _store[account.Id] = account;

        /// <summary>
        /// Удаляет счёт по идентификатору (если существует).
        /// </summary>
        /// <param name="id">Идентификатор удаляемого счёта.</param>
        /// <remarks>
        /// Если записи нет — метод тихо ничего не делает
        /// (см. <see cref="Dictionary{TKey, TValue}.Remove(TKey)"/>).
        /// </remarks>
        public void Remove(string id) => _store.Remove(id);
    }
}
