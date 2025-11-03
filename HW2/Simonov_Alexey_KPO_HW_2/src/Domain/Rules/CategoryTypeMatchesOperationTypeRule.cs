using Domain.ValueObjects;

namespace Domain.Rules;

public static class CategoryTypeMatchesOperationTypeRule
{
    public static bool IsSatisfiedBy(OperationType opType, CategoryType categoryType)
        => (opType == OperationType.Income && categoryType == CategoryType.Income)
           || (opType == OperationType.Expense && categoryType == CategoryType.Expense);
}