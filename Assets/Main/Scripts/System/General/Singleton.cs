using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public abstract class Singleton<T> : MonoBehaviour
    where T : MonoBehaviour
{
    protected static T instance;
    protected static bool applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                Debug.Log($"[{typeof(T)}] Creating new instance \n{GetCallStack()}");
                var go = new GameObject(typeof(T).Name);
                instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    private static string GetCallStack()
    {
        StackTrace stackTrace = new StackTrace(2, true);

        string callStack = "Call Stack :";

        for (int i = 0; i < stackTrace.FrameCount && i < 10; i++)
        {
            StackFrame frame = stackTrace.GetFrame(i);
            string fileName = frame.GetFileName();
            int lineNumber = frame.GetFileLineNumber();
            string methodName = frame.GetMethod().Name;
            string className = frame.GetMethod().DeclaringType?.Name;

            if (string.IsNullOrEmpty(fileName))
            {
                callStack += $"\n  at {className}.{methodName}()";
            }
            else
            {
                string shortFileName = Path.GetFileName(fileName);
                callStack +=
                    $"\n  at {className}.{methodName}() in {shortFileName}:line {lineNumber}";
            }
        }

        return callStack;
    }
}
