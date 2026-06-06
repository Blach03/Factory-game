using System.Collections.Generic;
using UnityEngine;

public class TransportTickManager : MonoBehaviour
{
    private enum TransportQualityTier
    {
        Smooth = 0,
        Balanced = 1,
        Performance = 2,
        Emergency = 3
    }

    private static TransportTickManager instance;

    private static readonly HashSet<ConveyorBelt> conveyorRegistry = new HashSet<ConveyorBelt>();
    private static readonly Queue<ConveyorBelt> readyConveyors = new Queue<ConveyorBelt>();
    private static readonly HashSet<ConveyorBelt> scheduledConveyors = new HashSet<ConveyorBelt>();
    private static readonly Dictionary<int, List<ConveyorBelt>> delayedConveyorBuckets = new Dictionary<int, List<ConveyorBelt>>();

    private static readonly List<OverheadConveyor> overheadConveyors = new List<OverheadConveyor>();
    private static readonly List<Item> movingItems = new List<Item>();
    private static readonly HashSet<OverheadConveyor> overheadConveyorSet = new HashSet<OverheadConveyor>();
    private static readonly HashSet<Item> movingItemSet = new HashSet<Item>();

    [Header("Adaptive Quality")]
    [SerializeField] private bool useAdaptiveQuality = true;
    [SerializeField] private bool logQualityTierChanges = false;
    [SerializeField] private TransportQualityTier startTier = TransportQualityTier.Smooth;
    [SerializeField] private float frameTimeEmaSmoothing = 0.08f;
    [SerializeField] private float degradeFrameTimeMs = 27f;
    [SerializeField] private float recoverFrameTimeMs = 20f;
    [SerializeField] private float degradeHoldSeconds = 0.6f;
    [SerializeField] private float recoverHoldSeconds = 1.2f;

    [Header("Manual Fallback (used when adaptive quality is disabled)")]
    [Range(1, 4)]
    [SerializeField] private int fallbackOverheadTickStrideFrames = 2;
    [Range(1, 8)]
    [SerializeField] private int fallbackItemTickStrideFrames = 2;
    [Range(1, 8)]
    [SerializeField] private int fallbackPhysicsSyncStrideFrames = 1;

    [Header("Physics Sync")]
    [SerializeField] private bool disableAutoPhysicsSync2D = true;

    private int frameCounter;
    private bool pendingPhysicsSync;
    private float frameTimeEmaMs;
    private float timeAboveDegradeThreshold;
    private float timeBelowRecoverThreshold;
    private TransportQualityTier currentTier;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
        conveyorRegistry.Clear();
        readyConveyors.Clear();
        scheduledConveyors.Clear();
        delayedConveyorBuckets.Clear();
        overheadConveyors.Clear();
        movingItems.Clear();
        overheadConveyorSet.Clear();
        movingItemSet.Clear();
    }

    private void Awake()
    {
        currentTier = startTier;
        frameTimeEmaMs = Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime * 1000f : 16.67f;

        if (disableAutoPhysicsSync2D)
        {
            Physics2D.autoSyncTransforms = false;
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        GameObject go = new GameObject("TransportTickManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<TransportTickManager>();
    }

    public static void RegisterConveyor(ConveyorBelt conveyor)
    {
        if (conveyor == null || conveyorRegistry.Contains(conveyor)) return;
        EnsureInstance();
        conveyorRegistry.Add(conveyor);
    }

    public static void UnregisterConveyor(ConveyorBelt conveyor)
    {
        if (conveyor == null) return;
        conveyorRegistry.Remove(conveyor);
        scheduledConveyors.Remove(conveyor);
    }

    public static void RequestConveyorTick(ConveyorBelt conveyor, int delayFrames = 0)
    {
        if (conveyor == null) return;
        EnsureInstance();

        if (!conveyorRegistry.Contains(conveyor) || scheduledConveyors.Contains(conveyor))
        {
            return;
        }

        scheduledConveyors.Add(conveyor);

        if (delayFrames <= 0)
        {
            readyConveyors.Enqueue(conveyor);
            return;
        }

        int dueFrame = instance.frameCounter + delayFrames;
        if (!delayedConveyorBuckets.TryGetValue(dueFrame, out List<ConveyorBelt> bucket))
        {
            bucket = new List<ConveyorBelt>(8);
            delayedConveyorBuckets.Add(dueFrame, bucket);
        }

        bucket.Add(conveyor);
    }

    public static void RegisterOverheadConveyor(OverheadConveyor conveyor)
    {
        if (conveyor == null || overheadConveyorSet.Contains(conveyor)) return;
        EnsureInstance();
        overheadConveyors.Add(conveyor);
        overheadConveyorSet.Add(conveyor);
    }

    public static void UnregisterOverheadConveyor(OverheadConveyor conveyor)
    {
        if (conveyor == null) return;
        if (!overheadConveyorSet.Remove(conveyor)) return;
        overheadConveyors.Remove(conveyor);
    }

    public static void RegisterMovingItem(Item item)
    {
        if (item == null || movingItemSet.Contains(item)) return;
        EnsureInstance();
        movingItems.Add(item);
        movingItemSet.Add(item);
    }

    public static void UnregisterMovingItem(Item item)
    {
        if (item == null) return;
        if (!movingItemSet.Remove(item)) return;
        movingItems.Remove(item);
    }

    private void Update()
    {
        frameCounter++;
        float deltaTime = Time.deltaTime;
        float unscaledDeltaTime = Time.unscaledDeltaTime;
        bool anyItemTransformChanged = false;

        UpdateAdaptiveTier(unscaledDeltaTime);
        ResolveActiveStrides(out int overheadStride, out int itemStride, out int physicsSyncStride);

        if (delayedConveyorBuckets.TryGetValue(frameCounter, out List<ConveyorBelt> dueConveyors))
        {
            for (int i = 0; i < dueConveyors.Count; i++)
            {
                ConveyorBelt conveyor = dueConveyors[i];
                if (conveyor != null)
                {
                    readyConveyors.Enqueue(conveyor);
                }
            }

            delayedConveyorBuckets.Remove(frameCounter);
        }

        int conveyorWorkCount = readyConveyors.Count;
        for (int i = 0; i < conveyorWorkCount; i++)
        {
            ConveyorBelt conveyor = readyConveyors.Dequeue();
            if (conveyor == null)
            {
                continue;
            }

            scheduledConveyors.Remove(conveyor);

            if (!conveyorRegistry.Contains(conveyor))
            {
                continue;
            }

            if (conveyor.ProcessTransportStep(out int retryDelayFrames))
            {
                RequestConveyorTick(conveyor, retryDelayFrames);
            }
        }

        float overheadDelta = deltaTime * overheadStride;
        float itemDelta = deltaTime * itemStride;

        for (int i = overheadConveyors.Count - 1; i >= 0; i--)
        {
            OverheadConveyor overhead = overheadConveyors[i];
            if (overhead == null)
            {
                overheadConveyorSet.Remove(overhead);
                overheadConveyors.RemoveAt(i);
                continue;
            }

            if (ShouldTickIndex(i, overheadStride))
            {
                overhead.TickTransport(overheadDelta);
            }
        }

        for (int i = movingItems.Count - 1; i >= 0; i--)
        {
            Item item = movingItems[i];
            if (item == null)
            {
                movingItemSet.Remove(item);
                movingItems.RemoveAt(i);
                continue;
            }

            if (ShouldTickIndex(i, itemStride))
            {
                anyItemTransformChanged |= item.TickTransport(itemDelta);
            }

            if (!item.isBeingMoved)
            {
                movingItemSet.Remove(item);
                movingItems.RemoveAt(i);
            }
        }

        if (disableAutoPhysicsSync2D && anyItemTransformChanged)
        {
            pendingPhysicsSync = true;
        }

        if (disableAutoPhysicsSync2D && pendingPhysicsSync && (frameCounter % physicsSyncStride == 0))
        {
            Physics2D.SyncTransforms();
            pendingPhysicsSync = false;
        }
    }

    private void UpdateAdaptiveTier(float unscaledDeltaTime)
    {
        if (!useAdaptiveQuality)
        {
            return;
        }

        float dtMs = unscaledDeltaTime * 1000f;
        frameTimeEmaMs = Mathf.Lerp(frameTimeEmaMs, dtMs, Mathf.Clamp01(frameTimeEmaSmoothing));

        if (frameTimeEmaMs >= degradeFrameTimeMs)
        {
            timeAboveDegradeThreshold += unscaledDeltaTime;
        }
        else
        {
            timeAboveDegradeThreshold = 0f;
        }

        if (frameTimeEmaMs <= recoverFrameTimeMs)
        {
            timeBelowRecoverThreshold += unscaledDeltaTime;
        }
        else
        {
            timeBelowRecoverThreshold = 0f;
        }

        if (timeAboveDegradeThreshold >= degradeHoldSeconds && currentTier < TransportQualityTier.Emergency)
        {
            SetTier(currentTier + 1);
            timeAboveDegradeThreshold = 0f;
            timeBelowRecoverThreshold = 0f;
        }
        else if (timeBelowRecoverThreshold >= recoverHoldSeconds && currentTier > TransportQualityTier.Smooth)
        {
            SetTier(currentTier - 1);
            timeAboveDegradeThreshold = 0f;
            timeBelowRecoverThreshold = 0f;
        }
    }

    private void SetTier(TransportQualityTier newTier)
    {
        if (currentTier == newTier)
        {
            return;
        }

        currentTier = newTier;

        if (logQualityTierChanges)
        {
            Debug.Log($"[TransportTickManager] Quality tier switched to {currentTier} (EMA {frameTimeEmaMs:F1} ms)");
        }
    }

    private void ResolveActiveStrides(out int overheadStride, out int itemStride, out int physicsSyncStride)
    {
        if (!useAdaptiveQuality)
        {
            overheadStride = Mathf.Max(1, fallbackOverheadTickStrideFrames);
            itemStride = Mathf.Max(1, fallbackItemTickStrideFrames);
            physicsSyncStride = Mathf.Max(1, fallbackPhysicsSyncStrideFrames);
            return;
        }

        switch (currentTier)
        {
            case TransportQualityTier.Smooth:
                overheadStride = 1;
                itemStride = 1;
                physicsSyncStride = 1;
                break;
            case TransportQualityTier.Balanced:
                overheadStride = 1;
                itemStride = 2;
                physicsSyncStride = 1;
                break;
            case TransportQualityTier.Performance:
                overheadStride = 2;
                itemStride = 4;
                physicsSyncStride = 2;
                break;
            default:
                overheadStride = 2;
                itemStride = 6;
                physicsSyncStride = 2;
                break;
        }
    }

    private bool ShouldTickIndex(int index, int stride)
    {
        if (stride <= 1)
        {
            return true;
        }

        return ((index + frameCounter) % stride) == 0;
    }
}