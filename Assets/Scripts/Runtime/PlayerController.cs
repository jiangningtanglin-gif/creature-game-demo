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

        // 相机跟随
        if (cameraPivot != null)
        {
            var targetPos = transform.position + new Vector3(0, 7f, -10f);
            cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, Time.deltaTime * 5f);
            cameraPivot.LookAt(transform.position + Vector3.up * 1.5f);
        }
    }
}
