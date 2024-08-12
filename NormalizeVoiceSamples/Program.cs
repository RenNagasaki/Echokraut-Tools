// See https://aka.ms/new-console-template for more information
using NAudio.Wave;
using System.IO;

Console.WriteLine("Please enter folder path of the voice samples:");
var path = Console.ReadLine();
var donePath = path + @"\Done\";
var files = Directory.GetFiles(path, "*.wav");

foreach (var file in files)
{
    float max = 0;

    using (var reader = new AudioFileReader(file))
    {
        // find the max peak
        float[] buffer = new float[reader.WaveFormat.SampleRate];
        int read;
        do
        {
            read = reader.Read(buffer, 0, buffer.Length);
            for (int n = 0; n < read; n++)
            {
                var abs = Math.Abs(buffer[n]);
                if (abs > max) max = abs;
            }
        } while (read > 0);

        // rewind and amplify
        reader.Position = 0;
        Console.WriteLine($"Old Volume: {reader.Volume} for file: {file}");
        reader.Volume *= .8f;
        Console.WriteLine($"New Volume: {reader.Volume} for file: {file}");

        // write out to a new WAV file
        if (!Directory.Exists(donePath))
            Directory.CreateDirectory(donePath);

        WaveFileWriter.CreateWaveFile16(donePath + Path.GetFileName(file), reader);
    }
}