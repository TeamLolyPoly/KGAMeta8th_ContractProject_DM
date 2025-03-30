using System;
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
        private List<TrackData> tracks = new List<TrackData>();
        public List<TrackData> Tracks => tracks;

        public bool IsInitialized { get; private set; }

        public async void Initialize()
        {
            ResourcePath.EnsureDirectoriesExist();
            await LoadAllTracksAsync();
            IsInitialized = true;
        }

        public async Task LoadAllTracksAsync()
        {
            var metadata = await ResourceIO.LoadMetadataAsync();
            Debug.Log(
                $"[EditorDataManager] 메타데이터에서 {metadata.Count}개의 트랙 정보를 로드했습니다."
            );

            tracks.Clear();

            foreach (var trackMetadata in metadata)
            {
                TrackData track = new TrackData
                {
                    id = trackMetadata.id != Guid.Empty ? trackMetadata.id : Guid.NewGuid(),
                    trackName = trackMetadata.trackName,
                    bpm = trackMetadata.bpm,
                    artistName = trackMetadata.artistName,
                    albumName = trackMetadata.albumName,
                    year = trackMetadata.year,
                    genre = trackMetadata.genre,
                    duration = trackMetadata.duration,
                };

                tracks.Add(track);

                await LoadAlbumArtAsync(track);
                await LoadTrackAudioAsync(track);
            }

            foreach (var track in tracks)
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
        }

        /// <summary>
        /// 앨범 아트를 비동기적으로 로드합니다.
        /// <param name="trackName">트랙 이름</param>
        /// </summary>
        private async Task LoadAlbumArtAsync(TrackData track)
        {
            if (string.IsNullOrEmpty(track.trackName))
            {
                Debug.LogWarning("앨범 아트 로드: 트랙 이름이 비어 있습니다");
                return;
            }

            try
            {
                var albumArtCoroutine = ResourceIO.LoadAlbumArtAsync(track);
                if (albumArtCoroutine == null)
                {
                    Debug.LogError($"앨범 아트 코루틴 생성 실패: {track.trackName}");
                    return;
                }

                int maxIterations = 100;
                int iterations = 0;

                while (albumArtCoroutine.MoveNext() && iterations < maxIterations)
                {
                    iterations++;
                    if (albumArtCoroutine.Current != null)
                    {
                        track.AlbumArt = albumArtCoroutine.Current;
                        Debug.Log($"앨범 아트 로드 성공: {track.trackName}");
                        break;
                    }
                    await Task.Yield();
                }

                if (iterations >= maxIterations)
                {
                    Debug.LogWarning($"앨범 아트 로드 타임아웃: {track.trackName}");
                }

                return;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"앨범 아트 초기 로드 중 오류 발생: {track.trackName} - {ex.Message}"
                );
                return;
            }
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
            var result = await ResourceIO.ImportAudioFileAsync(filePath, progress);

            if (result.clip == null)
            {
                Debug.LogError($"트랙 추가 실패: {filePath}");
                return null;
            }

            TrackData existingTrack = tracks.FirstOrDefault(t => t.id == result.trackData.id);

            if (existingTrack != null)
            {
                existingTrack.TrackAudio = result.clip;

                await UpdateTrackMetadataAsync();

                Debug.Log($"트랙 업데이트: {existingTrack.trackName}");

                return existingTrack;
            }
            else
            {
                TrackData newTrack = result.trackData;

                newTrack.TrackAudio = result.clip;

                await SaveNoteMapAsync(
                    newTrack,
                    new NoteMap()
                    {
                        bpm = 120f,
                        beatsPerBar = 4,
                        notes = new List<NoteData>(),
                    }
                );

                tracks.Add(newTrack);

                await UpdateTrackMetadataAsync();

                Debug.Log($"새 트랙 추가: {newTrack.trackName}, ID: {newTrack.id}");

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
                    await ResourceIO.SaveAudioAsync(track.TrackAudio, track);
                }

                if (track.AlbumArt != null)
                {
                    await ResourceIO.SaveAlbumArtAsync(track.AlbumArt, track);
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
                await ResourceIO.DeleteTrackFilesAsync(trackToRemove.id);

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
            await ResourceIO.DeleteAllTrackFilesAsync();

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
            TrackData track,
            IProgress<float> progress = null
        )
        {
            if (string.IsNullOrEmpty(track.trackName))
            {
                Debug.LogError("트랙 이름이 없습니다.");
                return null;
            }

            if (track != null)
            {
                try
                {
                    AudioClip audioClip = await ResourceIO.LoadAudioAsync(track, progress);
                    track.TrackAudio = audioClip;

                    if (track.TrackAudio == null)
                    {
                        Debug.LogError(
                            $"트랙 오디오 로드 실패: {track.trackName} - 파일이 존재하지 않거나 형식이 잘못되었을 수 있습니다."
                        );
                    }
                    else
                    {
                        Debug.Log(
                            $"트랙 오디오 로드 성공: {track.trackName}, 길이: {track.TrackAudio.length}초"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"트랙 오디오 로드 중 예외 발생: {track.trackName} - {ex.Message}"
                    );
                }
            }
            else
            {
                Debug.LogError($"트랙을 찾을 수 없음: {track.trackName}");
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
                await SaveNoteMapAsync(track, track.noteMap);

                Debug.Log($"트랙 BPM 업데이트: {trackName}, BPM: {bpm}");
            }
        }

        private async Task UpdateTrackMetadataAsync()
        {
            List<TrackData> metadataList = new List<TrackData>();

            foreach (var track in tracks)
            {
                TrackData metadata = new TrackData
                {
                    id = track.id,
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
                    string noteMapPath = ResourcePath.GetNoteMapPath(track.id);
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

            await ResourceIO.SaveMetadataAsync(metadataList);
        }

        /// <summary>
        /// 트랙의 노트맵을 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <returns>로드된 노트맵</returns>
        public async Task<NoteMap> LoadNoteMapAsync(TrackData track)
        {
            if (track == null)
                return null;

            try
            {
                string noteMapPath = ResourcePath.GetNoteMapPath(track.id);

                if (!File.Exists(noteMapPath))
                {
                    return null;
                }

                string json = await Task.Run(() => File.ReadAllText(noteMapPath));
                NoteMap noteMap = JsonConvert.DeserializeObject<NoteMap>(json);

                if (track != null)
                {
                    track.noteMap = noteMap;
                }

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
        public async Task<bool> SaveNoteMapAsync(TrackData track, NoteMap noteMap)
        {
            if (track == null || noteMap == null)
                return false;

            try
            {
                string noteMapPath = ResourcePath.GetNoteMapPath(track.id);

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
                Sprite albumArt = await ResourceIO.ImportAlbumArtAsync(
                    albumArtPath,
                    track,
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
