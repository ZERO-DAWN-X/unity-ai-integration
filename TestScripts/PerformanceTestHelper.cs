using UnityEngine;
using System.Collections;

/// <summary>
/// Performance Test Helper for Unity Cursor Integration Performance Monitor
/// Add this script to a GameObject in your scene to test performance monitoring features
/// </summary>
public class PerformanceTestHelper : MonoBehaviour
{
    [Header("Performance Stress Testing")]
    [SerializeField] private GameObject testPrefab;
    [SerializeField] private int spawnCount = 100;
    [SerializeField] private bool enableFPSStressTest = false;
    [SerializeField] private bool enableMemoryStressTest = false;
    [SerializeField] private bool enableDrawCallStressTest = false;
    
    [Header("Test Controls")]
    [SerializeField] private KeyCode triggerFPSTest = KeyCode.F;
    [SerializeField] private KeyCode triggerMemoryTest = KeyCode.M;
    [SerializeField] private KeyCode triggerDrawCallTest = KeyCode.D;
    [SerializeField] private KeyCode clearTests = KeyCode.C;
    
    [Header("Test Status")]
    [SerializeField] private bool isFPSTestRunning = false;
    [SerializeField] private bool isMemoryTestRunning = false;
    [SerializeField] private int currentSpawnedObjects = 0;
    
    private void Start()
    {
        // Create a default test prefab if none is assigned
        if (testPrefab == null)
        {
            testPrefab = CreateDefaultTestPrefab();
        }
        
        Debug.Log("[Performance Test Helper] Ready! Press keys to test:");
        Debug.Log($"[Performance Test Helper] {triggerFPSTest} = FPS Stress Test");
        Debug.Log($"[Performance Test Helper] {triggerMemoryTest} = Memory Stress Test");
        Debug.Log($"[Performance Test Helper] {triggerDrawCallTest} = Draw Call Stress Test");
        Debug.Log($"[Performance Test Helper] {clearTests} = Clear All Tests");
    }
    
    private void Update()
    {
        HandleKeyboardInput();
        
        // Run continuous stress tests if enabled
        if (enableFPSStressTest && !isFPSTestRunning)
        {
            StartFPSStressTest();
        }
        
        if (enableMemoryStressTest && !isMemoryTestRunning)
        {
            StartMemoryStressTest();
        }
        
        if (enableDrawCallStressTest)
        {
            RunDrawCallStressTest();
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(triggerFPSTest))
        {
            if (isFPSTestRunning)
                StopFPSStressTest();
            else
                StartFPSStressTest();
        }
        
        if (Input.GetKeyDown(triggerMemoryTest))
        {
            if (isMemoryTestRunning)
                StopMemoryStressTest();
            else
                StartMemoryStressTest();
        }
        
        if (Input.GetKeyDown(triggerDrawCallTest))
        {
            SpawnTestObjects();
        }
        
        if (Input.GetKeyDown(clearTests))
        {
            ClearAllTests();
        }
    }
    
    private void StartFPSStressTest()
    {
        Debug.Log("[Performance Test] Starting FPS Stress Test - This will cause frame rate drops!");
        isFPSTestRunning = true;
        StartCoroutine(FPSStressTestCoroutine());
    }
    
    private void StopFPSStressTest()
    {
        Debug.Log("[Performance Test] Stopping FPS Stress Test");
        isFPSTestRunning = false;
        StopCoroutine(FPSStressTestCoroutine());
    }
    
    private IEnumerator FPSStressTestCoroutine()
    {
        while (isFPSTestRunning)
        {
            // Intentionally waste CPU cycles to drop FPS
            for (int i = 0; i < 100000; i++)
            {
                float waste = Mathf.Sin(i) * Mathf.Cos(i) * Time.time;
            }
            yield return null;
        }
    }
    
    private void StartMemoryStressTest()
    {
        Debug.Log("[Performance Test] Starting Memory Stress Test - This will increase memory usage!");
        isMemoryTestRunning = true;
        StartCoroutine(MemoryStressTestCoroutine());
    }
    
    private void StopMemoryStressTest()
    {
        Debug.Log("[Performance Test] Stopping Memory Stress Test");
        isMemoryTestRunning = false;
        StopCoroutine(MemoryStressTestCoroutine());
        System.GC.Collect(); // Force garbage collection
    }
    
    private IEnumerator MemoryStressTestCoroutine()
    {
        while (isMemoryTestRunning)
        {
            // Allocate large arrays to stress memory
            byte[] memoryWaste = new byte[1024 * 1024]; // 1MB allocation
            int[] moreWaste = new int[100000]; // Additional allocation
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void SpawnTestObjects()
    {
        Debug.Log($"[Performance Test] Spawning {spawnCount} test objects for draw call stress test");
        
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(-20f, 20f),
                Random.Range(0f, 10f),
                Random.Range(-20f, 20f)
            );
            
            GameObject spawnedObject = Instantiate(testPrefab, randomPosition, Random.rotation);
            spawnedObject.name = $"TestObject_{currentSpawnedObjects}";
            spawnedObject.tag = "PerformanceTest"; // Tag for easy cleanup
            
            currentSpawnedObjects++;
        }
        
        Debug.Log($"[Performance Test] Total spawned objects: {currentSpawnedObjects}");
    }
    
    private void RunDrawCallStressTest()
    {
        // This test spawns objects continuously (use with caution!)
        if (Time.frameCount % 30 == 0) // Every 30 frames
        {
            SpawnTestObjects();
        }
    }
    
    private void ClearAllTests()
    {
        Debug.Log("[Performance Test] Clearing all performance tests");
        
        // Stop all stress tests
        StopFPSStressTest();
        StopMemoryStressTest();
        enableDrawCallStressTest = false;
        
        // Destroy all test objects
        GameObject[] testObjects = GameObject.FindGameObjectsWithTag("PerformanceTest");
        foreach (GameObject obj in testObjects)
        {
            DestroyImmediate(obj);
        }
        
        currentSpawnedObjects = 0;
        
        // Force garbage collection
        System.GC.Collect();
        
        Debug.Log("[Performance Test] All tests cleared and memory cleaned up");
    }
    
    private GameObject CreateDefaultTestPrefab()
    {
        // Create a simple cube prefab for testing
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DefaultTestCube";
        
        // Add a simple rotating behavior
        cube.AddComponent<TestObjectRotator>();
        
        // Add some visual variety
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(Random.value, Random.value, Random.value, 1f);
            renderer.material = material;
        }
        
        // Convert to prefab-like object
        cube.SetActive(false);
        return cube;
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Performance Test Helper", GUI.skin.box);
        
        GUILayout.Label($"FPS Test: {(isFPSTestRunning ? "RUNNING" : "STOPPED")}");
        GUILayout.Label($"Memory Test: {(isMemoryTestRunning ? "RUNNING" : "STOPPED")}");
        GUILayout.Label($"Spawned Objects: {currentSpawnedObjects}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button($"FPS Test ({triggerFPSTest})"))
        {
            if (isFPSTestRunning) StopFPSStressTest();
            else StartFPSStressTest();
        }
        
        if (GUILayout.Button($"Memory Test ({triggerMemoryTest})"))
        {
            if (isMemoryTestRunning) StopMemoryStressTest();
            else StartMemoryStressTest();
        }
        
        if (GUILayout.Button($"Spawn Objects ({triggerDrawCallTest})"))
        {
            SpawnTestObjects();
        }
        
        if (GUILayout.Button($"Clear All ({clearTests})"))
        {
            ClearAllTests();
        }
        
        GUILayout.EndArea();
    }
}

/// <summary>
/// Simple component to make test objects rotate for visual effect
/// </summary>
public class TestObjectRotator : MonoBehaviour
{
    private Vector3 rotationSpeed;
    
    private void Start()
    {
        rotationSpeed = new Vector3(
            Random.Range(-90f, 90f),
            Random.Range(-90f, 90f),
            Random.Range(-90f, 90f)
        );
    }
    
    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
} 