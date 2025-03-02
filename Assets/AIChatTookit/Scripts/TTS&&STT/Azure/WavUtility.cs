using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System;

public static class WavUtility
{
    /// <summary>
    /// Converts an AudioClip to a byte array containing a WAV file.
    /// </summary>
    public static byte[] FromAudioClip(AudioClip clip)
    {
        // Create a new WAV file
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // Write the WAV header
        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + clip.samples * 2);
        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((ushort)(clip.channels * 2));
        writer.Write((ushort)16);
        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
        writer.Write(clip.samples * 2);

        // Write the audio data
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        int intMax = 32767; // max value for a 16-bit signed integer
        for (int i = 0; i < clip.samples; i++)
        {
            writer.Write((short)(samples[i] * intMax));
        }

        // Clean up
        writer.Close();
        byte[] wavBytes = stream.ToArray();
        stream.Close();
        return wavBytes;
    }

    /// <summary>
    /// byte[] 转换为audioClip
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="sampleRate"></param>
    /// <returns></returns>
    public static AudioClip ConvertBytesToAudioClip(byte[] bytes, int sampleRate)
    {
        // 将byte数组转换为float数组
        float[] floatArray = ConvertBytesToFloatArray(bytes);

        // 创建AudioClip
        AudioClip audioClip = AudioClip.Create("GeneratedAudioClip", floatArray.Length, 1, sampleRate, false);

        // 设置数据
        audioClip.SetData(floatArray, 0);

        return audioClip;
    }

    public static float[] ConvertBytesToFloatArray(byte[] bytes)
    {
        float[] floatArray = new float[bytes.Length / 2]; // Assumes 16-bit audio

        for (int i = 0; i < floatArray.Length; i++)
        {
            short value = BitConverter.ToInt16(bytes, i * 2);
            floatArray[i] = value / 32768.0f; // Convert to normalized float (-1.0 to 1.0)
        }

        return floatArray;
    }

    #region 保存音频文件
    public static void SaveAudioClip(AudioClip clip, string path, string name)
    {
        // 获取音频数据
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        // 将音频数据转换为字节数组
        byte[] byteArray = ConvertFloatArrayToByteArray(samples);

        // 创建保存路径
        string filePath = Path.Combine(path, name);

        // 创建文件流并写入文件
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // 写入WAV文件头
                WriteWavHeader(writer, clip);

                // 写入音频数据
                writer.Write(byteArray);
            }
        }

        Debug.Log("AudioClip saved at: " + filePath);
    }

    public static byte[] ConvertFloatArrayToByteArray(float[] floatArray)
    {
        byte[] byteArray = new byte[floatArray.Length * 2]; // 16-bit audio, 2 bytes per sample

        for (int i = 0; i < floatArray.Length; i++)
        {
            short value = (short)(floatArray[i] * 32767.0f); // Convert to 16-bit PCM
            BitConverter.GetBytes(value).CopyTo(byteArray, i * 2);
        }

        return byteArray;
    }

    public static void WriteWavHeader(BinaryWriter writer, AudioClip clip)
    {
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + clip.samples * 2);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1); // PCM format
        writer.Write((short)1); // Mono (change to 2 for stereo)
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * 2); // Byte rate
        writer.Write((short)2); // Block align
        writer.Write((short)16); // Bits per sample
        writer.Write("data".ToCharArray());
        writer.Write(clip.samples * 2);
    }

    #endregion
}
