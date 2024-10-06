using NAudio.Dmo;
using NAudio.Wave;

namespace NWaveTest;

public class AudioManager
{
    private WaveInEvent waveIn;
    private WaveFileWriter writer;
    private string outputFilePath;

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
        outputFilePath = filePath;
        waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex, // Set the device index
            WaveFormat = new WaveFormat(16000, 1)
        };

        waveIn.DataAvailable += (sender, e) =>
        {
            if (writer == null)
            {
                writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);
            }
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        };

        waveIn.RecordingStopped += (sender, e) =>
        {
            writer?.Dispose();
            writer = null;
            waveIn.Dispose();
        };

        waveIn.StartRecording();
        Console.WriteLine("Recording... Press any key to stop.");
        Console.ReadKey();
        waveIn.StopRecording();
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
}