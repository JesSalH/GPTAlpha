using Serilog;
namespace ConsoleTalker;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Welcome to the talker");

        // Set up Serilog for logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();


    }
}
