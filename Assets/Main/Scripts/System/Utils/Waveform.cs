using UnityEngine;

public static class Waveform
{
    private static Color defaultWaveformBG = new Color(0.25f, 0.25f, 0.25f);
    private static Color defaultWaveformColor = new Color(1f, 0.6f, 0.2f);
    private static Color defaultPlayMarkerColor = new Color(1f, 0f, 0f, 0.8f);
    private static Color defaultBeatMarkerColor = new Color(1f, 1f, 1f, 0.5f);
    private static Color defaultDownBeatMarkerColor = new Color(1f, 1f, 0f, 0.5f);

    public static void DrawWaveform(
        Rect waveformRect,
        AudioClip clip,
        Vector2 size,
        float playMarker = -1f,
        float[] beatMarkers = null,
        int downBeat = 0
    )
    {
        DrawWaveform(
            waveformRect,
            clip,
            size,
            defaultWaveformBG,
            defaultWaveformColor,
            playMarker,
            defaultPlayMarkerColor,
            beatMarkers,
            defaultBeatMarkerColor,
            defaultDownBeatMarkerColor,
            downBeat
        );
    }

    public static void DrawWaveform(
        Rect waveformRect,
        AudioClip clip,
        Vector2 size,
        Color backgroundColor,
        Color waveformColor,
        float playMarker = -1f,
        Color playMarkerColor = default,
        float[] beatMarkers = null,
        Color beatMarkerColor = default,
        Color downBeatMarkerColor = default,
        int downBeat = 0
    )
    {
        if (playMarkerColor == default)
            playMarkerColor = defaultPlayMarkerColor;
        if (beatMarkerColor == default)
            beatMarkerColor = defaultBeatMarkerColor;
        if (downBeatMarkerColor == default)
            downBeatMarkerColor = defaultDownBeatMarkerColor;

        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();
        if (Event.current.type != EventType.Repaint)
            return;
        Texture2D waveformTexture = GetWaveformTexture(clip, size, backgroundColor, waveformColor);
        if (waveformTexture == null)
            return;
        GUIStyle tempStyle = new GUIStyle();
        tempStyle.normal.background = waveformTexture;
        tempStyle.Draw(waveformRect, GUIContent.none, false, false, false, false);
        if (playMarker >= 0f && playMarker <= 1f)
        {
            DrawPlayMarker(waveformRect, playMarker, playMarkerColor);
        }

        if (beatMarkers != null && beatMarkers.Length > 0)
        {
            DrawBeatMarkers(
                waveformRect,
                beatMarkers,
                downBeat,
                beatMarkerColor,
                downBeatMarkerColor
            );
        }
    }

    public static Texture2D GetWaveformTexture(
        AudioClip clip,
        Vector2 size,
        Color bgColor,
        Color waveColor
    )
    {
        float[] samples = new float[clip.samples * clip.channels];
        if (clip.GetData(samples, 0) == false)
            return null;

        int width = (int)size.x;
        int height = (int)size.y;
        Texture2D texture = new Texture2D(width, height);
        int resolution = clip.samples / width;
        Color[] colors = new Color[height];
        float midHeight = height / 2f;

        for (int i = 0; i < width; i++)
        {
            float sampleChunk = 0;
            for (int ii = 0; ii < resolution; ii++)
                sampleChunk += Mathf.Abs(samples[(i * resolution) + ii]);
            sampleChunk = sampleChunk / resolution * 1.5f;
            for (int h = 0; h < height; h++)
            {
                float sampleComp;
                if (h < midHeight)
                    sampleComp = Mathf.InverseLerp(midHeight, 0, h);
                else
                    sampleComp = Mathf.InverseLerp(midHeight, height, h);
                if (sampleComp > sampleChunk)
                    colors[h] = bgColor;
                else
                    colors[h] = waveColor;
            }
            texture.SetPixels(i, 0, 1, height, colors);
        }
        texture.Apply();
        return texture;
    }

    private static void DrawPlayMarker(
        Rect waveformRect,
        float normalizedPosition,
        Color markerColor
    )
    {
        float xPos = waveformRect.x + waveformRect.width * normalizedPosition;

        Texture2D markerTexture = new Texture2D(1, 1);
        markerTexture.SetPixel(0, 0, markerColor);
        markerTexture.Apply();

        GUI.DrawTexture(new Rect(xPos - 1, waveformRect.y, 2, waveformRect.height), markerTexture);

        Object.Destroy(markerTexture);
    }

    private static void DrawBeatMarkers(
        Rect waveformRect,
        float[] beatMarkers,
        int downBeat,
        Color normalBeatColor,
        Color downBeatColor
    )
    {
        Texture2D normalBeatTexture = new Texture2D(1, 1);
        normalBeatTexture.SetPixel(0, 0, normalBeatColor);
        normalBeatTexture.Apply();

        Texture2D downBeatTexture = new Texture2D(1, 1);
        downBeatTexture.SetPixel(0, 0, downBeatColor);
        downBeatTexture.Apply();

        for (int i = 0; i < beatMarkers.Length; i++)
        {
            float xPos = waveformRect.x + waveformRect.width * beatMarkers[i];

            bool isDownBeat = (downBeat > 0) && (i % downBeat == 0);
            Texture2D markerTexture = isDownBeat ? downBeatTexture : normalBeatTexture;

            GUI.DrawTexture(
                new Rect(xPos - 0.5f, waveformRect.y, 1, waveformRect.height),
                markerTexture
            );
        }

        Object.Destroy(normalBeatTexture);
        Object.Destroy(downBeatTexture);
    }
}
