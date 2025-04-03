using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NoteEditor;
using UnityEngine;

public class DataManager : Singleton<DataManager>, IInitializable
{
    private List<TrackData> metadata = new List<TrackData>();
    public bool IsInitialized { get; private set; }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => LogManager.Instance.IsInitialized);
        Initialize();
    }

    public void Initialize()
    {
        try
        {
            List<TrackData> tracks = LoadAllTrackDatas();
            if (tracks == null)
            {
                tracks = new List<TrackData>();
                Debug.LogWarning("트랙 데이터를 불러올 수 없어 새로운 리스트를 생성합니다.");
            }

            metadata.Clear();

            foreach (var trackMetadata in tracks)
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

                    track.noteMapData = LoadNoteMap(track);
                    track.TrackAudio = LoadTrackAudio(track);
                    track.AlbumArt = LoadAlbumArt(track);

                    metadata.Add(track);
                    Debug.Log($"트랙 로드 성공: {track.trackName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"트랙 데이터 처리 중 오류 발생: {ex.Message}");
                    continue;
                }
            }

            foreach (var track in metadata)
            {
                Debug.Log(
                    $"========= {track.trackName} 로드 완료 =========\n"
                        + $"GUID : {track.id}\n"
                        + $"트랙 오디오 : {(track.TrackAudio != null ? track.TrackAudio.name : "없음")}\n"
                        + $"트랙 길이 : {(track.TrackAudio != null ? track.TrackAudio.length.ToString() : "0.0")}\n"
                        + $"트랙 BPM : {track.bpm}\n"
                        + $"트랙 아티스트 : {track.artistName}\n"
                        + $"트랙 앨범 : {track.albumName}\n"
                        + $"트랙 연도 : {track.year}\n"
                        + $"트랙 장르 : {track.genre}\n"
                        + $"트랙 길이 : {track.duration}\n"
                );
            }
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Initialize 중 치명적 오류 발생: {ex.Message}\n{ex.StackTrace}");
            metadata = new List<TrackData>();
            IsInitialized = true;
        }
    }

    public List<NoteMapData> LoadNoteMap(TrackData track)
    {
        string noteMapPath = ResourcePath.GetNoteMapPath(track.id);
        if (!File.Exists(noteMapPath))
        {
            Debug.LogWarning($"[TrackDataManager] 노트맵 파일이 없습니다: {noteMapPath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(noteMapPath);
            json = "{\"noteMaps\":" + json + "}";
            var wrapper = JsonUtility.FromJson<NoteMapDataList>(json);
            return wrapper?.noteMaps;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 노트맵 파일 불러오기 실패: {ex.Message}");
            return null;
        }
    }

    public Sprite LoadAlbumArt(TrackData track)
    {
        string albumArtPath = ResourcePath.GetAlbumArtPath(track.id);
        if (!File.Exists(albumArtPath))
        {
            Debug.LogWarning($"[TrackDataManager] 앨범 아트 파일이 없습니다: {albumArtPath}");
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(albumArtPath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                Vector2.zero
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 앨범 아트 파일 불러오기 실패: {ex.Message}");
            return null;
        }
    }

    public AudioClip LoadTrackAudio(TrackData track)
    {
        string audioPath = ResourcePath.GetAudioFilePath(track.id);
        if (!File.Exists(audioPath))
        {
            Debug.LogWarning($"[TrackDataManager] 오디오 파일이 없습니다: {audioPath}");
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(audioPath);
            AudioClip audioClip = WavUtility.ToAudioClip(bytes, track.trackName);

            return audioClip;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 오디오 파일 불러오기 실패: {ex.Message}");
            return null;
        }
    }

    public List<TrackData> LoadAllTrackDatas()
    {
        if (!File.Exists(ResourcePath.TRACK_METADATA_PATH))
        {
            Debug.LogWarning("[TrackDataManager] 트랙 데이터 파일이 없습니다.");
            return new List<TrackData>();
        }

        try
        {
            string json = File.ReadAllText(ResourcePath.TRACK_METADATA_PATH);

            var guidMatches = Regex.Matches(
                json,
                "\"id\"\\s*:\\s*\"([0-9a-fA-F-]{8}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{4}-[0-9a-fA-F-]{12})\""
            );

            var guidStrings = new List<string>();
            foreach (Match match in guidMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string guid = match.Groups[1].Value;
                    guidStrings.Add(guid);
                    Debug.Log($"[TrackDataManager] 발견된 GUID: {guid}\n");
                }
            }

            json = "{\"tracks\":" + json + "}";

            var wrapper = JsonUtility.FromJson<TrackDataList>(json);
            if (wrapper?.tracks == null)
                return new List<TrackData>();

            for (int i = 0; i < wrapper.tracks.Count && i < guidStrings.Count; i++)
            {
                if (Guid.TryParse(guidStrings[i], out Guid parsedGuid))
                {
                    wrapper.tracks[i].id = parsedGuid;
                    Debug.Log(
                        $"[TrackDataManager] 트랙 '{wrapper.tracks[i].trackName}'의 GUID 설정: {parsedGuid}"
                    );
                }
            }

            return wrapper.tracks;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[TrackDataManager] 트랙 데이터 불러오기 실패: {ex.Message}\n{ex.StackTrace}"
            );
            return new List<TrackData>();
        }
    }

    public TrackData GetTrack(string trackName)
    {
        return metadata.FirstOrDefault(t => t.trackName == trackName);
    }

    public List<TrackData> GetAllTracks()
    {
        return new List<TrackData>(metadata);
    }
}
