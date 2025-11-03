using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services
{
    /// <summary>
    /// Доменный сервис пересчёта баланса счёта на основании перечня операций.
    /// </summary>
    /// <remarks>
    /// Паттерн: <b>Domain Service</b> — чистая бизнес-логика вне сущностей/агрегатов.
    /// Сервис не хранит состояния: все данные приходят параметрами.
    /// </remarks>
    public static class BalanceRecalculationService
    {
        /// <summary>
        /// Пересчитывает баланс указанного счёта как разницу суммы доходов и расходов
        /// по всем операциям, относящимся к этому счёту.
        /// </summary>
        /// <param name="account">Счёт, баланс которого требуется пересчитать.</param>
        /// <param name="operations">
        /// Последовательность операций (доходы/расходы), среди которых будут выбраны только операции с
        /// <see cref="Operation.BankAccountId"/> равным <see cref="BankAccount.Id"/> переданного счёта.
        /// </param>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Сложность: O(n) по числу переданных операций.</description></item>
        /// <item><description>Границы периодов/фильтры по датам не применяются — ожидается, что фильтрация произведена заранее.</description></item>
        /// <item><description>Метод изменяет состояние сущности <paramref name="account"/> через <see cref="BankAccount.SetBalance(decimal)"/>.</description></item>
        /// <item><description>Проверки на <c>null</c> не выполняются — предполагается корректный вызов.</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var ops = repo.GetByAccount(acc.Id);
        /// BalanceRecalculationService.Recalculate(acc, ops);
        /// // acc.Balance обновлён на сумму доходов минус расходы
        /// </code>
        /// </example>
        public static void Recalculate(BankAccount account, IEnumerable<Operation> operations)
        {
            // Накопители для сумм по типам операций
            decimal income = 0, expense = 0;

            // Берём только операции данного счёта и суммируем по типам
            foreach (var op in operations.Where(o => o.BankAccountId == account.Id))
            {
                if (op.Type == OperationType.Income) income += op.Amount;
                else expense += op.Amount;
            }

            // Новый баланс = суммарные доходы - суммарные расходы
            account.SetBalance(income - expense);
        }
    }
}
