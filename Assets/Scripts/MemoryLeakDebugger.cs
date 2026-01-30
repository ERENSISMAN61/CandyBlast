using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

public class MemoryLeakDebugger : MonoBehaviour
{
    [Header("Test Memory Leak Validation")]
    [Tooltip("Enable this to simulate -diag-job-temp-memory-leak-validation")]
    [SerializeField] private bool enableMemoryLeakValidation = false;

    [Header("Memory Tracking")]
    [SerializeField] private float checkInterval = 5f; // Her 5 saniyede bir kontrol et
    [SerializeField] private bool logDetailedInfo = true;
    [SerializeField] private bool trackObjectAllocations = true;

    private long lastTotalMemory = 0;
    private long lastMonoHeapMemory = 0;
    private float nextCheckTime = 0f;
    private Dictionary<string, int> objectCounts = new Dictionary<string, int>();

    void Awake()
    {
        // Check command line arguments
        string[] args = System.Environment.GetCommandLineArgs();

        bool hasMemoryLeakArg = false;
        foreach (string arg in args)
        {
            if (arg.Contains("diag-job-temp-memory-leak-validation"))
            {
                hasMemoryLeakArg = true;
                break;
            }
        }

        // Log status
        if (hasMemoryLeakArg || enableMemoryLeakValidation)
        {
            Debug.Log("<color=yellow>═══ MEMORY LEAK VALIDATION ENABLED ═══</color>");
            Debug.Log("Monitoring job temp memory allocations...");

            // Enable Unity's built-in leak detection
#if UNITY_EDITOR
            Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled = true;
#endif

            // İlk snapshot
            TakeMemorySnapshot("Initial");
        }
        else
        {
            Debug.Log("Memory leak validation: <color=red>DISABLED</color>");
            Debug.Log("Enable 'enableMemoryLeakValidation' in inspector or run build with -diag-job-temp-memory-leak-validation");
        }
    }

    void Update()
    {
        if (!enableMemoryLeakValidation) return;

        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckMemoryLeaks();
        }
    }

    void CheckMemoryLeaks()
    {
        long currentTotalMemory = Profiler.GetTotalAllocatedMemoryLong();
        long currentMonoHeap = Profiler.GetMonoHeapSizeLong();

        long totalDelta = currentTotalMemory - lastTotalMemory;
        long heapDelta = currentMonoHeap - lastMonoHeapMemory;

        // Eğer bellek sürekli artıyorsa uyar
        if (totalDelta > 1024 * 1024) // 1 MB'dan fazla artış
        {
            Debug.LogWarning($"<color=orange>⚠ POTENTIAL MEMORY LEAK DETECTED!</color>");
            Debug.LogWarning($"Memory increased by: {FormatBytes(totalDelta)}");
            Debug.LogWarning($"Current Total Memory: {FormatBytes(currentTotalMemory)}");
            Debug.LogWarning($"Mono Heap: {FormatBytes(currentMonoHeap)} (Delta: {FormatBytes(heapDelta)})");

            if (logDetailedInfo)
            {
                LogDetailedMemoryInfo();
            }

            if (trackObjectAllocations)
            {
                TrackObjectAllocations();
            }
        }
        else if (logDetailedInfo)
        {
            Debug.Log($"<color=green>✓ Memory Status:</color> Total: {FormatBytes(currentTotalMemory)}, Delta: {FormatBytes(totalDelta)}");
        }

        lastTotalMemory = currentTotalMemory;
        lastMonoHeapMemory = currentMonoHeap;
    }

    void LogDetailedMemoryInfo()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("<color=cyan>DETAILED MEMORY INFO:</color>");
        Debug.Log($"• Total Allocated: {FormatBytes(Profiler.GetTotalAllocatedMemoryLong())}");
        Debug.Log($"• Total Reserved: {FormatBytes(Profiler.GetTotalReservedMemoryLong())}");
        Debug.Log($"• Mono Heap Size: {FormatBytes(Profiler.GetMonoHeapSizeLong())}");
        Debug.Log($"• Mono Used Size: {FormatBytes(Profiler.GetMonoUsedSizeLong())}");

        // CRITICAL: Temp Allocator monitoring for Job System leaks
        long tempAllocSize = Profiler.GetTempAllocatorSize();
        Debug.Log($"• Temp Allocator Size: {FormatBytes(tempAllocSize)}");
        if (tempAllocSize > 1024 * 100) // 100 KB'dan fazlaysa uyar
        {
            Debug.LogWarning($"<color=orange>⚠ HIGH TEMP ALLOCATOR SIZE! Possible Job System leak: {FormatBytes(tempAllocSize)}</color>");
        }

        Debug.Log($"• GFX Driver Memory: {FormatBytes(Profiler.GetAllocatedMemoryForGraphicsDriver())}");

#if UNITY_EDITOR
        Debug.Log($"• Total Unused Reserved: {FormatBytes(Profiler.GetTotalUnusedReservedMemoryLong())}");

        // Job System leak detection
        if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled)
        {
            Debug.Log("<color=yellow>✓ Job Debugger: ENABLED (monitoring leaks)</color>");
        }
#endif

        Debug.Log("═══════════════════════════════════════");
    }

    void TrackObjectAllocations()
    {
        Dictionary<string, int> currentCounts = new Dictionary<string, int>();

        // Tüm MonoBehaviour componentlerini say (daha anlamlı)
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>(true); // true = inactive olanları da dahil et
        foreach (var component in allComponents)
        {
            string typeName = component.GetType().Name;
            if (!currentCounts.ContainsKey(typeName))
                currentCounts[typeName] = 0;
            currentCounts[typeName]++;
        }

        // GameObjects sayısını da ekle
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>(true);
        currentCounts["[Total GameObjects]"] = allGameObjects.Length;

        // Artışları kontrol et
        Debug.Log("<color=yellow>═══ OBJECT ALLOCATION TRACKING ═══</color>");
        bool hasLeaks = false;

        foreach (var kvp in currentCounts)
        {
            if (objectCounts.ContainsKey(kvp.Key))
            {
                int delta = kvp.Value - objectCounts[kvp.Key];
                if (delta > 0)
                {
                    hasLeaks = true;
                    Debug.LogWarning($"<color=orange>↑ {kvp.Key}: +{delta} (Total: {kvp.Value})</color>");
                }
                else if (delta < 0)
                {
                    Debug.Log($"<color=green>↓ {kvp.Key}: {delta} (Total: {kvp.Value})</color>");
                }
            }
            else if (kvp.Value > 5) // Yeni tip ve 5'ten fazla instance varsa göster
            {
                Debug.Log($"<color=cyan>⊕ {kvp.Key}: {kvp.Value} (New Type)</color>");
            }
        }

        if (!hasLeaks)
        {
            Debug.Log("<color=green>✓ No object leaks detected in this check</color>");
        }

        objectCounts = currentCounts;
    }

    void TakeMemorySnapshot(string label)
    {
        Debug.Log($"<color=magenta>📸 Memory Snapshot [{label}]:</color>");
        Debug.Log($"  Total Allocated: {FormatBytes(Profiler.GetTotalAllocatedMemoryLong())}");
        Debug.Log($"  Mono Heap: {FormatBytes(Profiler.GetMonoHeapSizeLong())}");
    }

    string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    void OnApplicationQuit()
    {
        if (enableMemoryLeakValidation)
        {
            Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
            Debug.Log("<color=yellow>Application Quitting - Final Memory Report</color>");
            TakeMemorySnapshot("Final");
            Debug.Log("Check console for any memory leak callstacks above");
            Debug.Log("<color=yellow>═══════════════════════════════════════</color>");
        }
    }

    // Manuel snapshot almak için
    [ContextMenu("Take Memory Snapshot Now")]
    public void TakeSnapshotNow()
    {
        TakeMemorySnapshot("Manual");
        LogDetailedMemoryInfo();
        TrackObjectAllocations();
    }

    [ContextMenu("Force Garbage Collection")]
    public void ForceGC()
    {
        Debug.Log("<color=cyan>🗑 Forcing Garbage Collection...</color>");
        long before = Profiler.GetMonoUsedSizeLong();
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        long after = Profiler.GetMonoUsedSizeLong();
        Debug.Log($"<color=green>✓ GC Complete. Freed: {FormatBytes(before - after)}</color>");
    }
}
