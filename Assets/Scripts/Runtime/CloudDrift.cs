using UnityEngine;

/// <summary>云漂移：沿 X 轴缓慢移动，飞出边界后绕回。</summary>
public class CloudDrift : MonoBehaviour
{
    public float speed = 0.4f;
    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
        if (transform.position.x > 75f) transform.position = new Vector3(-75f, transform.position.y, transform.position.z);
    }
}
