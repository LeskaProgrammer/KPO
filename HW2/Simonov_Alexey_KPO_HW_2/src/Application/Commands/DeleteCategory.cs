using Application.Commands;
using Application.Facade;

namespace Application.Commands.Categories;

public sealed class DeleteCategory : ICommand
{
    private readonly CategoryFacade _facade;
    private readonly string _id;

    public DeleteCategory(CategoryFacade facade, string id)
    { _facade = facade; _id = id; }

    public void Execute() => _facade.Delete(_id);
}