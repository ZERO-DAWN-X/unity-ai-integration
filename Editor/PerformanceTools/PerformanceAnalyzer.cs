using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.Performance
{
    public class PerformanceAnalyzer
    {
        private static readonly float MemoryDivider = 1024 * 1024f; // Convert to MB
        private static readonly int MaxSamples = 300; // 5 minutes at 1 sample per second
        private float _lastFrameTime;
        private Queue<float> _fpsHistory = new Queue<float>();
        
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
        }

        public PerformanceAnalyzer()
        {
            _lastFrameTime = Time.realtimeSinceStartup;
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;

            float fps = 1.0f / Mathf.Max(deltaTime, 0.000001f);
            
            // Add to history and maintain max size
            _fpsHistory.Enqueue(fps);
            if (_fpsHistory.Count > MaxSamples)
                _fpsHistory.Dequeue();

            // Calculate average FPS
            float avgFps = _fpsHistory.Count > 0 ? _fpsHistory.Average() : fps;

            return new PerformanceMetrics
            {
                FPS = avgFps,
                FrameTime = deltaTime * 1000f, // Convert to milliseconds
                TotalMemory = Profiler.GetTotalReservedMemoryLong() / MemoryDivider,
                AllocatedMemory = Profiler.GetTotalAllocatedMemoryLong() / MemoryDivider,
                MonoMemory = Profiler.GetMonoUsedSizeLong() / MemoryDivider,
                DrawCalls = GetDrawCallCount(),
                VertexCount = GetVertexCount(),
                BatchCount = GetBatchCount()
            };
        }

        private int GetDrawCallCount()
        {
            int drawCalls = 0;
            var renderers = Object.FindObjectsOfType<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.isVisible)
                {
                    drawCalls++;
                }
            }
            return drawCalls;
        }

        private int GetVertexCount()
        {
            int vertexCount = 0;
            var meshFilters = Object.FindObjectsOfType<MeshFilter>();
            var skinnedMeshes = Object.FindObjectsOfType<SkinnedMeshRenderer>();

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null && meshFilter.GetComponent<Renderer>()?.isVisible == true)
                {
                    vertexCount += meshFilter.sharedMesh.vertexCount;
                }
            }

            foreach (var skinnedMesh in skinnedMeshes)
            {
                if (skinnedMesh.sharedMesh != null && skinnedMesh.isVisible)
                {
                    vertexCount += skinnedMesh.sharedMesh.vertexCount;
                }
            }

            return vertexCount;
        }

        private int GetBatchCount()
        {
            int batchCount = 0;
            var renderers = Object.FindObjectsOfType<Renderer>();
            var materials = new HashSet<Material>();

            foreach (var renderer in renderers)
            {
                if (renderer.isVisible)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }

            batchCount = materials.Count;
            return batchCount;
        }
    }
} 