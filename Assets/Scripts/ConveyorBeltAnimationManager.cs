using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltAnimationManager : MonoBehaviour
{
    private static ConveyorBeltAnimationManager instance;
    private static readonly List<ConveyorBeltAnimation> registeredAnimations = new List<ConveyorBeltAnimation>();

    public static void Register(ConveyorBeltAnimation animation)
    {
        if (animation == null || registeredAnimations.Contains(animation)) return;
        registeredAnimations.Add(animation);
    }

    public static void Unregister(ConveyorBeltAnimation animation)
    {
        if (animation == null) return;
        registeredAnimations.Remove(animation);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject go = new GameObject("ConveyorBeltAnimationManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ConveyorBeltAnimationManager>();
    }

    private void Update()
    {
        for (int i = registeredAnimations.Count - 1; i >= 0; i--)
        {
            ConveyorBeltAnimation animation = registeredAnimations[i];
            if (animation == null)
            {
                registeredAnimations.RemoveAt(i);
                continue;
            }

            animation.ApplyScrollOffset();
        }
    }
}