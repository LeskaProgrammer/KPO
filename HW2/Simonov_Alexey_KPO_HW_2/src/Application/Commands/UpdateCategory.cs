using Application.Commands;
using Application.Facade;
using Domain.ValueObjects;

namespace Application.Commands.Categories;

/// <summary>
/// Команда обновления категории (имени и типа).
/// </summary>
/// <remarks>Паттерны: Command, Facade.</remarks>
public sealed class UpdateCategory : ICommand
{
    private readonly CategoryFacade _facade;
    private readonly string _id;
    private readonly string _name;
    private readonly CategoryType _type;

    /// <summary>
    /// Создать команду обновления категории.
    /// </summary>
    public UpdateCategory(CategoryFacade facade, string id, string name, CategoryType type)
    { _facade = facade; _id = id; _name = name; _type = type; }

    /// <summary>Выполняет обновление категории.</summary>
    public void Execute() => _facade.Update(_id, _name, _type);
}