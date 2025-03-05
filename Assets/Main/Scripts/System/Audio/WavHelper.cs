using System;
using System.IO;
using UnityEngine;

public static class WavHelper
{
    const int HEADER_SIZE = 44;

    public static bool Save(string filepath, AudioClip clip)
    {
        if (!filepath.ToLower().EndsWith(".wav"))
        {
            filepath = filepath + ".wav";
        }

        Debug.Log(filepath);

        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        FileStream fileStream = null;
        try
        {
            fileStream = CreateEmpty(filepath);

            WriteHeader(fileStream, clip);

            fileStream.Write(bytesData, 0, bytesData.Length);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"WAV 파일 저장 중 오류 발생: {e.Message}");
            return false;
        }
        finally
        {
            if (fileStream != null)
            {
                fileStream.Close();
            }
        }
    }

    private static FileStream CreateEmpty(string filepath)
    {
        string directoryPath = Path.GetDirectoryName(filepath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        FileStream fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var frequency = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = BitConverter.GetBytes(samples * channels * 2 + 36);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        ushort audioFormat = 1;
        byte[] audioFormatBytes = BitConverter.GetBytes(audioFormat);
        fileStream.Write(audioFormatBytes, 0, 2);

        ushort numChannels = (ushort)channels;
        byte[] numChannelsBytes = BitConverter.GetBytes(numChannels);
        fileStream.Write(numChannelsBytes, 0, 2);

        byte[] sampleRate = BitConverter.GetBytes(frequency);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = BitConverter.GetBytes(frequency * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        ushort blockAlign = (ushort)(channels * 2);
        byte[] blockAlignBytes = BitConverter.GetBytes(blockAlign);
        fileStream.Write(blockAlignBytes, 0, 2);

        ushort bitsPerSample = 16;
        byte[] bitsPerSampleBytes = BitConverter.GetBytes(bitsPerSample);
        fileStream.Write(bitsPerSampleBytes, 0, 2);

        byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}