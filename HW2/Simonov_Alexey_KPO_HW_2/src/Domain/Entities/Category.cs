using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Категория операции».
    /// </summary>
    /// <remarks>
    /// Инварианты:
    /// <list type="bullet">
    /// <item><description><see cref="Id"/> — непустой идентификатор.</description></item>
    /// <item><description><see cref="Name"/> — непустое человекочитаемое имя.</description></item>
    /// </list>
    /// Тип категории задаётся значением <see cref="CategoryType"/> (доход/расход).
    /// Семантические проверки (например, уникальность имени в пределах пользователя)
    /// должны выполняться на уровне доменных сервисов/репозиториев.
    /// </remarks>
    public class Category
    {
        /// <summary>
        /// Уникальный идентификатор категории.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Название категории (редактируемое свойство).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Тип категории: <see cref="CategoryType.Income"/> или <see cref="CategoryType.Expense"/>.
        /// </summary>
        public CategoryType Type { get; private set; }

        /// <summary>
        /// Создаёт категорию операций.
        /// </summary>
        /// <param name="id">Идентификатор категории (обязан быть непустым).</param>
        /// <param name="name">Название категории (обязательное, непустое).</param>
        /// <param name="type">Тип категории (доход/расход).</param>
        /// <exception cref="ArgumentException">
        /// Бросается, если <paramref name="id"/> или <paramref name="name"/> пустые или состоят из пробелов.
        /// </exception>
        public Category(string id, string name, CategoryType type)
        {
            // Базовая валидация аргументов конструктора
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required");

            Id = id;
            Name = name.Trim(); // нормализуем пробелы по краям
            Type = type;
        }

        /// <summary>
        /// Обновляет редактируемые поля категории.
        /// </summary>
        /// <param name="name">
        /// Новое имя категории. Если пустая/whitespace — имя остаётся прежним.
        /// </param>
        /// <param name="type">Новый тип категории.</param>
        /// <remarks>
        /// Метод предназначен для «мягкого» обновления: имя меняется только при передаче непустого значения;
        /// тип обновляется всегда.
        /// Проверки уникальности/конфликтов — на уровне приложения/доменных сервисов.
        /// </remarks>
        public void Update(string name, CategoryType type)
        {
            // Обновляем имя только если передано непустое значение
            if (!string.IsNullOrWhiteSpace(name))
                Name = name.Trim();

            // Тип обновляется всегда
            Type = type;
        }
    }
}
