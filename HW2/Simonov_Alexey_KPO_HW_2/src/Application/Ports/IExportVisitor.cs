using Application.Dtos;

namespace Application.Ports;

public interface IExportVisitor
{
    void Export(string path,
        IEnumerable<BankAccountDto> accounts,
        IEnumerable<CategoryDto> categories,
        IEnumerable<OperationDto> operations);
}