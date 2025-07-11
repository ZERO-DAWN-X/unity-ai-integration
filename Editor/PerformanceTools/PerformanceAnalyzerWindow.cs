using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.Performance
{
    public class PerformanceAnalyzerWindow : EditorWindow
    {
        private PerformanceAnalyzer _analyzer;
        private Vector2 _scrollPosition;
        private bool _isRecording = false;
        private List<PerformanceAnalyzer.PerformanceMetrics> _metricsHistory = new List<PerformanceAnalyzer.PerformanceMetrics>();
        private readonly int _maxHistorySize = 300; // 5 minutes at 1 sample per second
        private float _updateInterval = 1.0f;
        private float _timeSinceLastUpdate = 0f;

        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _warningStyle;

        [MenuItem("Window/Visual Studio/Performance Tools/Performance Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<PerformanceAnalyzerWindow>("Performance Analyzer");
        }

        private void OnEnable()
        {
            _analyzer = new PerformanceAnalyzer();
            InitializeStyles();
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
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
                margin = new RectOffset(10, 5, 2, 2),
                padding = new RectOffset(5, 5, 2, 2)
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                margin = new RectOffset(5, 10, 2, 2),
                padding = new RectOffset(5, 5, 2, 2)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow },
                margin = new RectOffset(10, 5, 2, 2),
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        private void OnUpdate()
        {
            if (!_isRecording || !EditorApplication.isPlaying) return;

            _timeSinceLastUpdate += Time.deltaTime;
            if (_timeSinceLastUpdate >= _updateInterval)
            {
                var metrics = _analyzer.GetCurrentMetrics();
                _metricsHistory.Add(metrics);
                
                // Keep history size in check
                while (_metricsHistory.Count > _maxHistorySize)
                {
                    _metricsHistory.RemoveAt(0);
                }

                _timeSinceLastUpdate = 0f;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawPerformanceData();
            DrawGraph();
            DrawSuggestions();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button(_isRecording ? "Stop Recording" : "Start Recording", EditorStyles.toolbarButton))
                {
                    _isRecording = !_isRecording;
                    if (!_isRecording)
                    {
                        // Optional: Save or process recorded data
                    }
                }

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                {
                    _metricsHistory.Clear();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPerformanceData()
        {
            if (_metricsHistory.Count == 0) return;

            var currentMetrics = _metricsHistory.Last();
            var avgFps = _metricsHistory.Average(m => m.FPS);
            var minFps = _metricsHistory.Min(m => m.FPS);
            var maxFps = _metricsHistory.Max(m => m.FPS);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Current Performance", _headerStyle);

                DrawMetricRow("FPS", $"{currentMetrics.FPS:F1}", GetFPSColor(currentMetrics.FPS));
                DrawMetricRow("Frame Time", $"{currentMetrics.FrameTime:F1} ms");
                DrawMetricRow("Draw Calls", currentMetrics.DrawCalls.ToString());
                DrawMetricRow("Total Memory", $"{currentMetrics.TotalMemory:F1} MB");
                DrawMetricRow("Allocated Memory", $"{currentMetrics.AllocatedMemory:F1} MB");
                DrawMetricRow("Mono Memory", $"{currentMetrics.MonoMemory:F1} MB");
                DrawMetricRow("Vertex Count", $"{currentMetrics.VertexCount:N0}");
                DrawMetricRow("Batch Count", currentMetrics.BatchCount.ToString());

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Statistics", _headerStyle);
                DrawMetricRow("Average FPS", $"{avgFps:F1}");
                DrawMetricRow("Min FPS", $"{minFps:F1}");
                DrawMetricRow("Max FPS", $"{maxFps:F1}");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMetricRow(string label, string value, Color? colorOverride = null)
        {
            EditorGUILayout.BeginHorizontal();
            {
                var style = _labelStyle;
                if (colorOverride.HasValue)
                {
                    style = new GUIStyle(_labelStyle);
                    style.normal.textColor = colorOverride.Value;
                }

                EditorGUILayout.LabelField(label, style);
                EditorGUILayout.LabelField(value, _valueStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGraph()
        {
            if (_metricsHistory.Count < 2) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("FPS Over Time", _headerStyle);
                
                var rect = GUILayoutUtility.GetRect(Screen.width, 150);
                var padding = 20f;
                var graphRect = new Rect(rect.x + padding, rect.y + padding, 
                                      rect.width - (padding * 2), rect.height - (padding * 2));

                // Draw background
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

                // Draw graph
                var fpsValues = _metricsHistory.Select(m => m.FPS).ToArray();
                var maxValue = Mathf.Max(fpsValues.Max(), 60); // At least show up to 60 FPS
                
                // Draw horizontal lines
                for (int i = 0; i <= 5; i++)
                {
                    float y = graphRect.y + (graphRect.height * (1 - (i / 5f)));
                    float value = maxValue * (i / 5f);
                    Handles.color = new Color(1, 1, 1, 0.2f);
                    Handles.DrawLine(new Vector2(graphRect.x, y), new Vector2(graphRect.x + graphRect.width, y));
                    EditorGUI.LabelField(new Rect(graphRect.x - 40, y - 8, 35, 16), 
                                       $"{value:F0}", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });
                }

                // Draw FPS line
                Handles.color = Color.green;
                for (int i = 0; i < fpsValues.Length - 1; i++)
                {
                    float x1 = graphRect.x + (graphRect.width * (i / (float)(fpsValues.Length - 1)));
                    float x2 = graphRect.x + (graphRect.width * ((i + 1) / (float)(fpsValues.Length - 1)));
                    float y1 = graphRect.y + (graphRect.height * (1 - (fpsValues[i] / maxValue)));
                    float y2 = graphRect.y + (graphRect.height * (1 - (fpsValues[i + 1] / maxValue)));
                    
                    Handles.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2));
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSuggestions()
        {
            if (_metricsHistory.Count == 0) return;

            var currentMetrics = _metricsHistory.Last();
            var suggestions = new List<string>();

            if (currentMetrics.FPS < 60)
                suggestions.Add("FPS is below 60. Consider optimizing performance.");
            if (currentMetrics.DrawCalls > 1000)
                suggestions.Add("High number of draw calls. Consider using batching or atlasing.");
            if (currentMetrics.TotalMemory > 1000)
                suggestions.Add("High memory usage. Check for memory leaks.");
            if (currentMetrics.BatchCount > 100)
                suggestions.Add("High batch count. Consider using material instancing or atlasing.");

            if (suggestions.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Optimization Suggestions", _headerStyle);
                    foreach (var suggestion in suggestions)
                    {
                        EditorGUILayout.LabelField(suggestion, _warningStyle);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private Color GetFPSColor(float fps)
        {
            if (fps >= 60) return Color.green;
            if (fps >= 30) return Color.yellow;
            return Color.red;
        }
    }
} 