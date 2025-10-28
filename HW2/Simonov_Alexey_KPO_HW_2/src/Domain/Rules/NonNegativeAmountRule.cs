namespace Domain.Rules;
public static class NonNegativeAmountRule { public static bool IsSatisfiedBy(decimal v) => v >= 0; }
