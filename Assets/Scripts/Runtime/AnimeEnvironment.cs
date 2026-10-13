using UnityEngine;

/// <summary>
/// 动漫风环境生成器：地形 / 天空 / 植被 / 远山 / 云。
/// 全部用程序化几何体拼搭，无外部美术资源，保证 headless 也能跑。
/// </summary>
public static class AnimeEnvironment
{
    static Material Mat(Color c, float smoothness = 0f)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color c, string name, Transform parent)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = Mat(c);
        return go;
    }

    public static void Build()
    {
        var root = new GameObject("Environment").transform;

        // ---- 天空：内置 Procedural Skybox，天空蓝 ----
        var sky = new Material(Shader.Find("Skybox/Procedural"));
        sky.SetFloat("_SunSize", 0.02f);
        sky.SetFloat("_SunSizeConvergence", 2f);
        sky.SetFloat("_AtmosphereThickness", 0.9f);
        sky.SetColor("_SkyTint", new Color(0.32f, 0.58f, 0.96f));
        sky.SetColor("_GroundColor", new Color(0.55f, 0.72f, 0.62f));
        sky.SetFloat("_Exposure", 1.15f);
        RenderSettings.skybox = sky;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.87f, 0.96f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 110f;
        // 三段式环境光：动漫风的通透感就靠它
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.76f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.85f, 0.80f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.55f, 0.42f);
        RenderSettings.ambientIntensity = 1.0f;

        // ---- 太阳 ----
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
        sun.transform.SetParent(root);

        // ---- 地面：大圆盘，动漫绿 ----
        var ground = Prim(PrimitiveType.Cylinder, Vector3.zero, new Vector3(90f, 0.4f, 90f),
            new Color(0.45f, 0.74f, 0.38f), "Ground", root);
        ground.transform.position = new Vector3(0f, -0.2f, 0f);

        // 深绿色块：动漫草地常见的色块
        var patchMat = new Color(0.38f, 0.66f, 0.32f);
        var rnd = new System.Random(7);
        for (int i = 0; i < 14; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 12f + (float)rnd.NextDouble() * 22f;
            float s = 4f + (float)rnd.NextDouble() * 7f;
            // 装饰色块：关掉碰撞体，玩家走路不会撞上隐形墙
            Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, -0.25f, Mathf.Sin(a) * r),
                new Vector3(s, 0.7f, s * 0.8f), patchMat, "Patch" + i, root).GetComponent<Collider>().enabled = false;
        }

        // ---- 小山包（装饰，活动区外）----
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2 + 0.4f;
            float r = 30f + (i % 2) * 6f;
            var hill = Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, -1.6f, Mathf.Sin(a) * r),
                new Vector3(14f, 5f, 12f), new Color(0.42f, 0.68f, 0.36f), "Hill" + i, root);
            hill.GetComponent<Collider>().enabled = false;
        }

        // ---- 远山：蓝灰色锥体 + 白顶 ----
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2 + 0.2f;
            float r = 62f;
            float h = 22f + (i % 3) * 7f;
            var m = Prim(PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * r, h / 2 - 2f, Mathf.Sin(a) * r),
                new Vector3(20f, h, 20f), new Color(0.45f, 0.55f, 0.68f), "Mountain" + i, root);
            m.GetComponent<Collider>().enabled = false;
            var cap = Prim(PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * r, h - 2f - h * 0.18f, Mathf.Sin(a) * r),
                new Vector3(20f * 0.36f, h * 0.36f, 20f * 0.36f), Color.white, "Snowcap" + i, root);
            cap.GetComponent<Collider>().enabled = false;
        }

        // ---- 树：圆柱树干 + 2~3 个球形树冠 ----
        for (int i = 0; i < 16; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 15f + (float)rnd.NextDouble() * 16f;
            var tp = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            var tree = new GameObject("Tree" + i);
            tree.transform.SetParent(root);
            tree.transform.position = tp;
            float s = 0.8f + (float)rnd.NextDouble() * 0.7f;
            // 树是装饰：碰撞体全关，否则树干/树冠都是隐形墙
            Prim(PrimitiveType.Cylinder, tp + new Vector3(0f, 1.2f * s, 0f), new Vector3(0.5f * s, 2.4f * s, 0.5f * s),
                new Color(0.42f, 0.30f, 0.20f), "Trunk", tree.transform).GetComponent<Collider>().enabled = false;
            var leaf = new Color(0.30f, 0.62f, 0.30f);
            Prim(PrimitiveType.Sphere, tp + new Vector3(0f, 3.1f * s, 0f), new Vector3(3.2f * s, 2.6f * s, 3.2f * s),
                leaf, "Crown1", tree.transform).GetComponent<Collider>().enabled = false;
            Prim(PrimitiveType.Sphere, tp + new Vector3(1.1f * s, 2.4f * s, 0.6f * s), new Vector3(2.0f * s, 1.7f * s, 2.0f * s),
                new Color(0.36f, 0.68f, 0.32f), "Crown2", tree.transform).GetComponent<Collider>().enabled = false;
        }

        // ---- 草丛：交叉面片，60 簇 ----
        var grassMat = Mat(new Color(0.36f, 0.68f, 0.30f));
        grassMat.SetFloat("_Mode", 2f); // cutout 备用
        for (int i = 0; i < 60; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 4f + (float)rnd.NextDouble() * 26f;
            var gp = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            var tuft = new GameObject("Grass" + i);
            tuft.transform.SetParent(root);
            tuft.transform.position = gp;
            float s = 0.5f + (float)rnd.NextDouble() * 0.5f;
            for (int k = 0; k < 2; k++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blade.transform.SetParent(tuft.transform);
                blade.transform.localPosition = new Vector3(0f, 0.35f * s, 0f);
                blade.transform.localScale = new Vector3(0.7f * s, 0.7f * s, 1f);
                blade.transform.localRotation = Quaternion.Euler(-12f, k * 90f, 0f);
                blade.GetComponent<Renderer>().material = grassMat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }
        }

        // ---- 石头 ----
        for (int i = 0; i < 9; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 8f + (float)rnd.NextDouble() * 22f;
            float s = 0.5f + (float)rnd.NextDouble() * 0.9f;
            // 石头是装饰：关掉碰撞体
            Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, 0.15f * s, Mathf.Sin(a) * r),
                new Vector3(s, s * 0.7f, s), new Color(0.62f, 0.63f, 0.60f), "Rock" + i, root).GetComponent<Collider>().enabled = false;
        }

        // ---- 花：小圆球点缀 ----
        var flowerCols = new[] { new Color(0.98f, 0.85f, 0.90f), new Color(0.99f, 0.93f, 0.55f), new Color(0.95f, 0.60f, 0.65f) };
        for (int i = 0; i < 26; i++)
        {
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 3f + (float)rnd.NextDouble() * 24f;
            Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * r, 0.22f, Mathf.Sin(a) * r),
                new Vector3(0.28f, 0.28f, 0.28f), flowerCols[i % 3], "Flower" + i, root).GetComponent<Collider>().enabled = false;
        }

        // ---- 云：白球簇，缓慢漂移 ----
        var cloudMat = Mat(Color.white);
        cloudMat.SetFloat("_Mode", 3f);
        cloudMat.SetColor("_Color", new Color(1f, 1f, 1f, 0.92f));
        for (int i = 0; i < 8; i++)
        {
            var cloud = new GameObject("Cloud" + i);
            cloud.transform.SetParent(root);
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2);
            float r = 25f + (float)rnd.NextDouble() * 35f;
            cloud.transform.position = new Vector3(Mathf.Cos(a) * r, 17f + (float)rnd.NextDouble() * 7f, Mathf.Sin(a) * r);
            int puffs = 3 + rnd.Next(3);
            for (int p = 0; p < puffs; p++)
            {
                float ps = 2.5f + (float)rnd.NextDouble() * 2.5f;
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.transform.SetParent(cloud.transform);
                puff.transform.localPosition = new Vector3((p - puffs / 2f) * 2.6f, (float)rnd.NextDouble() * 1.2f, (float)rnd.NextDouble() * 2f - 1f);
                puff.transform.localScale = new Vector3(ps * 1.4f, ps * 0.75f, ps);
                puff.GetComponent<Renderer>().material = cloudMat;
                Object.DestroyImmediate(puff.GetComponent<Collider>());
            }
            var drift = cloud.AddComponent<CloudDrift>();
            drift.speed = 0.25f + (float)rnd.NextDouble() * 0.4f;
        }
    }
}
