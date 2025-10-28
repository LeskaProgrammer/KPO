namespace Presentation.Console;

public static class CommandRouter
{
    public static void Dispatch(string[] args)
    {
        if (args.Length == 0)
        {
            System.Console.WriteLine("usage: <cmd> [args]\ncmds: hello");
            return;
        }

        var cmd = args[0].ToLowerInvariant();
        switch (cmd)
        {
            case "hello":
                System.Console.WriteLine("hi from router");
                break;
            default:
                System.Console.WriteLine($"unknown cmd: {cmd}");
                break;
        }
    }
}
