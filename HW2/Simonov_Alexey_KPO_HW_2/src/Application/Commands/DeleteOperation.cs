using Application.Commands;
using Application.Facade;

namespace Application.Commands.Operations;

/// <summary>
/// Команда удаления операции.
/// </summary>
/// <remarks>
/// Делегирует удаление фасаду <see cref="OperationFacade"/>.
/// Паттерны: Command, Facade.
/// </remarks>
public sealed class DeleteOperation : ICommand
{
    private readonly OperationFacade _facade;
    private readonly string _opId;

    /// <summary>
    /// Создать команду удаления операции.
    /// </summary>
    /// <param name="facade">Фасад операций.</param>
    /// <param name="opId">Идентификатор удаляемой операции.</param>
    public DeleteOperation(OperationFacade facade, string opId)
    { _facade = facade; _opId = opId; }

    /// <summary>Выполняет удаление операции.</summary>
    public void Execute() => _facade.Delete(_opId);
}