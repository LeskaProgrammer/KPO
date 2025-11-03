using Application.Commands;
using Application.Dtos;
using Application.Facade;
using Domain.ValueObjects;

namespace Application.Commands.Categories
{
    /// <summary>
    /// Команда создания категории.
    /// </summary>
    /// <remarks>
    /// Паттерны: <b>Command</b> (c результатом) и <b>Facade</b> (делегирование в <see cref="CategoryFacade"/>).
    /// Возвращает созданную категорию как <see cref="CategoryDto"/>.
    /// </remarks>
    public sealed class CreateCategory : ICommand<CategoryDto>
    {
        // Неизменяемые поля параметров команды
        private readonly CategoryFacade _facade;
        private readonly string _name;
        private readonly CategoryType _type;

        /// <summary>
        /// Инициализирует команду создания категории.
        /// </summary>
        /// <param name="facade">Фасад операций с категориями.</param>
        /// <param name="name">Название категории.</param>
        /// <param name="type">Тип категории: доход или расход.</param>
        public CreateCategory(CategoryFacade facade, string name, CategoryType type)
        { 
            _facade = facade; 
            _name = name; 
            _type = type; 
        }

        /// <summary>
        /// Выполняет команду и возвращает DTO созданной категории.
        /// </summary>
        public CategoryDto Execute() => _facade.Create(_name, _type);
    }
}