using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.Performance
{
    public class PerformanceAnalyzer
    {
        private static readonly float MemoryDivider = 1024 * 1024f; // Convert to MB
        private static readonly int MaxSamples = 300; // 5 minutes at 1 sample per second
        
        public class PerformanceMetrics
        {
            public float FPS { get; set; }
            public float FrameTime { get; set; }
            public float TotalMemory { get; set; }
            public float AllocatedMemory { get; set; }
            public float MonoMemory { get; set; }
            public int DrawCalls { get; set; }
            public int VertexCount { get; set; }
            public int BatchCount { get; set; }
            public float GpuTime { get; set; }
            public float CpuFrameTime { get; set; }
            public float GCMemory { get; set; }
            public int ActiveGameObjects { get; set; }
            public int TotalComponents { get; set; }
            public float PhysicsTime { get; set; }
            public float AnimationTime { get; set; }
            public float ScriptTime { get; set; }
        }

        private static readonly Queue<PerformanceMetrics> MetricsHistory = new Queue<PerformanceMetrics>();
        private static float _lastGCMemory;
        private static float _deltaTime;
        private static float _updateInterval = 1.0f;
        private static float _timeSinceLastUpdate;
        private static float _lastPhysicsTime;
        private static float _lastAnimationTime;
        private static float _lastScriptTime;

        public static PerformanceMetrics CurrentMetrics { get; private set; }
        public static IEnumerable<PerformanceMetrics> History => MetricsHistory;

        public static void StartRecording()
        {
            _lastPhysicsTime = Time.realtimeSinceStartup;
            _lastAnimationTime = Time.realtimeSinceStartup;
            _lastScriptTime = Time.realtimeSinceStartup;
        }

        public static void StopRecording()
        {
            // No cleanup needed for built-in profiling
        }

        public static void Update()
        {
            _deltaTime += Time.unscaledDeltaTime;
            _timeSinceLastUpdate += Time.unscaledDeltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                CollectMetrics();
                _timeSinceLastUpdate = 0f;
            }
        }

        private static void CollectMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                FPS = 1.0f / Time.unscaledDeltaTime,
                FrameTime = Time.unscaledDeltaTime * 1000f,
                TotalMemory = Profiler.GetTotalReservedMemoryLong() / MemoryDivider,
                AllocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / MemoryDivider,
                MonoMemory = Profiler.GetMonoUsedSizeLong() / MemoryDivider,
                DrawCalls = UnityStats.drawCalls,
                VertexCount = UnityStats.vertices,
                BatchCount = UnityStats.batches,
                GpuTime = UnityStats.gpuTime,
                CpuFrameTime = UnityStats.renderTime,
                ActiveGameObjects = Object.FindObjectsOfType<GameObject>().Length,
                TotalComponents = Object.FindObjectsOfType<Component>().Length
            };

            // Track GC allocations
            float currentGCMemory = System.GC.GetTotalMemory(false) / MemoryDivider;
            metrics.GCMemory = currentGCMemory - _lastGCMemory;
            _lastGCMemory = currentGCMemory;

            // Track physics time
            Profiler.BeginSample("Physics");
            float currentPhysicsTime = Time.realtimeSinceStartup;
            metrics.PhysicsTime = (currentPhysicsTime - _lastPhysicsTime) * 1000f; // Convert to ms
            _lastPhysicsTime = currentPhysicsTime;
            Profiler.EndSample();

            // Track animation time
            Profiler.BeginSample("Animation");
            float currentAnimationTime = Time.realtimeSinceStartup;
            metrics.AnimationTime = (currentAnimationTime - _lastAnimationTime) * 1000f;
            _lastAnimationTime = currentAnimationTime;
            Profiler.EndSample();

            // Track script time
            Profiler.BeginSample("Scripts");
            float currentScriptTime = Time.realtimeSinceStartup;
            metrics.ScriptTime = (currentScriptTime - _lastScriptTime) * 1000f;
            _lastScriptTime = currentScriptTime;
            Profiler.EndSample();

            CurrentMetrics = metrics;
            MetricsHistory.Enqueue(metrics);

            if (MetricsHistory.Count > MaxSamples)
                MetricsHistory.Dequeue();
        }

        public static PerformanceReport GenerateReport()
        {
            if (!MetricsHistory.Any())
                return null;

            return new PerformanceReport
            {
                AverageFPS = MetricsHistory.Average(m => m.FPS),
                MinFPS = MetricsHistory.Min(m => m.FPS),
                MaxFPS = MetricsHistory.Max(m => m.FPS),
                AverageMemory = MetricsHistory.Average(m => m.TotalMemory),
                PeakMemory = MetricsHistory.Max(m => m.TotalMemory),
                AverageDrawCalls = (int)MetricsHistory.Average(m => m.DrawCalls),
                PeakDrawCalls = MetricsHistory.Max(m => m.DrawCalls),
                AverageGpuTime = MetricsHistory.Average(m => m.GpuTime),
                AverageCpuTime = MetricsHistory.Average(m => m.CpuFrameTime),
                TotalGCAllocations = MetricsHistory.Sum(m => m.GCMemory),
                AveragePhysicsTime = MetricsHistory.Average(m => m.PhysicsTime),
                AverageAnimationTime = MetricsHistory.Average(m => m.AnimationTime),
                AverageScriptTime = MetricsHistory.Average(m => m.ScriptTime)
            };
        }

        public class PerformanceReport
        {
            public float AverageFPS { get; set; }
            public float MinFPS { get; set; }
            public float MaxFPS { get; set; }
            public float AverageMemory { get; set; }
            public float PeakMemory { get; set; }
            public int AverageDrawCalls { get; set; }
            public int PeakDrawCalls { get; set; }
            public float AverageGpuTime { get; set; }
            public float AverageCpuTime { get; set; }
            public float TotalGCAllocations { get; set; }
            public float AveragePhysicsTime { get; set; }
            public float AverageAnimationTime { get; set; }
            public float AverageScriptTime { get; set; }

            public List<string> GetOptimizationSuggestions()
            {
                var suggestions = new List<string>();

                if (AverageFPS < 60)
                    suggestions.Add($"Low FPS ({AverageFPS:F1}). Consider optimizing heavy operations or reducing scene complexity.");

                if (AverageDrawCalls > 1000)
                    suggestions.Add($"High draw call count ({AverageDrawCalls}). Consider using batching or atlasing.");

                if (AverageMemory > 1000)
                    suggestions.Add($"High memory usage ({AverageMemory:F1} MB). Check for memory leaks and optimize asset loading.");

                if (AverageGpuTime > 16.67f) // 60 FPS threshold
                    suggestions.Add($"High GPU time ({AverageGpuTime:F1}ms). Consider optimizing shaders or reducing visual complexity.");

                if (AverageCpuTime > 16.67f)
                    suggestions.Add($"High CPU time ({AverageCpuTime:F1}ms). Profile scripts and physics operations.");

                if (TotalGCAllocations > 100)
                    suggestions.Add($"Significant GC allocations ({TotalGCAllocations:F1} MB). Review object creation and destruction patterns.");

                if (AveragePhysicsTime > 8f)
                    suggestions.Add($"High physics time ({AveragePhysicsTime:F1}ms). Optimize colliders and rigidbodies.");

                if (AverageAnimationTime > 5f)
                    suggestions.Add($"High animation time ({AverageAnimationTime:F1}ms). Consider reducing animator complexity.");

                if (AverageScriptTime > 5f)
                    suggestions.Add($"High script execution time ({AverageScriptTime:F1}ms). Profile and optimize MonoBehaviour scripts.");

                return suggestions;
            }
        }
    }
} 