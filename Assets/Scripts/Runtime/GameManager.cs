using UnityEngine;
using System.Collections.Generic;

/// <summary>游戏总控：精灵定义、出生点、镜头焦点。由 ProjectSetup 组装。</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("三只精灵定义（Setup 时填充）")]
    public List<CreatureDef> defs = new List<CreatureDef>();

    [Header("运行时")]
    public List<GameObject> spawned = new List<GameObject>();
    public TouchOrbit orbit;
    public Transform originTarget;

    void Awake() { Instance = this; }

    void Start()
    {
        // 三角站位出生
        var spots = new[] { new Vector3(-4.5f, 0f, -1f), new Vector3(4.5f, 0f, -1f), new Vector3(0f, 0f, -5f) };
        for (int i = 0; i < defs.Count && i < spots.Length; i++)
        {
            var d = defs[i];
            if (d.prefab == null) continue;
            var go = Instantiate(d.prefab, spots[i], Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.name = d.id;
            NormalizeScale(go, 1.4f);
            var w = go.AddComponent<CreatureWander>();
            w.home = Vector3.zero;
            w.wanderRadius = 9f;
            FitCollider(go);
            var tag = go.AddComponent<CreatureTag>();
            tag.def = d;
            spawned.Add(go);
        }
    }

    /// <summary>把 GLB 统一缩放到目标高度（米）。</summary>
    public static void NormalizeScale(GameObject go, float targetHeight)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        if (bounds.size.y > 0.01f)
        {
            float s = targetHeight / bounds.size.y;
            go.transform.localScale *= s;
        }
    }

    /// <summary>
    /// 按网格实际包围盒配点击碰撞盒。
    /// 取水平最大边做方形底（旋转无关），点击判定宽松一点手感更好。
    /// </summary>
    static void FitCollider(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        var ls = go.transform.lossyScale;
        float horiz = Mathf.Max(bounds.size.x / ls.x, bounds.size.z / ls.z);
        box.size = new Vector3(horiz, bounds.size.y / ls.y, horiz);
        box.center = go.transform.InverseTransformPoint(bounds.center);
    }

    public void FocusCreature(int index)
    {
        if (index >= 0 && index < spawned.Count && orbit != null)
            orbit.FocusOn(spawned[index].transform);
    }

    public void FocusOrigin()
    {
        if (orbit != null && originTarget != null) orbit.FocusOrigin(originTarget);
    }
}

/// <summary>挂在精灵根节点上，标记它对应哪只精灵（供点击识别）。</summary>
public class CreatureTag : MonoBehaviour
{
    public CreatureDef def;
}
