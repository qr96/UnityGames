using System;
using UnityEngine;

public class TriggerEvent : MonoBehaviour
{
    Action<Collider> onTriggerEnter;
    Action<Collider> onTriggerExit;

    public void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }

    public void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke(other);
    }

    public void Set(Action<Collider> onEnter, Action<Collider> onExit)
    {
        onTriggerEnter = onEnter;
        onTriggerExit = onExit;
    }
}
