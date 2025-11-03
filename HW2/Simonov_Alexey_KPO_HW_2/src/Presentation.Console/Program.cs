namespace Presentation.Console
{
    /// <summary>
    /// Точка входа консольного приложения.
    /// </summary>
    /// <remarks>
    /// Работает в двух режимах:
    /// <list type="number">
    /// <item>
    /// <description>
    /// <b>CLI-режим</b> — если переданы аргументы командной строки. В этом режиме
    /// происходит регистрация зависимостей (DI) и делегирование выполнения в
    /// <see cref="CommandRouter"/> (без интерактивного меню).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Интерактивный режим</b> — если аргументов нет. Управление передаётся в
    /// <see cref="Presentation.Cli.InteractiveUi.Run"/>; там DI-регистрация вызывается внутри UI.
    /// </description>
    /// </item>
    /// </list>
    /// Примеры запуска CLI:
    /// <code>
    /// app.exe account create "Мой счёт"
    /// app.exe category list
    /// app.exe operation record &lt;accId&gt; &lt;catId&gt; income 100.50 2025-11-03 "Комментарий"
    /// </code>
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Главный метод приложения (entry point).
        /// </summary>
        /// <param name="args">
        /// Аргументы командной строки. Если непустые — запускается CLI-режим
        /// через <see cref="CommandRouter"/>; если пустые — запускается интерактивный UI.
        /// </param>
        public static void Main(string[] args)
        {
            // Если есть хотя бы один аргумент — идём по «скриптовому» пути:
            // инициализируем DI-контейнер и передаём управление роутеру команд.
            // Паттерн-матчинг по массиву: { Length: > 0 } читается как «длина больше нуля».
            if (args is { Length: > 0 })
            {
                Bootstrap.CompositionRoot.Register();
                CommandRouter.Dispatch(args);
                return;
            }

            // Без аргументов — интерактивное меню.
            // DI-регистрация вызывается внутри InteractiveUi.Run().
            Presentation.Cli.InteractiveUi.Run();
        }
    }
}
