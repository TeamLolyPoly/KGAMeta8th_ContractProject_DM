using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingManager : Singleton<LoadingManager>
{
    public const string LOADING_SCENE_NAME = "Loading";
    public float minimumLoadingTime = 0.5f;

    private LoadingPanel loadingUI;
    private ProgressBar progressBar;

    private bool isLoading = false;

    public event Action OnLoadingStarted;
    public event Action<float> OnProgressUpdated;
    public event Action OnLoadingFinished;

    /// <summary>
    /// 씬을 로딩합니다.
    /// </summary>
    /// <param name="sceneName">로드할 씬 이름</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(string sceneName, Action onComplete = null)
    {
        if (isLoading)
            return;

        isLoading = true;
        OnLoadingStarted?.Invoke();
        StartCoroutine(LoadSceneWithLoadingScene(sceneName, onComplete));
    }

    private IEnumerator LoadSceneWithLoadingScene(string sceneName, Action onComplete)
    {
        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        loadingUI = FindObjectOfType<LoadingPanel>();
        progressBar = FindObjectOfType<ProgressBar>();

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        float startTime = Time.time;
        float progress = 0;

        while (!asyncOperation.isDone)
        {
            progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            UpdateProgress(progress);

            if (progress >= 1.0f && (Time.time - startTime) >= minimumLoadingTime)
            {
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        isLoading = false;
        OnLoadingFinished?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 여러 비동기 작업을 로딩합니다.
    /// </summary>
    /// <param name="operations">비동기 작업 목록</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadMultipleOperations(List<AsyncOperation> operations, Action onComplete = null)
    {
        if (isLoading)
            return;

        isLoading = true;
        OnLoadingStarted?.Invoke();
        StartCoroutine(LoadOperationsWithLoadingScene(operations, onComplete));
    }

    private IEnumerator LoadOperationsWithLoadingScene(
        List<AsyncOperation> operations,
        Action onComplete
    )
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        loadingUI = FindObjectOfType<LoadingPanel>();
        progressBar = FindObjectOfType<ProgressBar>();

        float startTime = Time.time;
        bool allOperationsComplete = false;

        while (!allOperationsComplete)
        {
            float totalProgress = 0f;

            foreach (AsyncOperation operation in operations)
            {
                totalProgress += operation.progress;
            }

            float averageProgress = totalProgress / operations.Count;
            UpdateProgress(averageProgress);

            allOperationsComplete = true;
            foreach (AsyncOperation operation in operations)
            {
                if (!operation.isDone)
                {
                    allOperationsComplete = false;
                    break;
                }
            }

            if (allOperationsComplete && (Time.time - startTime) < minimumLoadingTime)
            {
                allOperationsComplete = false;
            }

            yield return null;
        }

        AsyncOperation loadOriginalScene = SceneManager.LoadSceneAsync(currentSceneName);
        while (!loadOriginalScene.isDone)
        {
            yield return null;
        }

        isLoading = false;
        OnLoadingFinished?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 비동기 작업을 로딩합니다.
    /// </summary>
    /// <typeparam name="T">반환 값 타입</typeparam>
    /// <param name="asyncOperation">비동기 작업</param>
    /// <param name="loadingText">로딩 텍스트</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadAsyncOperation<T>(
        Func<IEnumerator> asyncOperation,
        string loadingText = "로딩 중...",
        Action<T> onComplete = null
    )
        where T : class
    {
        if (isLoading)
            return;

        isLoading = true;
        OnLoadingStarted?.Invoke();
        StartCoroutine(
            ProcessAsyncOperationWithLoadingScene(asyncOperation, loadingText, onComplete)
        );
    }

    private IEnumerator ProcessAsyncOperationWithLoadingScene<T>(
        Func<IEnumerator> asyncOperation,
        string loadingText,
        Action<T> onComplete
    )
        where T : class
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        loadingUI = FindObjectOfType<LoadingPanel>();
        progressBar = FindObjectOfType<ProgressBar>();

        if (loadingUI != null)
        {
            loadingUI.SetLoadingText(loadingText);
        }

        float startTime = Time.time;
        T result = null;

        var operationCoroutine = asyncOperation();
        while (operationCoroutine.MoveNext())
        {
            if (operationCoroutine.Current is T resultValue)
            {
                result = resultValue;
            }
            else if (operationCoroutine.Current is float progressValue)
            {
                UpdateProgress(progressValue);
            }
            yield return operationCoroutine.Current;
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        AsyncOperation loadOriginalScene = SceneManager.LoadSceneAsync(currentSceneName);
        while (!loadOriginalScene.isDone)
        {
            yield return null;
        }

        isLoading = false;
        OnLoadingFinished?.Invoke();
        onComplete?.Invoke(result);
    }

    /// <summary>
    /// 로딩 진행 상태를 업데이트합니다.
    /// </summary>
    /// <param name="progress">진행 상태 (0~1)</param>
    public void UpdateProgress(float progress)
    {
        if (loadingUI != null)
        {
            loadingUI.UpdateProgress(progress);
        }
        else if (progressBar != null)
        {
            progressBar.currentValue = progress * 100f;
            progressBar.UpdateUI();
        }

        OnProgressUpdated?.Invoke(progress);
    }

    /// <summary>
    /// 로딩 텍스트를 설정합니다.
    /// </summary>
    /// <param name="text">로딩 텍스트</param>
    public void SetLoadingText(string text)
    {
        if (loadingUI != null)
        {
            loadingUI.SetLoadingText(text);
        }
    }
}
