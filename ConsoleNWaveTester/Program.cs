
using NWaveApp;
using Serilog;

namespace ConsoleNWaveTester;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("NWave Voice tester!");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Create logger instance
        var logger = Log.Logger;

        var audioManager = new AudioManager(logger);

        audioManager.RecordVoice();
       
    }
}
