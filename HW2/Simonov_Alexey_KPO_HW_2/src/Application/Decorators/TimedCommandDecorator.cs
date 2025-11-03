using System.Diagnostics;

namespace Application.Decorators
{
    /// <summary>
    /// Утилитарный декоратор-замерщик времени выполнения произвольной функции.
    /// </summary>
    /// <remarks>
    /// Паттерн: <b>Decorator</b> (оборачивает вызов и добавляет кросс-срезочную функциональность — тайминг).
    /// Если <paramref name="sink"/> не передан, пишет сообщение в стандартный вывод.
    /// Сообщение формата: <c>[timing] {name}: {ms} ms</c>.
    /// </remarks>
    public static class TimedCommandDecorator
    {
        /// <summary>
        /// Измеряет время выполнения функции и сообщает результат через <paramref name="sink"/> или <c>Console.WriteLine</c>.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого функцией результата.</typeparam>
        /// <param name="name">Человекочитаемое имя операции (попадёт в лог).</param>
        /// <param name="func">Делегат, выполнение которого нужно измерить.</param>
        /// <param name="sink">
        /// Необязательный потребитель строки лога. Если <c>null</c> — запись уходит в стандартный вывод.
        /// </param>
        /// <returns>Результат выполнения <paramref name="func"/>.</returns>
        /// <remarks>
        /// Время замеряется <see cref="Stopwatch"/>. Сообщение выводится в блоке <c>finally</c>,
        /// поэтому попадёт в лог даже при исключении (само исключение не перехватывается).
        /// </remarks>
        public static T Measure<T>(string name, Func<T> func, Action<string>? sink = null)
        {
            var sw = Stopwatch.StartNew();
            try { return func(); }
            finally
            {
                sw.Stop();
                var msg = $"[timing] {name}: {sw.Elapsed.TotalMilliseconds:F2} ms";
                if (sink != null) sink(msg); else System.Console.WriteLine(msg);
            }
        }
    }
}