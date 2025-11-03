using Application.Dtos;

namespace Application.Ports;

public interface IImporter
{
    (IReadOnlyList<BankAccountDto> accounts,
        IReadOnlyList<CategoryDto> categories,
        IReadOnlyList<OperationDto> operations) Import(string path);
}