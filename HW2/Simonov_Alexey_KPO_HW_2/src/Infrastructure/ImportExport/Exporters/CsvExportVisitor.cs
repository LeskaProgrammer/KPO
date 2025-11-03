using System.Globalization;
using Application.Dtos;

namespace Infrastructure.ImportExport.Exporters;
using Application.Ports;
public sealed class CsvExportVisitor : IExportVisitor
{
    public void Export(string folderPath, IEnumerable<BankAccountDto> accounts, IEnumerable<CategoryDto> categories, IEnumerable<OperationDto> operations)
    {
        Directory.CreateDirectory(folderPath);
        File.WriteAllLines(Path.Combine(folderPath, "accounts.csv"),
            new[]{ "Id;Name;Balance" }.Concat(accounts.Select(a => $"{a.Id};{a.Name};{a.Balance.ToString(CultureInfo.InvariantCulture)}")));

        File.WriteAllLines(Path.Combine(folderPath, "categories.csv"),
            new[]{ "Id;Name;Type" }.Concat(categories.Select(c => $"{c.Id};{c.Name};{c.Type}")));

        File.WriteAllLines(Path.Combine(folderPath, "operations.csv"),
            new[]{ "Id;Type;BankAccountId;CategoryId;Amount;Date;Description" }
                .Concat(operations.Select(o => $"{o.Id};{o.Type};{o.BankAccountId};{o.CategoryId};{o.Amount.ToString(CultureInfo.InvariantCulture)};{o.Date:yyyy-MM-dd};{o.Description}")));
    }
}