using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Финансовая операция».
    /// </summary>
    /// <remarks>
    /// Содержит инварианты:
    /// <list type="bullet">
    /// <item><description><see cref="Id"/> — непустой.</description></item>
    /// <item><description><see cref="BankAccountId"/> — непустой.</description></item>
    /// <item><description><see cref="CategoryId"/> — непустой.</description></item>
    /// <item><description><see cref="Amount"/> ≥ 0.</description></item>
    /// </list>
    /// Тип операции (<see cref="Type"/>) и связанные поля могут изменяться методами-мутабторами
    /// при сохранении инвариантов.
    /// </remarks>
    public class Operation
    {
        /// <summary>
        /// Идентификатор операции (уникальный в пределах системы).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Тип операции: доход или расход.
        /// </summary>
        public OperationType Type { get; private set; }

        /// <summary>
        /// Идентификатор счёта, к которому привязана операция.
        /// </summary>
        public string BankAccountId { get; }

        /// <summary>
        /// Идентификатор категории операции.
        /// </summary>
        public string CategoryId { get; private set; }

        /// <summary>
        /// Сумма операции (в условных денежных единицах); неотрицательная.
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Дата совершения операции.
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Необязательное текстовое описание/комментарий.
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Создаёт новую финансовую операцию.
        /// </summary>
        /// <param name="id">Идентификатор операции (непустой).</param>
        /// <param name="type">Тип операции: <see cref="OperationType.Income"/> или <see cref="OperationType.Expense"/>.</param>
        /// <param name="bankAccountId">Идентификатор счёта (непустой).</param>
        /// <param name="categoryId">Идентификатор категории (непустой).</param>
        /// <param name="amount">Сумма операции (≥ 0).</param>
        /// <param name="date">Дата операции.</param>
        /// <param name="description">Опциональное описание.</param>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="id"/>, <paramref name="bankAccountId"/> или <paramref name="categoryId"/> пустые/whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Бросается, если <paramref name="amount"/> &lt; 0.
        /// </exception>
        public Operation(string id, OperationType type, string bankAccountId, string categoryId, decimal amount, DateTime date, string? description)
        {
            // Базовые проверки аргументов на валидность.
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required");
            if (string.IsNullOrWhiteSpace(bankAccountId)) throw new ArgumentException("bankAccountId is required");
            if (string.IsNullOrWhiteSpace(categoryId)) throw new ArgumentException("categoryId is required");
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            // Инициализация неизменяемых и изменяемых свойств.
            Id = id;
            Type = type;
            BankAccountId = bankAccountId;
            CategoryId = categoryId;
            Amount = amount;
            Date = date;
            Description = description;
        }

        /// <summary>
        /// Обновляет сумму операции.
        /// </summary>
        /// <param name="amount">Новая сумма (≥ 0).</param>
        /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="amount"/> &lt; 0.</exception>
        public void UpdateAmount(decimal amount)
        {
            // Инвариант по неотрицательности.
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }

        /// <summary>
        /// Обновляет дату операции.
        /// </summary>
        /// <param name="date">Новая дата.</param>
        public void UpdateDate(DateTime date) => Date = date;

        /// <summary>
        /// Обновляет текстовое описание операции.
        /// </summary>
        /// <param name="description">Новое описание (может быть <c>null</c> или пустым).</param>
        public void UpdateDescription(string? description) => Description = description;

        /// <summary>
        /// Обновляет категорию операции.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории (непустой).</param>
        /// <exception cref="ArgumentException">Если <paramref name="categoryId"/> пустой/whitespace.</exception>
        public void UpdateCategory(string categoryId)
        {
            // Валидация новой категории.
            if (string.IsNullOrWhiteSpace(categoryId)) throw new ArgumentException(nameof(categoryId));
            CategoryId = categoryId;
        }

        /// <summary>
        /// Изменяет тип операции.
        /// </summary>
        /// <param name="type">Новый тип.</param>
        /// <remarks>
        /// Семантическая валидность (например, соответствие типа выбранной категории)
        /// должна проверяться на уровне доменных правил/сервисов.
        /// </remarks>
        public void UpdateType(OperationType type) => Type = type;
    }
}
