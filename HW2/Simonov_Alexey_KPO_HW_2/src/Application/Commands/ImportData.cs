using Application.Commands;
using Application.Ports;
using Domain.Entities;

namespace Application.Commands.IO;

/// <summary>
/// Команда импорта данных из внешнего источника через <see cref="IImporter"/>.
/// </summary>
/// <remarks>
/// Паттерны: Command, Repository, Unit of Work.
/// <para/>
/// Внутри создаёт доменные сущности из полученных DTO и сохраняет в репозитории,
/// затем делает коммит.
/// </remarks>
public sealed class ImportData : ICommand
{
    private readonly IImporter _importer;   // <-- было BaseImporter
    private readonly string _path;
    private readonly IBankAccountRepository _accRepo;
    private readonly ICategoryRepository _catRepo;
    private readonly IOperationRepository _opRepo;
    private readonly IUnitOfWork _uow;

    /// <summary>
    /// Создать команду импорта.
    /// </summary>
    /// <param name="importer">Импортёр данных.</param>
    /// <param name="path">Источник (файл/папка).</param>
    /// <param name="accRepo">Репозиторий счетов.</param>
    /// <param name="catRepo">Репозиторий категорий.</param>
    /// <param name="opRepo">Репозиторий операций.</param>
    /// <param name="uow">Единица работы для коммита.</param>
    public ImportData(IImporter importer, string path,  // <-- было BaseImporter
        IBankAccountRepository accRepo,
        ICategoryRepository catRepo,
        IOperationRepository opRepo,
        IUnitOfWork uow)
    {
        _importer = importer; _path = path;
        _accRepo = accRepo; _catRepo = catRepo; _opRepo = opRepo; _uow = uow;
    }

    /// <summary>
    /// Выполняет импорт: читает DTO, маппит в доменные сущности, сохраняет и коммитит.
    /// </summary>
    public void Execute()
    {
        var (accs, cats, ops) = _importer.Import(_path);

        // Создание доменных сущностей из DTO (прямые конструкторы).
        foreach (var a in accs) _accRepo.Add(new BankAccount(a.Id, a.Name, a.Balance));
        foreach (var c in cats) _catRepo.Add(new Category(c.Id, c.Name, c.Type));
        foreach (var o in ops) _opRepo.Add(new Operation(o.Id, o.Type, o.BankAccountId, o.CategoryId, o.Amount, o.Date, o.Description));

        _uow.Commit();
    }
}
