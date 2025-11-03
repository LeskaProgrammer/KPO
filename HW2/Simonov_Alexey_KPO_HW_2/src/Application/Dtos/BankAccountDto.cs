namespace Application.Dtos;

public sealed class BankAccountDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public decimal Balance { get; init; }
}