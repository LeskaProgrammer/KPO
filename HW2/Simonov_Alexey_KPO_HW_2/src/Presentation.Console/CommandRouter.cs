using Bootstrap;
using Domain.ValueObjects;
using Application.Decorators;
using Application.Commands.IO;           // команды уровня Application (импорт/экспорт)
using Application.Ports;               // порты (репозитории/единица работы)
using Infrastructure.ImportExport.Importers; // импортёры (Infrastructure)
using Infrastructure.ImportExport.Exporters; // экспортёры (Infrastructure)

namespace Presentation.Console
{
    /// <summary>
    /// Роутер консольных команд верхнего уровня (CLI).
    /// </summary>
    /// <remarks>
    /// Принимает массив аргументов, определяет корневую команду
    /// (<c>account</c>, <c>category</c>, <c>operation</c>, <c>analytics</c>, <c>export</c>, <c>import</c>, <c>recalc</c>, <c>help</c>)
    /// и делегирует обработку в соответствующие приватные методы-обработчики.
    /// </remarks>
    public static class CommandRouter
    {
        /// <summary>
        /// Точка входа для обработки аргументов CLI.
        /// </summary>
        /// <param name="args">Массив аргументов командной строки.</param>
        public static void Dispatch(string[] args)
        {
            // Если нет аргументов — печать справки и выход.
            if (args.Length == 0) { PrintUsage(); return; }

            var cmd = args[0].ToLowerInvariant(); // корневая команда
            try
            {
                // Маршрутизация по корневой команде.
                switch (cmd)
                {
                    case "account":   HandleAccount(args.Skip(1).ToArray()); break;
                    case "category":  HandleCategory(args.Skip(1).ToArray()); break;
                    case "operation": HandleOperation(args.Skip(1).ToArray()); break;
                    case "analytics": HandleAnalytics(args.Skip(1).ToArray()); break;
                    case "export":    HandleExport(args.Skip(1).ToArray()); break;
                    case "import":    HandleImport(args.Skip(1).ToArray()); break;
                    case "recalc":    HandleRecalc(args.Skip(1).ToArray()); break;
                    case "help":      PrintUsage(); break;

                    default:
                        System.Console.WriteLine($"unknown command: {cmd}");
                        PrintUsage();
                        break;
                }
            }
            catch (Exception ex) { System.Console.WriteLine("ERROR: " + ex.Message); }
        }

        /// <summary>
        /// Печатает краткую справку по доступным командам и синтаксису.
        /// </summary>
        private static void PrintUsage()
        {
            System.Console.WriteLine(@"usage:
  account create <name>
  account list
  account rename <id> <name>
  account delete <id>

  category create <income|expense> <name>
  category list
  category update <id> <income|expense> <name>
  category delete <id>

  operation record <accId> <catId> <income|expense> <amount> <yyyy-mm-dd> [desc]
  operation list <accId> [from yyyy-mm-dd] [to yyyy-mm-dd]
  operation update <opId> [amount x] [date yyyy-mm-dd] [desc text] [catId id] [type income|expense]
  operation delete <id>

  analytics net <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>
  analytics bycat <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>

  export json <path> | export csv <folder> | export yaml <path>
  import json <path> | import csv <folder> | import yaml <path>

  recalc <accId>");
        }

        /// <summary>
        /// Обработчик команд <c>account</c>.
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>account</c>.</param>
        private static void HandleAccount(string[] a)
        {
            if (a.Length == 0) { System.Console.WriteLine("usage: account <create|list|rename|delete> ..."); return; }
            switch (a[0])
            {
                case "create":
                    // Имя счёта может содержать пробелы — склеиваем хвост.
                    var name = string.Join(' ', a.Skip(1));
                    var acc = CompositionRoot.AccountFacade.Create(name);
                    System.Console.WriteLine($"OK. account created: {acc.Id} {acc.Name} balance={acc.Balance}");
                    break;

                case "list":
                    foreach (var x in CompositionRoot.AccountFacade.List())
                        System.Console.WriteLine($"{x.Id}\t{x.Name}\tbalance={x.Balance}");
                    break;

                case "rename":
                    // Переименование: account rename <id> <new name...>
                    if (a.Length < 3) { System.Console.WriteLine("usage: account rename <id> <name>"); return; }
                    CompositionRoot.AccountFacade.Rename(a[1], string.Join(' ', a.Skip(2)));
                    System.Console.WriteLine("OK");
                    break;

                case "delete":
                    if (a.Length < 2) { System.Console.WriteLine("usage: account delete <id>"); return; }
                    CompositionRoot.AccountFacade.Delete(a[1]);
                    System.Console.WriteLine("OK");
                    break;

                default: System.Console.WriteLine("unknown subcommand for account"); break;
            }
        }

        /// <summary>
        /// Обработчик команд <c>category</c>.
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>category</c>.</param>
        private static void HandleCategory(string[] a)
        {
            if (a.Length == 0) { System.Console.WriteLine("usage: category <create|list|update|delete> ..."); return; }
            switch (a[0])
            {
                case "create":
                    // Создание: category create <income|expense> <name...>
                    if (a.Length < 3) { System.Console.WriteLine("usage: category create <income|expense> <name>"); return; }
                    var type = ParseCategoryType(a[1]);
                    var name = string.Join(' ', a.Skip(2));
                    var c = CompositionRoot.CategoryFacade.Create(name, type);
                    System.Console.WriteLine($"OK. category created: {c.Id} {c.Type} {c.Name}");
                    break;

                case "list":
                    foreach (var x in CompositionRoot.CategoryFacade.List())
                        System.Console.WriteLine($"{x.Id}\t{x.Type}\t{x.Name}");
                    break;

                case "update":
                    // Обновление: category update <id> <income|expense> <name...>
                    if (a.Length < 4) { System.Console.WriteLine("usage: category update <id> <income|expense> <name>"); return; }
                    var ct = ParseCategoryType(a[2]);
                    var nm = string.Join(' ', a.Skip(3));
                    CompositionRoot.CategoryFacade.Update(a[1], nm, ct);
                    System.Console.WriteLine("OK");
                    break;

                case "delete":
                    if (a.Length < 2) { System.Console.WriteLine("usage: category delete <id>"); return; }
                    CompositionRoot.CategoryFacade.Delete(a[1]);
                    System.Console.WriteLine("OK");
                    break;

                default: System.Console.WriteLine("unknown subcommand for category"); break;
            }
        }

        /// <summary>
        /// Обработчик команд <c>operation</c>.
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>operation</c>.</param>
        private static void HandleOperation(string[] a)
        {
            if (a.Length == 0) { System.Console.WriteLine("usage: operation <record|list|update|delete> ..."); return; }
            switch (a[0])
            {
                case "record":
                    // Добавление операции:
                    // operation record <accId> <catId> <income|expense> <amount> <yyyy-mm-dd> [desc...]
                    if (a.Length < 7) { System.Console.WriteLine("usage: operation record <accId> <catId> <income|expense> <amount> <yyyy-mm-dd> [desc]"); return; }
                    var accId = a[1]; var catId = a[2];
                    var type = ParseOpType(a[3]);
                    var amount = decimal.Parse(a[4], System.Globalization.CultureInfo.InvariantCulture);
                    var date = DateTime.Parse(a[5]); // ожидается локальный парсинг в текущей культуре
                    var desc = a.Length > 6 ? string.Join(' ', a.Skip(6)) : null;

                    // Замерить время выполнения записи операции.
                    var result = TimedCommandDecorator.Measure("RecordOperation",
                        () => CompositionRoot.OperationFacade.Record(accId, catId, type, amount, date, desc));

                    System.Console.WriteLine($"OK. New operation {result.Id}. Acc={result.BankAccountId} Amount={result.Amount} Type={result.Type} Date={result.Date:yyyy-MM-dd}");
                    break;

                case "list":
                    // Список операций с необязательными параметрами from/to:
                    // operation list <accId> [from yyyy-mm-dd] [to yyyy-mm-dd]
                    if (a.Length < 2) { System.Console.WriteLine("usage: operation list <accId> [from yyyy-mm-dd] [to yyyy-mm-dd]"); return; }
                    DateTime? from = null; DateTime? to = null;
                    if (a.Length >= 4 && a[2] == "from") from = DateTime.Parse(a[3]);
                    if (a.Length >= 6 && a[4] == "to") to = DateTime.Parse(a[5]);

                    foreach (var o in CompositionRoot.OperationFacade.List(a[1], from, to))
                        System.Console.WriteLine($"{o.Id}\t{o.Type}\t{o.Amount}\t{o.Date:yyyy-MM-dd}\tcat={o.CategoryId}\tacc={o.BankAccountId}\t{o.Description}");
                    break;

                case "update":
                    // Частичное обновление полей операции (паттерн: «ключ-значение»):
                    // operation update <opId> [amount x] [date yyyy-mm-dd] [desc text...] [catId id] [type income|expense]
                    if (a.Length < 2) { System.Console.WriteLine("usage: operation update <opId> [amount x] [date yyyy-mm-dd] [desc text] [catId id] [type income|expense]"); return; }
                    var opId = a[1];
                    decimal? newAmount = null; DateTime? newDate = null; string? newDesc = null; string? newCat = null; OperationType? newType = null;

                    // Парсим пары аргументов; для desc забираем остаток строки целиком.
                    for (int i = 2; i < a.Length; i++)
                    {
                        switch (a[i])
                        {
                            case "amount": newAmount = decimal.Parse(a[++i], System.Globalization.CultureInfo.InvariantCulture); break;
                            case "date": newDate = DateTime.Parse(a[++i]); break;
                            case "desc": newDesc = string.Join(' ', a.Skip(i+1)); i = a.Length; break; // остальное — описание
                            case "catId": newCat = a[++i]; break;
                            case "type": newType = ParseOpType(a[++i]); break;
                        }
                    }

                    CompositionRoot.OperationFacade.Update(opId, newAmount, newDate, newDesc, newCat, newType);
                    System.Console.WriteLine("OK");
                    break;

                case "delete":
                    if (a.Length < 2) { System.Console.WriteLine("usage: operation delete <id>"); return; }
                    CompositionRoot.OperationFacade.Delete(a[1]);
                    System.Console.WriteLine("OK");
                    break;

                default: System.Console.WriteLine("unknown subcommand for operation"); break;
            }
        }

        /// <summary>
        /// Обработчик команд <c>analytics</c>.
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>analytics</c>.</param>
        private static void HandleAnalytics(string[] a)
        {
            if (a.Length < 2) { System.Console.WriteLine("usage: analytics <net|bycat> ..."); return; }
            switch (a[0])
            {
                case "net":
                    // Чистая прибыль/расход/доход за период:
                    // analytics net <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>
                    if (a.Length < 4) { System.Console.WriteLine("usage: analytics net <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>"); return; }
                    var dto = CompositionRoot.AnalyticsFacade.GetNetIncome(a[1], DateTime.Parse(a[2]), DateTime.Parse(a[3]));
                    System.Console.WriteLine($"income={dto.Income} expense={dto.Expense} net={dto.Net}");
                    break;

                case "bycat":
                    // Суммы по категориям за период:
                    // analytics bycat <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>
                    if (a.Length < 4) { System.Console.WriteLine("usage: analytics bycat <accId> <from yyyy-mm-dd> <to yyyy-mm-dd>"); return; }
                    foreach (var x in CompositionRoot.AnalyticsFacade.GetSumByCategory(a[1], DateTime.Parse(a[2]), DateTime.Parse(a[3])))
                        System.Console.WriteLine($"{x.CategoryId}\t{x.CategoryName}\t{x.Amount}");
                    break;

                default: System.Console.WriteLine("unknown subcommand for analytics"); break;
            }
        }

        /// <summary>
        /// Обработчик команд <c>export</c> (выгрузка в JSON/YAML/CSV).
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>export</c>.</param>
        private static void HandleExport(string[] a)
        {
            if (a.Length < 2) { System.Console.WriteLine("usage: export <json|csv|yaml> <path>"); return; }
            var fmt = a[0]; var path = a[1];

            // 1) Выбираем конкретный экспортер (реализация IExportVisitor из Infrastructure).
            IExportVisitor exporter = fmt switch
            {
                "json" => new JsonExportVisitor(),
                "yaml" => new YamlExportVisitor(),
                "csv"  => new CsvExportVisitor(),
                _ => throw new ArgumentException("unknown export format")
            };

            // 2) Команда Application получает порт и фасады/репозитории.
            var cmd = new ExportData(exporter, path,
                Bootstrap.CompositionRoot.AccountFacade,
                Bootstrap.CompositionRoot.CategoryFacade,
                Bootstrap.CompositionRoot.Operations);

            // 3) (опционально) замеряем время выполнения экспорта через декоратор.
            new Application.Decorators.TimedCommand("ExportData", cmd).Execute();
            System.Console.WriteLine("OK");
        }

        /// <summary>
        /// Обработчик команд <c>import</c> (загрузка JSON/YAML/CSV).
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>import</c>.</param>
        private static void HandleImport(string[] a)
        {
            if (a.Length < 2) { System.Console.WriteLine("usage: import <json|csv|yaml> <path>"); return; }
            var fmt = a[0]; var path = a[1];

            // 1) Выбираем конкретный импортёр из Infrastructure (разрешено в Presentation).
            IImporter importer = fmt switch
            {
                "json" => new JsonImporter(),
                "yaml" => new YamlImporter(),
                "csv"  => new CsvImporter(),
                _ => throw new ArgumentException("unknown import format")
            };

            // 2) Создаём команду Application и передаём ей порт.
            var cmd = new ImportData(importer, path,
                Bootstrap.CompositionRoot.Accounts,
                Bootstrap.CompositionRoot.Categories,
                Bootstrap.CompositionRoot.Operations,
                Bootstrap.CompositionRoot.Uow);

            // 3) (опционально) оборачиваем таймером.
            new Application.Decorators.TimedCommand("ImportData", cmd).Execute();
            System.Console.WriteLine("OK");
        }
        
        /// <summary>
        /// Обработчик технической команды пересчёта баланса <c>recalc</c>.
        /// </summary>
        /// <param name="a">Аргументы подкоманды без корневого слова <c>recalc</c>.</param>
        private static void HandleRecalc(string[] a)
        {
            if (a.Length < 1) { System.Console.WriteLine("usage: recalc <accId>"); return; }
            var acc = CompositionRoot.Accounts.Get(a[0]) ?? throw new InvalidOperationException("account not found");
            var ops = CompositionRoot.Operations.GetByAccount(a[0]);

            // Сервис домена пересчитывает баланс по операциям.
            Domain.Services.BalanceRecalculationService.Recalculate(acc, ops);

            // Сохраняем изменения.
            CompositionRoot.Accounts.Update(acc);
            CompositionRoot.Uow.Commit();
            System.Console.WriteLine($"OK. New balance: {acc.Balance}");
        }
        
        /// <summary>
        /// Парсинг текстового типа категории (<c>income</c>/<c>expense</c>) в <see cref="CategoryType"/>.
        /// </summary>
        private static CategoryType ParseCategoryType(string s) => s.ToLowerInvariant() switch
        {
            "income" => CategoryType.Income,
            "expense" => CategoryType.Expense,
            _ => throw new ArgumentException("type must be income|expense")
        };

        /// <summary>
        /// Парсинг текстового типа операции (<c>income</c>/<c>expense</c>) в <see cref="OperationType"/>.
        /// </summary>
        private static OperationType ParseOpType(string s) => s.ToLowerInvariant() switch
        {
            "income" => OperationType.Income,
            "expense" => OperationType.Expense,
            _ => throw new ArgumentException("type must be income|expense")
        };
    }
}
