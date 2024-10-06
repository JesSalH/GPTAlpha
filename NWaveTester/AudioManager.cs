using NAudio.Wave;
using System;


namespace NWaveTester;

public class AudioManager
{
    private WaveInEvent waveIn;
    private WaveFileWriter writer;
    private string outputFilePath;

    public void StartRecording(string filePath)
    {
        outputFilePath = filePath;
        waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(16000, 1);

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

    public void PlayRecording(string filePath)
    {
        using (var audioFile = new AudioFileReader(filePath))
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(audioFile);
            outputDevice.Play();
            Console.WriteLine("Playing... Press any key to stop.");
            Console.ReadKey();
            outputDevice.Stop();
        }
    }
}
