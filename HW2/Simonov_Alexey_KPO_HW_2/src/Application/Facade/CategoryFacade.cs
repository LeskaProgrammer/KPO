using Application.Dtos;
using Application.Ports;
using Domain.Entities;
using Domain.ValueObjects;
using Domain.Factories;

namespace Application.Facade
{
    /// <summary>
    /// Фасад операций над категориями (создание, обновление, удаление, список).
    /// </summary>
    /// <remarks>
    /// Паттерны:
    /// <list type="bullet">
    /// <item><description><b>Facade</b> — упрощённая точка входа для слоя Presentation/CLI.</description></item>
    /// <item><description><b>Repository</b> — доступ к агрегату <see cref="Category"/> через <see cref="ICategoryRepository"/>.</description></item>
    /// <item><description><b>Unit of Work</b> — единица коммита изменений (<see cref="IUnitOfWork"/>).</description></item>
    /// <item><description><b>Factory</b> — создание категории через <see cref="ICategoryFactory"/> с базовой валидацией.</description></item>
    /// <item><description><b>DTO mapping</b> — проекция доменной модели в <see cref="CategoryDto"/> методом <see cref="ToDto(Domain.Entities.Category)"/>.</description></item>
    /// </list>
    /// Фасад не содержит бизнес-логики предметной области — только оркестрацию портов/сервисов приложения.
    /// </remarks>
    public sealed class CategoryFacade
    {
        private readonly ICategoryRepository _categories;
        private readonly IIdGenerator _ids;
        private readonly IUnitOfWork _uow;
        private readonly ICategoryFactory _factory;

        /// <summary>
        /// Создаёт фасад категорий.
        /// </summary>
        /// <param name="categories">Репозиторий категорий.</param>
        /// <param name="ids">Генератор идентификаторов.</param>
        /// <param name="uow">Единица работы для транзакционного коммита.</param>
        /// <param name="factory">Фабрика доменных категорий.</param>
        public CategoryFacade(
            ICategoryRepository categories,
            IIdGenerator ids,
            IUnitOfWork uow,
            ICategoryFactory factory)
        {
            _categories = categories; _ids = ids; _uow = uow; _factory = factory;
        }

        /// <summary>
        /// Создаёт новую категорию и сохраняет её.
        /// </summary>
        /// <param name="name">Название категории (непустое).</param>
        /// <param name="type">Тип категории (доход/расход).</param>
        /// <returns>Созданная категория в виде <see cref="CategoryDto"/>.</returns>
        /// <exception cref="ArgumentException">Если <paramref name="name"/> некорректно (проверяется фабрикой).</exception>
        public CategoryDto Create(string name, CategoryType type)
        {
            // Создание доменной сущности централизовано через фабрику (единые проверки).
            var c = _factory.Create(_ids.NewId(), name, type);

            // Сохранение и коммит транзакции.
            _categories.Add(c); _uow.Commit();

            // Проекция доменной модели в DTO для внешнего слоя.
            return ToDto(c);
        }

        /// <summary>
        /// Обновляет существующую категорию (имя/тип) и сохраняет изменения.
        /// </summary>
        /// <param name="id">Идентификатор редактируемой категории.</param>
        /// <param name="name">Новое имя (если пустое/whitespace — останется прежним, логика в <see cref="Category.Update(string, CategoryType)"/>).</param>
        /// <param name="type">Новый тип категории.</param>
        /// <exception cref="InvalidOperationException">Если категория с указанным <paramref name="id"/> не найдена.</exception>
        public void Update(string id, string name, CategoryType type)
        {
            var c = _categories.Get(id) ?? throw new InvalidOperationException("category not found");

            // Мягкое обновление доменной сущности (инварианты проверяются внутри сущности).
            c.Update(name, type);

            // Персистирование и коммит.
            _categories.Update(c); _uow.Commit();
        }

        /// <summary>
        /// Удаляет категорию по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой категории.</param>
        public void Delete(string id) { _categories.Remove(id); _uow.Commit(); }

        /// <summary>
        /// Возвращает все категории в виде DTO.
        /// </summary>
        public IEnumerable<CategoryDto> List() => _categories.GetAll().Select(ToDto);

        /// <summary>
        /// Проекция доменной категории в DTO для слоя представления.
        /// </summary>
        private static CategoryDto ToDto(Category c) =>
            new() { Id = c.Id, Name = c.Name, Type = c.Type };
    }
}
