using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 开发期屏显日志：把异常和关键日志直接画在屏幕左上角，
/// 真机 / WebGL 出问题时不用开控制台就能看到。正式版删掉此组件即可。
/// </summary>
public class DebugConsole : MonoBehaviour
{
    struct Line
    {
        public string text;
        public Color color;
    }

    public static DebugConsole Instance;
    readonly List<Line> lines = new List<Line>();
    const int MaxLines = 8;
    bool show = true;
    GUIStyle style;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += HandleLog;
        Add("调试台就绪", Color.green);
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    public static void Add(string text, Color color)
    {
        if (Instance == null) return;
        Instance.lines.Add(new Line { text = text, color = color });
        while (Instance.lines.Count > MaxLines) Instance.lines.RemoveAt(0);
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        Color c = Color.white;
        if (type == LogType.Error || type == LogType.Exception) c = Color.red;
        else if (type == LogType.Warning) c = Color.yellow;
        Add(condition, c);
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
        }
        if (GUI.Button(new Rect(8, 8, 90, 44), show ? "日志开" : "日志关"))
            show = !show;
        if (!show) return;

        float y = 60f;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.Box(new Rect(8, y - 6, 1000, lines.Count * 30 + 12), GUIContent.none);
        GUI.color = Color.white;
        foreach (var line in lines)
        {
            style.normal.textColor = line.color;
            var t = line.text.Length > 90 ? line.text.Substring(0, 90) : line.text;
            GUI.Label(new Rect(16, y, 1200, 30), t, style);
            y += 30;
        }
    }
}
