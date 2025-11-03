using Application.Dtos;
using Application.Ports;
using Domain.ValueObjects;
using Application.Analytics; 

namespace Application.Facade
{
    /// <summary>
    /// Фасад аналитики по операциям.
    /// </summary>
    /// <remarks>
    /// Паттерны:
    /// <list type="bullet">
    /// <item><description><b>Facade</b> — предоставляет упрощённый API для слоя Presentation (CLI/команды).</description></item>
    /// <item><description><b>Repository</b> — извлекает данные через порты <see cref="IOperationRepository"/> и <see cref="ICategoryRepository"/>.</description></item>
    /// <item><description><b>Strategy</b> — метод <see cref="GetGrouped(string, System.DateTime, System.DateTime, IGroupingStrategy)"/> делегирует группировку внедряемой стратегии.</description></item>
    /// </list>
    /// Вся бизнес-логика здесь ограничена агрегациями и делегированием — без знания деталей хранения.
    /// </remarks>
    public sealed class AnalyticsFacade
    {
        private readonly IOperationRepository _operations;
        private readonly ICategoryRepository _categories;

        /// <summary>
        /// Создаёт фасад аналитики.
        /// </summary>
        /// <param name="operations">Репозиторий операций.</param>
        /// <param name="categories">Репозиторий категорий.</param>
        public AnalyticsFacade(IOperationRepository operations, ICategoryRepository categories)
        {
            _operations = operations; _categories = categories;
        }

        /// <summary>
        /// Возвращает суммарные значения доходов и расходов по счёту за период.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начало периода (включительно).</param>
        /// <param name="to">Конец периода (включительно).</param>
        /// <returns>DTO с суммой доходов и расходов (<see cref="NetIncomeDto"/>).</returns>
        public NetIncomeDto GetNetIncome(string accountId, DateTime from, DateTime to)
        {
            // Берём операции по счёту за период
            var ops = _operations.GetByAccountAndPeriod(accountId, from, to);

            decimal income = 0, expense = 0;

            // Линейно суммируем по типам операций
            foreach (var o in ops)
            {
                if (o.Type == OperationType.Income) income += o.Amount;
                else expense += o.Amount;
            }

            // Чистый доход можно получить на стороне DTO как Income - Expense
            return new NetIncomeDto { Income = income, Expense = expense };
        }

        /// <summary>
        /// Возвращает суммы по категориям за период, отсортированные по убыванию суммы.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начало периода (включительно).</param>
        /// <param name="to">Конец периода (включительно).</param>
        /// <returns>Перечень агрегатов по категориям (<see cref="CategorySumDto"/>), убывающе по сумме.</returns>
        public IEnumerable<CategorySumDto> GetSumByCategory(string accountId, DateTime from, DateTime to)
        {
            // Операции за период
            var ops = _operations.GetByAccountAndPeriod(accountId, from, to).ToArray();

            // Справочник категорий Id -> Name (для понятной печати)
            var cats = _categories.GetAll().ToDictionary(c => c.Id, c => c.Name);

            // Группируем по CategoryId и считаем сумму Amount
            return ops.GroupBy(o => o.CategoryId)
                .Select(g => new CategorySumDto
                {
                    CategoryId   = g.Key,
                    CategoryName = cats.GetValueOrDefault(g.Key, "?"),
                    Amount       = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Amount);
        }

        /// <summary>
        /// Универсальная группировка операций с применением стратегии.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начало периода (включительно).</param>
        /// <param name="to">Конец периода (включительно).</param>
        /// <param name="strategy">
        /// Стратегия группировки (<see cref="IGroupingStrategy"/>), которая определяет правило группировки
        /// и формат ключа группы (например, по датам, по типам, по неделям и т.д.).
        /// </param>
        /// <returns>
        /// Последовательность пар (ключ группы/сумма) в виде <see cref="GroupAmount"/>.
        /// Конкретный смысл ключа зависит от выбранной стратегии.
        /// </returns>
        /// <exception cref="ArgumentNullException">Если <paramref name="strategy"/> равна <c>null</c>.</exception>
        public IEnumerable<GroupAmount> GetGrouped(
            string accountId, DateTime from, DateTime to, IGroupingStrategy strategy)
        {
            if (strategy is null) throw new ArgumentNullException(nameof(strategy));

            // 1) Забираем доменные операции из репозитория за заданный период по счёту.
            // 2) Маппим в DTO (как это делает OperationFacade), чтобы передать наружу только нужные поля.
            var ops = _operations.GetByAccountAndPeriod(accountId, from, to)
                .Select(o => new OperationDto
                {
                    Id            = o.Id,
                    BankAccountId = o.BankAccountId,
                    CategoryId    = o.CategoryId,
                    Type          = o.Type,
                    Amount        = o.Amount,
                    Date          = o.Date,
                    Description   = o.Description
                });

            // 3) Делегируем группировку выбранной стратегии.
            return strategy.Group(ops);
        }
    }
}
