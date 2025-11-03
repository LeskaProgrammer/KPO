using Application.Commands;
using Application.Facade;
using Domain.ValueObjects;

namespace Application.Commands.Operations;

/// <summary>
/// Команда частичного обновления полей операции.
/// </summary>
/// <remarks>
/// Позволяет обновлять сумму/дату/описание/категорию/тип по необходимости.
/// Паттерны: Command, Facade.
/// </remarks>
public sealed class UpdateOperation : ICommand
{
    private readonly OperationFacade _facade;
    private readonly string _opId;
    private readonly decimal? _amount;
    private readonly DateTime? _date;
    private readonly string? _desc;
    private readonly string? _catId;
    private readonly OperationType? _type;

    /// <summary>
    /// Создать команду обновления операции.
    /// </summary>
    public UpdateOperation(OperationFacade facade, string opId,
        decimal? amount = null, DateTime? date = null, string? desc = null,
        string? catId = null, OperationType? type = null)
    {
        _facade = facade; _opId = opId; _amount = amount; _date = date;
        _desc = desc; _catId = catId; _type = type;
    }

    /// <summary>Выполняет обновление операции.</summary>
    public void Execute() => _facade.Update(_opId, _amount, _date, _desc, _catId, _type);
}