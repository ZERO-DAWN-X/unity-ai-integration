using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

namespace Microsoft.Unity.VisualStudio.Editor.PerformanceMonitor
{
    /// <summary>
    /// Real-time Unity Performance Monitor for Cursor integration
    /// Tracks FPS, memory usage, draw calls, and other vital metrics
    /// </summary>
    public class PerformanceMonitor : EditorWindow
    {
        private static PerformanceMonitor instance;
        private static bool isMonitoringEnabled = true;
        private static float updateInterval = 0.5f;
        private static double lastUpdateTime;
        
        // Performance Metrics
        private static PerformanceMetrics currentMetrics = new PerformanceMetrics();
        private static Queue<PerformanceMetrics> metricsHistory = new Queue<PerformanceMetrics>();
        private const int maxHistorySize = 60; // Store 30 seconds of data at 0.5s intervals
        
        // GUI
        private Vector2 scrollPosition;
        private bool showDetailedView = true;
        private bool autoScrollToBottom = true;

        [Serializable]
        public class PerformanceMetrics
        {
            public float fps;
            public float frameTime;
            public long totalMemory;
            public long usedMemory;
            public long gcMemory;
            public int drawCalls;
            public int triangles;
            public int vertices;
            public float cpuUsage;
            public float gpuUsage;
            public DateTime timestamp;
            public string buildTarget;
            public bool isPlaying;

            public PerformanceMetrics()
            {
                timestamp = DateTime.Now;
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
                isPlaying = Application.isPlaying;
            }
        }

        [MenuItem("Window/Unity Cursor Integration/Performance Monitor")]
        public static void ShowWindow()
        {
            instance = GetWindow<PerformanceMonitor>("Performance Monitor");
            instance.minSize = new Vector2(400, 300);
            instance.Show();
        }

        private void OnEnable()
        {
            instance = this;
            EditorApplication.update += UpdateMetrics;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateMetrics;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        private void OnBeforeAssemblyReload()
        {
            SendMetricsToCursor(currentMetrics, "assembly_reload");
        }

        private static void UpdateMetrics()
        {
            if (!isMonitoringEnabled) return;
            
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastUpdateTime < updateInterval) return;
            
            lastUpdateTime = currentTime;
            
            // Collect performance metrics
            CollectMetrics();
            
            // Add to history
            metricsHistory.Enqueue(currentMetrics);
            if (metricsHistory.Count > maxHistorySize)
                metricsHistory.Dequeue();
            
            // Send to Cursor
            SendMetricsToCursor(currentMetrics, "performance_update");
            
            // Refresh window if open
            if (instance != null)
                instance.Repaint();
        }

        private static void CollectMetrics()
        {
            currentMetrics = new PerformanceMetrics();
            
            // FPS and Frame Time
            if (Application.isPlaying)
            {
                currentMetrics.fps = 1.0f / Time.unscaledDeltaTime;
                currentMetrics.frameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
            }
            else
            {
                currentMetrics.fps = 0f;
                currentMetrics.frameTime = 0f;
            }
            
            // Memory Usage
            currentMetrics.totalMemory = Profiler.GetTotalAllocatedMemory(0);
            currentMetrics.usedMemory = Profiler.GetTotalReservedMemory(0);
            currentMetrics.gcMemory = GC.GetTotalMemory(false);
            
            // Rendering Stats
            if (Application.isPlaying && Camera.main != null)
            {
                // Note: These are estimates as Unity doesn't expose exact real-time stats
                currentMetrics.drawCalls = UnityStats.drawCalls;
                currentMetrics.triangles = UnityStats.triangles;
                currentMetrics.vertices = UnityStats.vertices;
            }
            
            // CPU/GPU usage would require platform-specific implementations
            currentMetrics.cpuUsage = GetCPUUsage();
            currentMetrics.gpuUsage = GetGPUUsage();
        }

        private static float GetCPUUsage()
        {
            // Simplified CPU usage estimation based on frame time
            if (Application.isPlaying && currentMetrics.frameTime > 0)
            {
                return Mathf.Clamp01(currentMetrics.frameTime / 16.67f) * 100f; // Assume 60 FPS target
            }
            return 0f;
        }

        private static float GetGPUUsage()
        {
            // GPU usage estimation - would need platform-specific implementation for accuracy
            if (Application.isPlaying)
            {
                return Mathf.Clamp01((currentMetrics.drawCalls * currentMetrics.triangles) / 1000000f) * 100f;
            }
            return 0f;
        }

        private static void SendMetricsToCursor(PerformanceMetrics metrics, string eventType)
        {
            try
            {
                var message = new
                {
                    type = "unity_performance",
                    eventType = eventType,
                    timestamp = metrics.timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    data = new
                    {
                        fps = Math.Round(metrics.fps, 1),
                        frameTime = Math.Round(metrics.frameTime, 2),
                        memory = new
                        {
                            total = FormatBytes(metrics.totalMemory),
                            used = FormatBytes(metrics.usedMemory),
                            gc = FormatBytes(metrics.gcMemory),
                            totalBytes = metrics.totalMemory,
                            usedBytes = metrics.usedMemory,
                            gcBytes = metrics.gcMemory
                        },
                        rendering = new
                        {
                            drawCalls = metrics.drawCalls,
                            triangles = metrics.triangles,
                            vertices = metrics.vertices
                        },
                        system = new
                        {
                            cpuUsage = Math.Round(metrics.cpuUsage, 1),
                            gpuUsage = Math.Round(metrics.gpuUsage, 1),
                            buildTarget = metrics.buildTarget,
                            isPlaying = metrics.isPlaying
                        }
                    }
                };

                string jsonMessage = EditorJsonUtility.ToJson(message);
                
                // Write to .vscode/unity-performance.json for Cursor to read
                WritePerformanceFile(jsonMessage);
                
                // Also try to send via stdout for real-time updates
                Console.WriteLine($"[UNITY_PERFORMANCE] {jsonMessage}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to send metrics to Cursor: {e.Message}");
            }
        }

        private static void WritePerformanceFile(string jsonData)
        {
            try
            {
                string projectPath = Application.dataPath.Replace("/Assets", "");
                string vscodeDir = System.IO.Path.Combine(projectPath, ".vscode");
                
                if (!System.IO.Directory.Exists(vscodeDir))
                    System.IO.Directory.CreateDirectory(vscodeDir);
                
                string performanceFile = System.IO.Path.Combine(vscodeDir, "unity-performance.json");
                System.IO.File.WriteAllText(performanceFile, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to write performance file: {e.Message}");
            }
        }

        private static string FormatBytes(long bytes)
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

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            // Header
            EditorGUILayout.LabelField("Unity Performance Monitor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Controls
            EditorGUILayout.BeginHorizontal();
            isMonitoringEnabled = EditorGUILayout.Toggle("Enable Monitoring", isMonitoringEnabled);
            updateInterval = EditorGUILayout.Slider("Update Interval", updateInterval, 0.1f, 2.0f);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            showDetailedView = EditorGUILayout.Toggle("Detailed View", showDetailedView);
            autoScrollToBottom = EditorGUILayout.Toggle("Auto Scroll", autoScrollToBottom);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Current Metrics
            if (currentMetrics != null)
            {
                DrawCurrentMetrics();
            }
            
            if (showDetailedView)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Performance History", EditorStyles.boldLabel);
                DrawMetricsHistory();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentMetrics()
        {
            EditorGUILayout.LabelField("Current Performance", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // System Status
            EditorGUILayout.LabelField($"Status: {(Application.isPlaying ? "Playing" : "Editor")} | Build Target: {currentMetrics.buildTarget}");
            
            // Performance Metrics
            if (Application.isPlaying)
            {
                Color originalColor = GUI.color;
                
                // FPS with color coding
                if (currentMetrics.fps >= 55) GUI.color = Color.green;
                else if (currentMetrics.fps >= 30) GUI.color = Color.yellow;
                else GUI.color = Color.red;
                
                EditorGUILayout.LabelField($"FPS: {currentMetrics.fps:F1} | Frame Time: {currentMetrics.frameTime:F2}ms");
                GUI.color = originalColor;
            }
            else
            {
                EditorGUILayout.LabelField("FPS: N/A (Not Playing)");
            }
            
            // Memory
            EditorGUILayout.LabelField($"Memory - Total: {FormatBytes(currentMetrics.totalMemory)} | Used: {FormatBytes(currentMetrics.usedMemory)} | GC: {FormatBytes(currentMetrics.gcMemory)}");
            
            // Rendering (only when playing)
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Rendering - Draw Calls: {currentMetrics.drawCalls} | Triangles: {currentMetrics.triangles:N0} | Vertices: {currentMetrics.vertices:N0}");
                EditorGUILayout.LabelField($"Estimated Usage - CPU: {currentMetrics.cpuUsage:F1}% | GPU: {currentMetrics.gpuUsage:F1}%");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMetricsHistory()
        {
            if (metricsHistory.Count == 0)
            {
                EditorGUILayout.LabelField("No performance history available.");
                return;
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            foreach (var metrics in metricsHistory)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(metrics.timestamp.ToString("HH:mm:ss"), GUILayout.Width(80));
                
                if (metrics.isPlaying)
                {
                    EditorGUILayout.LabelField($"FPS: {metrics.fps:F1}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Memory: {FormatBytes(metrics.usedMemory)}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Draws: {metrics.drawCalls}", GUILayout.Width(80));
                }
                else
                {
                    EditorGUILayout.LabelField("Editor Mode", GUILayout.Width(240));
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (autoScrollToBottom)
                scrollPosition.y = float.MaxValue;
            
            EditorGUILayout.EndScrollView();
        }

        // Public API for external access
        public static PerformanceMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }

        public static PerformanceMetrics[] GetMetricsHistory()
        {
            return metricsHistory.ToArray();
        }

        public static void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Clamp(interval, 0.1f, 5.0f);
        }

        public static void EnableMonitoring(bool enable)
        {
            isMonitoringEnabled = enable;
        }
    }
} 