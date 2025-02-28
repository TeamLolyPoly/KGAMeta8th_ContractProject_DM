using UnityEngine;

public static class WaveformDisplayExtensions
{
    /// <summary>
    /// 웨이브폼 텍스처를 생성합니다.
    /// </summary>
    public static Texture2D CreateDualColorWaveformTexture(
        AudioClip clip,
        Vector2 size,
        Color waveformColor
    )
    {
        if (clip == null)
            return null;

        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
            return null;

        int width = (int)size.x;
        int height = (int)size.y;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.alphaIsTransparency = true;
        texture.filterMode = FilterMode.Point;

        float[] waveform = ProcessSamples(samples, width);

        Color32[] pixels = new Color32[width * height];
        DrawWaveformToArray(pixels, width, height, waveform, waveformColor);
        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    private static float[] ProcessSamples(float[] samples, int targetWidth)
    {
        float[] waveform = new float[targetWidth];
        int samplesPerPixel = samples.Length / targetWidth;

        if (samplesPerPixel == 0)
        {
            for (int i = 0; i < targetWidth; i++)
            {
                float position = (float)i * samples.Length / targetWidth;
                int index = Mathf.FloorToInt(position);
                waveform[i] = index < samples.Length ? Mathf.Abs(samples[index]) : 0f;
            }
        }
        else
        {
            for (int i = 0; i < targetWidth; i++)
            {
                int startSample = i * samplesPerPixel;
                int endSample = Mathf.Min(startSample + samplesPerPixel, samples.Length);
                float maxValue = 0f;

                for (int j = startSample; j < endSample; j++)
                {
                    maxValue = Mathf.Max(maxValue, Mathf.Abs(samples[j]));
                }

                waveform[i] = maxValue;
            }
        }

        float maxAmplitude = 0f;
        for (int i = 0; i < waveform.Length; i++)
        {
            maxAmplitude = Mathf.Max(maxAmplitude, waveform[i]);
        }

        if (maxAmplitude > 0f)
        {
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] /= maxAmplitude;
            }
        }

        return waveform;
    }

    private static void DrawWaveformToArray(Color32[] pixels, int width, int height, float[] waveform, Color color)
    {
        Color32 waveformColor = color;
        Color32 clearColor = new Color32(0, 0, 0, 0);

        for (int x = 0; x < width; x++)
        {
            int waveformHeight = Mathf.RoundToInt(waveform[x] * height);
            int startY = (height - waveformHeight) / 2;
            int endY = startY + waveformHeight;

            for (int y = 0; y < height; y++)
            {
                pixels[y * width + x] = (y >= startY && y < endY) ? waveformColor : clearColor;
            }
        }
    }
}
