using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 纯 C# 教程数据服务：解析、整份校验、建立索引并查询只读 Definition。
/// 不是 MonoBehaviour、不是 Singleton，由 BF_TutorialManager 创建和持有。
/// 校验失败时整份 Catalog 标记为不可用，不生成部分可用的静默结果。
/// </summary>
public class BF_TutorialService
{
    #region 索引数据

    private readonly Dictionary<string, BF_TutorialDefinition> _byTutorialId = new();
    private readonly Dictionary<string, BF_TutorialDefinition> _bySceneAddress = new();

    #endregion

    #region 对外状态

    public bool IsAvailable { get; private set; }

    #endregion

    #region 初始化

    /// <summary>
    /// 解析并校验整份 Catalog；任何一条失败都会使整份不可用。
    /// </summary>
    /// <param name="catalogAsset">工程内 Catalog TextAsset。</param>
    /// <param name="failureReason">失败原因；成功时为空字符串。</param>
    /// <returns>整份 Catalog 有效且索引建立完成为 true。</returns>
    public bool TryInitialize(TextAsset catalogAsset, out string failureReason)
    {
        failureReason = string.Empty;
        IsAvailable = false;
        _byTutorialId.Clear();
        _bySceneAddress.Clear();

        if (catalogAsset == null || string.IsNullOrWhiteSpace(catalogAsset.text))
        {
            failureReason = "Catalog TextAsset 为空";
            return false;
        }

        BF_TutorialCatalogData catalog;
        try
        {
            catalog = JsonUtility.FromJson<BF_TutorialCatalogData>(catalogAsset.text);
        }
        catch (System.Exception exception)
        {
            failureReason = $"JSON 解析失败：{exception.Message}";
            return false;
        }

        if (catalog == null)
        {
            failureReason = "JSON 解析结果为 null";
            return false;
        }

        if (catalog.schemaVersion != 1)
        {
            failureReason = $"不支持的 schemaVersion：{catalog.schemaVersion}";
            return false;
        }

        BF_TutorialDefinition[] definitions = catalog.tutorials;
        if (definitions == null || definitions.Length == 0)
        {
            failureReason = "Catalog 没有教程条目";
            return false;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            BF_TutorialDefinition definition = definitions[i];
            if (definition == null)
            {
                failureReason = $"第 {i} 条教程为 null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.tutorialId))
            {
                failureReason = $"第 {i} 条教程的 tutorialId 为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.sceneAddress))
            {
                failureReason = $"教程 {definition.tutorialId} 的 sceneAddress 为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.title))
            {
                failureReason = $"教程 {definition.tutorialId} 的 title 为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.body))
            {
                failureReason = $"教程 {definition.tutorialId} 的 body 为空";
                return false;
            }

            if (_byTutorialId.ContainsKey(definition.tutorialId))
            {
                failureReason = $"tutorialId 重复：{definition.tutorialId}";
                return false;
            }

            if (_bySceneAddress.ContainsKey(definition.sceneAddress))
            {
                failureReason = $"sceneAddress 重复（同一场景只允许一条教程）：{definition.sceneAddress}";
                return false;
            }

            _byTutorialId.Add(definition.tutorialId, definition);
            _bySceneAddress.Add(definition.sceneAddress, definition);
        }

        IsAvailable = true;
        return true;
    }

    #endregion

    #region 查询

    public bool TryGetById(string tutorialId, out BF_TutorialDefinition definition)
    {
        if (!IsAvailable || string.IsNullOrEmpty(tutorialId))
        {
            definition = null;
            return false;
        }

        return _byTutorialId.TryGetValue(tutorialId, out definition);
    }

    public bool TryGetByScene(string sceneAddress, out BF_TutorialDefinition definition)
    {
        if (!IsAvailable || string.IsNullOrEmpty(sceneAddress))
        {
            definition = null;
            return false;
        }

        return _bySceneAddress.TryGetValue(sceneAddress, out definition);
    }

    #endregion
}
