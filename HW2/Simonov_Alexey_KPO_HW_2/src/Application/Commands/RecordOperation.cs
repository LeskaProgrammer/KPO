using Application.Commands;
using Application.Dtos;
using Application.Facade;
using Domain.ValueObjects;

namespace Application.Commands.Operations;

/// <summary>
/// Команда записи новой операции с возвратом её DTO.
/// </summary>
/// <remarks>
/// Паттерны: Command (generic), Facade.
/// </remarks>
public sealed class RecordOperation : ICommand<OperationDto>
{
    private readonly OperationFacade _facade;
    private readonly string _accId;
    private readonly string _catId;
    private readonly OperationType _type;
    private readonly decimal _amount;
    private readonly DateTime _date;
    private readonly string? _desc;

    /// <summary>
    /// Создать команду записи операции.
    /// </summary>
    public RecordOperation(OperationFacade facade, string accId, string catId,
        OperationType type, decimal amount, DateTime date, string? desc)
    {
        _facade = facade; _accId = accId; _catId = catId;
        _type = type; _amount = amount; _date = date; _desc = desc;
    }

    /// <summary>Выполняет запись операции и возвращает её DTO.</summary>
    public OperationDto Execute() => _facade.Record(_accId, _catId, _type, _amount, _date, _desc);
}