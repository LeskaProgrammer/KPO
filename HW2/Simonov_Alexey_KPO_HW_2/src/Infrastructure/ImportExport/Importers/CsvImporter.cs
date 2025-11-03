using System.Globalization;
using Application.Dtos;
using Application.Ports;

namespace Infrastructure.ImportExport.Importers;

/// <summary>
/// CSV-импорт: ожидает папку с файлами:
///   accounts.csv   (Id;Name;Balance)
///   categories.csv (Id;Name;Type)            // Type: Income|Expense
///   operations.csv (Id;Type;BankAccountId;CategoryId;Amount;Date;Description)
/// Разделитель — ';'. Первая строка — заголовок.
/// </summary>
public sealed class CsvImporter : BaseImporter
{
    // path — это ПАПКА. Для CSV читаем сразу три файла.
    public override (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) Import(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"CSV folder not found: {path}");
        return ImportFolder(path);
    }

    private static (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) ImportFolder(string folderPath)
    {
        var accs = new List<BankAccountDto>();
        var cats = new List<CategoryDto>();
        var ops  = new List<OperationDto>();

        var accFile = Path.Combine(folderPath, "accounts.csv");
        var catFile = Path.Combine(folderPath, "categories.csv");
        var opFile  = Path.Combine(folderPath, "operations.csv");

        if (File.Exists(accFile))
        {
            foreach (var line in File.ReadAllLines(accFile).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (cols.Length < 3) continue;

                accs.Add(new BankAccountDto
                {
                    Id = cols[0],
                    Name = cols[1],
                    Balance = decimal.Parse(cols[2], CultureInfo.InvariantCulture)
                });
            }
        }

        if (File.Exists(catFile))
        {
            foreach (var line in File.ReadAllLines(catFile).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (cols.Length < 3) continue;

                cats.Add(new CategoryDto
                {
                    Id = cols[0],
                    Name = cols[1],
                    Type = Enum.Parse<Domain.ValueObjects.CategoryType>(cols[2], true)
                });
            }
        }

        if (File.Exists(opFile))
        {
            foreach (var line in File.ReadAllLines(opFile).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (cols.Length < 7) continue;

                ops.Add(new OperationDto
                {
                    Id = cols[0],
                    Type = Enum.Parse<Domain.ValueObjects.OperationType>(cols[1], true),
                    BankAccountId = cols[2],
                    CategoryId = cols[3],
                    Amount = decimal.Parse(cols[4], CultureInfo.InvariantCulture),
                    Date = DateTime.Parse(cols[5], CultureInfo.InvariantCulture),
                    Description = string.IsNullOrWhiteSpace(cols[6]) ? null : cols[6]
                });
            }
        }

        return (accs, cats, ops);
    }

    // Для CSV мы не парсим одиночный текст, поэтому этот путь запрещаем.
    protected override (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) Parse(string _)
        => throw new NotSupportedException("CSV importer expects a folder with accounts.csv/categories.csv/operations.csv");
}
