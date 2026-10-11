using UnityEngine;

/// <summary>触屏轨道相机：单指拖动旋转，双指缩放，静置 5 秒自动慢旋转。</summary>
public class TouchOrbit : MonoBehaviour
{
    public Transform target;
    public float distance = 17f;
    public float minDistance = 8f;
    public float maxDistance = 26f;
    float yaw = 0f, pitch = 22f;
    float idleTime;

    void LateUpdate()
    {
        if (target == null) return;

        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                yaw -= t.deltaPosition.x * 0.25f;
                pitch = Mathf.Clamp(pitch + t.deltaPosition.y * 0.2f, 8f, 60f);
                idleTime = 0f;
            }
        }
        else if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
            var prev = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            var cur = t0.position - t1.position;
            distance = Mathf.Clamp(distance - (cur.magnitude - prev.magnitude) * 0.02f, minDistance, maxDistance);
            idleTime = 0f;
        }
        else
        {
            // 鼠标（编辑器里调试用）
            if (Input.GetMouseButton(0))
            {
                yaw -= Input.GetAxis("Mouse X") * 4f;
                pitch = Mathf.Clamp(pitch + Input.GetAxis("Mouse Y") * 3f, 8f, 60f);
                idleTime = 0f;
            }
            idleTime += Time.deltaTime;
            if (idleTime > 5f) yaw += Time.deltaTime * 6f; // 自动慢旋转
        }

        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position + rot * new Vector3(0f, 0f, -distance);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    /// <summary>把镜头焦点切到某只精灵。</summary>
    public void FocusOn(Transform t)
    {
        target = t;
        distance = 7f;
        idleTime = 0f;
    }

    public void FocusOrigin(Transform origin)
    {
        target = origin;
        distance = 17f;
        idleTime = 0f;
    }
}
