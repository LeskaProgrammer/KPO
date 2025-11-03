namespace Domain.Rules;

public static class OperationDateValidRule
{
    // простое правило: дата операции не позже "сегодня + 1 день"
    public static bool IsSatisfiedBy(System.DateTime opDate, System.DateTime now)
        => opDate <= now.AddDays(1);
}