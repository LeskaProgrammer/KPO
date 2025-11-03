using Application.Dtos;

namespace Infrastructure.ImportExport.Importers;

// минимальный псевдо-YAML под задание (не полноценный парсер)
public sealed class YamlImporter : BaseImporter
{
    protected override (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) Parse(string text)
    {
        var accs = new List<BankAccountDto>();
        var cats = new List<CategoryDto>();
        var ops = new List<OperationDto>();

        var lines = text.Split('\n').Select(l => l.TrimEnd()).ToArray();
        List<Dictionary<string, string>>? current = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var l = lines[i].Trim();
            if (l.Equals("Accounts:", StringComparison.OrdinalIgnoreCase)) { current = new(); ParseBlock(lines, ref i, current); foreach (var d in current) accs.Add(ToAccount(d)); }
            else if (l.Equals("Categories:", StringComparison.OrdinalIgnoreCase)) { current = new(); ParseBlock(lines, ref i, current); foreach (var d in current) cats.Add(ToCategory(d)); }
            else if (l.Equals("Operations:", StringComparison.OrdinalIgnoreCase)) { current = new(); ParseBlock(lines, ref i, current); foreach (var d in current) ops.Add(ToOperation(d)); }
        }

        return (accs, cats, ops);
    }

    private static void ParseBlock(string[] lines, ref int i, List<Dictionary<string,string>> target)
    {
        Dictionary<string, string>? cur = null;
        for (i = i + 1; i < lines.Length; i++)
        {
            var s = lines[i];
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (!s.StartsWith("- ") && !s.StartsWith("  ") && !s.Contains(':')) { i--; break; }
            var t = s.Trim();
            if (t.StartsWith("- ")) { if (cur != null) target.Add(cur); cur = new(); t = t[2..]; }
            var idx = t.IndexOf(':');
            if (idx > 0 && cur != null)
            {
                var key = t[..idx].Trim();
                var val = t[(idx+1)..].Trim();
                cur[key] = val.Trim('"');
            }
        }
        if (cur != null && cur.Count > 0) target.Add(cur);
    }

    private static BankAccountDto ToAccount(Dictionary<string,string> d) => new()
    {
        Id = d.GetValueOrDefault("Id") ?? throw new Exception("Id required"),
        Name = d.GetValueOrDefault("Name") ?? "",
        Balance = decimal.TryParse(d.GetValueOrDefault("Balance"), out var b) ? b : 0m
    };

    private static CategoryDto ToCategory(Dictionary<string,string> d) => new()
    {
        Id = d.GetValueOrDefault("Id") ?? throw new Exception("Id required"),
        Name = d.GetValueOrDefault("Name") ?? "",
        Type = Enum.TryParse<Domain.ValueObjects.CategoryType>(d.GetValueOrDefault("Type"), true, out var t) ? t : Domain.ValueObjects.CategoryType.Expense
    };

    private static OperationDto ToOperation(Dictionary<string,string> d) => new()
    {
        Id = d.GetValueOrDefault("Id") ?? throw new Exception("Id required"),
        Type = Enum.TryParse<Domain.ValueObjects.OperationType>(d.GetValueOrDefault("Type"), true, out var t) ? t : Domain.ValueObjects.OperationType.Expense,
        BankAccountId = d.GetValueOrDefault("BankAccountId") ?? "",
        CategoryId = d.GetValueOrDefault("CategoryId") ?? "",
        Amount = decimal.TryParse(d.GetValueOrDefault("Amount"), out var a) ? a : 0m,
        Date = DateTime.TryParse(d.GetValueOrDefault("Date"), out var dt) ? dt : DateTime.UtcNow,
        Description = d.GetValueOrDefault("Description")
    };
}
