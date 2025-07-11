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
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized = false;

        [MenuItem("Window/Visual Studio/Performance Tools/Performance Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<PerformanceAnalyzerWindow>("Performance Analyzer");
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(5, 5, 2, 2)
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                margin = new RectOffset(5, 5, 2, 2)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 5, 5)
            };

            _stylesInitialized = true;
        }

        private void OnEnable()
        {
            _analyzer = new PerformanceAnalyzer();
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        private void OnUpdate()
        {
            if (!_isRecording) return;

            _timeSinceLastUpdate += Time.deltaTime;
            if (_timeSinceLastUpdate >= _updateInterval)
            {
                var metrics = _analyzer.GetCurrentMetrics();
                _metricsHistory.Add(metrics);
                
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
            if (!_stylesInitialized)
            {
                InitializeStyles();
            }

            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.LabelField("Performance Metrics", _headerStyle);

            // Record button
            GUI.backgroundColor = _isRecording ? Color.red : Color.green;
            if (GUILayout.Button(_isRecording ? "Stop Recording" : "Start Recording", _buttonStyle))
            {
                _isRecording = !_isRecording;
                if (!_isRecording)
                {
                    _metricsHistory.Clear();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            // Current metrics
            if (_analyzer != null)
            {
                var metrics = _analyzer.GetCurrentMetrics();
                if (metrics != null)
                {
                    DrawMetrics(metrics);
                }
            }

            // History graph
            if (_isRecording && _metricsHistory.Count > 0)
            {
                DrawPerformanceGraph();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMetrics(PerformanceAnalyzer.PerformanceMetrics metrics)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Real-time Metrics", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FPS", _labelStyle);
            EditorGUILayout.LabelField(metrics.FPS.ToString("F1"), GetFPSStyle(metrics.FPS));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Frame Time", _labelStyle);
            EditorGUILayout.LabelField($"{metrics.FrameTime:F1} ms", _valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Memory", _labelStyle);
            EditorGUILayout.LabelField($"{metrics.TotalMemory:F1} MB", _valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Draw Calls", _labelStyle);
            EditorGUILayout.LabelField(metrics.DrawCalls.ToString(), _valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vertices", _labelStyle);
            EditorGUILayout.LabelField(metrics.VertexCount.ToString(), _valueStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private GUIStyle GetFPSStyle(float fps)
        {
            var style = new GUIStyle(_valueStyle);
            if (fps >= 55)
                style.normal.textColor = Color.green;
            else if (fps >= 30)
                style.normal.textColor = Color.yellow;
            else
                style.normal.textColor = Color.red;
            return style;
        }

        private void DrawPerformanceGraph()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("FPS History", _headerStyle);

            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, 200);
            var graphRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);

            // Draw background
            EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));

            // Draw grid lines
            DrawGridLines(graphRect);

            // Draw FPS line
            if (_metricsHistory.Count > 1)
            {
                var maxFPS = Mathf.Max(_metricsHistory.Max(m => m.FPS), 60);
                var points = new Vector3[_metricsHistory.Count];

                for (int i = 0; i < _metricsHistory.Count; i++)
                {
                    var normalizedX = (float)i / (_metricsHistory.Count - 1);
                    var normalizedY = _metricsHistory[i].FPS / maxFPS;
                    points[i] = new Vector3(
                        graphRect.x + normalizedX * graphRect.width,
                        graphRect.y + (1 - normalizedY) * graphRect.height,
                        0
                    );
                }

                Handles.color = Color.green;
                Handles.DrawAAPolyLine(2f, points);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGridLines(Rect rect)
        {
            Handles.color = new Color(1, 1, 1, 0.2f);

            // Vertical lines
            for (int i = 0; i <= 10; i++)
            {
                float x = rect.x + (rect.width * i / 10);
                Handles.DrawLine(new Vector3(x, rect.y, 0), new Vector3(x, rect.y + rect.height, 0));
            }

            // Horizontal lines
            for (int i = 0; i <= 4; i++)
            {
                float y = rect.y + (rect.height * i / 4);
                Handles.DrawLine(new Vector3(rect.x, y, 0), new Vector3(rect.x + rect.width, y, 0));
            }
        }
    }
} 