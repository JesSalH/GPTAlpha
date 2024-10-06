using NAudio.Wave;
using Serilog;

namespace NWaveApp;

public class AudioManager
{
    private WaveInEvent _waveIn;
    private WaveFileWriter _writer;
    private string _outputFilePath;
    private BufferedWaveProvider _bufferedWaveProvider;
    private WaveOutEvent _waveOut;
    private readonly ILogger _logger;

    public AudioManager(ILogger logger)
    {
        _logger = logger;     
    }

    public void ListInputDevices()
    {
        for (int i = 0; i < WaveIn.DeviceCount; i++)
        {
            var deviceInfo = WaveIn.GetCapabilities(i);
            Console.WriteLine($"{i}: {deviceInfo.ProductName}");
        }
    }

    public void ListOutputDevices()
    {
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var deviceInfo = WaveOut.GetCapabilities(i);
            Console.WriteLine($"{i}: {deviceInfo.ProductName}");
        }
    }

    public void StartRecording(string filePath, int deviceIndex)
    {
        _outputFilePath = filePath;
        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex, // Set the device index
            WaveFormat = new WaveFormat(16000, 1)
        };

        _waveIn.DataAvailable += (sender, e) =>
        {
            if (_writer == null)
            {
                _writer = new WaveFileWriter(_outputFilePath, _waveIn.WaveFormat);
            }
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
        };

        _waveIn.RecordingStopped += (sender, e) =>
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn.Dispose();
        };

        _waveIn.StartRecording();
        Console.WriteLine("Recording... Press any key to stop.");
        Console.ReadKey();
        _waveIn.StopRecording();
    }

    public void PlayRecording(string filePath, int deviceIndex)
    {
        using (var audioFile = new AudioFileReader(filePath))
        using (var outputDevice = new WaveOutEvent { DeviceNumber = deviceIndex }) // Set the device index
        {
            outputDevice.Init(audioFile);
            outputDevice.Play();
            Console.WriteLine("Playing... Press any key to stop.");
            Console.ReadKey();
            outputDevice.Stop();
        }
    }

    public void PlayAudio(byte[] audioBuffer, int count)
    {
        try
        {
            if (_bufferedWaveProvider == null)
            {
                var waveFormat = new WaveFormat(16000, 16, 1);
                _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_bufferedWaveProvider);
                _waveOut.Play();
            }

            // Add audio data to the buffer
            _bufferedWaveProvider.AddSamples(audioBuffer, 0, count);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error playing audio: {ex.Message}");
            _waveOut?.Stop();
        }
    }

    public void RecordVoice()
    {
        string filePath = "test.wav";

        Console.WriteLine("Available input devices:");
        ListInputDevices();

        Console.WriteLine("Enter the device index to use for recording:");
        if (int.TryParse(Console.ReadLine(), out int inputDeviceIndex))
        {
            Console.WriteLine("Press 'R' to start recording...");
            while (Console.ReadKey(true).Key != ConsoleKey.R)
            {
                // Wait for the user to press 'R'
            }

            StartRecording(filePath, inputDeviceIndex);
        }
        else
        {
            Console.WriteLine("Invalid input device index.");
        }

        Console.WriteLine("Let's hear what you've recorded. Available output devices:");
        ListOutputDevices();
        if (int.TryParse(Console.ReadLine(), out int outputDeviceIndex))
        {

            PlayRecording(filePath, outputDeviceIndex);
        }
        else
        {
            Console.WriteLine("Invalid output device index.");
        }
    }
}