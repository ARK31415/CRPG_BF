using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 关卡标题卡 View：显示“第 N 关”，播放入场 / 停留 / 退场后发布完成事实。
/// 面板根节点缺失时安全跳过并立即发布完成，不阻塞入场流程。
/// </summary>
public class BF_LevelTitlePanel : MonoBehaviour
{
    [SerializeField]
    private GameObject _panelRoot;

    [SerializeField]
    private TMP_Text _levelText;

    [SerializeField]
    private float _enterSeconds = 0.4f;

    [SerializeField]
    private float _holdSeconds = 1.2f;

    [SerializeField]
    private float _exitSeconds = 0.4f;

    private Coroutine _routine;

    public bool IsOpen => _panelRoot != null && _panelRoot.activeInHierarchy;

    public void Play(int level)
    {
        if (_panelRoot == null)
        {
            Debug.LogError("[BF_LevelTitlePanel] 面板根节点缺失，标题卡安全跳过。");
            PublishCompleted();
            return;
        }

        if (_levelText != null)
        {
            _levelText.text = $"第{level}关";
        }

        _panelRoot.SetActive(true);
        GameEventBus.Instance.Publish(
            new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.LevelTitle, true));

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(PlayRoutine());
    }

    public void Close()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        bool wasOpen = IsOpen;
        _panelRoot?.SetActive(false);
        if (wasOpen)
        {
            GameEventBus.Instance.Publish(
                new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.LevelTitle, false));
        }
    }

    /// <summary>立即结束标题卡并发布完成事实（Esc 跳过）；重复调用无副作用。</summary>
    public void Skip()
    {
        if (!IsOpen)
        {
            return;
        }

        Close();
        PublishCompleted();
    }

    private IEnumerator PlayRoutine()
    {
        // 入场 / 停留 / 退场的具体视觉与时长在美术阶段细化；此处只保证顺序与完成事实。
        // 使用不受 timeScale 影响的计时：暂停期间标题卡仍会正常收尾，不会永久停留。
        yield return new WaitForSecondsRealtime(_enterSeconds + _holdSeconds + _exitSeconds);

        _routine = null;
        bool wasOpen = IsOpen;
        _panelRoot.SetActive(false);
        if (wasOpen)
        {
            GameEventBus.Instance.Publish(
                new BF_BlockingPresentationChangedEvent(BF_BlockingPresentation.LevelTitle, false));
        }
        PublishCompleted();
    }

    private void PublishCompleted()
    {
        GameEventBus.Instance.Publish(new BF_LevelTitleCompletedEvent());
    }
}
