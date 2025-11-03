namespace Domain.Factories
{
    using System;
    using Domain.Entities;
    using Domain.ValueObjects; 

    /// <summary>
    /// Контракт фабрики для создания доменных сущностей <see cref="BankAccount"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Factory</b>: инкапсулирует процесс конструирования счёта и возможные инварианты/валидации.
    /// Реализация может проверять уникальность <paramref name="id"/>, корректность имени и т.д.
    /// </remarks>
    public interface IBankAccountFactory
    {
        /// <summary>
        /// Создаёт новый банковский счёт.
        /// </summary>
        /// <param name="id">Идентификатор счёта (как правило, генерируется на уровне сервиса/порта).</param>
        /// <param name="name">Человекочитаемое имя счёта (не пустое).</param>
        /// <returns>Экземпляр <see cref="BankAccount"/> в валидном состоянии.</returns>
        BankAccount Create(string id, string name);
    }

    /// <summary>
    /// Контракт фабрики для создания доменных сущностей <see cref="Category"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Factory</b>: централизует создание категорий и проверку инвариантов
    /// (например, непустые <paramref name="id"/> и <paramref name="name"/>).
    /// </remarks>
    public interface ICategoryFactory
    {
        /// <summary>
        /// Создаёт новую категорию операций.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="name">Название категории (не пустое).</param>
        /// <param name="type">Тип категории: <see cref="CategoryType.Income"/> или <see cref="CategoryType.Expense"/>.</param>
        /// <returns>Экземпляр <see cref="Category"/>.</returns>
        Category Create(string id, string name, CategoryType type);
    }

    /// <summary>
    /// Контракт фабрики для создания доменных сущностей <see cref="Operation"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Factory</b>: создание операций в одном месте с консистентной валидацией входных аргументов.
    /// Реализация может дополнять проверки (соответствие типа категории типу операции, допустимость даты и т.п.).
    /// </remarks>
    public interface IOperationFactory
    {
        /// <summary>
        /// Создаёт новую финансовую операцию.
        /// </summary>
        /// <param name="id">Идентификатор операции.</param>
        /// <param name="type">Тип операции: <see cref="OperationType.Income"/> или <see cref="OperationType.Expense"/>.</param>
        /// <param name="accountId">Идентификатор счёта, к которому относится операция.</param>
        /// <param name="categoryId">Идентификатор категории операции.</param>
        /// <param name="amount">Сумма операции (обычно ожидается значение ≥ 0).</param>
        /// <param name="date">Дата совершения операции.</param>
        /// <param name="description">Опциональное текстовое описание.</param>
        /// <returns>Экземпляр <see cref="Operation"/>.</returns>
        Operation Create(
            string id,
            OperationType type,
            string accountId,
            string categoryId,
            decimal amount,
            DateTime date,
            string? description);
    }
}
