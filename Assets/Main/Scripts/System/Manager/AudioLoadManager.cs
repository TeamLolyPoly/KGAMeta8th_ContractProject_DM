using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 오디오 파일 로드 작업을 관리하는 매니저 클래스
/// </summary>
namespace NoteEditor
{
    public class AudioLoadManager : Singleton<AudioLoadManager>
    {
        private string pendingAudioFilePath;
        private string pendingAlbumArtFilePath;
        private int selectedTrackIndex = -1;
        private string currentSceneName;

        public event Action<TrackData> OnTrackLoaded;
        public event Action<string, Sprite> OnAlbumArtLoaded;

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

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
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
        public void LoadAlbumArt(string filePath, int trackIndex)
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

            // 진행 상황 보고를 위한 Progress 객체 생성
            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.3f + p * 0.6f;
                updateProgress(currentProgress);
            });

            // AudioDataManager를 통해 트랙 추가
            var addTrackTask = AudioDataManager.Instance.AddTrackAsync(
                pendingAudioFilePath,
                progress
            );

            // 작업이 완료될 때까지 대기
            while (!addTrackTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, audioLoadingTips);
                }
                yield return null;
            }

            TrackData loadedTrack = addTrackTask.Result;

            if (loadedTrack == null)
            {
                Debug.LogError("오디오 파일 로드 실패");
                pendingAudioFilePath = null;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            updateProgress(0.9f);
            loadingUI?.SetLoadingText("오디오 매니저에 트랙 추가 중...");
            yield return new WaitForSeconds(0.2f);

            // 트랙 선택
            AudioManager.Instance.SelectTrack(loadedTrack);

            updateProgress(1.0f);
            loadingUI?.SetLoadingText("완료! 노트 에디터로 돌아가는 중...");
            yield return new WaitForSeconds(0.5f);

            pendingAudioFilePath = null;

            LoadingManager.Instance.LoadScene(
                currentSceneName,
                () =>
                {
                    OnTrackLoaded?.Invoke(loadedTrack);
                }
            );
        }

        /// <summary>
        /// 앨범 아트 로드 프로세스
        /// </summary>
        private IEnumerator LoadAlbumArtProcess()
        {
            if (string.IsNullOrEmpty(pendingAlbumArtFilePath) || selectedTrackIndex < 0)
            {
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            LoadingUI loadingUI = FindObjectOfType<LoadingUI>();

            Action<float> updateProgress = LoadingManager.Instance.UpdateProgress;

            updateProgress(0.1f);
            loadingUI?.SetLoadingText("앨범 아트 로드 중...");

            if (loadingUI != null)
            {
                SetRandomTip(loadingUI, albumArtLoadingTips);
            }

            yield return new WaitForSeconds(0.2f);

            updateProgress(0.4f);
            loadingUI?.SetLoadingText("이미지 파일 처리 중...");

            // 현재 선택된 트랙 가져오기
            var tracks = AudioManager.Instance.GetAllTrackInfo();
            if (selectedTrackIndex >= tracks.Count)
            {
                Debug.LogError("선택된 트랙 인덱스가 유효하지 않습니다.");
                pendingAlbumArtFilePath = null;
                selectedTrackIndex = -1;
                LoadingManager.Instance.LoadScene(currentSceneName);
                yield break;
            }

            TrackData selectedTrack = tracks[selectedTrackIndex];

            // 진행 상황 보고를 위한 Progress 객체 생성
            var progress = new Progress<float>(p =>
            {
                float currentProgress = 0.4f + p * 0.5f;
                updateProgress(currentProgress);
            });

            // AudioDataManager를 통해 앨범 아트 설정
            var setAlbumArtTask = AudioDataManager.Instance.SetAlbumArtAsync(
                selectedTrack.trackName,
                pendingAlbumArtFilePath,
                progress
            );

            // 작업이 완료될 때까지 대기
            while (!setAlbumArtTask.IsCompleted)
            {
                if (loadingUI != null && UnityEngine.Random.value < 0.05f)
                {
                    SetRandomTip(loadingUI, albumArtLoadingTips);
                }
                yield return null;
            }

            TrackData updatedTrack = setAlbumArtTask.Result;

            if (updatedTrack == null || updatedTrack.albumArt == null)
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
            loadingUI?.SetLoadingText("완료! 노트 에디터로 돌아가는 중...");
            yield return new WaitForSeconds(0.5f);

            string trackName = updatedTrack.trackName;
            Sprite albumArt = updatedTrack.albumArt;

            pendingAlbumArtFilePath = null;
            selectedTrackIndex = -1;

            LoadingManager.Instance.LoadScene(
                currentSceneName,
                () =>
                {
                    OnAlbumArtLoaded?.Invoke(trackName, albumArt);
                }
            );
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
    }
}
