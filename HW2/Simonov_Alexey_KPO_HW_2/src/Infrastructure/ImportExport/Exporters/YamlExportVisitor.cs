using Application.Dtos;
using System.Text;

namespace Infrastructure.ImportExport.Exporters;
using Application.Ports;
public sealed class YamlExportVisitor : IExportVisitor
{
    public void Export(string path, IEnumerable<BankAccountDto> accounts, IEnumerable<CategoryDto> categories, IEnumerable<OperationDto> operations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Accounts:");
        foreach (var a in accounts)
        {
            sb.AppendLine($"- Id: {a.Id}");
            sb.AppendLine($"  Name: {a.Name}");
            sb.AppendLine($"  Balance: {a.Balance}");
        }
        sb.AppendLine("Categories:");
        foreach (var c in categories)
        {
            sb.AppendLine($"- Id: {c.Id}");
            sb.AppendLine($"  Name: {c.Name}");
            sb.AppendLine($"  Type: {c.Type}");
        }
        sb.AppendLine("Operations:");
        foreach (var o in operations)
        {
            sb.AppendLine($"- Id: {o.Id}");
            sb.AppendLine($"  Type: {o.Type}");
            sb.AppendLine($"  BankAccountId: {o.BankAccountId}");
            sb.AppendLine($"  CategoryId: {o.CategoryId}");
            sb.AppendLine($"  Amount: {o.Amount}");
            sb.AppendLine($"  Date: {o.Date:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(o.Description))
                sb.AppendLine($"  Description: {o.Description}");
        }
        File.WriteAllText(path, sb.ToString());
    }
}