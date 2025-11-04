# КПО-ДЗ-2 — отчёт

> sample.json - файл для импорта который содержит много счетов, категорий, операций для тестирования

## Паттерны GoF
1) **Facade** — AccountFacade / CategoryFacade / OperationFacade / AnalyticsFacade  
   Зачем использовал: один простой вход для сценариев вместо обращения к сервисам напрямую.

2) **Command** — ImportData / ExportData / RecalculateBalance (+ прочие сценарии)  
   Зачем использовал: каждый пользовательский сценарий как отдельная команда с Execute(), удобно запускать/логировать/декорировать. Без него сценарная логика расползлась бы по функциям в UI.

3) **Decorator** — TimedCommand  
   Зачем использовал: добавить замер времени к любой команде без изменения её кода. Без него пришлось бы вписывать Stopwatch и лог в каждую команду.

4) **Template Method** — BaseImporter -> JsonImporter/YamlImporter/CsvImporter  
   Зачем использовал: общий скелет импорта, различия только в парсинге формата.

5) **Visitor** — IExportVisitor -> JsonExportVisitor/YamlExportVisitor/CsvExportVisitor  
   Зачем использовал: единый «визит» для выгрузки данных, разные форматы — разными визиторами.

6) **Proxy** — Cached*RepositoryProxy  
   Зачем использовал: кэш поверх реального репозитория (сейчас InMemory), можно подменить хранилище не меняя клиентов.

7) **Factory** — BankAccountFactory / CategoryFactory / OperationFactory (используются в фасадах)  
   Зачем использовал: централизованное создание доменных сущностей + валидация на входе, убирает дубли new/проверок.

8) **Strategy** — IGroupingStrategy: ByCategory / ByDay / ByType  
   Зачем использовал: Чтобы не писать switch по всему коду для взаимозаменяемых алгоритмов.

## Архитектурные / DDD
- Repository (+ InMemory реализации): абстракции для хранения BankAccount/Category/Operation.
- Unit of Work: атомарные коммиты после сценариев.
- DTO: перенос данных наружу из домена (маппинг в фасадах).
- Value Objects: OperationType, CategoryType (и др. значимые типы).
- Rules/Specification: NonNegativeAmountRule, OperationDateValidRule, CategoryTypeMatchesOperationTypeRule.
- DI/IoC: Microsoft.Extensions.DependencyInjection; регистрация в Bootstrap.CompositionRoot.
- Layered Architecture: Domain / Application / Infrastructure / Presentation (CLI).

## Общая структура проекта
- **Presentation (CLI):**  
  * InteractiveUi — меню: счета / категории / операции / аналитика / импорт-экспорт / пересчёт.  
  * Вызывает фасады из CompositionRoot. Для «Группировка (Strategy)» берёт IGroupingStrategy из DI и отдаёт её в AnalyticsFacade.

- **Application:**  
  * Facade-классы инкапсулируют сценарии: Create/Rename/Delete/List/Record/Update/Delete/List.  
  * Commands — сценарии как команды (импорт/экспорт/пересчёт), оборачиваются TimedCommand.  
  * AnalyticsFacade — классические метрики (доход/расход/категории) + новый метод GetGrouped(..., IGroupingStrategy).  
  * Порты (интерфейсы) репозиториев/UnitOfWork/IdGenerator/Clock.  
  * DTO и маппинги (ToDto).

- **Domain:**  
  * Entities: BankAccount / Category / Operation (бизнес-методы: ApplyIncome/ApplyExpense, Update*, Rename и т.д.).  
  * Rules (валидация инвариантов).  
  * Factories: создание сущностей с проверками (используются фасадами).  
  * ValueObjects: типы, даты и т.п.

- **Infrastructure:**  
  * Persistence.InMemory: InMemory*Repository + InMemoryUnitOfWork.  
  * Proxies: Cached*RepositoryProxy (прокси над репозиториями).  
  * ImportExport: BaseImporter (+ Json/Yaml/Csv) и ExportVisitors (Json/Yaml/Csv).

- **Bootstrap:**  
  * CompositionRoot.Register(): регистрирует всё в DI, предоставляет шорткаты для фасадов/репозиториев.
