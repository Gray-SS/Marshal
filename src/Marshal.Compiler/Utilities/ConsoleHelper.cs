namespace Marshal.Compiler.Utilities;

public static class ConsoleHelper
{
    public static void Write(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ResetColor();
    }

    public static void WriteLine(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}