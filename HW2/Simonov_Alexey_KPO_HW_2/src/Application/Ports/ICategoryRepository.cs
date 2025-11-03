using Domain.Entities;

namespace Application.Ports;

public interface ICategoryRepository
{
    Category? Get(string id);
    IEnumerable<Category> GetAll();
    void Add(Category category);
    void Update(Category category);
    void Remove(string id);
}