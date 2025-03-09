using UnityEngine;

namespace NoteEditor
{
    public static class WaveformDisplayExtensions
    {
        private const int CHUNK_SIZE = 1024;
        private const int MAX_TEXTURE_SIZE = 4096;

        public static Texture2D CreateDualColorWaveformTexture(
            AudioClip clip,
            Vector2 size,
            Color waveformColor,
            float pixelsPerUnit
        )
        {
            if (clip == null)
                return null;

            float[] samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
                return null;

            int textureWidth = Mathf.Min((int)(size.x * pixelsPerUnit), MAX_TEXTURE_SIZE);
            int textureHeight = Mathf.Min((int)(size.y * pixelsPerUnit), MAX_TEXTURE_SIZE);

            textureWidth = Mathf.Max(textureWidth, 1);
            textureHeight = Mathf.Max(textureHeight, 1);

            try
            {
                Texture2D texture = new Texture2D(
                    textureWidth,
                    textureHeight,
                    TextureFormat.RGBA32,
                    false
                );
#if UNITY_EDITOR
                texture.alphaIsTransparency = true;
#endif
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.anisoLevel = 16;

                float[] waveform = ProcessSamplesInChunks(samples, textureWidth);

                for (int x = 0; x < textureWidth; x += CHUNK_SIZE)
                {
                    int chunkWidth = Mathf.Min(CHUNK_SIZE, textureWidth - x);
                    Color32[] pixels = new Color32[chunkWidth * textureHeight];
                    DrawWaveformChunk(
                        pixels,
                        chunkWidth,
                        textureHeight,
                        waveform,
                        x,
                        waveformColor
                    );
                    texture.SetPixels32(x, 0, chunkWidth, textureHeight, pixels);
                }

                texture.Apply();
                return texture;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"웨이브폼 텍스처 생성 중 오류 발생: {e.Message}");
                return null;
            }
        }

        private static float[] ProcessSamplesInChunks(float[] samples, int targetWidth)
        {
            if (samples == null || samples.Length == 0 || targetWidth <= 0)
                return new float[targetWidth];

            float[] waveform = new float[targetWidth];
            float samplesPerPixel = (float)samples.Length / targetWidth;

            for (int i = 0; i < targetWidth; i++)
            {
                float startPosition = i * samplesPerPixel;
                float endPosition = (i + 1) * samplesPerPixel;

                int startSample = Mathf.FloorToInt(startPosition);
                int endSample = Mathf.CeilToInt(endPosition);

                float startWeight = 1.0f - (startPosition - startSample);
                float endWeight = endPosition - endSample + 1.0f;

                float sumSquares = 0f;
                float totalWeight = 0f;

                if (startSample >= 0 && startSample < samples.Length)
                {
                    sumSquares += samples[startSample] * samples[startSample] * startWeight;
                    totalWeight += startWeight;
                }

                for (int j = startSample + 1; j < endSample; j++)
                {
                    if (j >= 0 && j < samples.Length)
                    {
                        sumSquares += samples[j] * samples[j];
                        totalWeight += 1.0f;
                    }
                }

                if (endSample >= 0 && endSample < samples.Length)
                {
                    sumSquares += samples[endSample] * samples[endSample] * endWeight;
                    totalWeight += endWeight;
                }

                if (totalWeight > 0)
                {
                    waveform[i] = Mathf.Sqrt(sumSquares / totalWeight);
                }
                else
                {
                    waveform[i] = 0f;
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

        private static void DrawWaveformChunk(
            Color32[] pixels,
            int chunkWidth,
            int height,
            float[] waveform,
            int startX,
            Color color
        )
        {
            if (pixels == null || waveform == null || chunkWidth <= 0 || height <= 0)
                return;

            Color32 waveformColor = color;
            Color32 clearColor = new Color32(0, 0, 0, 0);

            for (int x = 0; x < chunkWidth; x++)
            {
                int waveformHeight = Mathf.RoundToInt(waveform[startX + x] * height * 0.8f);
                int startY = (height - waveformHeight) / 2;
                int endY = startY + waveformHeight;

                for (int y = 0; y < height; y++)
                {
                    pixels[y * chunkWidth + x] =
                        (y >= startY && y < endY) ? waveformColor : clearColor;
                }
            }
        }
    }
}
