using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>游戏 UI：标题、精灵切换按钮、图鉴卡、孵化按钮、状态提示。纯代码搭建 uGUI。</summary>
public class DexUI : MonoBehaviour
{
    [Header("场景引用（Setup 时填充；公开字段会序列化进场景，运行时直接可用）")]
    public GameManager gm;
    public EggHatch hatch;
    public Font font;
    public Camera cam;

    [Header("UI 引用（BuildUI 生成后自动填充，运行时直接可用）")]
    public GameObject cardPanel;
    public Text cardName, cardType, cardDesc;
    public Text statusText;
    [Header("按钮（BuildUI 填充；监听在 Start 里重建，AddListener 的 lambda 不会序列化进场景）")]
    public List<UnityEngine.UI.Button> creatureButtons = new List<UnityEngine.UI.Button>();
    public UnityEngine.UI.Button hatchButton;
    public UnityEngine.UI.Button closeButton;
    bool uiBusy; // 点在 UI 上时不触发 3D 点击

    public void Init(GameManager g, EggHatch h, Font f, Camera c)
    {
        gm = g; hatch = h; font = f; cam = c;
        BuildUI();
        hatch.Init(gm, statusText);
    }

    // ---------- UI 搭建 ----------
    Texture2D whiteTex;
    Texture2D WhiteTex()
    {
        if (!whiteTex) { whiteTex = new Texture2D(1, 1); whiteTex.SetPixel(0, 0, Color.white); whiteTex.Apply(); }
        return whiteTex;
    }

    Text MakeText(Transform parent, string s, int size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent);
        var t = go.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = anchor;
        t.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        t.rectTransform.anchoredPosition = Vector2.zero;
        t.rectTransform.sizeDelta = new Vector2(900f, size * 1.6f);
        return t;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bg, int fontSize = 40)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent);
        var img = go.AddComponent<Image>();
        img.sprite = Sprite.Create(WhiteTex(), new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        img.type = Image.Type.Sliced;
        img.color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var btn = go.AddComponent<Button>();
        var t = MakeText(go.transform, label, fontSize, Color.white);
        t.rectTransform.sizeDelta = size;
        // 按压反馈
        var colors = btn.colors;
        colors.pressedColor = bg * 0.85f;
        btn.colors = colors;
        return btn;
    }

    GameObject MakePanel(Transform parent, Vector2 size, Color bg)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent);
        var img = go.AddComponent<Image>();
        img.sprite = Sprite.Create(WhiteTex(), new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        img.color = bg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
        return go;
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var uiRoot = canvasGo.transform;

        // 标题
        var title = MakeText(uiRoot, "精灵收集", 64, new Color(0.25f, 0.22f, 0.18f));
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f); title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -90f);
        var sub = MakeText(uiRoot, "点一只精灵查看图鉴 · 双指缩放", 32, new Color(0.55f, 0.52f, 0.47f));
        sub.rectTransform.anchorMin = new Vector2(0.5f, 1f); sub.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        sub.rectTransform.anchoredPosition = new Vector2(0f, -160f);

        // 状态提示
        statusText = MakeText(uiRoot, "", 36, new Color(0.35f, 0.30f, 0.25f));
        statusText.rectTransform.anchorMin = new Vector2(0.5f, 1f); statusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, -230f);

        // 底部按钮区
        var names = new[] { "苔铃兽", "烬尾貘", "浮鳐灵" };
        var btnColors = new[] { new Color(0.35f, 0.62f, 0.32f), new Color(0.75f, 0.35f, 0.22f), new Color(0.30f, 0.55f, 0.85f) };
        creatureButtons.Clear();
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var b = MakeButton(uiRoot, names[i], new Vector2(-320f + i * 320f, -1560f), new Vector2(280f, 110f), btnColors[i]);
            b.onClick.AddListener(() => { if (gm != null) gm.FocusCreature(idx); });
            creatureButtons.Add(b);
        }
        hatchButton = MakeButton(uiRoot, "孵一颗蛋", new Vector2(0f, -1720f), new Vector2(560f, 130f),
            new Color(0.976f, 0.451f, 0.086f), 46); // 品牌橙
        hatchButton.onClick.AddListener(() => { if (hatch != null) hatch.OnHatchButton(); });

        // 图鉴卡（默认隐藏）
        cardPanel = MakePanel(uiRoot, new Vector2(860f, 900f), new Color(1f, 1f, 1f, 0.97f));
        cardPanel.SetActive(false);
        // 卡片顶部色带
        var band = MakePanel(cardPanel.transform, new Vector2(860f, 200f), new Color(0.35f, 0.62f, 0.32f));
        band.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 350f);
        cardName = MakeText(cardPanel.transform, "", 72, Color.white);
        cardName.rectTransform.anchoredPosition = new Vector2(0f, 350f);
        cardType = MakeText(cardPanel.transform, "", 40, new Color(0.4f, 0.38f, 0.34f));
        cardType.rectTransform.anchoredPosition = new Vector2(0f, 180f);
        cardDesc = MakeText(cardPanel.transform, "", 38, new Color(0.35f, 0.32f, 0.28f), TextAnchor.UpperCenter);
        cardDesc.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        cardDesc.rectTransform.sizeDelta = new Vector2(740f, 420f);
        var close = MakeButton(cardPanel.transform, "关闭", new Vector2(0f, -360f), new Vector2(300f, 110f),
            new Color(0.55f, 0.52f, 0.48f));
        close.onClick.AddListener(() => { if (cardPanel != null) cardPanel.SetActive(false); });
        closeButton = close;
    }

    /// <summary>
    /// 运行时重建按钮监听：AddListener 注册的 lambda 不会序列化进场景，
    /// 构建包里按钮默认是"哑"的，这里在 Start 重新接上（先清后加，保证幂等）。
    /// </summary>
    void Start()
    {
        for (int i = 0; i < creatureButtons.Count; i++)
        {
            int idx = i;
            var b = creatureButtons[idx];
            if (b == null) continue;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { if (gm != null) gm.FocusCreature(idx); });
        }
        if (hatchButton != null)
        {
            hatchButton.onClick.RemoveAllListeners();
            hatchButton.onClick.AddListener(() => { if (hatch != null) hatch.OnHatchButton(); });
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => { if (cardPanel != null) cardPanel.SetActive(false); });
        }
        DebugConsole.Add($"UI 就绪：精灵按钮 {creatureButtons.Count} 个", Color.green);
    }

    public void ShowCard(CreatureDef d)
    {
        cardPanel.SetActive(true);
        cardName.text = d.name;
        cardType.text = d.type;
        cardDesc.text = d.desc;
        cardPanel.transform.GetChild(0).GetComponent<Image>().color = d.typeColor;
    }

    // ---------- 点击精灵 ----------
    void Update()
    {
        bool tapped = false;
        Vector2 pos = Vector2.zero;
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        { tapped = true; pos = Input.GetTouch(0).position; }
#else
        if (Input.GetMouseButtonDown(0)) { tapped = true; pos = Input.mousePosition; }
#endif
        if (!tapped || cardPanel.activeSelf) return;
        // 点在 UI 上就不往 3D 透传
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
#endif
        var ray = cam.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit, 100f))
        {
            var tag = hit.collider.GetComponentInParent<CreatureTag>();
            if (tag != null && tag.def != null) ShowCard(tag.def);
        }
    }
}
