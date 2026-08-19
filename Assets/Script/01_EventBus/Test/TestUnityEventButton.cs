using UnityEngine;
using UnityEngine.UI;

public class TestUnityEventButton : MonoBehaviour
{
    public TestEventCube Cube;

    public void OnClick()
    {
        Cube.OnUIClickEvent?.Invoke();
    }
}
