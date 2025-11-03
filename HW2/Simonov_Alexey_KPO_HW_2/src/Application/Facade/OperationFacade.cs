using Application.Dtos;
using Application.Ports;
using Domain.Entities;
using Domain.Rules;
using Domain.ValueObjects;
using Domain.Factories;

namespace Application.Facade
{
    /// <summary>
    /// Фасад для работы с финансовыми операциями (создание/изменение/удаление/список).
    /// </summary>
    /// <remarks>
    /// Паттерны:
    /// <list type="bullet">
    /// <item><description><b>Facade</b> — упрощает API для слоя Presentation/CLI.</description></item>
    /// <item><description><b>Repository</b> — доступ к агрегатам (<see cref="IBankAccountRepository"/>, <see cref="ICategoryRepository"/>, <see cref="IOperationRepository"/>).</description></item>
    /// <item><description><b>Unit of Work</b> — единица транзакционной работы (<see cref="IUnitOfWork"/>).</description></item>
    /// <item><description><b>Factory</b> — создание операций через <see cref="IOperationFactory"/>.</description></item>
    /// <item><description><b>Domain Rules / Specification</b> — валидация бизнес-правил (<see cref="NonNegativeAmountRule"/>, <see cref="CategoryTypeMatchesOperationTypeRule"/>, <see cref="OperationDateValidRule"/>).</description></item>
    /// <item><description><b>Clock</b> (порт) — контролируемое время для тестируемости (<see cref="IClock"/>).</description></item>
    /// </list>
    /// Все операции завершаются коммитом через <see cref="IUnitOfWork"/>; фасад конвертирует доменные модели в DTO.
    /// </remarks>
    public sealed class OperationFacade
    {
        // --- Порты/зависимости слоя приложения ---
        private readonly IBankAccountRepository _accounts;
        private readonly ICategoryRepository _categories;
        private readonly IOperationRepository _operations;
        private readonly IIdGenerator _ids;
        private readonly IClock _clock;
        private readonly IUnitOfWork _uow;
        private readonly IOperationFactory _factory;

        /// <summary>
        /// Создаёт фасад операций.
        /// </summary>
        /// <param name="accounts">Репозиторий счетов.</param>
        /// <param name="categories">Репозиторий категорий.</param>
        /// <param name="operations">Репозиторий операций.</param>
        /// <param name="ids">Генератор идентификаторов.</param>
        /// <param name="clock">Источник времени (для правил по дате).</param>
        /// <param name="uow">Единица работы (транзакционный коммит).</param>
        /// <param name="factory">Фабрика создания <see cref="Domain.Entities.Operation"/>.</param>
        public OperationFacade(
            IBankAccountRepository accounts,
            ICategoryRepository categories,
            IOperationRepository operations,
            IIdGenerator ids,
            IClock clock,
            IUnitOfWork uow,
            IOperationFactory factory)
        {
            _accounts = accounts; _categories = categories; _operations = operations;
            _ids = ids; _clock = clock; _uow = uow; _factory = factory;
        }

        /// <summary>
        /// Записывает новую операцию, применяет её к балансу счёта и коммитит изменения.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="categoryId">Идентификатор категории.</param>
        /// <param name="type">Тип операции: доход/расход.</param>
        /// <param name="amount">Сумма операции (≥ 0).</param>
        /// <param name="date">Дата операции (валидируется относительно текущего времени).</param>
        /// <param name="description">Необязательное описание.</param>
        /// <returns>DTO созданной операции.</returns>
        /// <exception cref="InvalidOperationException">Если счёт или категория не найдены.</exception>
        /// <exception cref="ArgumentException">Если нарушены доменные правила (сумма, дата, тип категории).</exception>
        public OperationDto Record(
            string accountId,
            string categoryId,
            OperationType type,
            decimal amount,
            DateTime date,
            string? description)
        {
            // 1) Загрузка агрегатов
            var acc = _accounts.Get(accountId) ?? throw new InvalidOperationException("account not found");
            var cat = _categories.Get(categoryId) ?? throw new InvalidOperationException("category not found");

            // 2) Валидация доменных правил через спецификации/правила
            if (!NonNegativeAmountRule.IsSatisfiedBy(amount)) throw new ArgumentException("amount must be >= 0");
            if (!CategoryTypeMatchesOperationTypeRule.IsSatisfiedBy(type, cat.Type)) throw new ArgumentException("category type mismatch");
            if (!OperationDateValidRule.IsSatisfiedBy(date, _clock.Now)) throw new ArgumentException("invalid date");

            // 3) Создание операции через фабрику (единая точка конструирования)
            var op = _factory.Create(_ids.NewId(), type, accountId, categoryId, amount, date, description);

            // 4) Сохранение операции
            _operations.Add(op);

            // 5) Применение эффекта на баланс счёта (инкремент/декремент)
            if (type == OperationType.Income) acc.ApplyIncome(amount);
            else acc.ApplyExpense(amount);
            _accounts.Update(acc);

            // 6) Транзакционный коммит
            _uow.Commit();

            // 7) Проекция в DTO для Presentation
            return ToDto(op);
        }

        /// <summary>
        /// Частичное обновление полей операции с откатом/повторным применением суммы к балансу счёта.
        /// </summary>
        /// <param name="operationId">Идентификатор операции.</param>
        /// <param name="amount">Новая сумма (опционально).</param>
        /// <param name="date">Новая дата (опционально).</param>
        /// <param name="description">Новое описание (опционально; <c>null</c> сохранит null).</param>
        /// <param name="categoryId">Новая категория (опционально).</param>
        /// <param name="newType">Новый тип операции (опционально).</param>
        /// <exception cref="InvalidOperationException">Если операция/счёт/категория не найдены.</exception>
        /// <exception cref="ArgumentException">Если нарушены доменные правила (сумма, дата, тип/категория несовместимы).</exception>
        public void Update(string operationId, decimal? amount = null, DateTime? date = null, string? description = null, string? categoryId = null, OperationType? newType = null)
        {
            // 1) Загрузка доменных объектов
            var op = _operations.Get(operationId) ?? throw new InvalidOperationException("operation not found");
            var acc = _accounts.Get(op.BankAccountId) ?? throw new InvalidOperationException("account not found");

            // 2) Откат старой суммы с баланса (инвертируем эффект)
            if (op.Type == OperationType.Income) acc.ApplyExpense(op.Amount);
            else acc.ApplyIncome(op.Amount);

            // 3) Применяем точечные изменения с валидациями
            if (amount.HasValue)
            {
                if (!NonNegativeAmountRule.IsSatisfiedBy(amount.Value)) throw new ArgumentException("amount must be >= 0");
                op.UpdateAmount(amount.Value);
            }
            if (date.HasValue)
            {
                if (!OperationDateValidRule.IsSatisfiedBy(date.Value, _clock.Now)) throw new ArgumentException("invalid date");
                op.UpdateDate(date.Value);
            }
            if (description != null) op.UpdateDescription(description);
            if (categoryId != null)
            {
                var cat = _categories.Get(categoryId) ?? throw new InvalidOperationException("category not found");
                op.UpdateCategory(categoryId);
                if (!CategoryTypeMatchesOperationTypeRule.IsSatisfiedBy(op.Type, cat.Type))
                    throw new ArgumentException("category type mismatch");
            }
            if (newType.HasValue)
            {
                var cat = _categories.Get(op.CategoryId) ?? throw new InvalidOperationException("category not found");
                if (!CategoryTypeMatchesOperationTypeRule.IsSatisfiedBy(newType.Value, cat.Type))
                    throw new ArgumentException("category type mismatch");
                op.UpdateType(newType.Value);
            }

            // 4) Повторно применяем новую сумму на баланс
            if (op.Type == OperationType.Income) acc.ApplyIncome(op.Amount);
            else acc.ApplyExpense(op.Amount);

            // 5) Сохраняем изменения и коммитим
            _operations.Update(op);
            _accounts.Update(acc);
            _uow.Commit();
        }

        /// <summary>
        /// Удаляет операцию и откатывает её эффект на балансе счёта.
        /// </summary>
        /// <param name="id">Идентификатор операции для удаления.</param>
        /// <exception cref="InvalidOperationException">Если операция или счёт не найдены.</exception>
        public void Delete(string id)
        {
            // 1) Загрузка сущностей
            var op = _operations.Get(id) ?? throw new InvalidOperationException("operation not found");
            var acc = _accounts.Get(op.BankAccountId) ?? throw new InvalidOperationException("account not found");

            // 2) Откатим влияние операции на баланс
            if (op.Type == OperationType.Income) acc.ApplyExpense(op.Amount);
            else acc.ApplyIncome(op.Amount);

            // 3) Удаление из репозитория операций + сохранение счёта
            _operations.Remove(id);
            _accounts.Update(acc);

            // 4) Коммит
            _uow.Commit();
        }

        /// <summary>
        /// Возвращает список операций по счёту с необязательным фильтром по периоду.
        /// </summary>
        /// <param name="accountId">Идентификатор счёта.</param>
        /// <param name="from">Начало периода (включительно), опционально.</param>
        /// <param name="to">Конец периода (включительно), опционально.</param>
        /// <returns>Последовательность DTO операций.</returns>
        public IEnumerable<OperationDto> List(string accountId, DateTime? from = null, DateTime? to = null)
        {
            // Если обе границы заданы — используем выборку по периоду; иначе — по счёту целиком.
            IEnumerable<Domain.Entities.Operation> src = from.HasValue && to.HasValue
                ? _operations.GetByAccountAndPeriod(accountId, from.Value, to.Value)
                : _operations.GetByAccount(accountId);

            // Проекция доменных моделей в DTO
            return src.Select(ToDto);
        }

        /// <summary>
        /// Проекция доменной операции в транспортный объект.
        /// </summary>
        private static OperationDto ToDto(Domain.Entities.Operation o) => new()
        {
            Id = o.Id,
            Type = o.Type,
            BankAccountId = o.BankAccountId,
            CategoryId = o.CategoryId,
            Amount = o.Amount,
            Date = o.Date,
            Description = o.Description
        };
    }
}
