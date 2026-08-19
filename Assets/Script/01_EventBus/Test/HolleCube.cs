using System;
using UnityEngine;

public class HolleCube : MonoBehaviour
{

    void Start()
    {
        GameEventBus.Instance.Publish(new TestEvent 
        { 
            Message = "Hello, GameEventBus!" 
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
