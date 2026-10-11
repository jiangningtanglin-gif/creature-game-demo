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
        var p0 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/taolingshou.glb");
        var p1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/jinweimo.glb");
        var p2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/fuyaoling.glb");
        if (!p0 || !p1 || !p2)
        {
            Debug.LogError("GLB 还没导入完成：请等 Unity 右下角导入进度走完再点一次。");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansSC.ttf");
        if (!font) Debug.LogWarning("没找到 NotoSansSC.ttf，中文会显示为方框。");

        // ---- 新场景 ----
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
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

        // ---- 保存 ----
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        Debug.Log("场景搭建完成：Assets/Scenes/Main.unity");
    }
}
