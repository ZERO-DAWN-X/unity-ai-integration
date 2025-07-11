using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Microsoft.Unity.VisualStudio.Editor.PerformanceMonitor
{
    /// <summary>
    /// Performance notification system that alerts developers when performance thresholds are exceeded
    /// Integrates with Cursor to provide real-time performance warnings
    /// </summary>
    public static class PerformanceNotificationSystem
    {
        [Serializable]
        public class PerformanceThresholds
        {
            public float minFPS = 30f;
            public float maxFrameTime = 33.33f; // milliseconds for 30 FPS
            public long maxMemoryUsage = 1024 * 1024 * 1024; // 1 GB
            public int maxDrawCalls = 1000;
            public float maxCPUUsage = 80f;
            public float maxGPUUsage = 80f;
        }

        [Serializable]
        public class PerformanceAlert
        {
            public string type;
            public string severity; // "warning", "critical"
            public string message;
            public float value;
            public float threshold;
            public DateTime timestamp;
            public string suggestion;

            public PerformanceAlert(string alertType, string alertSeverity, string alertMessage, 
                                  float currentValue, float thresholdValue, string improvementSuggestion = "")
            {
                type = alertType;
                severity = alertSeverity;
                message = alertMessage;
                value = currentValue;
                threshold = thresholdValue;
                timestamp = DateTime.Now;
                suggestion = improvementSuggestion;
            }
        }

        private static PerformanceThresholds thresholds = new PerformanceThresholds();
        private static List<PerformanceAlert> activeAlerts = new List<PerformanceAlert>();
        private static Dictionary<string, DateTime> lastAlertTimes = new Dictionary<string, DateTime>();
        private static readonly TimeSpan alertCooldown = TimeSpan.FromSeconds(5); // Prevent spam

        // Performance improvement suggestions
        private static readonly Dictionary<string, string[]> performanceSuggestions = new Dictionary<string, string[]>
        {
            ["fps"] = new[]
            {
                "Reduce polygon count in meshes",
                "Optimize shaders and materials",
                "Use object pooling for frequently spawned objects",
                "Enable GPU instancing for similar objects",
                "Reduce shadow quality or distance",
                "Use LOD (Level of Detail) systems"
            },
            ["memory"] = new[]
            {
                "Use object pooling to reduce allocations",
                "Optimize texture compression settings",
                "Reduce audio clip quality where appropriate",
                "Clear unused references to prevent memory leaks",
                "Use UnityEngine.Object.DestroyImmediate() carefully",
                "Consider using addressable assets for large content"
            },
            ["drawcalls"] = new[]
            {
                "Batch similar objects together",
                "Use texture atlases to reduce material count",
                "Enable static batching for static objects",
                "Use GPU instancing for identical objects",
                "Reduce the number of lights in the scene",
                "Combine meshes where possible"
            },
            ["cpu"] = new[]
            {
                "Move heavy calculations to Update() from FixedUpdate()",
                "Use coroutines for spread operations over multiple frames",
                "Cache component references instead of GetComponent calls",
                "Use object pooling for frequently created/destroyed objects",
                "Optimize physics by reducing rigidbody count",
                "Use Unity's Job System for heavy computations"
            },
            ["gpu"] = new[]
            {
                "Reduce shader complexity",
                "Lower texture resolution or use compression",
                "Reduce particle count and complexity",
                "Optimize lighting setup",
                "Use simpler materials for distant objects",
                "Enable occlusion culling"
            }
        };

        public static void CheckPerformanceThresholds(PerformanceMonitor.PerformanceMetrics metrics)
        {
            if (!Application.isPlaying) return;

            activeAlerts.Clear();

            // Check FPS
            if (metrics.fps > 0 && metrics.fps < thresholds.minFPS)
            {
                CreateAlert("fps", "critical", 
                    $"FPS below threshold: {metrics.fps:F1} < {thresholds.minFPS}",
                    metrics.fps, thresholds.minFPS, GetRandomSuggestion("fps"));
            }

            // Check Frame Time
            if (metrics.frameTime > thresholds.maxFrameTime)
            {
                CreateAlert("frametime", "warning",
                    $"Frame time too high: {metrics.frameTime:F2}ms > {thresholds.maxFrameTime:F2}ms",
                    metrics.frameTime, thresholds.maxFrameTime, GetRandomSuggestion("fps"));
            }

            // Check Memory Usage
            if (metrics.usedMemory > thresholds.maxMemoryUsage)
            {
                CreateAlert("memory", "warning",
                    $"Memory usage high: {FormatBytes(metrics.usedMemory)} > {FormatBytes(thresholds.maxMemoryUsage)}",
                    metrics.usedMemory, thresholds.maxMemoryUsage, GetRandomSuggestion("memory"));
            }

            // Check Draw Calls
            if (metrics.drawCalls > thresholds.maxDrawCalls)
            {
                CreateAlert("drawcalls", "warning",
                    $"Draw calls too high: {metrics.drawCalls} > {thresholds.maxDrawCalls}",
                    metrics.drawCalls, thresholds.maxDrawCalls, GetRandomSuggestion("drawcalls"));
            }

            // Check CPU Usage
            if (metrics.cpuUsage > thresholds.maxCPUUsage)
            {
                CreateAlert("cpu", "warning",
                    $"CPU usage high: {metrics.cpuUsage:F1}% > {thresholds.maxCPUUsage:F1}%",
                    metrics.cpuUsage, thresholds.maxCPUUsage, GetRandomSuggestion("cpu"));
            }

            // Check GPU Usage
            if (metrics.gpuUsage > thresholds.maxGPUUsage)
            {
                CreateAlert("gpu", "warning",
                    $"GPU usage high: {metrics.gpuUsage:F1}% > {thresholds.maxGPUUsage:F1}%",
                    metrics.gpuUsage, thresholds.maxGPUUsage, GetRandomSuggestion("gpu"));
            }

            // Send alerts to Cursor
            if (activeAlerts.Count > 0)
            {
                SendAlertsToCursor();
            }
        }

        private static void CreateAlert(string type, string severity, string message, 
                                      float value, float threshold, string suggestion)
        {
            // Check cooldown to prevent spam
            string alertKey = $"{type}_{severity}";
            if (lastAlertTimes.ContainsKey(alertKey) && 
                DateTime.Now - lastAlertTimes[alertKey] < alertCooldown)
            {
                return;
            }

            var alert = new PerformanceAlert(type, severity, message, value, threshold, suggestion);
            activeAlerts.Add(alert);
            lastAlertTimes[alertKey] = DateTime.Now;

            // Also log to Unity console
            if (severity == "critical")
            {
                Debug.LogError($"[Performance Monitor] {message}\nSuggestion: {suggestion}");
            }
            else
            {
                Debug.LogWarning($"[Performance Monitor] {message}\nSuggestion: {suggestion}");
            }
        }

        private static void SendAlertsToCursor()
        {
            try
            {
                var alertMessage = new
                {
                    type = "unity_performance_alert",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    alerts = activeAlerts,
                    summary = new
                    {
                        totalAlerts = activeAlerts.Count,
                        criticalCount = activeAlerts.FindAll(a => a.severity == "critical").Count,
                        warningCount = activeAlerts.FindAll(a => a.severity == "warning").Count
                    }
                };

                string jsonMessage = EditorJsonUtility.ToJson(alertMessage);
                
                // Write to .vscode/unity-performance-alerts.json
                WriteAlertsFile(jsonMessage);
                
                // Send via stdout
                Console.WriteLine($"[UNITY_PERFORMANCE_ALERT] {jsonMessage}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to send alerts to Cursor: {e.Message}");
            }
        }

        private static void WriteAlertsFile(string jsonData)
        {
            try
            {
                string projectPath = Application.dataPath.Replace("/Assets", "");
                string vscodeDir = System.IO.Path.Combine(projectPath, ".vscode");
                
                if (!System.IO.Directory.Exists(vscodeDir))
                    System.IO.Directory.CreateDirectory(vscodeDir);
                
                string alertsFile = System.IO.Path.Combine(vscodeDir, "unity-performance-alerts.json");
                System.IO.File.WriteAllText(alertsFile, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to write alerts file: {e.Message}");
            }
        }

        private static string GetRandomSuggestion(string category)
        {
            if (performanceSuggestions.ContainsKey(category))
            {
                var suggestions = performanceSuggestions[category];
                return suggestions[UnityEngine.Random.Range(0, suggestions.Length)];
            }
            return "Consider optimizing your code and assets for better performance.";
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

        // Public API for customizing thresholds
        public static void SetThresholds(PerformanceThresholds newThresholds)
        {
            thresholds = newThresholds;
            SaveThresholds();
        }

        public static PerformanceThresholds GetThresholds()
        {
            LoadThresholds();
            return thresholds;
        }

        private static void SaveThresholds()
        {
            try
            {
                string json = EditorJsonUtility.ToJson(thresholds, true);
                string projectPath = Application.dataPath.Replace("/Assets", "");
                string settingsPath = System.IO.Path.Combine(projectPath, ".vscode", "unity-performance-thresholds.json");
                
                string vscodeDir = System.IO.Path.Combine(projectPath, ".vscode");
                if (!System.IO.Directory.Exists(vscodeDir))
                    System.IO.Directory.CreateDirectory(vscodeDir);
                
                System.IO.File.WriteAllText(settingsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to save thresholds: {e.Message}");
            }
        }

        private static void LoadThresholds()
        {
            try
            {
                string projectPath = Application.dataPath.Replace("/Assets", "");
                string settingsPath = System.IO.Path.Combine(projectPath, ".vscode", "unity-performance-thresholds.json");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    string json = System.IO.File.ReadAllText(settingsPath);
                    thresholds = EditorJsonUtility.FromJson<PerformanceThresholds>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Performance Monitor: Failed to load thresholds, using defaults: {e.Message}");
                thresholds = new PerformanceThresholds();
            }
        }

        // Initialize on editor load
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            LoadThresholds();
            
            // Hook into the performance monitor
            EditorApplication.update += () =>
            {
                var metrics = PerformanceMonitor.GetCurrentMetrics();
                if (metrics != null)
                {
                    CheckPerformanceThresholds(metrics);
                }
            };
        }
    }
} 