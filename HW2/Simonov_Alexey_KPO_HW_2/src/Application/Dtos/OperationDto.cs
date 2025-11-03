using Domain.ValueObjects;

namespace Application.Dtos;

public sealed class OperationDto
{
    public required string Id { get; init; }
    public required OperationType Type { get; init; }
    public required string BankAccountId { get; init; }
    public required string CategoryId { get; init; }
    public required decimal Amount { get; init; }
    public required System.DateTime Date { get; init; }
    public string? Description { get; init; }
}