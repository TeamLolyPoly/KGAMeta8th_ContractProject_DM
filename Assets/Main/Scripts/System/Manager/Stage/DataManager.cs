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
    private List<TrackData> trackDataList = new List<TrackData>();
    public List<TrackData> TrackDataList => trackDataList;

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = false;
        StartCoroutine(InitializeCoroutine());
    }

    public IEnumerator InitializeCoroutine()
    {
        yield return LoadAllTrackDatasCoroutine(
            (tracks) =>
            {
                if (tracks == null)
                {
                    tracks = new List<TrackData>();
                    Debug.LogWarning("트랙 데이터를 불러올 수 없어 새로운 리스트를 생성합니다.");
                }

                trackDataList.Clear();

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

                        List<IEnumerator> loadingOperations = new List<IEnumerator>
                        {
                            LoadNoteMapCoroutine(
                                track,
                                (noteMap) =>
                                {
                                    track.noteMapData = noteMap;
                                }
                            ),
                            LoadAlbumArtCoroutine(
                                track,
                                (albumArt) =>
                                {
                                    track.AlbumArt = albumArt;
                                }
                            ),
                            LoadTrackAudioCoroutine(
                                track,
                                (audio) =>
                                {
                                    track.TrackAudio = audio;
                                }
                            ),
                        };

                        foreach (var operation in loadingOperations)
                        {
                            StartCoroutine(operation);
                        }

                        trackDataList.Add(track);
                        Debug.Log($"트랙 로드 시작: {track.trackName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"트랙 데이터 처리 중 오류 발생: {ex.Message}");
                        continue;
                    }
                }
            }
        );

        yield return new WaitForSeconds(0.5f);

        foreach (var track in trackDataList)
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

    public IEnumerator LoadNoteMapCoroutine(TrackData track, Action<List<NoteMapData>> callback)
    {
        string noteMapPath = ResourcePath.GetNoteMapPath(track.id);
        if (!File.Exists(noteMapPath))
        {
            Debug.LogWarning($"[TrackDataManager] 노트맵 파일이 없습니다: {noteMapPath}");
            callback(null);
            yield break;
        }

        yield return null;

        string json = "";
        try
        {
            json = File.ReadAllText(noteMapPath);
            json = "{\"noteMaps\":" + json + "}";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 노트맵 파일 불러오기 실패: {ex.Message}");
            callback(null);
            yield break;
        }

        yield return null;

        NoteMapDataList wrapper = null;
        try
        {
            wrapper = JsonUtility.FromJson<NoteMapDataList>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 노트맵 파싱 실패: {ex.Message}");
            callback(null);
            yield break;
        }

        callback(wrapper?.noteMaps);
        yield return 0.1f;
    }

    public IEnumerator LoadAlbumArtCoroutine(TrackData track, Action<Sprite> callback)
    {
        string albumArtPath = ResourcePath.GetAlbumArtPath(track.id);
        if (!File.Exists(albumArtPath))
        {
            Debug.LogWarning($"[TrackDataManager] 앨범 아트 파일이 없습니다: {albumArtPath}");
            callback(null);
            yield break;
        }

        yield return null;

        byte[] bytes = null;
        try
        {
            bytes = File.ReadAllBytes(albumArtPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 앨범 아트 파일 불러오기 실패: {ex.Message}");
            callback(null);
            yield break;
        }

        yield return null;

        Sprite sprite = null;
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
            callback(null);
            yield break;
        }

        callback(sprite);
        yield return 0.1f;
    }

    public IEnumerator LoadTrackAudioCoroutine(TrackData track, Action<AudioClip> callback)
    {
        string audioPath = ResourcePath.GetAudioFilePath(track.id);
        if (!File.Exists(audioPath))
        {
            Debug.LogWarning($"[TrackDataManager] 오디오 파일이 없습니다: {audioPath}");
            callback(null);
            yield break;
        }

        yield return null;

        byte[] bytes = null;
        try
        {
            bytes = File.ReadAllBytes(audioPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 오디오 파일 불러오기 실패: {ex.Message}");
            callback(null);
            yield break;
        }

        yield return null;

        AudioClip audioClip = null;
        try
        {
            audioClip = WavUtility.ToAudioClip(bytes, track.trackName);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 오디오 클립 생성 실패: {ex.Message}");
            callback(null);
            yield break;
        }

        callback(audioClip);
        yield return 0.1f;
    }

    public IEnumerator LoadAllTrackDatasCoroutine(Action<List<TrackData>> callback)
    {
        if (!File.Exists(ResourcePath.TRACK_METADATA_PATH))
        {
            Debug.LogWarning("[TrackDataManager] 트랙 데이터 파일이 없습니다.");
            callback(new List<TrackData>());
            yield break;
        }

        yield return null;

        string json = "";
        try
        {
            json = File.ReadAllText(ResourcePath.TRACK_METADATA_PATH);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 트랙 데이터 파일 불러오기 실패: {ex.Message}");
            callback(new List<TrackData>());
            yield break;
        }

        yield return null;

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
                    Debug.Log($"[TrackDataManager] 발견된 GUID: {guid}\n");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] GUID 파싱 실패: {ex.Message}");
            callback(new List<TrackData>());
            yield break;
        }

        yield return null;

        TrackDataList wrapper = null;
        try
        {
            json = "{\"tracks\":" + json + "}";
            wrapper = JsonUtility.FromJson<TrackDataList>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] JSON 파싱 실패: {ex.Message}");
            callback(new List<TrackData>());
            yield break;
        }

        if (wrapper?.tracks == null)
        {
            callback(new List<TrackData>());
            yield break;
        }

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

        callback(wrapper.tracks);
        yield return 0.1f;
    }

    public List<Func<IEnumerator>> GetInitializationOperations()
    {
        List<Func<IEnumerator>> operations = new List<Func<IEnumerator>>();

        operations.Add(
            () =>
                LoadAllTrackDatasCoroutine(tracks =>
                {
                    trackDataList.Clear();

                    foreach (var trackMetadata in tracks)
                    {
                        if (trackMetadata == null)
                            continue;

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
                })
        );

        operations.Add(() => LoadTrackResourcesCoroutine());

        return operations;
    }

    private IEnumerator LoadTrackResourcesCoroutine()
    {
        int totalTracks = trackDataList.Count;
        int completedTracks = 0;

        foreach (var track in trackDataList)
        {
            yield return LoadNoteMapCoroutine(track, noteMap => track.noteMapData = noteMap);
            yield return LoadAlbumArtCoroutine(track, albumArt => track.AlbumArt = albumArt);
            yield return LoadTrackAudioCoroutine(track, audio => track.TrackAudio = audio);

            completedTracks++;
            yield return (float)completedTracks / totalTracks;
        }
    }
}
