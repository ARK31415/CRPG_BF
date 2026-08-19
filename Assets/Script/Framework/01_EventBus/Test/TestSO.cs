using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "TestSO", menuName = "Scriptable Objects/TestSO", order = 1)]
public class TestSO : ScriptableObject
{

    public UnityAction OnTestEvent;

    public void RaiseEvent()
    {
        OnTestEvent?.Invoke();
    }
}
