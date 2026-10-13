using UnityEngine;

/// <summary>
/// 玩家走路：WASD / 方向键在草原上走，相机跟随（第三人称）。
/// 后续可换摇杆适配手机。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float speed = 6f;
    public float turnSpeed = 10f;
    public Transform cameraPivot;

    CharacterController cc;
    Vector3 moveDir;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        // 简单小人占位：胶囊体
        if (GetComponent<CapsuleCollider>() == null)
            gameObject.AddComponent<CapsuleCollider>();
    }

    void Start()
    {
        // 开机自检：玩家渲染器数量直接决定"有没有角色"
        var renderers = GetComponentsInChildren<Renderer>();
        DebugConsole.Add($"玩家就绪：渲染器 {renderers.Length} 个 @ {transform.position}",
            renderers.Length > 0 ? Color.green : Color.red);
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 以相机朝向为基准
        Vector3 camFwd = cameraPivot != null ? cameraPivot.forward : Vector3.forward;
        camFwd.y = 0f; camFwd.Normalize();
        Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);

        moveDir = (camFwd * v + camRight * h);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        cc.SimpleMove(moveDir * speed);

        // 朝向移动方向
        if (moveDir.sqrMagnitude > 0.01f)
        {
            var want = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed);
        }
        // 注意：相机由 TouchOrbit 统一驱动（含跟随玩家），这里不再碰相机，
        // 否则 Update/LateUpdate 两处打架，相机永远跟不上玩家。
    }
}
