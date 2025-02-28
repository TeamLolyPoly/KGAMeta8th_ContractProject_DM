using UnityEngine;

public static class WaveformDisplayExtensions
{
    /// <summary>
    /// 이중 색상 웨이브폼 텍스처를 생성합니다.
    /// 재생 전/후 색상이 다른 웨이브폼을 만들기 위해 사용됩니다.
    /// </summary>
    /// <param name="clip">오디오 클립</param>
    /// <param name="size">텍스처 크기</param>
    /// <param name="bgColor">배경 색상</param>
    /// <param name="unplayedColor">재생 전 웨이브폼 색상</param>
    /// <param name="playedColor">재생 후 웨이브폼 색상</param>
    /// <returns>생성된 웨이브폼 텍스처</returns>
    public static Texture2D CreateDualColorWaveformTexture(
        AudioClip clip,
        Vector2 size,
        Color bgColor,
        Color unplayedColor,
        Color playedColor
    )
    {
        if (clip == null)
            return null;

        float[] samples = new float[clip.samples * clip.channels];
        if (clip.GetData(samples, 0) == false)
            return null;

        int width = (int)size.x;
        int height = (int)size.y;
        Texture2D texture = new Texture2D(width, height);

        int resolution = clip.samples / width;

        Color[] unplayedColors = new Color[height];
        Color[] playedColors = new Color[height];
        float midHeight = height / 2f;
        float sampleComp = 0f;

        for (int i = 0; i < width; i++)
        {
            float sampleChunk = 0;
            for (int ii = 0; ii < resolution; ii++)
                sampleChunk += Mathf.Abs(samples[(i * resolution) + ii]);
            sampleChunk = sampleChunk / resolution * 1.5f;

            for (int h = 0; h < height; h++)
            {
                if (h < midHeight)
                    sampleComp = Mathf.InverseLerp(midHeight, 0, h);
                else
                    sampleComp = Mathf.InverseLerp(midHeight, height, h);

                if (sampleComp > sampleChunk)
                {
                    unplayedColors[h] = bgColor;
                    playedColors[h] = bgColor;
                }
                else
                {
                    unplayedColors[h] = unplayedColor;
                    playedColors[h] = playedColor;
                }
            }

            texture.SetPixels(i, 0, 1, height, unplayedColors);
        }

        texture.Apply();

        return texture;
    }

    /// <summary>
    /// 웨이브폼 텍스처의 일부를 재생 후 색상으로 업데이트합니다.
    /// </summary>
    /// <param name="texture">웨이브폼 텍스처</param>
    /// <param name="progress">진행률 (0-1)</param>
    /// <param name="size">텍스처 크기</param>
    /// <param name="bgColor">배경 색상</param>
    /// <param name="playedColor">재생 후 웨이브폼 색상</param>
    public static void UpdateWaveformProgress(
        Texture2D texture,
        float progress,
        Vector2 size,
        Color bgColor,
        Color playedColor
    )
    {
        if (texture == null)
            return;

        int width = (int)size.x;
        int height = (int)size.y;
        int progressWidth = Mathf.FloorToInt(width * progress);

        for (int i = 0; i < progressWidth; i++)
        {
            Color[] pixels = texture.GetPixels(i, 0, 1, height);

            for (int h = 0; h < height; h++)
            {
                if (pixels[h] != bgColor)
                {
                    pixels[h] = playedColor;
                }
            }

            texture.SetPixels(i, 0, 1, height, pixels);
        }

        texture.Apply();
    }
}
