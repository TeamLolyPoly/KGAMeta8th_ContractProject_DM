using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageLoadingManager : Singleton<StageLoadingManager>
{
    private const string LOADING_SCENE_NAME = "Loading_Stage";
    private const float MINIMUM_LOADING_TIME = 1.5f;

    private StageLoadingPanel loadingUI;
    public StageLoadingPanel LoadingUI => loadingUI;
    private ProgressBar progressBar;

    private bool isLoading = false;

    public event Action<float> OnProgressUpdated;

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

        if (!StageUIManager.Instance.IsInitialized)
        {
            StageUIManager.Instance.Initialize();
        }

        loadingUI = StageUIManager.Instance.OpenPanel(PanelType.Loading) as StageLoadingPanel;

        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    /// <summary>
    /// 여러 비동기 작업을 로딩합니다.
    /// </summary>
    /// <param name="operations">비동기 작업 목록</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(
        string targetSceneName,
        List<Func<IEnumerator>> operations,
        Action onComplete = null
    )
    {
        if (isLoading)
            return;

        isLoading = true;

        if (!StageUIManager.Instance.IsInitialized)
        {
            StageUIManager.Instance.Initialize();
        }

        loadingUI = StageUIManager.Instance.OpenPanel(PanelType.Loading) as StageLoadingPanel;

        StartCoroutine(LoadOperations(targetSceneName, operations, onComplete));
    }

    /// <summary>
    /// 비동기 작업을 로딩합니다.
    /// </summary>
    /// <param name="asyncOperation">비동기 작업</param>
    /// <param name="loadingText">로딩 텍스트</param>
    /// <param name="onComplete">로딩 완료 후 실행할 콜백</param>
    public void LoadScene(
        string targetSceneName,
        Func<IEnumerator> asyncOperation,
        Action onComplete = null
    )
    {
        if (isLoading)
            return;

        isLoading = true;

        if (!EditorUIManager.Instance.IsInitialized)
        {
            StageUIManager.Instance.Initialize();
        }

        StageUIManager.Instance.CloseAllPanels();

        loadingUI = StageUIManager.Instance.OpenPanel(PanelType.Loading) as StageLoadingPanel;

        if (asyncOperation != null)
        {
            StartCoroutine(LoadOpertion(targetSceneName, asyncOperation, onComplete));
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private IEnumerator LoadSceneRoutine(string targetSceneName, Action onComplete)
    {
        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        AsyncOperation loadTargetScene = SceneManager.LoadSceneAsync(targetSceneName);

        loadTargetScene.allowSceneActivation = false;

        float startTime = Time.time;
        float progress = 0;

        while (!loadTargetScene.isDone)
        {
            UpdateProgress(0);

            progress = Mathf.Clamp01(loadTargetScene.progress / 0.9f);

            UpdateProgress(progress);

            if (progress >= 1.0f && (Time.time - startTime) >= MINIMUM_LOADING_TIME)
            {
                loadTargetScene.allowSceneActivation = true;
            }

            yield return null;
        }

        isLoading = false;

        loadTargetScene.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetScene.isDone);

        loadingUI.Close();

        onComplete?.Invoke();
    }

    private IEnumerator LoadOpertion(
        string targetSceneName,
        Func<IEnumerator> asyncOperation,
        Action onComplete = null
    )
    {
        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        AsyncOperation loadTargetScene = SceneManager.LoadSceneAsync(targetSceneName);

        loadTargetScene.allowSceneActivation = false;

        float startTime = Time.time;

        var operationCoroutine = asyncOperation();

        while (operationCoroutine.MoveNext())
        {
            if (operationCoroutine.Current is float progressValue)
            {
                UpdateProgress(progressValue);
            }
            yield return operationCoroutine.Current;
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < MINIMUM_LOADING_TIME)
        {
            yield return new WaitForSeconds(MINIMUM_LOADING_TIME - elapsedTime);
        }

        loadTargetScene.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetScene.isDone);

        loadingUI.Close();

        isLoading = false;

        onComplete?.Invoke();
    }

    private IEnumerator LoadOperations(
        string targetSceneName,
        List<Func<IEnumerator>> operations,
        Action onComplete
    )
    {
        AsyncOperation loadLoadingScene = SceneManager.LoadSceneAsync(LOADING_SCENE_NAME);
        while (!loadLoadingScene.isDone)
        {
            yield return null;
        }

        AsyncOperation loadTargetSceneAsync = SceneManager.LoadSceneAsync(targetSceneName);
        loadTargetSceneAsync.allowSceneActivation = false;

        float startTime = Time.time;

        float totalOperations = operations.Count;
        float accumulatedProgress = 0f;

        for (int i = 0; i < operations.Count; i++)
        {
            var operation = operations[i];
            var operationCoroutine = operation();

            float baseProgress = i / totalOperations;
            float operationWeight = 1f / totalOperations;

            while (operationCoroutine.MoveNext())
            {
                if (operationCoroutine.Current is float progressValue)
                {
                    float scaledProgress = baseProgress + (progressValue * operationWeight);
                    UpdateProgress(scaledProgress);
                }
                yield return operationCoroutine.Current;
            }

            accumulatedProgress = (i + 1) / totalOperations;
            UpdateProgress(accumulatedProgress);
        }

        float elapsedTime = Time.time - startTime;
        if (elapsedTime < MINIMUM_LOADING_TIME)
        {
            yield return new WaitForSeconds(MINIMUM_LOADING_TIME - elapsedTime);
        }

        UpdateProgress(1.0f);

        loadTargetSceneAsync.allowSceneActivation = true;

        yield return new WaitUntil(() => loadTargetSceneAsync.isDone);

        isLoading = false;
        loadingUI.Close();

        onComplete?.Invoke();
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
