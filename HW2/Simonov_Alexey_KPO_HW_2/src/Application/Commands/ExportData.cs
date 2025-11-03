using Application.Commands;
using Application.Dtos;
using Application.Facade;
using Application.Ports;

namespace Application.Commands.IO;

/// <summary>
/// Команда экспорта данных (счета, категории, операции) во внешний формат.
/// </summary>
/// <remarks>
/// Паттерны: Command, Visitor (экспортёр как посетитель),
/// Repository (чтение данных), DTO mapping.
/// </remarks>
public sealed class ExportData : ICommand
{
    private readonly IExportVisitor _exporter;
    private readonly string _path;
    private readonly AccountFacade _acc;
    private readonly CategoryFacade _cat;
    private readonly Application.Ports.IOperationRepository _ops;

    /// <summary>
    /// Создать команду экспорта.
    /// </summary>
    /// <param name="exporter">Экспортёр-«посетитель».</param>
    /// <param name="path">Путь назначения.</param>
    /// <param name="acc">Фасад счётов.</param>
    /// <param name="cat">Фасад категорий.</param>
    /// <param name="ops">Репозиторий операций.</param>
    public ExportData(IExportVisitor exporter, string path,
        AccountFacade acc, CategoryFacade cat, Application.Ports.IOperationRepository ops)
    {
        _exporter = exporter; _path = path;
        _acc = acc; _cat = cat; _ops = ops;
    }

    /// <summary>
    /// Выполняет экспорт: собирает DTO и передаёт их экспортёру.
    /// </summary>
    public void Execute()
    {
        var accounts = _acc.List();
        var categories = _cat.List();
        var operations = _ops.GetAll().Select(o => new OperationDto
        {
            Id = o.Id, Type = o.Type, BankAccountId = o.BankAccountId, CategoryId = o.CategoryId,
            Amount = o.Amount, Date = o.Date, Description = o.Description
        });

        _exporter.Export(_path, accounts, categories, operations);
    }
}