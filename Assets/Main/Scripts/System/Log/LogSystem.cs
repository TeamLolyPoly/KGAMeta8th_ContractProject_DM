using System.Collections.Generic;
using System.IO;
using System.Text;
using Michsky.UI.Heat;
using TMPro;
using UnityEngine;

public class LogSystem : MonoBehaviour
{
    private struct LogEntry
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public System.DateTime Timestamp;

        public string GetDetailedLog()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Type}]");
            sb.AppendLine($"Message: {Message}");
            if (!string.IsNullOrEmpty(StackTrace))
            {
                sb.AppendLine("StackTrace:");
                sb.AppendLine(StackTrace);
            }
            sb.AppendLine("----------------------------------------");
            return sb.ToString();
        }

        public string GetDisplayLog()
        {
            return $"[{Timestamp:HH:mm:ss}] [{Type}] {Message}";
        }
    }

    public LogBox logBoxPrefab;
    public RectTransform logBoxParent;

    private Queue<string> logQueue;
    private StringBuilder logBuilder;
    private List<LogEntry> logEntries;
    private bool isSubscribed = false;
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private string logFilePath;
    public ButtonManager saveLogButton;

    [Header("게임 정보 UI")]
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI currentBarText;
    public TextMeshProUGUI dspTimeText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI highComboText;

    private float frameCount = 0;
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float updateRate = 0.5f;
    private float nextUpdate = 0.0f;

    private void Awake()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, "detailed_log.txt");

        logBuilder = new StringBuilder();
        logQueue = new Queue<string>();
        logEntries = new List<LogEntry>();
    }

    private void Start()
    {
        SubscribeToLogs();
        isInitialized = true;
    }

    private void OnEnable()
    {
        if (saveLogButton != null)
        {
            saveLogButton.onClick.AddListener(SaveLogs);
        }
    }

    private void OnDisable()
    {
        if (saveLogButton != null)
        {
            saveLogButton.onClick.RemoveListener(SaveLogs);
        }

        UnsubscribeFromLogs();
    }

    private void OnDestroy()
    {
        ClearLogs();
        logBuilder = null;
        logQueue = null;
        logEntries = null;
        isInitialized = false;
    }

    private void SubscribeToLogs()
    {
        if (!isSubscribed)
        {
            Application.logMessageReceived += HandleLog;
            isSubscribed = true;
        }
    }

    private void UnsubscribeFromLogs()
    {
        if (isSubscribed)
        {
            Application.logMessageReceived -= HandleLog;
            isSubscribed = false;
        }
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        if (logBuilder == null)
            logBuilder = new StringBuilder();
        if (logEntries == null)
            logEntries = new List<LogEntry>();
        if (logQueue == null)
            logQueue = new Queue<string>();

        LogEntry entry = new LogEntry
        {
            Message = message,
            StackTrace = stackTrace,
            Type = type,
            Timestamp = System.DateTime.Now,
        };

        logEntries.Add(entry);

        try
        {
            CreateLogBox(entry.GetDisplayLog(), type);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"로그 박스 생성 중 오류 발생: {e.Message}");
        }
    }

    private void CreateLogBox(string message, LogType type)
    {
        if (logBoxPrefab != null && logBoxParent != null)
        {
            LogBox newLogBox = Instantiate(logBoxPrefab, logBoxParent);
            newLogBox.Initialize(message, type);
        }
    }

    public void ClearLogs()
    {
        try
        {
            if (logBoxParent != null)
            {
                foreach (Transform child in logBoxParent)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }

            if (logQueue != null)
                logQueue.Clear();
            if (logEntries != null)
                logEntries.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"로그 정리 중 오류 발생: {e.Message}");
        }
    }

    public void SaveLogs()
    {
        try
        {
            if (logEntries == null || logEntries.Count == 0)
            {
                Debug.LogWarning("저장할 로그가 없습니다.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== Detailed Log File ===");
            sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Application: {Application.productName} v{Application.version}");
            sb.AppendLine("=============================\n");

            foreach (var entry in logEntries)
            {
                sb.Append(entry.GetDetailedLog());
            }

            File.WriteAllText(logFilePath, sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로그 저장 중 오류 발생: {e.Message}");
        }
    }

    private void Update()
    {
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;

        if (Time.unscaledTime > nextUpdate)
        {
            nextUpdate = Time.unscaledTime + updateRate;
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0.0f;

            UpdateFpsText();
        }

        UpdateGameInfoText();
    }

    private void UpdateFpsText()
    {
        if (fpsText != null)
        {
            fpsText.text = $"FPS: {fps:F1}";
        }
    }

    private void UpdateGameInfoText()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            if (currentBarText != null)
            {
                int currentBar = GameManager.Instance.CurrentBar;
                int currentBeat = GameManager.Instance.CurrentBeat;
                currentBarText.text = $"현재 위치: 마디 {currentBar + 1}, 비트 {currentBeat + 1}";
            }

            if (dspTimeText != null)
            {
                double startDspTime = GameManager.Instance.StartDspTime;
                double currentDspTime = AudioSettings.dspTime;
                dspTimeText.text = $"DSP 경과 시간: {(currentDspTime - startDspTime):F3}초";
            }

            var scoreSystem = GameManager.Instance.ScoreSystem;
            if (scoreSystem != null)
            {
                if (scoreText != null)
                {
                    int currentScore = (int)scoreSystem.currentScore;
                    scoreText.text = $"현재 점수: {currentScore}";
                }

                if (comboText != null)
                {
                    int combo = scoreSystem.combo;
                    comboText.text = $"콤보: {combo}";
                }

                if (highComboText != null)
                {
                    int highCombo = scoreSystem.highCombo;
                    highComboText.text = $"최고 콤보: {highCombo}";
                }
            }
        }
    }
}
