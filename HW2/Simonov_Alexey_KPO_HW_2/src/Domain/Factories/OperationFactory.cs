namespace Domain.Factories
{
    using System;
    using Domain.Entities;
    using Domain.ValueObjects;

    /// <summary>
    /// Фабрика создания доменных операций.
    /// </summary>
    /// <remarks>
    /// Паттерн: <b>Factory</b> (простая/интерфейсная фабрика).
    /// Инкапсулирует правила валидации входных данных и единообразно
    /// создаёт экземпляры <see cref="Operation"/> через контракт <see cref="IOperationFactory"/>.
    /// </remarks>
    public sealed class OperationFactory : IOperationFactory
    {
        /// <summary>
        /// Создаёт новую операцию с базовой валидацией входных аргументов.
        /// </summary>
        /// <param name="id">Идентификатор операции (не пустой).</param>
        /// <param name="type">Тип операции: доход или расход.</param>
        /// <param name="bankAccountId">Идентификатор счёта (не пустой).</param>
        /// <param name="categoryId">Идентификатор категории (не пустой).</param>
        /// <param name="amount">Сумма операции (должна быть больше либо равна нулю).</param>
        /// <param name="date">Дата операции.</param>
        /// <param name="description">Необязательное описание операции.</param>
        /// <returns>Инициализированный экземпляр <see cref="Operation"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="id"/>, <paramref name="bankAccountId"/> или <paramref name="categoryId"/> пустые или состоят из пробелов.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Бросается, если <paramref name="amount"/> &lt; 0.
        /// </exception>
        /// <remarks>
        /// Здесь выполняется только «механическая» валидация аргументов.
        /// Семантические проверки (например, соответствие типа категории и типа операции, допустимость даты)
        /// предположительно реализуются уровнем доменных правил/сервисов.
        /// </remarks>
        public Operation Create(
            string id,
            OperationType type,
            string bankAccountId,
            string categoryId,
            decimal amount,
            DateTime date,
            string? description)
        {
            // Базовые проверки на корректность входных данных
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Operation id must be non-empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(bankAccountId))
                throw new ArgumentException("AccountId must be non-empty.", nameof(bankAccountId));
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new ArgumentException("CategoryId must be non-empty.", nameof(categoryId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be >= 0.");

            // Непосредственное создание доменной сущности
            return new Operation(id, type, bankAccountId, categoryId, amount, date, description);
        }
    }
}
