using UnityEngine;

/// <summary>精灵定义：美术资源 + 图鉴数据。由 ProjectSetup 填充。</summary>
[System.Serializable]
public class CreatureDef
{
    public string id;          // taolingshou
    public string name;        // 苔铃兽
    public string type;        // 草系
    public Color typeColor;    // 系别徽章颜色
    public string desc;        // 图鉴描述
    public GameObject prefab;  // GLB 导入后生成的预制体
}
