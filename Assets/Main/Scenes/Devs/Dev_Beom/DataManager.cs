using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using NoteEditor;
using UnityEngine.Networking;

public class DataManager : Singleton<DataManager>, IInitializable
{
    private List<TrackData> tracks = new List<TrackData>(); // 트랙 목록
    private string trackDataPath => Path.Combine(AudioPathProvider.TrackDataPath, "TrackData.json");

    public bool IsInitialized { get; private set; }

    private void Start()
    {
        Initialize();
    }

    public async void Initialize()
    {
        AudioPathProvider.EnsureDirectoriesExist();
        await LoadAllTracksAsync();
        IsInitialized = true;
        Debug.Log("[TrackDataManager] 초기화 완료");
    }

    // 모든 트랙 데이터를 JSON에서 불러옴
    public async Task LoadAllTracksAsync()
    {
        if (!File.Exists(trackDataPath))
        {
            Debug.LogWarning("[TrackDataManager] 트랙 데이터 파일이 없습니다.");
            return;
        }

        try
        {
            string json = await Task.Run(() => File.ReadAllText(trackDataPath));
            tracks = JsonConvert.DeserializeObject<List<TrackData>>(json) ?? new List<TrackData>();

            Debug.Log($"[TrackDataManager] {tracks.Count}개의 트랙을 불러왔습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TrackDataManager] 트랙 데이터 불러오기 실패: {ex.Message}");
        }
    }

    // 특정 트랙 데이터 가져오기
    public TrackData GetTrack(string trackName)
    {
        return tracks.FirstOrDefault(t => t.trackName == trackName);
    }

    // 현재 저장된 모든 트랙 반환
    public List<TrackData> GetAllTracks()
    {
        return new List<TrackData>(tracks);
    }
}

