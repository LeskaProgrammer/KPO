using Application.Ports;
using Domain.Entities;

namespace Infrastructure.Proxies
{
    /// <summary>
    /// Кэширующий прокси над <see cref="ICategoryRepository"/>.
    /// </summary>
    /// <remarks>
    /// Паттерн <b>Proxy</b>: прозрачно добавляет in-memory кэширование поверх реального репозитория.
    /// <list type="bullet">
    /// <item>
    /// <description><b>Get</b> — сначала смотрит в кэш по ключу <c>Id</c>, при промахе подтягивает из <c>_inner</c> и кладёт в кэш.</description>
    /// </item>
    /// <item>
    /// <description><b>GetAll</b> — лениво (один раз) загружает все категории из <c>_inner</c>, далее читает только из кэша.</description>
    /// </item>
    /// <item>
    /// <description><b>Add/Update/Remove</b> — выполняют действие в базовом репозитории и синхронизируют кэш.</description>
    /// </item>
    /// </list>
    /// Потокобезопасность не обеспечивается (коллекции без синхронизации). Предполагается, что все изменения проходят через данный прокси,
    /// иначе возможна рассинхронизация кэша и источника.
    /// </remarks>
    public sealed class CachedCategoryRepositoryProxy : ICategoryRepository
    {
        // Делегируемый (реальный) репозиторий
        private readonly ICategoryRepository _inner;

        // Простой кэш категорий по Id
        private readonly Dictionary<string, Category> _cache = new();

        // Флаг: "все записи уже загружены в кэш"
        private bool _allLoaded;

        /// <summary>
        /// Создаёт прокси поверх конкретной реализации <see cref="ICategoryRepository"/>.
        /// </summary>
        /// <param name="inner">Базовый репозиторий, к которому делегируются вызовы.</param>
        public CachedCategoryRepositoryProxy(ICategoryRepository inner) => _inner = inner;

        /// <summary>
        /// Возвращает категорию по идентификатору, используя кэш при наличии.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <returns>Экземпляр <see cref="Category"/> или <c>null</c>, если не найдено.</returns>
        public Category? Get(string id)
        {
            // Пытаемся отдать из кэша
            if (_cache.TryGetValue(id, out var c)) return c;

            // При промахе — обращаемся к внутреннему репозиторию и, если нашли, кладём в кэш
            var fromInner = _inner.Get(id);
            if (fromInner != null) _cache[id] = fromInner;
            return fromInner;
        }

        /// <summary>
        /// Возвращает все категории.
        /// </summary>
        /// <remarks>
        /// При первом вызове инициализирует кэш полным снимком из <see cref="_inner"/>.
        /// Последующие вызовы читают только из кэша.
        /// </remarks>
        public IEnumerable<Category> GetAll()
        {
            // Ленивое наполнение кэша всеми объектами
            if (!_allLoaded)
            {
                foreach (var c in _inner.GetAll()) _cache[c.Id] = c;
                _allLoaded = true;
            }
            return _cache.Values;
        }

        /// <summary>
        /// Добавляет категорию в источник данных и синхронизирует кэш.
        /// </summary>
        /// <param name="c">Добавляемая категория.</param>
        public void Add(Category c) { _inner.Add(c); _cache[c.Id] = c; }

        /// <summary>
        /// Обновляет категорию в источнике данных и синхронизирует кэш.
        /// </summary>
        /// <param name="c">Обновляемая категория.</param>
        public void Update(Category c) { _inner.Update(c); _cache[c.Id] = c; }

        /// <summary>
        /// Удаляет категорию из источника данных и удаляет запись из кэша.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой категории.</param>
        public void Remove(string id) { _inner.Remove(id); _cache.Remove(id); }
    }
}
