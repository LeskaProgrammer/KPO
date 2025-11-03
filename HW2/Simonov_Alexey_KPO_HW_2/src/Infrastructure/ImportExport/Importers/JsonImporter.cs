using System.Text.Json;
using Application.Dtos;

namespace Infrastructure.ImportExport.Importers;

public sealed class JsonImporter : BaseImporter
{
    private sealed record Root(BankAccountDto[] Accounts, CategoryDto[] Categories, OperationDto[] Operations);

    protected override (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) Parse(string text)
    {
        var root = JsonSerializer.Deserialize<Root>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new Root(Array.Empty<BankAccountDto>(), Array.Empty<CategoryDto>(), Array.Empty<OperationDto>());
        return (root.Accounts, root.Categories, root.Operations);
    }
}