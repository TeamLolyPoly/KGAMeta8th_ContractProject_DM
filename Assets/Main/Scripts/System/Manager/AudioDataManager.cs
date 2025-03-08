using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NoteEditor
{
    /// <summary>
    /// 트랙 데이터를 관리하는 매니저 클래스
    /// </summary>
    public class AudioDataManager : Singleton<AudioDataManager>, IInitializable
    {
        private AudioFileService fileService;
        private List<TrackData> tracks = new List<TrackData>();

        /// <summary>
        /// 트랙이 추가되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<TrackData> OnTrackAdded;

        /// <summary>
        /// 트랙이 제거되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<TrackData> OnTrackRemoved;

        /// <summary>
        /// 트랙이 업데이트되었을 때 발생하는 이벤트
        /// </summary>
        public event Action<TrackData> OnTrackUpdated;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            fileService = new AudioFileService();
        }

        /// <summary>
        /// 매니저를 초기화합니다.
        /// </summary>
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

        /// <summary>
        /// 모든 트랙을 비동기적으로 로드합니다.
        /// </summary>
        /// <returns>비동기 작업</returns>
        public async Task LoadAllTracksAsync()
        {
            // 메타데이터 로드
            var metadata = await fileService.LoadMetadataAsync();
            Debug.Log($"메타데이터에서 {metadata.Count}개의 트랙 정보를 로드했습니다.");

            // 기존 트랙 목록 초기화
            tracks.Clear();

            // 각 트랙 정보 처리
            foreach (var trackMetadata in metadata)
            {
                TrackData track = new TrackData
                {
                    trackName = trackMetadata.trackName,
                    bpm = trackMetadata.bpm,
                };

                // 트랙 목록에 추가
                tracks.Add(track);

                // 오디오 및 앨범 아트 로드는 필요할 때 지연 로드
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
            // 오디오 파일 로드
            var result = await fileService.ImportAudioFileAsync(filePath, progress);

            if (result.clip == null)
            {
                Debug.LogError($"트랙 추가 실패: {filePath}");
                return null;
            }

            // 기존 트랙 확인
            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == result.trackName);

            if (existingTrack != null)
            {
                // 기존 트랙 업데이트
                existingTrack.trackAudio = result.clip;

                // 메타데이터 업데이트
                await UpdateTrackMetadataAsync();

                OnTrackUpdated?.Invoke(existingTrack);
                Debug.Log($"트랙 업데이트: {existingTrack.trackName}");

                return existingTrack;
            }
            else
            {
                // 새 트랙 생성
                TrackData newTrack = new TrackData
                {
                    trackName = result.trackName,
                    trackAudio = result.clip,
                    bpm = 120f, // 기본 BPM
                };

                // 트랙 목록에 추가
                tracks.Add(newTrack);

                // 메타데이터 업데이트
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
            if (track == null)
                return;

            // 트랙 찾기
            TrackData existingTrack = tracks.FirstOrDefault(t => t.trackName == track.trackName);

            if (existingTrack != null)
            {
                // 트랙 업데이트
                int index = tracks.IndexOf(existingTrack);
                tracks[index] = track;

                // 오디오 파일 저장 (필요한 경우)
                if (track.trackAudio != null)
                {
                    await fileService.SaveAudioAsync(track.trackAudio, track.trackName);
                }

                // 앨범 아트 저장 (필요한 경우)
                if (track.albumArt != null)
                {
                    await fileService.SaveAlbumArtAsync(track.albumArt, track.trackName);
                }

                // 메타데이터 업데이트
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
            // 트랙 찾기
            TrackData trackToRemove = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (trackToRemove != null)
            {
                // 트랙 파일 삭제
                await fileService.DeleteTrackFilesAsync(trackName);

                // 트랙 목록에서 제거
                tracks.Remove(trackToRemove);

                // 메타데이터 업데이트
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
            // 모든 트랙 파일 삭제
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
            // 트랙 찾기
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null && track.trackAudio == null)
            {
                // 오디오 로드
                track.trackAudio = await fileService.LoadAudioAsync(trackName, progress);

                if (track.trackAudio != null)
                {
                    Debug.Log($"트랙 오디오 로드됨: {trackName}");
                    OnTrackUpdated?.Invoke(track);
                }
            }

            return track;
        }

        /// <summary>
        /// 트랙의 앨범 아트를 로드합니다.
        /// </summary>
        /// <param name="trackName">트랙 이름</param>
        /// <param name="progress">진행 상황 보고 인터페이스</param>
        /// <returns>로드된 트랙 데이터</returns>
        public async Task<TrackData> LoadTrackAlbumArtAsync(
            string trackName,
            IProgress<float> progress = null
        )
        {
            // 트랙 찾기
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null && track.albumArt == null)
            {
                // 앨범 아트 로드
                track.albumArt = await fileService.LoadAlbumArtAsync(trackName, progress);

                if (track.albumArt != null)
                {
                    Debug.Log($"트랙 앨범 아트 로드됨: {trackName}");
                    OnTrackUpdated?.Invoke(track);
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
            if (bpm <= 0)
            {
                Debug.LogWarning("BPM은 0보다 커야 합니다.");
                return;
            }

            // 트랙 찾기
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null)
            {
                // BPM 업데이트
                track.bpm = bpm;

                // 메타데이터 업데이트
                await UpdateTrackMetadataAsync();

                OnTrackUpdated?.Invoke(track);
                Debug.Log($"트랙 BPM 업데이트: {trackName}, BPM: {bpm}");
            }
        }

        /// <summary>
        /// 트랙에 앨범 아트를 설정합니다.
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
            // 트랙 찾기
            TrackData track = tracks.FirstOrDefault(t => t.trackName == trackName);

            if (track != null)
            {
                // 앨범 아트 로드 및 저장
                track.albumArt = await fileService.ImportAlbumArtAsync(
                    albumArtPath,
                    trackName,
                    progress
                );

                if (track.albumArt != null)
                {
                    // 메타데이터 업데이트
                    await UpdateTrackMetadataAsync();

                    OnTrackUpdated?.Invoke(track);
                    Debug.Log($"트랙 앨범 아트 업데이트: {trackName}");
                }
            }

            return track;
        }

        /// <summary>
        /// 트랙 메타데이터를 업데이트합니다.
        /// </summary>
        /// <returns>비동기 작업</returns>
        private async Task UpdateTrackMetadataAsync()
        {
            List<TrackMetadata> metadataList = new List<TrackMetadata>();

            foreach (var track in tracks)
            {
                TrackMetadata metadata = new TrackMetadata
                {
                    trackName = track.trackName,
                    bpm = track.bpm,
                    // 다른 메타데이터 필드는 필요에 따라 추가
                };

                metadataList.Add(metadata);
            }

            await fileService.SaveMetadataAsync(metadataList);
        }
    }
}
