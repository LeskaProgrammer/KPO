using Microsoft.Extensions.DependencyInjection;

using Application.Facade;
using Application.Ports;
using Infrastructure.Persistence.InMemory;
using Infrastructure.Proxies;
using Domain.Factories;
using Application.Analytics;

namespace Bootstrap;

public static class CompositionRoot
{
    public static ServiceProvider Provider { get; private set; } = null!;

    public static void Register()
    {
        var s = new ServiceCollection();

        // Repos + кэширующие прокси
        s.AddSingleton<IBankAccountRepository>(_ =>
            new CachedBankAccountRepositoryProxy(new InMemoryBankAccountRepository()));
        s.AddSingleton<ICategoryRepository>(_ =>
            new CachedCategoryRepositoryProxy(new InMemoryCategoryRepository()));
        s.AddSingleton<IOperationRepository>(_ =>
            new CachedOperationRepositoryProxy(new InMemoryOperationRepository()));

        // Технические сервисы
        s.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        s.AddSingleton<IIdGenerator, GuidIdGenerator>();
        s.AddSingleton<IClock, SystemClock>();
        
        s.AddSingleton<IBankAccountFactory, BankAccountFactory>();
        s.AddSingleton<ICategoryFactory, CategoryFactory>();
        s.AddSingleton<IOperationFactory, OperationFactory>();

        // Фасады
        s.AddSingleton<AccountFacade>();
        s.AddSingleton<CategoryFacade>();
        s.AddSingleton<OperationFacade>();
        s.AddSingleton<AnalyticsFacade>();
        
        s.AddSingleton<IGroupingStrategy, ByCategoryStrategy>();
        s.AddSingleton<IGroupingStrategy, ByDayStrategy>();
        s.AddSingleton<IGroupingStrategy, ByTypeStrategy>();

        Provider = s.BuildServiceProvider();
    }

    // ===== Совместимые шорткаты (как раньше), но через контейнер =====
    public static AccountFacade AccountFacade =>
        Provider.GetRequiredService<AccountFacade>();
    public static CategoryFacade CategoryFacade =>
        Provider.GetRequiredService<CategoryFacade>();
    public static OperationFacade OperationFacade =>
        Provider.GetRequiredService<OperationFacade>();
    public static AnalyticsFacade AnalyticsFacade =>
        Provider.GetRequiredService<AnalyticsFacade>();

    public static IBankAccountRepository Accounts =>
        Provider.GetRequiredService<IBankAccountRepository>();
    public static ICategoryRepository Categories =>
        Provider.GetRequiredService<ICategoryRepository>();
    public static IOperationRepository Operations =>
        Provider.GetRequiredService<IOperationRepository>();
    public static IUnitOfWork Uow =>
        Provider.GetRequiredService<IUnitOfWork>();
    public static IIdGenerator Ids =>
        Provider.GetRequiredService<IIdGenerator>();
    public static IClock Clock =>
        Provider.GetRequiredService<IClock>();
}
