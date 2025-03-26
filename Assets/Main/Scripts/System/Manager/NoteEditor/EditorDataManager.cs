using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace NoteEditor
{
    /// <summary
    /// 트랙 데이터를 관리하는 매니저 클래스
    /// </summary>
    public class EditorDataManager : Singleton<EditorDataManager>, IInitializable
    {
        private AudioFileService fileService;
        private List<TrackData> tracks = new List<TrackData>();
        public List<TrackData> Tracks => tracks;

        public bool IsInitialized { get; private set; }

        public async void Initialize()
        {
            fileService = new AudioFileService();
            AudioPathProvider.EnsureDirectoriesExist();
            await LoadAllTracksAsync();
            IsInitialized = true;
            Debug.Log("[EditorDataManager] 초기화 완료");
        }

        public async Task LoadAllTracksAsync()
        {
            var metadata = await fileService.LoadMetadataAsync();
            Debug.Log(
                $"[EditorDataManager] 메타데이터에서 {metadata.Count}개의 트랙 정보를 로드했습니다."
            );

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

                await LoadTrackAudioAsync(track.trackName);
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

                Debug.Log($"트랙 업데이트: {existingTrack.trackName}");

                return existingTrack;
            }
            else
            {
                TrackData newTrack = new TrackData { trackName = result.trackName, bpm = 120f };

                newTrack.TrackAudio = result.clip;

                tracks.Add(newTrack);

                await UpdateTrackMetadataAsync();

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

            tracks.Clear();

            Debug.Log("모든 트랙이 삭제되었습니다.");
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
                await SaveNoteMapAsync(track.trackName, track.noteMap);

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

        private IEnumerator<Sprite> LoadAlbumArtCoroutine(string trackName)
        {
            var task = fileService.LoadAlbumArtAsync(trackName);

            yield return task.Result;

            if (!task.IsFaulted && task.Result != null)
            {
                Debug.Log($"앨범 아트 로드 완료: {trackName}");
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

                TrackData updatedTrack = tracks.FirstOrDefault(t => t.trackName == trackName);
                if (updatedTrack != null)
                {
                    updatedTrack.TrackAudio = task.Result;
                }
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
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"노트맵 저장 중 오류 발생: {ex.Message}");
                return false;
            }
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
                    track.AlbumArt = albumArt;
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

            base.OnDestroy();
        }
    }
}
