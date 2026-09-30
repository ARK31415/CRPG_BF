using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 对话 View：负责稳定角色槽位、逐字显示、按住快进、跳过确认与动态输入提示。
/// 只发布用户意图，不推进节点、不保存业务状态。
/// </summary>
public class BF_DialoguePanel : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_Text _speakerText;
    [SerializeField] private TMP_Text _bodyText;
    [SerializeField] private Button _advanceButton;
    [SerializeField] private Transform _choiceContainer;
    [SerializeField] private BF_DialogueChoiceButton _choiceButtonPrefab;

    [Header("立绘槽")]
    [SerializeField] private GameObject _portraitLayer;
    [SerializeField] private Image _portraitLeft;
    [SerializeField] private Image _portraitRight;

    [Header("操作栏")]
    [SerializeField] private Button _fastForwardButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private GameObject _fastForwardActiveIndicator;
    [SerializeField] private TMP_Text _primaryActionGlyph;
    [SerializeField] private TMP_Text _primaryActionLabel;
    [SerializeField] private TMP_Text _fastForwardGlyph;
    [SerializeField] private TMP_Text _fastForwardLabel;
    [SerializeField] private TMP_Text _skipGlyph;
    [SerializeField] private TMP_Text _skipLabel;

    [Header("跳过确认")]
    [SerializeField] private GameObject _skipConfirmPanel;
    [SerializeField] private Button _confirmSkipButton;
    [SerializeField] private Button _cancelSkipButton;
    [SerializeField] private TMP_Text _confirmSkipGlyph;
    [SerializeField] private TMP_Text _confirmSkipLabel;
    [SerializeField] private TMP_Text _cancelSkipGlyph;
    [SerializeField] private TMP_Text _cancelSkipLabel;

    [Header("逐字与快进")]
    [Min(1f)] [SerializeField] private float _typingCharactersPerSecond = 45f;
    [Min(1f)] [SerializeField] private float _fastForwardMultiplier = 5f;
    [Min(0f)] [SerializeField] private float _fastForwardAdvanceDelay = 0.12f;

    private static readonly Color PortraitDimmedTint = new(0.5f, 0.5f, 0.5f, 1f);

    private readonly List<BF_DialogueChoiceButton> _choiceButtons = new();
    private IDisposable _startedSubscription;
    private IDisposable _nodeChangedSubscription;
    private Coroutine _typingRoutine;
    private Coroutine _autoAdvanceRoutine;
    private BF_InputManager _boundInputManager;
    private string _fullText = string.Empty;
    private bool _isTyping;
    private bool _hasChoices;
    private bool _fastForwardHeld;
    private bool _skipConfirmOpen;
    private bool _skipRequestPublished;

    public bool IsOpen => _panelRoot != null && _panelRoot.activeInHierarchy;
    public bool IsSkipConfirmOpen => IsOpen && _skipConfirmOpen;

    private void OnEnable()
    {
        _startedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueStartedEvent>(
            startedEvent => ApplySnapshot(startedEvent.Snapshot));
        _nodeChangedSubscription = GameEventBus.Instance.Subscribe<BF_DialogueNodeChangedEvent>(
            changedEvent => ApplySnapshot(changedEvent.Snapshot));

        _advanceButton?.onClick.AddListener(OnAdvanceClicked);
        _skipButton?.onClick.AddListener(RequestSkipConfirmation);
        _confirmSkipButton?.onClick.AddListener(ConfirmSkip);
        _cancelSkipButton?.onClick.AddListener(CancelSkipConfirmation);
        BindInputManager();
    }

    private void OnDisable()
    {
        _startedSubscription?.Dispose();
        _startedSubscription = null;
        _nodeChangedSubscription?.Dispose();
        _nodeChangedSubscription = null;

        _advanceButton?.onClick.RemoveListener(OnAdvanceClicked);
        _skipButton?.onClick.RemoveListener(RequestSkipConfirmation);
        _confirmSkipButton?.onClick.RemoveListener(ConfirmSkip);
        _cancelSkipButton?.onClick.RemoveListener(CancelSkipConfirmation);
        UnbindInputManager();
        StopPresentationRoutines();
        ClearChoices();
    }

    public void Open()
    {
        _skipRequestPublished = false;
        _panelRoot?.SetActive(true);
        BindInputManager();
        SetSkipConfirmVisible(false);
        RefreshPrompts();
    }

    public void Close()
    {
        StopPresentationRoutines();
        ClearChoices();
        SetSkipConfirmVisible(false);
        _panelRoot?.SetActive(false);
    }

    /// <summary>由唯一 Esc 路由调用；确认框优先关闭，否则打开确认框。</summary>
    public bool TryHandleCancel()
    {
        if (!IsOpen)
        {
            return false;
        }

        if (_skipConfirmOpen)
        {
            CancelSkipConfirmation();
        }
        else
        {
            RequestSkipConfirmation();
        }

        return true;
    }

    private void ApplySnapshot(BF_DialogueNodeSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        SetSkipConfirmVisible(false);
        _fullText = snapshot.Text ?? string.Empty;
        _hasChoices = snapshot.HasChoices;
        if (_hasChoices)
        {
            EndFastForward();
        }

        ApplyPortraitsAndSpeaker(snapshot);
        RebuildChoices(snapshot);
        StartTyping();
        RefreshPrompts();

        if (!_hasChoices && _advanceButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_advanceButton.gameObject);
        }
    }

    private void ApplyPortraitsAndSpeaker(BF_DialogueNodeSnapshot snapshot)
    {
        string[] participants = snapshot.ParticipantUnitIds ?? Array.Empty<string>();
        string leftUnitId = participants.Length > 0 ? participants[0] : string.Empty;
        string rightUnitId = participants.Length > 1 ? participants[1] : string.Empty;
        bool hasLeft = !string.IsNullOrWhiteSpace(leftUnitId);
        bool hasRight = !string.IsNullOrWhiteSpace(rightUnitId);

        BF_UnitConfigSO speakerConfig = ResolveUnit(snapshot.SpeakerUnitId);
        if (_speakerText != null)
        {
            _speakerText.text = speakerConfig != null
                ? speakerConfig.DisplayName
                : snapshot.SpeakerName ?? string.Empty;
        }

        _portraitLayer?.SetActive(hasLeft || hasRight);
        SetPortrait(_portraitLeft, hasLeft, ResolveUnit(leftUnitId)?.Portrait, snapshot.SpeakerUnitId == leftUnitId);
        SetPortrait(_portraitRight, hasRight, ResolveUnit(rightUnitId)?.Portrait, snapshot.SpeakerUnitId == rightUnitId);
    }

    private static BF_UnitConfigSO ResolveUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return null;
        }

        return BF_BattleService.Instance != null
            ? BF_BattleService.Instance.GetUnitConfig(unitId)
            : null;
    }

    private static void SetPortrait(Image image, bool visible, Sprite portrait, bool active)
    {
        if (image == null)
        {
            return;
        }

        image.gameObject.SetActive(visible);
        image.sprite = portrait;
        image.color = active ? Color.white : PortraitDimmedTint;
    }

    private void RebuildChoices(BF_DialogueNodeSnapshot snapshot)
    {
        ClearChoices();
        if (!snapshot.HasChoices || _choiceContainer == null || _choiceButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < snapshot.Choices.Length; i++)
        {
            BF_DialogueChoiceButton button = Instantiate(_choiceButtonPrefab, _choiceContainer);
            button.Setup(snapshot.Choices[i].ChoiceId, snapshot.Choices[i].Text, OnChoiceSelected);
            if (button.Button != null)
            {
                button.Button.interactable = false;
            }
            _choiceButtons.Add(button);
        }

        if (_choiceButtons.Count > 0 && _advanceButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_advanceButton.gameObject);
        }
    }

    private void ClearChoices()
    {
        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (_choiceButtons[i] != null)
            {
                Destroy(_choiceButtons[i].gameObject);
            }
        }

        _choiceButtons.Clear();
    }

    private void StartTyping()
    {
        StopTyping();
        StopAutoAdvance();
        if (_bodyText == null)
        {
            return;
        }

        _isTyping = true;
        _typingRoutine = StartCoroutine(TypingRoutine());
    }

    private IEnumerator TypingRoutine()
    {
        _bodyText.text = string.Empty;
        float shownCharacters = 0f;
        while (shownCharacters < _fullText.Length)
        {
            float multiplier = _fastForwardHeld ? _fastForwardMultiplier : 1f;
            shownCharacters += _typingCharactersPerSecond * multiplier * Time.unscaledDeltaTime;
            int count = Mathf.Min(Mathf.FloorToInt(shownCharacters), _fullText.Length);
            _bodyText.text = _fullText.Substring(0, count);
            yield return null;
        }

        _bodyText.text = _fullText;
        _typingRoutine = null;
        _isTyping = false;
        EnableChoicesAfterTyping();
        RefreshPrompts();
        TryStartAutoAdvance();
    }

    private void CompleteTypingImmediately()
    {
        StopTyping();
        if (_bodyText != null)
        {
            _bodyText.text = _fullText;
        }

        EnableChoicesAfterTyping();
        RefreshPrompts();
        TryStartAutoAdvance();
    }

    private void EnableChoicesAfterTyping()
    {
        if (!_hasChoices)
        {
            return;
        }

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (_choiceButtons[i] != null && _choiceButtons[i].Button != null)
            {
                _choiceButtons[i].Button.interactable = true;
            }
        }

        if (_choiceButtons.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }
    }

    public void BeginFastForward()
    {
        if (!IsOpen || _hasChoices || _skipConfirmOpen)
        {
            return;
        }

        _fastForwardHeld = true;
        _fastForwardActiveIndicator?.SetActive(true);
        TryStartAutoAdvance();
    }

    public void EndFastForward()
    {
        _fastForwardHeld = false;
        _fastForwardActiveIndicator?.SetActive(false);
        StopAutoAdvance();
    }

    private void TryStartAutoAdvance()
    {
        if (!_fastForwardHeld || _isTyping || _hasChoices || _skipConfirmOpen || _autoAdvanceRoutine != null)
        {
            return;
        }

        _autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSecondsRealtime(_fastForwardAdvanceDelay);
        _autoAdvanceRoutine = null;
        if (_fastForwardHeld && !_hasChoices && !_skipConfirmOpen && IsOpen)
        {
            GameEventBus.Instance.Publish(new BF_DialogueAdvanceRequestEvent());
        }
    }

    private void StopPresentationRoutines()
    {
        StopTyping();
        EndFastForward();
    }

    private void StopTyping()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        _isTyping = false;
    }

    private void StopAutoAdvance()
    {
        if (_autoAdvanceRoutine != null)
        {
            StopCoroutine(_autoAdvanceRoutine);
            _autoAdvanceRoutine = null;
        }
    }

    public void RequestSkipConfirmation()
    {
        if (!IsOpen || _skipRequestPublished)
        {
            return;
        }

        EndFastForward();
        SetSkipConfirmVisible(true);
        if (_cancelSkipButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_cancelSkipButton.gameObject);
        }
    }

    public void CancelSkipConfirmation()
    {
        if (!_skipConfirmOpen)
        {
            return;
        }

        SetSkipConfirmVisible(false);
        RestoreDialogueFocus();
    }

    private void ConfirmSkip()
    {
        if (!_skipConfirmOpen || _skipRequestPublished)
        {
            return;
        }

        _skipRequestPublished = true;
        SetSkipConfirmVisible(false);
        GameEventBus.Instance.Publish(new BF_DialogueSkipRequestEvent());
    }

    private void SetSkipConfirmVisible(bool visible)
    {
        _skipConfirmOpen = visible;
        _skipConfirmPanel?.SetActive(visible);
        RefreshPrompts();
    }

    private void RestoreDialogueFocus()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (_hasChoices && !_isTyping && _choiceButtons.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }
        else if (_advanceButton != null)
        {
            EventSystem.current.SetSelectedGameObject(_advanceButton.gameObject);
        }
    }

    private void OnAdvanceClicked()
    {
        if (_skipConfirmOpen)
        {
            return;
        }

        if (_isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        if (!_hasChoices)
        {
            GameEventBus.Instance.Publish(new BF_DialogueAdvanceRequestEvent());
        }
    }

    private void OnChoiceSelected(string choiceId)
    {
        if (!_skipConfirmOpen && !_isTyping)
        {
            GameEventBus.Instance.Publish(new BF_DialogueChoiceRequestEvent(choiceId));
        }
    }

    private void BindInputManager()
    {
        BF_InputManager inputManager = BF_InputManager.Instance;
        if (_boundInputManager == inputManager)
        {
            return;
        }

        UnbindInputManager();
        _boundInputManager = inputManager;
        if (_boundInputManager == null)
        {
            return;
        }

        _boundInputManager.FastForwardStarted += BeginFastForward;
        _boundInputManager.FastForwardCanceled += EndFastForward;
        _boundInputManager.BindingDisplayChanged += RefreshPrompts;
    }

    private void UnbindInputManager()
    {
        if (_boundInputManager != null)
        {
            _boundInputManager.FastForwardStarted -= BeginFastForward;
            _boundInputManager.FastForwardCanceled -= EndFastForward;
            _boundInputManager.BindingDisplayChanged -= RefreshPrompts;
        }

        _boundInputManager = null;
    }

    private void RefreshPrompts()
    {
        BF_InputManager inputManager = BF_InputManager.Instance;
        string submit = inputManager != null ? inputManager.GetSubmitBindingDisplayString() : string.Empty;
        string cancel = inputManager != null ? inputManager.GetCancelBindingDisplayString() : string.Empty;
        string fastForward = inputManager != null ? inputManager.GetFastForwardBindingDisplayString() : string.Empty;

        if (_skipConfirmOpen)
        {
            SetText(_confirmSkipGlyph, submit);
            SetText(_confirmSkipLabel, "确认跳过");
            SetText(_cancelSkipGlyph, cancel);
            SetText(_cancelSkipLabel, "继续观看");
            return;
        }

        SetText(_primaryActionGlyph, submit);
        SetText(_primaryActionLabel, _hasChoices ? "导航选择 / 确认" : _isTyping ? "显示全文" : "继续");
        SetText(_fastForwardGlyph, fastForward);
        SetText(_fastForwardLabel, "按住快进");
        SetText(_skipGlyph, cancel);
        SetText(_skipLabel, "跳过剧情");

        if (_fastForwardButton != null)
        {
            _fastForwardButton.interactable = !_hasChoices;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
