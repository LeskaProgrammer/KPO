namespace Domain.Factories
{
    using System;
    using Domain.Entities;
    using Domain.ValueObjects; 

    /// <summary>
    /// Фабрика доменных категорий.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Factory</b>: централизует создание <see cref="Category"/>,
    /// инкапсулируя базовую валидацию входных аргументов.
    /// </remarks>
    public sealed class CategoryFactory : ICategoryFactory
    {
        /// <summary>
        /// Создаёт новую категорию операций.
        /// </summary>
        /// <param name="id">Идентификатор категории (не пустой).</param>
        /// <param name="name">Название категории (не пустое).</param>
        /// <param name="type">Тип категории: доход или расход (<see cref="CategoryType"/>).</param>
        /// <returns>Инициализированный экземпляр <see cref="Category"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="id"/> или <paramref name="name"/> пустые
        /// либо состоят из пробелов.
        /// </exception>
        /// <remarks>
        /// Здесь выполняется только механическая проверка аргументов.
        /// Семантические ограничения (например, уникальность названия) должны проверяться выше по слою.
        /// </remarks>
        public Category Create(string id, string name, CategoryType type)
        {
            // Базовая валидация входных данных
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Category id must be non-empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Category name must be non-empty.", nameof(name));

            // Непосредственное создание доменной сущности
            return new Category(id, name, type);
        }
    }
}