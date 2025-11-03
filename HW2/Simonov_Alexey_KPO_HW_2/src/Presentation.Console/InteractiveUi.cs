using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Application.Analytics;
using Microsoft.Extensions.DependencyInjection;
using Bootstrap;
using Application.Dtos;
using Domain.ValueObjects;
using Application.Decorators; // <-- добавлено: для TimedCommandDecorator

namespace Presentation.Cli
{
    /// <summary>
    /// Интерактивный консольный UI приложения.
    /// </summary>
    /// <remarks>
    /// Отвечает за отрисовку меню, обработку пользовательского ввода,
    /// вызовы фасадов приложения и отображение результатов.
    /// Использует зарегистрированные зависимости через <see cref="CompositionRoot"/>.
    /// </remarks>
    public static class InteractiveUi
    {
        /// <summary>
        /// Точка входа в интерактивный режим.
        /// </summary>
        /// <remarks>
        /// Выполняет регистрацию зависимостей, настраивает кодировку вывода
        /// и запускает главный цикл с отрисовкой корневого меню.
        /// </remarks>
        public static void Run()
        {
            CompositionRoot.Register();                         // DI-контейнер и все сервисы
            global::System.Console.OutputEncoding = Encoding.UTF8; // Корректный вывод кириллицы

            while (true)
            {
                // Главное меню приложения
                var choice = Menu("Главное меню", new[]
                {
                    "Счёта",
                    "Категории",
                    "Операции",
                    "Аналитика",
                    "Импорт / Экспорт",
                    "Пересчитать баланс",
                    "Выход"
                });

                switch (choice)
                {
                    case 0: Safe(AccountsMenu,"Счёта");break;
                    case 1: Safe(CategoriesMenu,"Категории");break;
                    case 2: Safe(OperationsMenu,"Операции");break;
                    case 3: Safe(AnalyticsMenu,"Аналитика");break;
                    case 4: Safe(ImportExportMenu,"Импорт/Экспорт");break;
                    case 5: Safe(RecalcMenu,"Пересчёт"); break;
                    case 6: return; 
                }

            }
        }
        
        
        
        
        private static void Safe(Action action, string context)
        {
            try { action(); }
            catch (FileNotFoundException ex)
            {
                Warn($"[{context}] Файл не найден: {(ex.FileName ?? ex.Message)}" +
                     $"\nТекущая папка: {System.IO.Directory.GetCurrentDirectory()}");
                Pause();
            }
            catch (DirectoryNotFoundException ex)
            {
                Warn($"[{context}] Папка не найдена: {ex.Message}");
                Pause();
            }
            catch (UnauthorizedAccessException ex)
            {
                Warn($"[{context}] Нет доступа: {ex.Message}");
                Pause();
            }
            catch (IOException ex)
            {
                Warn($"[{context}] Ошибка ввода-вывода: {ex.Message}");
                Pause();
            }
            catch (FormatException ex)
            {
                Warn($"[{context}] Неверный формат ввода: {ex.Message}");
                Pause();
            }
            catch (Exception ex)
            {
                Warn($"[{context}] Неожиданная ошибка: {ex.Message}");
                Pause();
            }
        }


        // ---------- Счёта ----------

        /// <summary>
        /// Отображает подменю управления счетами
        /// (список, создание, переименование, удаление).
        /// </summary>
        private static void AccountsMenu()
        {
            while (true)
            {
                var i = Menu("Счёта", new[]
                {
                    "Показать все",
                    "Создать",
                    "Переименовать",
                    "Удалить",
                    "Назад"
                });
                switch (i)
                {
                    case 0:
                        ShowAccounts(); // Печать всех счетов
                        Pause();
                        break;

                    case 1:
                    {
                        var name = Prompt("Название счёта: ");
                        var acc = TimedCommandDecorator.Measure(
                            "Account.Create",
                            () => CompositionRoot.AccountFacade.Create(name)); // тайминг создания
                        Info($"Создан счёт: {acc.Id} — {acc.Name}, баланс={acc.Balance}");
                        Pause();
                        break;
                    }

                    case 2:
                    {
                        var accSel = SelectAccountOrBack(); // Выбор счета из списка
                        if (accSel is null) break;
                        var newName = Prompt("Новое имя: ");
                        TimedCommandDecorator.Measure(
                            "Account.Rename",
                            () => { CompositionRoot.AccountFacade.Rename(accSel.Id, newName); return 0; }); // тайминг rename
                        Info("ОК");
                        Pause();
                        break;
                    }

                    case 3:
                    {
                        var accSel = SelectAccountOrBack();
                        if (accSel is null) break;
                        if (Confirm($"Удалить счёт {accSel.Name}?"))
                        {
                            TimedCommandDecorator.Measure(
                                "Account.Delete",
                                () => { CompositionRoot.AccountFacade.Delete(accSel.Id); return 0; }); // тайминг delete
                            Info("Удалено");
                        }
                        Pause();
                        break;
                    }

                    case 4: return; // Назад в главное меню
                }
            }
        }

        // ---------- Категории ----------

        /// <summary>
        /// Подменю категорий: просмотр, создание, изменение, удаление.
        /// </summary>
        private static void CategoriesMenu()
        {
            while (true)
            {
                var i = Menu("Категории", new[]
                {
                    "Показать все",
                    "Создать",
                    "Изменить",
                    "Удалить",
                    "Назад"
                });
                switch (i)
                {
                    case 0:
                        ShowCategories(); // Печать всех категорий
                        Pause();
                        break;

                    case 1:
                    {
                        // Выбор типа категории
                        var type = Menu("Тип категории", new[] {"Доход", "Расход"}) == 0 ? CategoryType.Income : CategoryType.Expense;
                        var name = Prompt("Название категории: ");
                        var c = TimedCommandDecorator.Measure(
                            "Category.Create",
                            () => CompositionRoot.CategoryFacade.Create(name, type)); // тайминг create
                        Info($"Создана категория: {c.Id} — {c.Type} — {c.Name}");
                        Pause();
                        break;
                    }

                    case 2:
                    {
                        var cat = SelectCategoryOrBack();
                        if (cat is null) break;

                        // Выбор нового типа и/или имени
                        var type = Menu("Тип", new[] {"Доход", "Расход"}) == 0 ? CategoryType.Income : CategoryType.Expense;
                        var newName = Prompt("Новое имя (пусто — оставить): ", allowEmpty:true);
                        TimedCommandDecorator.Measure(
                            "Category.Update",
                            () => { CompositionRoot.CategoryFacade.Update(cat.Id, string.IsNullOrWhiteSpace(newName) ? cat.Name : newName, type); return 0; });
                        Info("ОК");
                        Pause();
                        break;
                    }

                    case 3:
                    {
                        var cat = SelectCategoryOrBack();
                        if (cat is null) break;

                        if (Confirm($"Удалить категорию {cat.Name}?"))
                        {
                            TimedCommandDecorator.Measure(
                                "Category.Delete",
                                () => { CompositionRoot.CategoryFacade.Delete(cat.Id); return 0; });
                            Info("Удалено");
                        }
                        Pause();
                        break;
                    }

                    case 4: return;
                }
            }
        }

        // ---------- Операции ----------

        /// <summary>
        /// Подменю операций: просмотр по счёту, добавление, редактирование, удаление.
        /// </summary>
        private static void OperationsMenu()
        {
            while (true)
            {
                var i = Menu("Операции", new[]
                {
                    "Список по счёту",
                    "Добавить операцию",
                    "Изменить операцию",
                    "Удалить операцию",
                    "Назад"
                });

                switch (i)
                {
                    case 0:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        var ops = TimedCommandDecorator.Measure(
                            "Operation.List",
                            () => CompositionRoot.OperationFacade.List(acc.Id).ToArray()); // тайминг list
                        ShowOperations(ops);
                        Pause();
                        break;
                    }

                    case 1:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        // Тип операции определяет допустимые категории (доходные/расходные)
                        var t = Menu("Тип операции", new[] {"Доход", "Расход"}) == 0 ? OperationType.Income : OperationType.Expense;

                        // Отбираем только категории нужного типа
                        var cats = TimedCommandDecorator.Measure(
                            "Category.List.ForOperation",
                            () => CompositionRoot.CategoryFacade.List()
                                .Where(c => (t == OperationType.Income  && c.Type == CategoryType.Income) ||
                                            (t == OperationType.Expense && c.Type == CategoryType.Expense))
                                .ToArray());

                        if (cats.Length == 0) { Warn("Нет подходящих категорий."); Pause(); break; }

                        var cat = Select("Категория", cats, c => $"{c.Name} ({c.Type})");
                        if (cat is null) break;

                        // Ввод суммы/даты/описания
                        var amount = PromptDecimal("Сумма: ");
                        var dt = PromptDate("Дата (yyyy-MM-dd): ");
                        var desc = Prompt("Описание (опц.): ", allowEmpty:true);

                        // Фиксация операции через фасад (тайминг)
                        var dto = TimedCommandDecorator.Measure(
                            "Operation.Record",
                            () => CompositionRoot.OperationFacade.Record(
                                acc.Id, cat.Id, t, amount, dt, string.IsNullOrWhiteSpace(desc)? null : desc));
                        Info($"ОК. Операция {dto.Id}");
                        Pause();
                        break;
                    }

                    case 2:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        var ops = TimedCommandDecorator.Measure(
                            "Operation.List",
                            () => CompositionRoot.OperationFacade.List(acc.Id).ToArray());
                        if (ops.Length == 0) { Warn("Нет операций."); Pause(); break; }

                        // Выбор операции для редактирования
                        Application.Dtos.OperationDto? op =
                            Select<Application.Dtos.OperationDto>(
                                "Выбери операцию",
                                ops,
                                o => $"{o.Date:yyyy-MM-dd}  {o.Type}  {o.Amount}  cat={o.CategoryId}  \"{o.Description}\""
                            );
                        if (op is null) break;

                        // Буфер изменений (по умолчанию — ничего не меняем)
                        decimal? newAmount = null; DateTime? newDate = null; string? newDesc = null; string? newCat = null; OperationType? newType = null;

                        while (true)
                        {
                            // Локальное меню изменения полей
                            var edit = Menu("Что изменить", new[] { "Сумму", "Дату", "Описание", "Категорию", "Тип", "Применить", "Отмена" });
                            if (edit == 5)
                            {
                                // Применяем накопленные изменения единым вызовом фасада (тайминг)
                                TimedCommandDecorator.Measure(
                                    "Operation.Update",
                                    () => { 
                                        CompositionRoot.OperationFacade.Update(op.Id,
                                            amount: newAmount,
                                            date: newDate,
                                            description: newDesc == "" ? null : newDesc,
                                            categoryId: newCat,
                                            newType: newType);
                                        return 0;
                                    });
                                Info("ОК");
                                Pause();
                                break;
                            }
                            if (edit == 6) break;

                            // Ввод новых значений по выбранному полю
                            switch (edit)
                            {
                                case 0: newAmount = PromptDecimal("Новая сумма: "); break;
                                case 1: newDate = PromptDate("Новая дата (yyyy-MM-dd): "); break;
                                case 2:
                                    newDesc = Prompt("Новое описание (пусто — null): ", allowEmpty:true);
                                    if (string.IsNullOrWhiteSpace(newDesc)) newDesc = "";
                                    break;
                                case 3:
                                {
                                    var catsAll = TimedCommandDecorator.Measure(
                                        "Category.List",
                                        () => CompositionRoot.CategoryFacade.List().ToArray());
                                    if (catsAll.Length == 0) Warn("Категорий нет.");
                                    else
                                    {
                                        Application.Dtos.CategoryDto? c =
                                            Select("Категория", catsAll, x => $"{x.Name} ({x.Type})");
                                        if (c is not null) newCat = c.Id;
                                    }
                                    break;
                                }
                                case 4:
                                    newType = Menu("Тип", new[] {"Доход","Расход"})==0 ? OperationType.Income : OperationType.Expense;
                                    break;
                            }
                        }
                        break;
                    }

                    case 3:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        var ops = TimedCommandDecorator.Measure(
                            "Operation.List",
                            () => CompositionRoot.OperationFacade.List(acc.Id).ToArray());
                        if (ops.Length == 0) { Warn("Нет операций."); Pause(); break; }

                        var op = Select("Удалить операцию", ops,
                            o => $"{o.Date:yyyy-MM-dd}  {o.Type}  {o.Amount}  cat={o.CategoryId}");
                        if (op is null) break;

                        if (Confirm("Точно удалить?"))
                        {
                            TimedCommandDecorator.Measure(
                                "Operation.Delete",
                                () => { CompositionRoot.OperationFacade.Delete(op.Id); return 0; });
                            Info("Удалено");
                        }
                        Pause();
                        break;
                    }

                    case 4: return;
                }
            }
        }

        // ---------- Аналитика ----------

        /// <summary>
        /// Подменю аналитики: базовые агрегаты и стратегические группировки.
        /// </summary>
        private static void AnalyticsMenu()
        {
            while (true)
            {
                var i = Menu("Аналитика", new[]
                {
                    "Доход/Расход/Чистый за период",
                    "Суммы по категориям за период",
                    "Группировка (Strategy): выбрать стратегию",
                    "Назад"
                });
                switch (i)
                {
                    case 0:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        var from = PromptDate("От (yyyy-MM-dd): ");
                        var to   = PromptDate("До (yyyy-MM-dd): ");
                        var dto = TimedCommandDecorator.Measure(
                            "Analytics.NetIncome",
                            () => CompositionRoot.AnalyticsFacade.GetNetIncome(acc.Id, from, to)); // тайминг
                        Info($"Доход={dto.Income}, Расход={dto.Expense}, Чистый={dto.Net}");
                        Pause();
                        break;
                    }

                    case 1:
                    {
                        var acc = SelectAccountOrBack();
                        if (acc is null) break;

                        var from = PromptDate("От (yyyy-MM-dd): ");
                        var to   = PromptDate("До (yyyy-MM-dd): ");
                        var rows = TimedCommandDecorator.Measure(
                            "Analytics.SumByCategory",
                            () => CompositionRoot.AnalyticsFacade.GetSumByCategory(acc.Id, from, to).ToArray());

                        global::System.Console.Clear();
                        Title("Суммы по категориям");
                        foreach (var r in rows)
                            global::System.Console.WriteLine($"{r.CategoryName,-20}  {r.Amount}");
                        Pause();
                        break;
                    }

                    case 2:
                    {
                        ShowAnalyticsGrouped(); // Вызов стратегической группировки
                        break;
                    }

                    case 3: return;
                }
            }
        }

        /// <summary>
        /// Диалог группировки по выбранной стратегии (<see cref="IGroupingStrategy"/>).
        /// </summary>
        private static void ShowAnalyticsGrouped()
        {
            // Получаем все зарегистрированные стратегии из DI
            var strategies = Bootstrap.CompositionRoot.Provider
                .GetServices<IGroupingStrategy>()
                .ToList();

            if (strategies.Count == 0)
            {
                Warn("Стратегии не зарегистрированы в DI.");
                Pause();
                return;
            }

            // Меню выбора стратегии по её Title
            var items = strategies.Select(s => s.Title).Concat(new[] { "Назад" }).ToArray();
            var idx = Menu("Группировать по…", items);
            if (idx >= strategies.Count) return; // Выбрали «Назад»

            var strategy = strategies[idx];

            var acc = SelectAccountOrBack();
            if (acc is null) return;

            var from = PromptDate("От (yyyy-MM-dd): ");
            var to   = PromptDate("До (yyyy-MM-dd): ");

            // Получаем группы из фасада аналитики (тайминг)
            var groups = TimedCommandDecorator.Measure(
                $"Analytics.Grouped:{strategy.Title}",
                () => CompositionRoot.AnalyticsFacade.GetGrouped(acc.Id, from, to, strategy).ToArray());

            global::System.Console.Clear();
            Title($"Группировка: {strategy.Title}");
            if (groups.Length == 0)
            {
                Info("Нет данных за период.");
            }
            else
            {
                foreach (var g in groups)
                    global::System.Console.WriteLine($"{g.Key,-20}  {g.Amount}");
            }
            Pause();
        }

        // ---------- Импорт / Экспорт ----------

        /// <summary>
        /// Подменю импорта/экспорта данных в разные форматы.
        /// </summary>
        private static void ImportExportMenu()
        {
            while (true)
            {
                var i = Menu("Импорт / Экспорт", new[]
                {
                    "Импорт JSON (файл)",
                    "Импорт YAML (файл)",
                    "Импорт CSV (папка)",
                    "Экспорт JSON (файл)",
                    "Экспорт YAML (файл)",
                    "Экспорт CSV (папка)",
                    "Назад"
                });

                switch (i)
                {
                    case 0:
                    {
                        var path = Prompt("Путь к .json: ");
                        var importer = new Infrastructure.ImportExport.Importers.JsonImporter();
                        var cmd = new Application.Commands.IO.ImportData(importer, path,
                            CompositionRoot.Accounts, CompositionRoot.Categories, CompositionRoot.Operations, CompositionRoot.Uow);
                        new Application.Decorators.TimedCommand("ImportData", cmd).Execute(); // Оборачиваем декоратором тайминга
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 1:
                    {
                        var path = Prompt("Путь к .yaml/.yml: ");
                        var importer = new Infrastructure.ImportExport.Importers.YamlImporter();
                        var cmd = new Application.Commands.IO.ImportData(importer, path,
                            CompositionRoot.Accounts, CompositionRoot.Categories, CompositionRoot.Operations, CompositionRoot.Uow);
                        new Application.Decorators.TimedCommand("ImportData", cmd).Execute();
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 2:
                    {
                        var folder = Prompt("Папка с accounts.csv/categories.csv/operations.csv: ");
                        var importer = new Infrastructure.ImportExport.Importers.CsvImporter();
                        var cmd = new Application.Commands.IO.ImportData(importer, folder,
                            CompositionRoot.Accounts, CompositionRoot.Categories, CompositionRoot.Operations, CompositionRoot.Uow);
                        new Application.Decorators.TimedCommand("ImportData", cmd).Execute();
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 3:
                    {
                        var path = Prompt("Файл .json для экспорта: ");
                        var exporter = new Infrastructure.ImportExport.Exporters.JsonExportVisitor();
                        var cmd = new Application.Commands.IO.ExportData(exporter, path,
                            CompositionRoot.AccountFacade, CompositionRoot.CategoryFacade, CompositionRoot.Operations);
                        new Application.Decorators.TimedCommand("ExportData", cmd).Execute();
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 4:
                    {
                        var path = Prompt("Файл .yaml для экспорта: ");
                        var exporter = new Infrastructure.ImportExport.Exporters.YamlExportVisitor();
                        var cmd = new Application.Commands.IO.ExportData(exporter, path,
                            CompositionRoot.AccountFacade, CompositionRoot.CategoryFacade, CompositionRoot.Operations);
                        new Application.Decorators.TimedCommand("ExportData", cmd).Execute();
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 5:
                    {
                        var folder = Prompt("Папка для CSV: ");
                        var exporter = new Infrastructure.ImportExport.Exporters.CsvExportVisitor();
                        var cmd = new Application.Commands.IO.ExportData(exporter, folder,
                            CompositionRoot.AccountFacade, CompositionRoot.CategoryFacade, CompositionRoot.Operations);
                        new Application.Decorators.TimedCommand("ExportData", cmd).Execute();
                        Info("OK");
                        Pause();
                        break;
                    }

                    case 6: return;
                }
            }
        }

        // ---------- Пересчёт ----------

        /// <summary>
        /// Диалог пересчёта баланса выбранного счёта.
        /// </summary>
        private static void RecalcMenu()
        {
            var acc = SelectAccountOrBack();
            if (acc is null) return;

            var cmd = new Application.Commands.Maintenance.RecalculateBalance(
                CompositionRoot.Accounts, CompositionRoot.Operations, CompositionRoot.Uow, acc.Id);
            new Application.Decorators.TimedCommand("Recalc", cmd).Execute(); // Замер времени
            Info("ОК");
            Pause();
        }

        // ================== Вспомогалки UI ==================

        private static int Menu(string title, IReadOnlyList<string> items)
        {
            int index = 0;
            global::System.ConsoleKey key;
            do
            {
                global::System.Console.Clear();
                Title(title);
                for (int i = 0; i < items.Count; i++)
                {
                    // Подсвечиваем текущий элемент
                    if (i == index) { global::System.Console.Write("> "); global::System.Console.ForegroundColor = global::System.ConsoleColor.Cyan; }
                    else            { global::System.Console.Write("  "); global::System.Console.ResetColor(); }
                    global::System.Console.WriteLine(items[i]);
                    global::System.Console.ResetColor();
                }
                global::System.Console.WriteLine("\n↑/↓ — выбрать, Enter — подтвердить, Esc — назад");

                var ki = global::System.Console.ReadKey(true);
                key = ki.Key;
                if (key == global::System.ConsoleKey.UpArrow) index = (index + items.Count - 1) % items.Count;
                else if (key == global::System.ConsoleKey.DownArrow) index = (index + 1) % items.Count;
                else if (key == global::System.ConsoleKey.Escape) return items.Count - 1; // Возвращаем «Назад»
            }
            while (key != global::System.ConsoleKey.Enter);
            return index;
        }

        private static T? Select<T>(string title, IReadOnlyList<T> list, Func<T, string> render) where T : class
        {
            if (list.Count == 0) return null;
            int index = 0;
            global::System.ConsoleKey key;
            do
            {
                global::System.Console.Clear(); Title(title);
                for (int i = 0; i < list.Count; i++)
                {
                    if (i == index) { global::System.Console.Write("> "); global::System.Console.ForegroundColor = global::System.ConsoleColor.Cyan; }
                    else            { global::System.Console.Write("  "); global::System.Console.ResetColor(); }
                    global::System.Console.WriteLine(render(list[i]));
                    global::System.Console.ResetColor();
                }
                global::System.Console.WriteLine("\n↑/↓ — выбрать, Enter — подтвердить, Esc — отмена");
                var ki = global::System.Console.ReadKey(true);
                key = ki.Key;
                if (key == global::System.ConsoleKey.UpArrow) index = (index + list.Count - 1) % list.Count;
                else if (key == global::System.ConsoleKey.DownArrow) index = (index + 1) % list.Count;
                else if (key == global::System.ConsoleKey.Escape) return null;
            }
            while (key != global::System.ConsoleKey.Enter);
            return list[index];
        }

        private static BankAccountDto? SelectAccountOrBack()
        {
            var list = TimedCommandDecorator.Measure(
                "Account.List",
                () => CompositionRoot.AccountFacade.List().ToArray()); // тайминг list
            if (list.Length == 0) { Warn("Счета отсутствуют."); return null; }
            return Select("Выбери счёт", list, a => $"{a.Name}  (баланс={a.Balance})  [{a.Id}]");
        }

        private static CategoryDto? SelectCategoryOrBack()
        {
            var list = TimedCommandDecorator.Measure(
                "Category.List",
                () => CompositionRoot.CategoryFacade.List().ToArray()); // тайминг list
            if (list.Length == 0) { Warn("Категории отсутствуют."); return null; }
            return Select("Выбери категорию", list, c => $"{c.Name} ({c.Type}) [{c.Id}]");
        }

        private static void ShowAccounts()
        {
            var accounts = TimedCommandDecorator.Measure(
                "Account.List",
                () => CompositionRoot.AccountFacade.List().ToArray()); // тайминг list
            global::System.Console.Clear(); Title("Счёта");
            foreach (var a in accounts)
                global::System.Console.WriteLine($"{a.Id}  | {a.Name,-20} | баланс={a.Balance}");
        }

        private static void ShowCategories()
        {
            var cats = TimedCommandDecorator.Measure(
                "Category.List",
                () => CompositionRoot.CategoryFacade.List().ToArray()); // тайминг list
            global::System.Console.Clear(); Title("Категории");
            foreach (var c in cats)
                global::System.Console.WriteLine($"{c.Id}  | {c.Type,-7} | {c.Name}");
        }

        private static void ShowOperations(IEnumerable<OperationDto> ops)
        {
            global::System.Console.Clear(); Title("Операции");
            foreach (var o in ops)
                global::System.Console.WriteLine($"{o.Id} | {o.Date:yyyy-MM-dd} | {o.Type,-7} | {o.Amount,10} | cat={o.CategoryId} | acc={o.BankAccountId} | {o.Description}");
        }

        private static string Prompt(string label, bool allowEmpty = false)
        {
            global::System.Console.Write(label);
            while (true)
            {
                var s = global::System.Console.ReadLine() ?? "";
                if (allowEmpty || !string.IsNullOrWhiteSpace(s)) return s.Trim();
                global::System.Console.Write("Введите непустое значение: ");
            }
        }

        private static decimal PromptDecimal(string label)
        {
            global::System.Console.Write(label);
            while (true)
            {
                var s = global::System.Console.ReadLine();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v >= 0)
                    return v;
                global::System.Console.Write("Введите число ≥ 0 (через точку): ");
            }
        }

        private static DateTime PromptDate(string label)
        {
            global::System.Console.Write(label);
            while (true)
            {
                var s = global::System.Console.ReadLine();
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
                global::System.Console.Write("Формат yyyy-MM-dd: ");
            }
        }

        private static bool Confirm(string question)
        {
            global::System.Console.Write($"{question} [y/N]: ");
            var k = global::System.Console.ReadKey(true).Key;
            global::System.Console.WriteLine();
            return k == global::System.ConsoleKey.Y;
        }

        private static void Title(string s)
        {
            global::System.Console.ForegroundColor = global::System.ConsoleColor.Yellow;
            global::System.Console.WriteLine(s);
            global::System.Console.WriteLine(new string('─', Math.Max(10, s.Length)));
            global::System.Console.ResetColor();
        }

        private static void Info(string s)
        {
            global::System.Console.ForegroundColor = global::System.ConsoleColor.Green;
            global::System.Console.WriteLine(s);
            global::System.Console.ResetColor();
        }

        private static void Warn(string s)
        {
            global::System.Console.ForegroundColor = global::System.ConsoleColor.DarkYellow;
            global::System.Console.WriteLine(s);
            global::System.Console.ResetColor();
        }

        private static void Pause()
        {
            global::System.Console.WriteLine("\nНажми любую клавишу…");
            global::System.Console.ReadKey(true);
        }
    }
}
