using Application.Dtos;
using Application.Ports;

namespace Infrastructure.ImportExport.Importers;

public abstract class BaseImporter : IImporter
{
    public virtual (IReadOnlyList<BankAccountDto>, IReadOnlyList<CategoryDto>, IReadOnlyList<OperationDto>) Import(string path)
    {
        var text = File.ReadAllText(path);
        return Parse(text);
    }

    protected abstract (IReadOnlyList<BankAccountDto>,
        IReadOnlyList<CategoryDto>,
        IReadOnlyList<OperationDto>) Parse(string text);
}