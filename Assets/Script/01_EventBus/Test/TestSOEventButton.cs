using UnityEngine;
using UnityEngine.UI;

public class TestSOEventButton : MonoBehaviour
{
    public TestSO testSO;

    public void OnClick()
    {
        testSO.RaiseEvent();
    }
}
