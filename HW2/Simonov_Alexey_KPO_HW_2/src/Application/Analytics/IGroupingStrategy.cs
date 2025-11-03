namespace Application.Analytics
{
    using System.Collections.Generic;
    using Application.Dtos;

    // Результат группировки (ключ + сумма)
    public readonly record struct GroupAmount(string Key, decimal Amount);

    // Интерфейс стратегии группировки операций
    public interface IGroupingStrategy
    {
        string Title { get; }                     // Человеческое имя стратегии
        IEnumerable<GroupAmount> Group(IEnumerable<OperationDto> ops);
    }
}