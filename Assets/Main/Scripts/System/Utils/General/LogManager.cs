using System.Collections.Generic;
using System.IO;
using System.Text;
using Michsky.UI.Heat;
using UnityEngine;

public class LogManager : Singleton<LogManager>, IInitializable
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

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        logBuilder = new StringBuilder();
        logQueue = new Queue<string>();
        logEntries = new List<LogEntry>();
        logFilePath = Path.Combine(Application.persistentDataPath, "detailed_log.txt");

        if (!isInitialized)
        {
            SubscribeToLogs();
            isInitialized = true;
            Debug.Log("LogManager: Initialized");
            saveLogButton.onClick.AddListener(SaveLogs);
        }
    }

    private void OnEnable()
    {
        if (!isSubscribed)
        {
            SubscribeToLogs();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromLogs();
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
        {
            logBuilder = new StringBuilder();
        }

        // 로그 엔트리 생성 및 저장
        LogEntry entry = new LogEntry
        {
            Message = message,
            StackTrace = stackTrace,
            Type = type,
            Timestamp = System.DateTime.Now,
        };
        logEntries.Add(entry);

        // UI에 표시할 로그 생성
        CreateLogBox(entry.GetDisplayLog(), type);
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
        if (logBoxParent != null)
        {
            foreach (Transform child in logBoxParent)
            {
                Destroy(child.gameObject);
            }
        }
        logQueue.Clear();
        logEntries.Clear();
    }

    public void SaveLogs()
    {
        try
        {
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
            Debug.Log($"로그가 저장되었습니다: {logFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로그 저장 중 오류 발생: {e.Message}");
        }
    }
}
