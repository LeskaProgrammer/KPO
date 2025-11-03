using Domain.ValueObjects;

namespace Application.Dtos;

public sealed class CategoryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CategoryType Type { get; init; }
}