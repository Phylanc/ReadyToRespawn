using UnityEngine;

public class AudioBitcrusher : MonoBehaviour
{
    [Range(1, 16)] public int bitDepth = 4; // Имитация битности (например, 4-бит или 8-бит)
    [Range(1, 50)] public int downsample = 8; // Степень «пикселизации» частоты
    [Range(0f, 1f)] public float mix = 0.6f; // 0 = сухой звук, 1 = только эффект

    private int[] sampleCounters;
    private float[] lastSamples;
    private int cachedBitDepth;
    private float cachedMaxValue;

    private void OnValidate()
    {
        if (downsample < 1) downsample = 1;
        if (bitDepth < 1) bitDepth = 1;
        if (bitDepth > 16) bitDepth = 16;
    }

    private void EnsureState(int channels)
    {
        if (sampleCounters == null || sampleCounters.Length != channels)
        {
            sampleCounters = new int[channels];
            lastSamples = new float[channels];
        }

        if (cachedBitDepth != bitDepth)
        {
            cachedBitDepth = bitDepth;
            cachedMaxValue = Mathf.Pow(2f, cachedBitDepth - 1);
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        EnsureState(channels);

        for (int i = 0; i < data.Length; i++)
        {
            int ch = i % channels;
            float dry = data[i];

            // 1. Уменьшаем частоту дискретизации (Downsampling)
            if (sampleCounters[ch] >= downsample)
            {
                lastSamples[ch] = data[i];
                sampleCounters[ch] = 0;
            }
            else
            {
                sampleCounters[ch]++;
            }

            // 2. Уменьшаем битность звука (Quantization / Bitcrush)
            float crushed = Mathf.Round(lastSamples[ch] * cachedMaxValue) / cachedMaxValue;

            data[i] = Mathf.Lerp(dry, crushed, mix);
        }
    }
}
