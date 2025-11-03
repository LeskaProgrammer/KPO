using Application.Dtos;
using Application.Ports;
using Domain.Entities;
using Domain.Factories;

namespace Application.Facade
{
    /// <summary>
    /// Фасад операций над счетами (создание, переименование, удаление, список).
    /// </summary>
    /// <remarks>
    /// Паттерны и роли:
    /// <list type="bullet">
    /// <item><description><b>Facade</b> — упрощённая точка входа для верхних слоёв (CLI/Presentation).</description></item>
    /// <item><description><b>Repository</b> — доступ к агрегату <see cref="BankAccount"/> через <see cref="IBankAccountRepository"/>.</description></item>
    /// <item><description><b>Unit of Work</b> — транзакционный коммит изменений через <see cref="IUnitOfWork"/>.</description></item>
    /// <item><description><b>Factory</b> — создание доменных сущностей через <see cref="IBankAccountFactory"/> (единая валидация/инициализация).</description></item>
    /// <item><description><b>DTO mapping</b> — проекция доменной модели в <see cref="BankAccountDto"/> методом <see cref="ToDto(BankAccount)"/>.</description></item>
    /// </list>
    /// Логика фасада — это оркестрация портов/сервисов приложения без бизнес-правил предметной области.
    /// </remarks>
    public sealed class AccountFacade
    {
        private readonly IBankAccountRepository _accounts;
        private readonly IIdGenerator _ids;
        private readonly IUnitOfWork _uow;
        private readonly IBankAccountFactory _factory;

        /// <summary>
        /// Создаёт экземпляр фасада счетов.
        /// </summary>
        /// <param name="accounts">Репозиторий счетов.</param>
        /// <param name="ids">Генератор идентификаторов для новых сущностей.</param>
        /// <param name="uow">Единица работы для атомарного коммита изменений.</param>
        /// <param name="factory">Фабрика доменных счетов.</param>
        public AccountFacade(
            IBankAccountRepository accounts,
            IIdGenerator ids,
            IUnitOfWork uow,
            IBankAccountFactory factory)
        {
            _accounts = accounts; _ids = ids; _uow = uow; _factory = factory;
        }

        /// <summary>
        /// Создаёт новый счёт и сохраняет его.
        /// </summary>
        /// <param name="name">Человекочитаемое название счёта (непустое; проверяется фабрикой).</param>
        /// <returns>Созданный счёт в виде <see cref="BankAccountDto"/>.</returns>
        /// <exception cref="ArgumentException">Может быть выброшено фабрикой при некорректном имени.</exception>
        public BankAccountDto Create(string name)
        {
            // Создание доменной сущности через фабрику (единая точка конструирования)
            var acc = _factory.Create(_ids.NewId(), name);

            // Сохранение и коммит транзакции
            _accounts.Add(acc); _uow.Commit();

            // Проекция доменной модели в DTO
            return ToDto(acc);
        }

        /// <summary>
        /// Переименовывает существующий счёт.
        /// </summary>
        /// <param name="id">Идентификатор изменяемого счёта.</param>
        /// <param name="name">Новое имя счёта (непустое; проверка внутри доменной сущности).</param>
        /// <exception cref="InvalidOperationException">Если счёт с указанным <paramref name="id"/> не найден.</exception>
        public void Rename(string id, string name)
        {
            // Загрузка агрегата
            var a = _accounts.Get(id) ?? throw new InvalidOperationException("account not found");

            // Мутация доменной сущности и персистирование
            a.Rename(name); _accounts.Update(a); _uow.Commit();
        }

        /// <summary>
        /// Удаляет счёт по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого счёта.</param>
        public void Delete(string id)
        {
            // Удаление и коммит (репозиторий сам решает, что делать, если записи нет)
            _accounts.Remove(id); _uow.Commit();
        }

        /// <summary>
        /// Возвращает все счета в виде DTO.
        /// </summary>
        public IEnumerable<BankAccountDto> List() => _accounts.GetAll().Select(ToDto);

        /// <summary>
        /// Проецирует доменную сущность счёта в транспортный объект.
        /// </summary>
        private static BankAccountDto ToDto(BankAccount a) =>
            new() { Id = a.Id, Name = a.Name, Balance = a.Balance };
    }
}
