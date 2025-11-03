using Application.Commands;
using Application.Dtos;
using Application.Facade;

namespace Application.Commands.Accounts
{
    /// <summary>
    /// Команда создания банковского счёта.
    /// </summary>
    /// <remarks>
    /// Паттерны: <b>Command</b> (c результатом) и <b>Facade</b> (делегирование в <see cref="AccountFacade"/>).
    /// Возвращает созданный счёт как <see cref="BankAccountDto"/>.
    /// </remarks>
    public sealed class CreateBankAccount : ICommand<BankAccountDto>
    {
        // Зависимости/параметры команды неизменяемы после создания
        private readonly AccountFacade _facade;
        private readonly string _name;

        /// <summary>
        /// Инициализирует команду создания счёта.
        /// </summary>
        /// <param name="facade">Фасад операций со счетами.</param>
        /// <param name="name">Человекочитаемое имя счёта.</param>
        public CreateBankAccount(AccountFacade facade, string name)
        { 
            _facade = facade; 
            _name = name; 
        }

        /// <summary>
        /// Выполняет команду и возвращает DTO созданного счёта.
        /// </summary>
        public BankAccountDto Execute() => _facade.Create(_name);
    }
}