
using NWaveTest;

namespace ConsoleNWaveTester;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("NWave Voice tester!");

        var audioManager = new AudioManager();
        string filePath = "test.wav";

        Console.WriteLine("Available input devices:");
        audioManager.ListInputDevices();
        
        Console.WriteLine("Enter the device index to use for recording:");
        if (int.TryParse(Console.ReadLine(), out int inputDeviceIndex))
        {
            Console.WriteLine("Press 'R' to start recording...");
            while (Console.ReadKey(true).Key != ConsoleKey.R)
            {
                // Wait for the user to press 'R'
            }

            audioManager.StartRecording(filePath, inputDeviceIndex);          
        }
        else
        {
            Console.WriteLine("Invalid input device index.");
        }

        Console.WriteLine("Available output devices:");
        audioManager.ListOutputDevices();
        if (int.TryParse(Console.ReadLine(), out int outputDeviceIndex))
        {

            audioManager.PlayRecording(filePath, outputDeviceIndex);
        }
        else
        {
            Console.WriteLine("Invalid output device index.");
        }
    }
}
