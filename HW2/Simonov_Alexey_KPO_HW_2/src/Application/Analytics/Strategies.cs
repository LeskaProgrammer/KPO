namespace Application.Analytics
{
    using System.Linq;
    using System.Collections.Generic;
    using Application.Dtos;
    using Domain.ValueObjects; // OperationType

    // Группировка по категории
    public sealed class ByCategoryStrategy : IGroupingStrategy
    {
        public string Title => "By Category";

        public IEnumerable<GroupAmount> Group(IEnumerable<OperationDto> ops)
        {
            // суммируем с учётом знака: доход +, расход -
            return ops
                .GroupBy(o => o.CategoryId ?? "(no category)")
                .Select(g => new GroupAmount(
                    g.Key,
                    g.Sum(o => o.Type == OperationType.Income ? o.Amount : -o.Amount)
                ));
        }
    }

    // Группировка по дню (yyyy-MM-dd)
    public sealed class ByDayStrategy : IGroupingStrategy
    {
        public string Title => "By Day";

        public IEnumerable<GroupAmount> Group(IEnumerable<OperationDto> ops)
        {
            return ops
                .GroupBy(o => o.Date.ToString("yyyy-MM-dd"))
                .Select(g => new GroupAmount(
                    g.Key,
                    g.Sum(o => o.Type == OperationType.Income ? o.Amount : -o.Amount)
                ));
        }
    }

    // Группировка по типу операции (Income / Expense)
    public sealed class ByTypeStrategy : IGroupingStrategy
    {
        public string Title => "By Type";

        public IEnumerable<GroupAmount> Group(IEnumerable<OperationDto> ops)
        {
            return ops
                .GroupBy(o => o.Type.ToString())
                .Select(g => new GroupAmount(
                    g.Key,
                    g.Sum(o => o.Type == OperationType.Income ? o.Amount : -o.Amount)
                ));
        }
    }
}