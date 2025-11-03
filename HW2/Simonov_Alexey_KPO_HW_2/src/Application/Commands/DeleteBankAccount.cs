using Application.Commands;
using Application.Facade;

namespace Application.Commands.Accounts
{
    /// <summary>
    /// Команда удаления банковского счёта.
    /// </summary>
    /// <remarks>
    /// Паттерны: <b>Command</b> (без результата) и <b>Facade</b> (делегирование в <see cref="AccountFacade"/>).
    /// </remarks>
    public sealed class DeleteBankAccount : ICommand
    {
        // Ссылка на фасад и идентификатор удаляемого счёта
        private readonly AccountFacade _facade;
        private readonly string _id;

        /// <summary>
        /// Инициализирует команду удаления счёта.
        /// </summary>
        /// <param name="facade">Фасад операций со счетами.</param>
        /// <param name="id">Идентификатор удаляемого счёта.</param>
        public DeleteBankAccount(AccountFacade facade, string id)
        { 
            _facade = facade; 
            _id = id; 
        }

        /// <summary>
        /// Выполняет удаление счёта.
        /// </summary>
        public void Execute() => _facade.Delete(_id);
    }
}