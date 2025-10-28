namespace Presentation.Console;

public static class ConsoleApp
{
    public static void Run(string[] args)
    {
        CommandRouter.Dispatch(args);
    }
}
