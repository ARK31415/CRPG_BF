using System;
using UnityEngine;
using UnityEngine.Events;

public class TestEventCube : MonoBehaviour
{
    [Header("SO事件监听")]
    public TestSO testSO;

    [Header("UnityEvent事件监听")]
    public UnityEvent OnUIClickEvent;

    // 手动订阅令牌：演示"物体活着但业务结束 → 手动 Dispose"
    private IDisposable _manualToken;

    private void OnEnable()
    {
        testSO.OnTestEvent += TestEventByUnitySO;
        // 自动解绑：物体销毁时自动退订，无需手动 Unsubscribe
        GameEventBus.Instance.Subscribe<TestEvent>(TestEventByGameEventBus).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDisable()
    {
        testSO.OnTestEvent -= TestEventByUnitySO;
    }

    [ContextMenu("手动订阅")]
    private void ManualSubscribe()
    {
        // 持有令牌，不绑生命周期（演示手动管理）
        _manualToken = GameEventBus.Instance.Subscribe<TestEvent>(TestEventByManualToken);
        Debug.Log("手动订阅成功");
    }

    [ContextMenu("手动退订")]
    private void ManualDispose()
    {
        _manualToken?.Dispose();   // 物体还活着，业务结束 → 手动退订
        _manualToken = null;
        Debug.Log("手动退订完成");
    }

    private void TestEventByManualToken(TestEvent gameEvent)
    {
        Debug.Log("手动令牌收到: " + gameEvent.Message);
    }

    private void TestEventByUnitySO()
    {
        Debug.Log(" name: " + name + " SO: " + testSO.name);
    }


    public void TestEventByUnityEvent()
    {
        Debug.Log(" name: " + name + " event: " + OnUIClickEvent.GetPersistentEventCount());
    }

    private void TestEventByGameEventBus(TestEvent gameEvent)
    {
        Debug.Log("EventBus Listener name: " + name + " EventMessage: " + gameEvent.Message);
    }
}
