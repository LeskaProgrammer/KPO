using System.Text.Json;
using Application.Dtos;

namespace Infrastructure.ImportExport.Exporters;
using Application.Ports;
public sealed class JsonExportVisitor : IExportVisitor
{
    private sealed record Root(IEnumerable<BankAccountDto> Accounts, IEnumerable<CategoryDto> Categories, IEnumerable<OperationDto> Operations);

    public void Export(string path, IEnumerable<BankAccountDto> accounts, IEnumerable<CategoryDto> categories, IEnumerable<OperationDto> operations)
    {
        var root = new Root(accounts, categories, operations);
        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}