// https://gist.github.com/R-WebsterNoble/70614880b0d3940d3b2b741fbbb311a2
//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

// 
// Fork by R-WebsterNoble
// Optimized! Now 20 times quicker
// Easy to get byte[] instead of saving file

// Note: GetWav() with trimming returns the full buffer with a load of zeros on the end. 
// Use the length out parameter to know where the data stops.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace SavWavWebsterNoble
{
public static class SavWav
{
    private const uint HeaderSize = 44;
    private const float RescaleFactor = 32767; //to convert float to Int16


    public struct AudioData
    {
        public int samples;
        public int frequency;
        public int channels;
        public float[] buffer;

        public AudioData(AudioClip clip)
        {
            samples = clip.samples;
            frequency = clip.frequency;
            channels = clip.channels;
            buffer = new float[clip.samples];
            if (clip.samples > 0)
            {
                clip.GetData(buffer, 0);
            }
            else
            {
                Debug.LogError("clip " + clip.name + " has no samples");
            }
        }
    }


    public static AudioData ConvertAudioClipToAudioData(AudioClip clip)
    {
        return new AudioData(clip);
    }

    public static byte[] ConvertAudioClipToByteData(AudioClip clip)
    {
        //AudioData audioData = ConvertAudioClipToAudioData(clip);
        //StringBuilder str = new StringBuilder();
        //str
        //byte[] bytes = new byte[str.Length * sizeof(char)];
        //System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        AudioData audioData = ConvertAudioClipToAudioData(clip);
        int size = Marshal.SizeOf(audioData);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(audioData, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    public static AudioData ConvertByteDataToAudioData(byte[] arr)
    {
        AudioData audioData = new AudioData();

        int size = Marshal.SizeOf(audioData);
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.Copy(arr, 0, ptr, size);

        audioData = (AudioData)Marshal.PtrToStructure(ptr, audioData.GetType());
        Marshal.FreeHGlobal(ptr);

        return audioData;
    }


    public static bool Save(string filename, AudioClip clip, bool trim = false)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return false;
        }
        if (clip == null)
        {
            return false;
        }
        filename = filename.Trim();
        if (string.IsNullOrEmpty(Path.GetExtension(filename)))
        {
            filename = string.Concat(filename, ".wav");
        }

        var filepath = Path.Combine(Application.persistentDataPath, filename);
        string directoryPath = Path.GetDirectoryName(filepath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(directoryPath);
        }

        using (var fileStream = new FileStream(filepath, FileMode.Create))
        {
            using (var writer = new BinaryWriter(fileStream))
            {
                var wav = GetWav(clip, out uint length, trim);
                writer.Write(wav, 0, (int)length);
            }
        }

        return true;
    }

    public static byte[] GetWav(AudioClip clip, out uint length, bool trim = false)
    {
        var data = ConvertAndWrite(clip, out length, out uint samples, trim);

        WriteHeader(data, clip, length, samples);

        return data;
    }

    private static byte[] ConvertAndWrite(AudioClip clip, out uint length, out uint samplesAfterTrimming, bool trim)
    {
        var samples = new float[clip.samples * clip.channels];

        clip.GetData(samples, 0);

        var sampleCount = samples.Length;

        var start = 0;
        var end = sampleCount - 1;

        if (trim)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                if ((short)(samples[i] * RescaleFactor) == 0)
                    continue;

                start = i;
                break;
            }

            for (var i = sampleCount -1; i >= 0; i--)
            {
                if ((short)(samples[i] * RescaleFactor) == 0)
                    continue;

                end = i;
                break;
            }
        }

        var buffer = new byte[sampleCount * 2 + HeaderSize];

        var p = HeaderSize;
        for (var i = start; i <= end; i++)
        {
            var value = (short) (samples[i] * RescaleFactor);
            buffer[p++] = (byte) (value >> 0);
            buffer[p++] = (byte) (value >> 8);
        }

        length = p;
        samplesAfterTrimming = (uint) (end - start + 1);
        return buffer;
    }

    private static void AddDataToBuffer(byte[] buffer, ref uint offset, byte[] addBytes)
    {
        foreach (var b in addBytes)
        {
            buffer[offset++] = b;
        }
    }

    private static void WriteHeader(byte[] stream, AudioClip clip, uint length, uint samples)
    {
        var hz = (uint)clip.frequency;
        var channels = (ushort)clip.channels;

        var offset = 0u;

        var riff = Encoding.UTF8.GetBytes("RIFF");
        AddDataToBuffer(stream, ref offset, riff);

        var chunkSize = BitConverter.GetBytes(length - 8);
        AddDataToBuffer(stream, ref offset, chunkSize);

        var wave = Encoding.UTF8.GetBytes("WAVE");
        AddDataToBuffer(stream, ref offset, wave);

        var fmt = Encoding.UTF8.GetBytes("fmt ");
        AddDataToBuffer(stream, ref offset, fmt);

        var subChunk1 = BitConverter.GetBytes(16u);
        AddDataToBuffer(stream, ref offset, subChunk1);

        //const ushort two = 2;
        const ushort one = 1;

        var audioFormat = BitConverter.GetBytes(one);
        AddDataToBuffer(stream, ref offset, audioFormat);

        var numChannels = BitConverter.GetBytes(channels);
        AddDataToBuffer(stream, ref offset, numChannels);

        var sampleRate = BitConverter.GetBytes(hz);
        AddDataToBuffer(stream, ref offset, sampleRate);

        var byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        AddDataToBuffer(stream, ref offset, byteRate);

        var blockAlign = (ushort)(channels * 2);
        AddDataToBuffer(stream, ref offset, BitConverter.GetBytes(blockAlign));

        ushort bps = 16;
        var bitsPerSample = BitConverter.GetBytes(bps);
        AddDataToBuffer(stream, ref offset, bitsPerSample);

        var dataString = Encoding.UTF8.GetBytes("data");
        AddDataToBuffer(stream, ref offset, dataString);

        var subChunk2 = BitConverter.GetBytes(samples * 2);
        AddDataToBuffer(stream, ref offset, subChunk2);
    }
}
}
