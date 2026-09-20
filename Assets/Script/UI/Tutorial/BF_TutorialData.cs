using System;

/// <summary>
/// 教程 Catalog 数据根（SchemaVersion 1）。
/// 由工程内 JSON/TextAsset 提供，Player 只读取工程内产物。
/// </summary>
[Serializable]
public class BF_TutorialCatalogData
{
    public int schemaVersion = 1;
    public BF_TutorialDefinition[] tutorials = Array.Empty<BF_TutorialDefinition>();
}

/// <summary>
/// 单条教程定义（只读内容模型）。
/// 第一版字段固定为 Id / SceneAddress / Title / Body / AutoShow，不预设分页或步骤。
/// </summary>
[Serializable]
public class BF_TutorialDefinition
{
    public string tutorialId;
    public string sceneAddress;
    public string title;
    public string body;
    public bool autoShow = true;
}

/// <summary>
/// 提供给教程 View 的显示数据；由 Manager 从 Definition 构造，View 不接触 Definition。
/// </summary>
public class BF_TutorialViewData
{
    public string Title { get; }
    public string Body { get; }

    public BF_TutorialViewData(string title, string body)
    {
        Title = title;
        Body = body;
    }
}
