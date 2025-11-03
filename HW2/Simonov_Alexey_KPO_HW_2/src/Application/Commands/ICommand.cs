namespace Application.Commands
{
    /// <summary>
    /// Базовый контракт команды без результата.
    /// </summary>
    /// <remarks>Паттерн Command.</remarks>
    public interface ICommand { void Execute(); }

    /// <summary>
    /// Базовый контракт команды с возвращаемым результатом.
    /// </summary>
    /// <typeparam name="T">Тип результата выполнения команды.</typeparam>
    /// <remarks>Паттерн Command.</remarks>
    public interface ICommand<out T> { T Execute(); }
}