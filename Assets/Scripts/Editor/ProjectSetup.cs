using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 一键搭建：菜单「精灵收集 / 一键搭建场景」。
/// 整个场景由代码生成，保证版本可控、headless 可重跑。
/// </summary>
public static class ProjectSetup
{
    [MenuItem("精灵收集 / 一键搭建场景")]
    public static void BuildScene()
    {
        AssetDatabase.Refresh();

        // ---- 取 GLB 预制体（glTFast 在 Editor 里把 .glb 导入为 GameObject）----
        // 注意：占位体必须在 NewScene() 之后创建，否则会被新场景清空
        AssetDatabase.Refresh();
        var p0 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/taolingshou.glb");
        var p1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/jinweimo.glb");
        var p2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/fuyaoling.glb");
        Debug.Log($"GLB 加载状态: taolingshou={p0 != null}, jinweimo={p1 != null}, fuyaoling={p2 != null}");
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansSC.ttf");
        if (!font) Debug.LogWarning("没找到 NotoSansSC.ttf，中文会显示为方框。");

        // ---- 新场景 ----
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        // GLB 没导进来就用占位体（云构建机常见），保证场景一定能生成
        if (!p0) { Debug.LogWarning("taolingshou.glb 未导入，用占位体"); p0 = MakePlaceholder("苔铃兽占位", new Color(0.35f, 0.62f, 0.32f)); }
        if (!p1) { Debug.LogWarning("jinweimo.glb 未导入，用占位体"); p1 = MakePlaceholder("烬尾貘占位", new Color(0.75f, 0.35f, 0.22f)); }
        if (!p2) { Debug.LogWarning("fuyaoling.glb 未导入，用占位体"); p2 = MakePlaceholder("浮鳐灵占位", new Color(0.30f, 0.55f, 0.85f)); }
        // 删掉默认的 Main Camera / Directional Light（环境脚本自己建太阳）
        foreach (var go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (go.name == "Main Camera" || go.name == "Directional Light")
                Object.DestroyImmediate(go);
        }

        // ---- 相机 ----
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 55f;
        var orbit = camGo.AddComponent<TouchOrbit>();
        orbit.distance = 17f; orbit.minDistance = 8f; orbit.maxDistance = 26f;

        var origin = new GameObject("CameraOrigin");
        origin.transform.position = new Vector3(0f, 1.5f, 0f);
        orbit.target = origin.transform;

        // ---- 玩家（WASD 走路）----
        var playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(0f, 1.2f, 5f); // 出生在地面上，避免胶囊体初始陷入地面
        // 小人占位：胶囊体 + 简单材质
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(playerGo.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        var bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(0.95f, 0.55f, 0.25f); // 橙色小人
        body.GetComponent<Renderer>().material = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>()); // 用 CharacterController 碰撞
        var pc = playerGo.AddComponent<PlayerController>();
        pc.cameraPivot = camGo.transform;
        pc.speed = 6f;
        // 镜头跟随玩家（TouchOrbit 统一驱动相机）
        orbit.followTarget = playerGo.transform;
        orbit.originTarget = origin.transform;

        // ---- 环境 ----
        AnimeEnvironment.Build();

        // ---- EventSystem（没它按钮点不了）----
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ---- GameManager + 精灵定义 ----
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.orbit = orbit;
        gm.originTarget = origin.transform;
        gm.defs.Add(new CreatureDef
        {
            id = "taolingshou", name = "苔铃兽", type = "草系",
            typeColor = new Color(0.35f, 0.62f, 0.32f),
            desc = "头顶木筒角、颈系琥珀铃的苔藓兽。据说它走过的地方，第二天会冒出小蘑菇。",
            prefab = p0
        });
        gm.defs.Add(new CreatureDef
        {
            id = "jinweimo", name = "烬尾貘", type = "火系",
            typeColor = new Color(0.75f, 0.35f, 0.22f),
            desc = "身披熔岩铠甲的貘，尾巴尖燃着不灭的火光。发怒时铠甲缝隙会透出岩浆般的光。",
            prefab = p1
        });
        gm.defs.Add(new CreatureDef
        {
            id = "fuyaoling", name = "浮鳐灵", type = "水系",
            typeColor = new Color(0.30f, 0.55f, 0.85f),
            desc = "驾着水环的鳐灵，性情温和。鳍尖滴落的水珠落在地上会变成小珍珠。",
            prefab = p2
        });

        // ---- UI + 孵化 ----
        var dexGo = new GameObject("DexUI");
        var dex = dexGo.AddComponent<DexUI>();
        var eggGo = new GameObject("EggHatch");
        var egg = eggGo.AddComponent<EggHatch>();
        dex.Init(gm, egg, font, cam);

        // ---- 开发期屏显日志（正式版删掉此组件即可）----
        var dbgGo = new GameObject("DebugConsole");
        dbgGo.AddComponent<DebugConsole>();

        // ---- 保存 ----
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        Debug.Log("场景搭建完成：Assets/Scenes/Main.unity");
    }

    /// <summary>
    /// GLB 导不进来时的占位精灵：彩色胶囊体，免得场景生成失败。
    /// </summary>
    static GameObject MakePlaceholder(string name, Color color)
    {
        var go = new GameObject(name);
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.SetParent(go.transform);
        capsule.transform.localPosition = Vector3.up * 1f;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        capsule.GetComponent<Renderer>().material = mat;
        // 删掉占位体自己的 collider（避免和生成逻辑冲突），保留外层 GameObject
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        return go;
    }
}
