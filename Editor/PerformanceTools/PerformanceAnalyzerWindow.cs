                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.Performance
{
    public class PerformanceAnalyzerWindow : EditorWindow
    {
        private bool _isRecording;
        private Vector2 _scrollPosition;
        private bool _showFPS = true;
        private bool _showMemory = true;
        private bool _showDrawCalls = true;
        private bool _showCPUUsage = true;
        private bool _showGPUUsage = true;
        private bool _showPhysics = true;
        private bool _showAnimation = true;
        private bool _showScripts = true;
        private bool _showSuggestions = true;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _graphBackgroundStyle;
        private Texture2D _graphBackground;
        private Color _graphLineColor = new Color(0.3f, 0.85f, 0.3f);
        private float _graphHeight = 100f;
        private float _graphWidth = 200f;

        [MenuItem("Window/Analysis/Performance Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceAnalyzerWindow>();
            window.titleContent = new GUIContent("Performance", EditorGUIUtility.IconContent("ProfilerCPU").image);
            window.minSize = new Vector2(450, 400);
        }

        private void OnEnable()
        {
            InitializeStyles();
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            if (_isRecording)
            {
                PerformanceAnalyzer.StopRecording();
                _isRecording = false;
            }
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                margin = new RectOffset(5, 5, 2, 2)
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(5, 5, 2, 2)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(1f, 0.7f, 0f) },
                margin = new RectOffset(5, 5, 2, 2)
            };

            _graphBackground = new Texture2D(1, 1);
            _graphBackground.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f));
            _graphBackground.Apply();

            _graphBackgroundStyle = new GUIStyle
            {
                normal = { background = _graphBackground }
            };
        }

        private void OnUpdate()
        {
            if (_isRecording)
            {
                PerformanceAnalyzer.Update();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_showFPS)
                DrawFPSSection();
            
            if (_showMemory)
                DrawMemorySection();
            
            if (_showDrawCalls)
                DrawDrawCallsSection();
            
            if (_showCPUUsage)
                DrawCPUSection();
            
            if (_showGPUUsage)
                DrawGPUSection();
            
            if (_showPhysics)
                DrawPhysicsSection();
            
            if (_showAnimation)
                DrawAnimationSection();
            
            if (_showScripts)
                DrawScriptsSection();
            
            if (_showSuggestions)
                DrawSuggestionsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(_isRecording ? "Stop" : "Record", EditorStyles.toolbarButton))
            {
                _isRecording = !_isRecording;
                if (_isRecording)
                    PerformanceAnalyzer.StartRecording();
                else
                    PerformanceAnalyzer.StopRecording();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                // Clear history implementation
            }

            GUILayout.FlexibleSpace();

            _showFPS = GUILayout.Toggle(_showFPS, "FPS", EditorStyles.toolbarButton);
            _showMemory = GUILayout.Toggle(_showMemory, "Memory", EditorStyles.toolbarButton);
            _showDrawCalls = GUILayout.Toggle(_showDrawCalls, "Draw Calls", EditorStyles.toolbarButton);
            _showCPUUsage = GUILayout.Toggle(_showCPUUsage, "CPU", EditorStyles.toolbarButton);
            _showGPUUsage = GUILayout.Toggle(_showGPUUsage, "GPU", EditorStyles.toolbarButton);
            _showPhysics = GUILayout.Toggle(_showPhysics, "Physics", EditorStyles.toolbarButton);
            _showAnimation = GUILayout.Toggle(_showAnimation, "Animation", EditorStyles.toolbarButton);
            _showScripts = GUILayout.Toggle(_showScripts, "Scripts", EditorStyles.toolbarButton);
            _showSuggestions = GUILayout.Toggle(_showSuggestions, "Suggestions", EditorStyles.toolbarButton);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFPSSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Framerate", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Current FPS:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.FPS:F1}", GetFPSStyle(metrics.FPS));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Frame Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.FrameTime:F1} ms", _valueStyle);
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.FPS).ToArray(), 0, 120);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMemorySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Memory Usage", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.TotalMemory:F1} MB", _valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Allocated:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.AllocatedMemory:F1} MB", _valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mono:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.MonoMemory:F1} MB", _valueStyle);
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.TotalMemory).ToArray(), 0, Mathf.Max(metrics.TotalMemory * 1.2f));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDrawCallsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rendering Statistics", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Draw Calls:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.DrawCalls}", GetDrawCallStyle(metrics.DrawCalls));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Batches:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.BatchCount}", _valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Vertices:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.VertexCount:N0}", _valueStyle);
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => (float)m.DrawCalls).ToArray(), 0, Mathf.Max(metrics.DrawCalls * 1.2f));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCPUSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CPU Performance", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Frame Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.CpuFrameTime:F1} ms", GetFrameTimeStyle(metrics.CpuFrameTime));
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.CpuFrameTime).ToArray(), 0, 33.33f);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGPUSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("GPU Performance", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("GPU Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.GpuTime:F1} ms", GetFrameTimeStyle(metrics.GpuTime));
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.GpuTime).ToArray(), 0, 33.33f);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPhysicsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Physics Performance", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Physics Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.PhysicsTime:F1} ms", GetFrameTimeStyle(metrics.PhysicsTime));
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.PhysicsTime).ToArray(), 0, 16.67f);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Animation Performance", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Animation Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.AnimationTime:F1} ms", GetFrameTimeStyle(metrics.AnimationTime));
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.AnimationTime).ToArray(), 0, 16.67f);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawScriptsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Script Performance", _headerStyle);

            var metrics = PerformanceAnalyzer.CurrentMetrics;
            if (metrics != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Script Time:", _labelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{metrics.ScriptTime:F1} ms", GetFrameTimeStyle(metrics.ScriptTime));
                EditorGUILayout.EndHorizontal();

                DrawGraph(PerformanceAnalyzer.History.Select(m => m.ScriptTime).ToArray(), 0, 16.67f);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSuggestionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Optimization Suggestions", _headerStyle);

            var report = PerformanceAnalyzer.GenerateReport();
            if (report != null)
            {
                var suggestions = report.GetOptimizationSuggestions();
                if (suggestions.Count > 0)
                {
                    foreach (var suggestion in suggestions)
                    {
                        EditorGUILayout.LabelField(suggestion, _warningStyle);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No optimization suggestions at this time.", _labelStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGraph(float[] values, float minValue, float maxValue)
        {
            if (values == null || values.Length == 0) return;

            var rect = GUILayoutUtility.GetRect(_graphWidth, _graphHeight);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            if (values.Length >= 2)
            {
                for (int i = 0; i < values.Length - 1; i++)
                {
                    float x1 = rect.x + (i * rect.width / (values.Length - 1));
                    float x2 = rect.x + ((i + 1) * rect.width / (values.Length - 1));
                    float y1 = rect.y + rect.height - ((values[i] - minValue) / (maxValue - minValue) * rect.height);
                    float y2 = rect.y + rect.height - ((values[i + 1] - minValue) / (maxValue - minValue) * rect.height);

                    Handles.color = _graphLineColor;
                    Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
                }
            }

            // Draw threshold line for FPS at 60
            if (values == PerformanceAnalyzer.History.Select(m => m.FPS).ToArray())
            {
                float thresholdY = rect.y + rect.height - ((60f - minValue) / (maxValue - minValue) * rect.height);
                Handles.color = new Color(1f, 0.5f, 0f, 0.5f);
                Handles.DrawLine(new Vector3(rect.x, thresholdY), new Vector3(rect.x + rect.width, thresholdY));
            }
        }

        private GUIStyle GetFPSStyle(float fps)
        {
            if (fps < 30)
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.red } };
            if (fps < 60)
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.yellow } };
            return new GUIStyle(_valueStyle) { normal = { textColor = Color.green } };
        }

        private GUIStyle GetDrawCallStyle(int drawCalls)
        {
            if (drawCalls > 1000)
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.red } };
            if (drawCalls > 500)
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.yellow } };
            return _valueStyle;
        }

        private GUIStyle GetFrameTimeStyle(float time)
        {
            if (time > 16.67f) // Below 60 FPS
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.red } };
            if (time > 8.33f) // Below 120 FPS
                return new GUIStyle(_valueStyle) { normal = { textColor = Color.yellow } };
            return _valueStyle;
        }
    }
} 