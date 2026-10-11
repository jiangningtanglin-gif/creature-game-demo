using UnityEngine;

/// <summary>
/// 精灵游荡 AI：无骨骼，用程序化位移 + 呼吸浮动 + 转向表现“活着”。
/// </summary>
[RequireComponent(typeof(Collider))]
public class CreatureWander : MonoBehaviour
{
    public float wanderRadius = 9f;
    public float speed = 1.2f;
    public Vector3 home = Vector3.zero;

    Vector3 target;
    float idleTimer;
    float bobPhase;
    bool idling = true;

    void Start()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        PickTarget();
        idleTimer = Random.Range(1f, 3f);
    }

    void PickTarget()
    {
        var a = Random.Range(0f, Mathf.PI * 2f);
        var r = Random.Range(2f, wanderRadius);
        target = home + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    void Update()
    {
        // 呼吸浮动：待机时也有生命力
        bobPhase += Time.deltaTime * 2.2f;
        var p = transform.position;
        p.y = Mathf.Sin(bobPhase) * 0.06f;
        transform.position = p;

        if (idling)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f) { idling = false; PickTarget(); }
            return;
        }

        var flat = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
        if (flat.magnitude < 0.4f)
        {
            idling = true;
            idleTimer = Random.Range(2f, 5f);
            return;
        }
        var dir = flat.normalized;
        transform.position += dir * speed * Time.deltaTime;
        // 朝向移动方向（平滑转）
        var want = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * 3f);
        // 走路时的小颠簸
        transform.position += Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.05f;
    }
}
