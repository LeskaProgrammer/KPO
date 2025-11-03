using Bootstrap;

namespace Presentation.Console
{
    /// <summary>
    /// Запуск консольного приложения в двух режимах:
    /// <list type="number">
    /// <item><description><b>CLI-режим</b> — если переданы аргументы; выполняется одна команда и приложение завершается.</description></item>
    /// <item><description><b>Интерактивный режим</b> — если аргументы не переданы; запускается REPL-петля с разбором строк.</description></item>
    /// </list>
    /// </summary>
    public static class ConsoleApp
    {
        /// <summary>
        /// Точка входа для CLI/REPL-обёртки над <see cref="CommandRouter"/>.
        /// </summary>
        /// <param name="args">
        /// Аргументы командной строки. Если не пустые — будет выполнен <see cref="CommandRouter.Dispatch(string[])"/> и выход.
        /// Если пустые — откроется интерактивный режим (REPL).
        /// </param>
        /// <remarks>
        /// Перед началом работы вызывает <see cref="CompositionRoot.Register"/> для настройки DI-контейнера.
        /// В интерактивном режиме поддерживаются команды <c>help</c> (печать справки) и <c>exit</c>/<c>quit</c> (выход).
        /// </remarks>
        /// <example>
        /// Запуск в CLI-режиме:
        /// <code>
        /// app.exe category create income "Кафе и еда"
        /// app.exe account list
        /// </code>
        /// Запуск в интерактивном режиме:
        /// <code>
        /// app.exe
        /// &gt; help
        /// &gt; account create "Наличные"
        /// &gt; exit
        /// </code>
        /// </example>
        public static void Run(string[] args)
        {
            // Инициализация DI/реестров приложения (репозитории, фасады и др.).
            CompositionRoot.Register();

            // Если есть аргументы — выполняем одноразовый вызов роутера и выходим.
            if (args is { Length: > 0 })
            {
                CommandRouter.Dispatch(args);
                return;
            }

            // Интерактивный режим (REPL): приветствие и подсказка.
            System.Console.WriteLine("KPO Finance CLI — interactive mode");
            System.Console.WriteLine("Type 'help' to show commands, 'exit' to quit.\n");

            // Показать usage/справку один раз при входе.
            CommandRouter.Dispatch(System.Array.Empty<string>());

            // Основной цикл чтения строк и их выполнения.
            while (true)
            {
                System.Console.Write("> ");
                var line = System.Console.ReadLine();
                if (line is null) break; // EOF/закрыт stdin

                line = line.Trim();
                if (line.Length == 0) continue; // пустая строка — пропускаем

                // Команды быстрого выхода.
                if (line.Equals("exit", System.StringComparison.OrdinalIgnoreCase) ||
                    line.Equals("quit", System.StringComparison.OrdinalIgnoreCase))
                    break;

                // Локальная справка.
                if (line.Equals("help", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Печать usage через роутер.
                    CommandRouter.Dispatch(System.Array.Empty<string>());
                    continue;
                }

                try
                {
                    // Разбор пользовательской строки в массив аргументов (с поддержкой кавычек).
                    var parsed = SplitArgs(line);
                    // Делегирование выполнения в роутер команд.
                    CommandRouter.Dispatch(parsed);
                }
                catch (System.Exception ex)
                {
                    // Лаконичная обработка ошибок верхнего уровня REPL.
                    System.Console.WriteLine("ERROR: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Простой парсер аргументов командной строки с поддержкой двойных кавычек.
        /// </summary>
        /// <param name="input">Исходная строка, например: <c>category create income "Кафе и еда"</c>.</param>
        /// <returns>Массив аргументов без кавычек; пробелы внутри кавычек сохраняются как единый аргумент.</returns>
        /// <remarks>
        /// Поддерживается только «переключение» состояния внутри двойных кавычек.
        /// Экранирование кавычек внутри кавычек (<c>\"</c>) или вложенные кавычки не обрабатываются.
        /// Лишние пробелы вне кавычек игнорируются.
        /// </remarks>
        /// <example>
        /// <code>
        /// SplitArgs("category create income \"Кафе и еда\"")
        /// // => ["category", "create", "income", "Кафе и еда"]
        /// </code>
        /// </example>
        private static string[] SplitArgs(string input)
        {
            var args = new System.Collections.Generic.List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (var ch in input)
            {
                if (ch == '"') { inQuotes = !inQuotes; continue; } // переключаемся в/из режима кавычек
                if (char.IsWhiteSpace(ch) && !inQuotes)
                {
                    // Разделитель аргументов — пробел вне кавычек.
                    if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); }
                }
                else current.Append(ch); // накапливаем символ в текущем аргументе
            }
            // Добавляем последний аргумент, если он был.
            if (current.Length > 0) args.Add(current.ToString());
            return args.ToArray();
        }
    }
}
