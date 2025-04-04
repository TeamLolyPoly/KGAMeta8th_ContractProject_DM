using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NoteEditor;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    private List<TrackData> trackDataList = new List<TrackData>();
    public List<TrackData> TrackDataList => trackDataList;

    public bool IsInitialized { get; private set; }

    public IEnumerator LoadTrackDataList()
    {
        List<TrackData> loadedTracks = new List<TrackData>();
        float progress = 0f;
        float progressStep = 0.1f;

        StageLoadingManager.Instance.SetLoadingText("트랙 데이터 로드 중...");

        yield return progress;

        if (!File.Exists(ResourcePath.TRACK_METADATA_PATH))
        {
            Debug.LogWarning("[TrackDataManager] 트랙 데이터 파일이 없습니다.");
            trackDataList = new List<TrackData>();
            yield break;
        }

        string json = "";
        try
        {
            json = File.ReadAllText(ResourcePath.TRACK_METADATA_PATH);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 트랙 데이터 파일 불러오기 실패: {ex.Message}");
            trackDataList = new List<TrackData>();
            yield break;
        }

        progress += progressStep;
        yield return progress;

        List<string> guidStrings = new List<string>();
        try
        {
            var guidMatches = Regex.Matches(
                json,
                "\"id\"\\s*:\\s*\"([0-9a-fA-F-]{8}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{12})\""
            );

            foreach (Match match in guidMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string guid = match.Groups[1].Value;
                    guidStrings.Add(guid);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] GUID 파싱 실패: {ex.Message}");
            trackDataList = new List<TrackData>();
            yield break;
        }

        progress += progressStep;
        yield return progress;

        TrackDataList wrapper = null;
        try
        {
            json = "{\"tracks\":" + json + "}";
            wrapper = JsonUtility.FromJson<TrackDataList>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] JSON 파싱 실패: {ex.Message}");
            trackDataList = new List<TrackData>();
            yield break;
        }

        progress += progressStep;
        yield return progress;

        if (wrapper?.tracks == null)
        {
            trackDataList = new List<TrackData>();
            yield break;
        }

        for (int i = 0; i < wrapper.tracks.Count && i < guidStrings.Count; i++)
        {
            if (Guid.TryParse(guidStrings[i], out Guid parsedGuid))
            {
                wrapper.tracks[i].id = parsedGuid;
            }
        }

        progress += progressStep;
        yield return progress;

        trackDataList.Clear();

        float trackProgressStep =
            (1.0f - progress) / (wrapper.tracks.Count > 0 ? wrapper.tracks.Count : 1);

        foreach (var trackMetadata in wrapper.tracks)
        {
            if (trackMetadata == null)
                continue;

            try
            {
                TrackData track = new TrackData
                {
                    id = trackMetadata.id,
                    trackName = trackMetadata.trackName ?? "Unknown Track",
                    bpm = trackMetadata.bpm,
                    artistName = trackMetadata.artistName ?? "Unknown Artist",
                    albumName = trackMetadata.albumName ?? "Unknown Album",
                    year = trackMetadata.year,
                    genre = trackMetadata.genre ?? "Unknown Genre",
                    duration = trackMetadata.duration,
                };

                trackDataList.Add(track);
            }
            catch (Exception ex)
            {
                Debug.LogError($"트랙 데이터 처리 중 오류 발생: {ex.Message}");
                continue;
            }

            progress += trackProgressStep;
            yield return progress;
        }

        yield return 1.0f;
    }

    public IEnumerator LoadNoteMapCoroutine()
    {
        float progress = 0f;
        int totalTracks = trackDataList.Count;
        StageLoadingManager.Instance.SetLoadingText($"노트맵 로드 중... ");

        if (totalTracks <= 0)
        {
            yield return 1.0f;
            yield break;
        }

        float progressPerTrack = 1.0f / totalTracks;

        for (int i = 0; i < totalTracks; i++)
        {
            var track = trackDataList[i];
            string noteMapPath = ResourcePath.GetNoteMapPath(track.id);

            if (!File.Exists(noteMapPath))
            {
                Debug.LogWarning($"[TrackDataManager] 노트맵 파일이 없습니다: {noteMapPath}");
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            string json = "";
            bool skipTrack = false;
            try
            {
                json = File.ReadAllText(noteMapPath);
                json = "{\"noteMaps\":" + json + "}";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrackDataManager] 노트맵 파일 불러오기 실패: {ex.Message}");
                skipTrack = true;
            }

            if (skipTrack)
            {
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            progress = (i + 0.5f) * progressPerTrack;
            yield return progress;

            NoteMapDataList wrapper = null;
            skipTrack = false;
            try
            {
                wrapper = JsonUtility.FromJson<NoteMapDataList>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrackDataManager] 노트맵 파싱 실패: {ex.Message}");
                skipTrack = true;
            }

            if (skipTrack)
            {
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            track.noteMapData = wrapper?.noteMaps;

            progress = (i + 1) * progressPerTrack;
            yield return progress;
        }

        yield return 1.0f;
    }

    public IEnumerator LoadAlbumArtCoroutine()
    {
        float progress = 0f;
        int totalTracks = trackDataList.Count;
        StageLoadingManager.Instance.SetLoadingText($"앨범 아트 로드 중... ");

        if (totalTracks <= 0)
        {
            yield return 1.0f;
            yield break;
        }

        float progressPerTrack = 1.0f / totalTracks;

        for (int i = 0; i < totalTracks; i++)
        {
            var track = trackDataList[i];
            string albumArtPath = ResourcePath.GetAlbumArtPath(track.id);

            if (!File.Exists(albumArtPath))
            {
                Debug.LogWarning($"[TrackDataManager] 앨범 아트 파일이 없습니다: {albumArtPath}");
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            byte[] bytes = null;
            bool skipTrack = false;
            try
            {
                bytes = File.ReadAllBytes(albumArtPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrackDataManager] 앨범 아트 파일 불러오기 실패: {ex.Message}");
                skipTrack = true;
            }

            if (skipTrack)
            {
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            progress = (i + 0.5f) * progressPerTrack;
            yield return progress;

            Sprite sprite = null;
            skipTrack = false;
            try
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);

                sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    Vector2.zero
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrackDataManager] 앨범 아트 생성 실패: {ex.Message}");
                skipTrack = true;
            }

            if (skipTrack)
            {
                progress = (i + 1) * progressPerTrack;
                yield return progress;
                continue;
            }

            track.AlbumArt = sprite;

            progress = (i + 1) * progressPerTrack;
            yield return progress;
        }

        yield return 1.0f;
    }

    public IEnumerator LoadTrackAudioCoroutine()
    {
        float progress = 0f;
        int totalTracks = trackDataList.Count;
        StageLoadingManager.Instance.SetLoadingText($"오디오 로드 중... ");

        if (totalTracks <= 0)
        {
            yield return 1.0f;
            yield break;
        }

        float progressPerTrack = 1.0f / totalTracks;

        for (int i = 0; i < totalTracks; i++)
        {
            var track = trackDataList[i];
            string audioPath = ResourcePath.GetAudioFilePath(track.id);

            if (!File.Exists(audioPath))
            {
                Debug.LogWarning($"[TrackDataManager] 오디오 파일이 없습니다: {audioPath}");
            }

            progress = (i + 1) * progressPerTrack;
            yield return progress;
        }

        yield return 1.0f;
    }

    public void LoadTrackAudio(Guid trackId, Action<AudioClip> onComplete = null)
    {
        TrackData track = trackDataList.Find(t => t.id == trackId);
        if (track == null)
        {
            Debug.LogError($"[TrackDataManager] 트랙을 찾을 수 없습니다: {trackId}");
            onComplete?.Invoke(null);
            return;
        }

        if (track.TrackAudio != null)
        {
            onComplete?.Invoke(track.TrackAudio);
            return;
        }

        StartCoroutine(LoadSingleTrackAudio(track, onComplete));
    }

    private IEnumerator LoadSingleTrackAudio(TrackData track, Action<AudioClip> onComplete)
    {
        string audioPath = ResourcePath.GetAudioFilePath(track.id);

        if (!File.Exists(audioPath))
        {
            Debug.LogWarning($"[TrackDataManager] 오디오 파일이 없습니다: {audioPath}");
            onComplete?.Invoke(null);
            yield break;
        }

        Debug.Log($"[TrackDataManager] 오디오 파일 로드 시작: {track.trackName}");

        yield return null;

        byte[] bytes = null;
        try
        {
            bytes = File.ReadAllBytes(audioPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 오디오 파일 불러오기 실패: {ex.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        yield return null;

        AudioClip audioClip = null;
        try
        {
            audioClip = WavUtility.ToAudioClip(bytes, track.trackName);
            track.TrackAudio = audioClip;
            Debug.Log($"[TrackDataManager] 오디오 파일 생성 완료: {audioClip.name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 오디오 클립 생성 실패: {ex.Message}");
            onComplete?.Invoke(null);
            yield break;
        }

        onComplete?.Invoke(audioClip);
    }

    public void UnloadTrackAudio(Guid trackId)
    {
        TrackData track = trackDataList.Find(t => t.id == trackId);
        if (track != null && track.TrackAudio != null)
        {
            Debug.Log($"[TrackDataManager] 오디오 언로드: {track.trackName}");
            Destroy(track.TrackAudio);
            track.TrackAudio = null;
        }
    }
}
