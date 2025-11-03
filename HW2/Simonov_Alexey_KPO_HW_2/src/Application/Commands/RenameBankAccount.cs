using Application.Commands;
using Application.Facade;

namespace Application.Commands.Accounts;

/// <summary>
/// Команда переименования банковского счёта.
/// </summary>
/// <remarks>Паттерны: Command, Facade.</remarks>
public sealed class RenameBankAccount : ICommand
{
    private readonly AccountFacade _facade;
    private readonly string _id;
    private readonly string _name;

    /// <summary>
    /// Создать команду переименования счёта.
    /// </summary>
    /// <param name="facade">Фасад счетов.</param>
    /// <param name="id">Идентификатор счёта.</param>
    /// <param name="name">Новое имя счёта.</param>
    public RenameBankAccount(AccountFacade facade, string id, string name)
    { _facade = facade; _id = id; _name = name; }

    /// <summary>Выполняет переименование счёта.</summary>
    public void Execute() => _facade.Rename(_id, _name);
}