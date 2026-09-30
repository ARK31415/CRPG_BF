using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent 教程协调器：持有纯 C# 教程数据服务，订阅内容场景成功变更事实，
/// 决定本次运行的首次自动显示与手动重看，并请求 BF_UIManager 显示。
/// Catalog 不可用时本次运行停用教程功能，只记录一次 Error，不阻断游戏流程。
/// </summary>
public class BF_TutorialManager : Singleton<BF_TutorialManager>
{
    #region 序列化配置与引用

    [Header("教程数据")]
    [SerializeField]
    private TextAsset _catalogAsset;

    #endregion

    #region 运行时数据

    private readonly BF_TutorialService _service = new();
    private readonly HashSet<string> _shownTutorialIds = new();
    private IDisposable _contentSceneSubscription;
    private IDisposable _introTutorialRequestSubscription;
    private IDisposable _blockingPresentationSubscription;
    private string _currentSceneAddress = string.Empty;
    private string _currentTutorialId = string.Empty;
    private bool _bindErrorReported;
    private bool _introTutorialPending;

    #endregion

    #region 对外状态

    // 当前内容场景地址；无内容场景时为空字符串。
    public string CurrentSceneAddress => _currentSceneAddress;

    #endregion

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        if (!_service.TryInitialize(_catalogAsset, out string failureReason))
        {
            // 不阻断启动：只停用本次运行的教程能力。
            Debug.LogError($"[BF_TutorialManager] 教程 Catalog 不可用，本次运行停用教程功能：{failureReason}");
        }
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _contentSceneSubscription = GameEventBus.Instance.Subscribe<BF_ContentSceneChangedEvent>(OnContentSceneChanged);
        _introTutorialRequestSubscription = GameEventBus.Instance.Subscribe<BF_IntroTutorialRequestEvent>(OnIntroTutorialRequested);
        _blockingPresentationSubscription = GameEventBus.Instance.Subscribe<BF_BlockingPresentationChangedEvent>(OnBlockingPresentationChanged);
    }

    private void OnDisable()
    {
        _contentSceneSubscription?.Dispose();
        _contentSceneSubscription = null;
        _introTutorialRequestSubscription?.Dispose();
        _introTutorialRequestSubscription = null;
        _blockingPresentationSubscription?.Dispose();
        _blockingPresentationSubscription = null;
    }

    #endregion

    #region 对外接口

    /// <summary>
    /// 手动重看当前场景教程；不改变首次显示集合，也不执行任何额外业务。
    /// </summary>
    public void OpenCurrentTutorial()
    {
        if (!_service.TryGetById(_currentTutorialId, out BF_TutorialDefinition definition))
        {
            return;
        }

        TryShow(definition);
    }

    #endregion

    #region 内容场景事件

    private void OnContentSceneChanged(BF_ContentSceneChangedEvent sceneEvent)
    {
        _currentSceneAddress = sceneEvent.Address;

        if (!_service.TryGetByScene(sceneEvent.Address, out BF_TutorialDefinition definition))
        {
            // 没有对应教程属于正常情况：清空当前教程并保持静默。
            _currentTutorialId = string.Empty;
            return;
        }

        _currentTutorialId = definition.tutorialId;

        // 战斗教程由 BF_BattleIntroFlow 在标题卡结束后显式请求，禁止在场景切换时抢跑。
        if (IsBattleScene(sceneEvent.Address)
            || !definition.autoShow
            || _shownTutorialIds.Contains(definition.tutorialId))
        {
            return;
        }

        if (TryShow(definition))
        {
            // 只有 UI 真正显示成功后才记录；失败保留后续重试机会。
            _shownTutorialIds.Add(definition.tutorialId);
        }
    }

    #endregion

    #region 战斗入场教程

    private void OnIntroTutorialRequested(BF_IntroTutorialRequestEvent requestEvent)
    {
        if (_introTutorialPending)
        {
            return;
        }

        if (!_service.TryGetById(_currentTutorialId, out BF_TutorialDefinition definition)
            || !definition.autoShow
            || _shownTutorialIds.Contains(definition.tutorialId))
        {
            PublishIntroTutorialCompleted();
            return;
        }

        if (!TryShow(definition))
        {
            PublishIntroTutorialCompleted();
            return;
        }

        _shownTutorialIds.Add(definition.tutorialId);
        _introTutorialPending = true;
    }

    private void OnBlockingPresentationChanged(BF_BlockingPresentationChangedEvent presentationEvent)
    {
        if (!_introTutorialPending
            || presentationEvent.Presentation != BF_BlockingPresentation.Tutorial
            || presentationEvent.IsOpen)
        {
            return;
        }

        _introTutorialPending = false;
        PublishIntroTutorialCompleted();
    }

    private static void PublishIntroTutorialCompleted()
    {
        GameEventBus.Instance.Publish(new BF_IntroTutorialCompletedEvent());
    }

    private static bool IsBattleScene(string sceneAddress)
    {
        return !string.IsNullOrEmpty(sceneAddress)
            && sceneAddress.StartsWith("Battle_Level_", StringComparison.Ordinal);
    }

    #endregion

    #region 内部

    private bool TryShow(BF_TutorialDefinition definition)
    {
        BF_UIManager uiManager = BF_UIManager.Instance;
        if (uiManager == null)
        {
            ReportBindErrorOnce("BF_UIManager 缺失");
            return false;
        }

        var viewData = new BF_TutorialViewData(definition.title, definition.body);
        if (!uiManager.TryShowTutorial(viewData))
        {
            ReportBindErrorOnce("BF_TutorialPanel 缺失或未成功显示");
            return false;
        }

        return true;
    }

    // 同一类绑定错误只报告一次，避免每次场景切换反复刷屏。
    private void ReportBindErrorOnce(string reason)
    {
        if (_bindErrorReported)
        {
            return;
        }

        _bindErrorReported = true;
        Debug.LogError($"[BF_TutorialManager] 教程显示绑定失败（本次运行只报告一次）：{reason}");
    }

    #endregion
}
