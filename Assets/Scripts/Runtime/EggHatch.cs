using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 精灵蛋孵化：按钮 → 蛋出现摇晃 → 孵化出随机精灵。
/// 商业化钩子：requirePayment=true 时先走支付，支付成功才调 StartHatch()。
/// 未来“定制精灵”在此处支付成功后调用 Tripo API 生成，再下载导入。
/// </summary>
public class EggHatch : MonoBehaviour
{
    [Header("支付开关：正式上线改为 true，并接入支付回调")]
    public bool requirePayment = false;

    [Header("场景引用（Setup 时填充；公开字段会序列化进场景，运行时直接可用）")]
    public GameManager gm;
    public Text statusText;
    GameObject egg;
    bool hatching;

    public void Init(GameManager g, Text status)
    {
        gm = g;
        statusText = status;
    }

    public void OnHatchButton()
    {
        if (hatching) return;
        if (requirePayment && !PaymentGranted())
        {
            SetStatus("请先完成支付再孵化");
            return;
        }
        StartHatch();
    }

    // TODO: 接入微信/支付宝支付 SDK，支付成功返回 true
    bool PaymentGranted() { return false; }

    void SetStatus(string s) { if (statusText) statusText.text = s; }

    void StartHatch()
    {
        hatching = true;
        // 蛋：白色球体拉成蛋形
        egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        egg.name = "HatchEgg";
        egg.transform.position = new Vector3(0f, 0.9f, 6f);
        egg.transform.localScale = new Vector3(1.1f, 1.45f, 1.1f);
        var m = new Material(Shader.Find("Standard"));
        m.color = new Color(0.98f, 0.95f, 0.88f);
        m.SetFloat("_Glossiness", 0.6f);
        egg.GetComponent<Renderer>().material = m;
        // 蛋斑点
        for (int i = 0; i < 6; i++)
        {
            var spot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spot.transform.SetParent(egg.transform);
            float a = i / 6f * Mathf.PI * 2f;
            spot.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.42f, (i % 3 - 1) * 0.3f, Mathf.Sin(a) * 0.42f);
            spot.transform.localScale = Vector3.one * 0.16f;
            var sm = new Material(Shader.Find("Standard"));
            sm.color = new Color(0.95f, 0.72f, 0.45f);
            spot.GetComponent<Renderer>().material = sm;
            Destroy(spot.GetComponent<Collider>());
        }
        SetStatus("精灵蛋孵化中…");
        StartCoroutine(HatchSequence());
    }

    System.Collections.IEnumerator HatchSequence()
    {
        // 摇晃 3 轮
        for (int round = 0; round < 3; round++)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.4f;
                egg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 18f) * 14f);
                egg.transform.position += Vector3.up * Mathf.Abs(Mathf.Sin(t * 18f)) * 0.008f;
                yield return null;
            }
            egg.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(0.35f);
        }
        // 孵化！
        var def = gm.defs[Random.Range(0, gm.defs.Count)];
        var baby = Instantiate(def.prefab, egg.transform.position, Quaternion.identity);
        baby.name = def.id + "_hatched";
        GameManager.NormalizeScale(baby, 1.4f);
        var w = baby.AddComponent<CreatureWander>();
        w.home = Vector3.zero; w.wanderRadius = 9f;
        var tag = baby.AddComponent<CreatureTag>(); tag.def = def;
        gm.spawned.Add(baby);
        // 出生弹跳
        var finalScale = baby.transform.localScale; // NormalizeScale 后的基准
        float st = 0f;
        while (st < 0.6f)
        {
            st += Time.deltaTime;
            float k = Mathf.Min(1f, st / 0.6f);
            float pop = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;
            baby.transform.localScale = finalScale * k * pop;
            yield return null;
        }
        baby.transform.localScale = finalScale;
        Destroy(egg);
        SetStatus("孵化成功！" + def.name + "加入了队伍");
        gm.FocusCreature(gm.spawned.Count - 1);
        hatching = false;
    }
}
