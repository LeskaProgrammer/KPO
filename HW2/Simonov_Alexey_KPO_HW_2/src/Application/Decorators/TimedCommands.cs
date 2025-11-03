﻿using System.Diagnostics;
using Application.Commands;

namespace Application.Decorators
{
    /// <summary>
    /// Декоратор команды без результата, измеряющий время её выполнения.
    /// </summary>
    /// <remarks>
    /// Паттерн: <b>Decorator</b> + <b>Command</b>.
    /// Сообщение формата <c>[timing] {name}: {ms} ms</c> отправляется в <paramref name="sink"/> или в стандартный вывод.
    /// Исключения из внутренней команды не подавляются: замер фиксируется в <c>finally</c>, затем исключение "всплывает".
    /// </remarks>
    public sealed class TimedCommand : ICommand
    {
        private readonly ICommand _inner;       // декорируемая команда
        private readonly string _name;          // метка для лога
        private readonly Action<string>? _sink; // потребитель строки лога

        /// <summary>
        /// Инициализирует декоратор тайминга для команды без результата.
        /// </summary>
        /// <param name="name">Имя операции/команды в логе.</param>
        /// <param name="inner">Внутренняя команда, которую нужно выполнить.</param>
        /// <param name="sink">Необязательный обработчик сообщения. Если не задан — печать в консоль.</param>
        public TimedCommand(string name, ICommand inner, Action<string>? sink = null)
        { _name = name; _inner = inner; _sink = sink; }

        /// <summary>
        /// Выполняет команду и логирует длительность выполнения.
        /// </summary>
        public void Execute()
        {
            var sw = Stopwatch.StartNew();
            try { _inner.Execute(); }
            finally
            {
                sw.Stop();
                var msg = $"[timing] { _name }: {sw.Elapsed.TotalMilliseconds:F2} ms"; // формат оставлен без изменений
                if (_sink != null) _sink(msg); else System.Console.WriteLine(msg);
            }
        }
    }

    /// <summary>
    /// Декоратор команды с результатом, измеряющий время её выполнения.
    /// </summary>
    /// <typeparam name="T">Тип результата команды.</typeparam>
    /// <remarks>
    /// Паттерн: <b>Decorator</b> + <b>Command</b>.
    /// Аналогично <see cref="TimedCommand"/>, но возвращает значение из внутренней команды.
    /// </remarks>
    public sealed class TimedCommand<T> : ICommand<T>
    {
        private readonly ICommand<T> _inner;    // декорируемая команда
        private readonly string _name;          // метка для лога
        private readonly Action<string>? _sink; // потребитель строки лога

        /// <summary>
        /// Инициализирует декоратор тайминга для команды с результатом.
        /// </summary>
        /// <param name="name">Имя операции/команды в логе.</param>
        /// <param name="inner">Внутренняя команда, которую нужно выполнить.</param>
        /// <param name="sink">Необязательный обработчик сообщения. Если не задан — печать в консоль.</param>
        public TimedCommand(string name, ICommand<T> inner, Action<string>? sink = null)
        { _name = name; _inner = inner; _sink = sink; }

        /// <summary>
        /// Выполняет команду, логирует длительность и возвращает результат.
        /// </summary>
        public T Execute()
        {
            var sw = Stopwatch.StartNew();
            try { return _inner.Execute(); }
            finally
            {
                sw.Stop();
                var msg = $"[timing] { _name }: {sw.Elapsed.TotalMilliseconds:F2} ms"; // формат оставлен без изменений
                if (_sink != null) _sink(msg); else System.Console.WriteLine(msg);
            }
        }
    }
}
