using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoteEditor
{
    /// <summary
    /// 트랙 데이터를 관리하는 매니저 클래스
    /// </summary>
    public class EditorDataManager : Singleton<EditorDataManager>, IInitializable
    {
        private AudioFileService fileService;
        private List<TrackData> tracks = new List<TrackData>();
        private string pendingAudioFilePath;
        private string pendingAlbumArtFilePath;
        private int selectedTrackIndex = -1;
        private string currentSceneName;

        private readonly string[] audioLoadingTips =
        {
            "오디오 파일을 분석하는 중입니다...",
            "웨이브폼을 생성하는 중입니다...",
            "트랙 정보를 처리하는 중입니다...",
            "대용량 오디오 파일은 처리 시간이 더 오래 걸릴 수 있습니다.",
            "고품질 오디오 파일을 사용하면 더 정확한 웨이브폼을 볼 수 있습니다.",
        };

        private readonly string[] albumArtLoadingTips =
        {
            "이미지 파일을 처리하는 중입니다...",
            "앨범 아트를 최적화하는 중입니다...",
            "트랙 정보에 앨범 아트를 연결하는 중입니다...",
            "앨범 아트는 트랙 정보와 함께 저장됩니다.",
        };

        public event Action<TrackData> OnTrackAdded;
        public event Action<TrackData> OnTrackRemoved;
        public event Action<TrackData> OnTrackUpdated;
        public event Action<TrackData> OnTrackLoaded;
        public event Action<string, Sprite> OnAlbumArtLoaded;

        public bool IsInitialized { get; private set; }

        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            fileService = new AudioFileService();
            Initialize();
        }

        public void Initialize()
        {
            AudioPathProvider.EnsureDirectoriesExist();
            LoadAllTracksAsync()
                .ContinueWith(_ =>
                {
                    IsInitialized = true;
                    Debug.Log("AudioDataManager 초기화 완료");
                });
        }

        public async Task LoadAllTracksAsync()
        {
            var metadata = await fileService.LoadMetadataAsync();
            Debug.Log($"메타데이터에서 {metadata.Count}개의 트랙 정보를 로드했습니다.");

            tracks.Clear();

            foreach (var trackMetadata in metadata)
            {
                TrackData track = new TrackData
                {
                    trackName = trackMetadata.trackName,
                    bpm = trackMetadata.bpm,
                    artistName = trackMetadata.artistName,
                    albumName = trackMetadata.albumName,
                    year = trackMetadata.year,
                    genre = trackMetadata.genre,
                    duration = trackMetadata.duration,
                };

                tracks.Add(track);
            }

            Debug.Log($"{tracks.Count}개의 트랙이 로드되었습니다.");
        }

        /// <summary>
        /// 외부 오디오 파일을 트랙으로 추가합니다.
        /// </summary>
        /// <param name="filePath">외부 오디오 파일 경로</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>추가된 트랙 데이터</returns>
        public async Task<TrackData> AddTrackAsync(
            string filePath,
            IProgress<float> progress = null
        )
        {
            var result = await fileService.ImportAudioFileAsync(filePath, progress);

            if (result.clip == null)
            {
                Debug.LogError($"트랙 추가 실패: {filePath}");
                return null;
            }

            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == result.trackName);

            if (existingTrack != null)
            {
                existingTrack.TrackAudio = result.clip;

                await UpdateTrackMetadataAsync();

                OnTrackUpdated?.Invoke(existingTrack);
                Debug.Log($"트랙 업데이트: {existingTrack.trackName}");

                return existingTrack;
            }
            else
            {
                TrackData newTrack = new TrackData { trackName = result.trackName, bpm = 120f };

                newTrack.TrackAudio = result.clip;

                tracks.Add(newTrack);

                await UpdateTrackMetadataAsync();

                OnTrackAdded?.Invoke(newTrack);
                Debug.Log($"새 트랙 추가: {newTrack.trackName}");

                return newTrack;
            }
        }

        /// <summary>
        /// 트랙을 업데이트합니다.
        /// </summary>
        /// <param name="track">업데이트할 트랙 데이터</param>
        /// <returns>비동기 작업</returns>
        public async Task UpdateTrackAsync(TrackData track)
        {
            if (track == null || string.IsNullOrEmpty(track.trackName))
            {
                Debug.LogError("유효하지 않은 트랙 데이터");
                return;
            }

            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == track.trackName);

            if (existingTrack != null)
            {
                int index = tracks.IndexOf(existingTrack);
                tracks[index] = track;

                if (track.TrackAudio != null)
                {
                    await fileService.SaveAudioAsync(track.TrackAudio, track.trackName);
                }

                if (track.AlbumArt != null)
                {
                    await fileService.SaveAlbumArtAsync(track.AlbumArt, track.trackName);
                }

                await UpdateTrackMetadataAsync();

                OnTrackUpdated?.Invoke(track);
                Debug.Log($"트랙 업데이트: {track.trackName}");
            }
        }

        /// <summary>
        /// 트랙을 삭제합니다.
        /// </summary>
        /// <param name="trackName">삭제할 트랙 이름</param>
        /// <returns>비동기 작업</returns>
        public async Task DeleteTrackAsync(string trackName)
        {
            TrackData trackToRemove = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (trackToRemove != null)
            {
                await fileService.DeleteTrackFilesAsync(trackName);

                tracks.Remove(trackToRemove);

                await UpdateTrackMetadataAsync();

                OnTrackRemoved?.Invoke(trackToRemove);
                Debug.Log($"트랙 삭제: {trackName}");
            }
        }

        /// <summary>
        /// 모든 트랙을 삭제합니다.
        /// </summary>
        /// <returns>비동기 작업</returns>
        public async Task DeleteAllTracksAsync()
        {
            await fileService.DeleteAllTrackFilesAsync();

            // 삭제된 트랙 목록 저장
            List<TrackData> removedTracks = new List<TrackData>(tracks);

            // 트랙 목록 초기화
            tracks.Clear();

            // 이벤트 발생
            foreach (var track in removedTracks)
            {
                OnTrackRemoved?.Invoke(track);
            }

            Debug.Log("모든 트랙이 삭제되었습니다.");
        }

        /// <summary>
        /// 모든 트랙 정보를 가져옵니다.
        /// </summary>
        /// <returns>트랙 데이터 목록</returns>
        public List<TrackData> GetAllTracks()
        {
            return new List<TrackData>(tracks);
        }

        /// <summary>
        /// 트랙 이름으로 트랙을 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>트랙 데이터 또는 null</returns>
        public TrackData GetTrack(string trackName)
        {
            return tracks.FirstOrDefault(t => t.trackName == trackName);
        }

        /// <summary>
        /// 트랙의 오디오를 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>로드된 트랙 데이터</returns>
        public async Task<TrackData> LoadTrackAudioAsync(
            string trackName,
            IProgress<float> progress = null
        )
        {
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null)
            {
                AudioClip audioClip = await fileService.LoadAudioAsync(trackName, progress);
                track.TrackAudio = audioClip;

                if (track.TrackAudio != null)
                {
                    OnTrackLoaded?.Invoke(track);
                    Debug.Log($"트랙 오디오 로드: {trackName}");
                }
            }

            return track;
        }

        /// <summary>
        /// 트랙의 BPM을 설정합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="bpm">설정할 BPM 값</param>
        /// <returns>비동기 작업</returns>
        public async Task SetBPMAsync(string trackName, float bpm)
        {
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null)
            {
                track.bpm = bpm;

                await UpdateTrackMetadataAsync();

                OnTrackUpdated?.Invoke(track);
                Debug.Log($"트랙 BPM 업데이트: {trackName}, BPM: {bpm}");
            }
        }

        /// <summary>
        /// 트랙 이름으로 앨범 아트를 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>앨범 아트 Sprite</returns>
        public Sprite GetAlbumArt(string trackName)
        {
            if (string.IsNullOrEmpty(trackName))
                return null;

            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);
            if (track != null && track.AlbumArt != null)
                return track.AlbumArt;

            StartCoroutine(LoadAlbumArtCoroutine(trackName));
            return null;
        }

        private IEnumerator LoadAlbumArtCoroutine(string trackName)
        {
            var task = fileService.LoadAlbumArtAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (!task.IsFaulted && task.Result != null)
            {
                Debug.Log($"앨범 아트 로드 완료: {trackName}");
                OnAlbumArtLoaded?.Invoke(trackName, task.Result);
            }
        }

        /// <summary>
        /// 트랙 이름으로 오디오 클립을 가져옵니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>AudioClip</returns>
        public AudioClip GetAudioClip(string trackName)
        {
            if (string.IsNullOrEmpty(trackName))
                return null;

            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);
            if (track != null && track.TrackAudio != null)
                return track.TrackAudio;

            StartCoroutine(LoadAudioClipCoroutine(trackName));
            return null;
        }

        private IEnumerator LoadAudioClipCoroutine(string trackName)
        {
            var task = fileService.LoadAudioAsync(trackName);
            yield return new WaitUntil(() => task.IsCompleted);

            if (!task.IsFaulted && task.Result != null)
            {
                Debug.Log($"오디오 로드 완료: {trackName}");

                // 트랙 찾기
                TrackData updatedTrack = tracks.FirstOrDefault(t => t.trackName == trackName);
                if (updatedTrack != null)
                {
                    updatedTrack.TrackAudio = task.Result;
                    OnTrackLoaded?.Invoke(updatedTrack);
                }
            }
        }

        /// <summary>
        /// 오디오 파일을 로드하는 메서드
        /// </summary>
        /// <param name="filePath">로드할 오디오 파일 경로</param>
        public void LoadAudioFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAudioFilePath = filePath;
            currentSceneName = SceneManager.GetActiveScene().name;

            LoadingManager.Instance.LoadScene(
                LoadingManager.Instance.loadingSceneName,
                () => StartCoroutine(LoadAudioFileProcess())
            );
        }

        /// <summary>
        /// 앨범 아트를 로드하는 메서드
        /// </summary>
        /// <param name="filePath">로드할 이미지 파일 경로</param>
        /// <param name="trackIndex">트랙 인덱스</param>
        public void SetAlbumArt(string filePath, int trackIndex)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            pendingAlbumArtFilePath = filePath;
            selectedTrackIndex = trackIndex;
            currentSceneName = SceneManager.GetActiveScene().name;

            LoadingManager.Instance.LoadScene(
                LoadingManager.Instance.loadingSceneName,
                () => StartCoroutine(LoadAlbumArtProcess())
            );
        }

        /// <summary>
        /// 오디오 파일 로드 프로세스
        /// </summary>
        private IEnumerator LoadAudioFileProcess()
        {
            if (string.IsNullOrEmpty(pendingAudioFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            LoadingUI loadingUI = FindObjectOfType<LoadingUI>();
            Action<float> updateProgress = LoadingManager.Instance.UpdateProgress;

            updateProgress(0.1f);
            loadingUI?.SetLoadingText("오디오 파일 로드 중...");

            if (loadingUI != null)
            {
                SetRandomTip(loadingUI, audioLoadingTips);
            }

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.3f);
            loadingUI?.SetLoadingText("오디오 파일 분석 중...");

            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.3f + p * 0.6f;
                updateProgress(currentProgress);
            });

            var addTrackTask = AddTrackAsync(pendingAudioFilePath, progress);

            while (!addTrackTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, audioLoadingTips);
                }
                yield return null;
            }

            TrackData newTrack = addTrackTask.Result;

            if (newTrack == null)
            {
                Debug.LogError("트랙 추가 실패");
                pendingAudioFilePath = null;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            updateProgress(0.9f);
            loadingUI?.SetLoadingText("트랙 정보 저장 중...");
            yield return new WaitForSeconds(0.2f);

            updateProgress(1.0f);
            pendingAudioFilePath = null;
            LoadingManager.Instance.LoadScene(currentSceneName);
        }

        /// <summary>
        /// 앨범 아트 로드 프로세스
        /// </summary>
        private IEnumerator LoadAlbumArtProcess()
        {
            if (string.IsNullOrEmpty(pendingAlbumArtFilePath))
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            LoadingUI loadingUI = FindObjectOfType<LoadingUI>();
            Action<float> updateProgress = LoadingManager.Instance.UpdateProgress;

            updateProgress(0.2f);
            loadingUI?.SetLoadingText("앨범 아트 로드 중...");

            if (loadingUI != null)
            {
                SetRandomTip(loadingUI, albumArtLoadingTips);
            }

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.4f);
            loadingUI?.SetLoadingText("이미지 파일 처리 중...");

            if (selectedTrackIndex >= tracks.Count)
            {
                Debug.LogError("선택된 트랙 인덱스가 유효하지 않습니다.");
                pendingAlbumArtFilePath = null;
                selectedTrackIndex = -1;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            TrackData selectedTrack = tracks[selectedTrackIndex];

            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.4f + p * 0.5f;
                updateProgress(currentProgress);
            });

            var setAlbumArtTask = SetAlbumArtAsync(
                selectedTrack.trackName,
                pendingAlbumArtFilePath,
                progress
            );

            while (!setAlbumArtTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, albumArtLoadingTips);
                }
                yield return null;
            }

            TrackData updatedTrack = setAlbumArtTask.Result;

            if (updatedTrack == null)
            {
                Debug.LogError("앨범 아트 로드 실패");
                pendingAlbumArtFilePath = null;
                selectedTrackIndex = -1;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            updateProgress(0.9f);
            loadingUI?.SetLoadingText("앨범 아트 적용 중...");
            yield return new WaitForSeconds(0.2f);

            updateProgress(1.0f);
            pendingAlbumArtFilePath = null;
            selectedTrackIndex = -1;
            LoadingManager.Instance.LoadScene(currentSceneName);
        }

        /// <summary>
        /// 랜덤 팁을 설정합니다.
        /// </summary>
        private void SetRandomTip(LoadingUI loadingUI, string[] tips)
        {
            if (tips.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, tips.Length);
                loadingUI.SetLoadingText(tips[randomIndex]);
            }
        }

        private async Task UpdateTrackMetadataAsync()
        {
            List<TrackData> metadataList = new List<TrackData>();

            foreach (var track in tracks)
            {
                TrackData metadata = new TrackData
                {
                    trackName = track.trackName,
                    bpm = track.bpm,
                    artistName = track.artistName,
                    albumName = track.albumName,
                    year = track.year,
                    genre = track.genre,
                    duration = track.duration,
                };

                if (track.noteMap != null)
                {
                    string noteMapPath = AudioPathProvider.GetNoteMapPath(track.trackName);
                    string noteMapJson = JsonConvert.SerializeObject(
                        track.noteMap,
                        Formatting.Indented
                    );

                    string directory = Path.GetDirectoryName(noteMapPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await Task.Run(() => File.WriteAllText(noteMapPath, noteMapJson));
                    Debug.Log($"노트맵 저장 완료: {noteMapPath}");
                }

                metadataList.Add(metadata);
            }

            await fileService.SaveMetadataAsync(metadataList);
        }

        /// <summary>
        /// 트랙 메타데이터를 업데이트합니다.
        /// </summary>
        /// <param name="track">업데이트할 트랙 데이터</param>
        /// <returns>업데이트된 트랙 데이터</returns>
        public async Task<TrackData> UpdateTrackMetadataAsync(TrackData track)
        {
            if (track == null)
                return null;

            try
            {
                TrackData existingTrack = tracks.FirstOrDefault(t =>
                    t.trackName == track.trackName
                );
                if (existingTrack != null)
                {
                    int index = tracks.IndexOf(existingTrack);
                    tracks[index] = track;

                    if (track.noteMap != null)
                    {
                        string noteMapPath = AudioPathProvider.GetNoteMapPath(track.trackName);
                        string noteMapJson = JsonConvert.SerializeObject(
                            track.noteMap,
                            Formatting.Indented
                        );

                        string directory = Path.GetDirectoryName(noteMapPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        await Task.Run(() => File.WriteAllText(noteMapPath, noteMapJson));
                        Debug.Log($"노트맵 저장 완료: {noteMapPath}");
                    }

                    await UpdateTrackMetadataAsync();

                    OnTrackUpdated?.Invoke(track);
                    return track;
                }
                else
                {
                    Debug.LogWarning($"업데이트할 트랙을 찾을 수 없음: {track.trackName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"트랙 메타데이터 업데이트 중 오류 발생: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 트랙의 노트맵을 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>로드된 노트맵</returns>
        public async Task<NoteMap> LoadNoteMapAsync(string trackName)
        {
            if (string.IsNullOrEmpty(trackName))
                return null;

            try
            {
                string noteMapPath = AudioPathProvider.GetNoteMapPath(trackName);

                if (!File.Exists(noteMapPath))
                {
                    Debug.LogWarning($"노트맵 파일을 찾을 수 없음: {noteMapPath}");
                    return null;
                }

                string json = await Task.Run(() => File.ReadAllText(noteMapPath));
                NoteMap noteMap = JsonConvert.DeserializeObject<NoteMap>(json);

                TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);
                if (track != null)
                {
                    track.noteMap = noteMap;
                    OnTrackUpdated?.Invoke(track);
                }

                Debug.Log($"노트맵 로드 완료: {noteMapPath}");
                return noteMap;
            }
            catch (Exception ex)
            {
                Debug.LogError($"노트맵 로드 중 오류 발생: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 노트맵을 저장합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="noteMap">저장할 노트맵</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> SaveNoteMapAsync(string trackName, NoteMap noteMap)
        {
            if (string.IsNullOrEmpty(trackName) || noteMap == null)
                return false;

            try
            {
                string noteMapPath = AudioPathProvider.GetNoteMapPath(trackName);

                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented,
                };

                string noteMapJson = JsonConvert.SerializeObject(noteMap, settings);

                string directory = Path.GetDirectoryName(noteMapPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await Task.Run(() => File.WriteAllText(noteMapPath, noteMapJson));

                TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);
                if (track != null)
                {
                    track.noteMap = noteMap;
                    OnTrackUpdated?.Invoke(track);
                }

                Debug.Log($"노트맵 저장 완료: {noteMapPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"노트맵 저장 중 오류 발생: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 트랙을 로드할 때 노트맵도 함께 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>로드된 트랙 데이터</returns>
        public async Task<TrackData> LoadTrackWithNoteMapAsync(string trackName)
        {
            TrackData track = await LoadTrackAudioAsync(trackName);

            if (track != null)
            {
                // 노트맵 로드 시도
                NoteMap noteMap = await LoadNoteMapAsync(trackName);
                if (noteMap != null)
                {
                    track.noteMap = noteMap;
                }
            }

            return track;
        }

        /// <summary>
        /// 앨범 아트를 설정합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="albumArtPath">앨범 아트 파일 경로</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>업데이트된 트랙 데이터</returns>
        public async Task<TrackData> SetAlbumArtAsync(
            string trackName,
            string albumArtPath,
            IProgress<float> progress = null
        )
        {
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null)
            {
                Sprite albumArt = await fileService.ImportAlbumArtAsync(
                    albumArtPath,
                    trackName,
                    progress
                );

                if (albumArt != null)
                {
                    OnAlbumArtLoaded?.Invoke(trackName, albumArt);
                    Debug.Log($"앨범 아트 설정: {trackName}");
                }
            }

            return track;
        }

        protected override void OnDestroy()
        {
            if (tracks != null)
            {
                foreach (var track in tracks)
                {
                    if (track.TrackAudio != null)
                    {
                        track.TrackAudio = null;
                    }
                }
            }

            pendingAudioFilePath = null;
            pendingAlbumArtFilePath = null;

            base.OnDestroy();
        }
    }
}
