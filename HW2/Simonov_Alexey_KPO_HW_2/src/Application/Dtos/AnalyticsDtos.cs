namespace Application.Dtos;

public sealed class NetIncomeDto
{
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
    public decimal Net => Income - Expense;
}

public sealed class CategorySumDto
{
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public decimal Amount { get; init; }
}