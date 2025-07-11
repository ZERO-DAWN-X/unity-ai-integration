using UnityEngine;
using UnityEditor;

namespace Microsoft.Unity.VisualStudio.Editor.Performance
{
    [InitializeOnLoad]
    public class PerformanceOverlay
    {
        private static bool _showOverlay = false;
        private static GUIStyle _labelStyle;
        private static Rect _windowRect = new Rect(10, 10, 200, 100);
        private static Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        private static Color _textColor = Color.white;
        private static int _targetFPS = 60;

        // Initialize the overlay
        static PerformanceOverlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem("Window/Visual Studio/Performance Tools/Toggle FPS Overlay %#f")]
        public static void ToggleOverlay()
        {
            _showOverlay = !_showOverlay;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                PerformanceAnalyzer.StartRecording();
                EditorApplication.update += UpdateOverlay;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                PerformanceAnalyzer.StopRecording();
                EditorApplication.update -= UpdateOverlay;
            }
        }

        private static void UpdateOverlay()
        {
            if (_showOverlay)
            {
                PerformanceAnalyzer.Update();
                EditorWindow.GetWindow<SceneView>()?.Repaint();
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!_showOverlay || !EditorApplication.isPlaying) return;

            InitializeStyles();

            Handles.BeginGUI();

            // Draw background
            GUI.color = _backgroundColor;
            GUI.Box(_windowRect, GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(_windowRect);
            {
                var metrics = PerformanceAnalyzer.CurrentMetrics;
                if (metrics != null)
                {
                    _labelStyle.normal.textColor = GetFPSColor(metrics.FPS);
                    GUILayout.Label($"FPS: {metrics.FPS:F1}", _labelStyle);

                    _labelStyle.normal.textColor = _textColor;
                    GUILayout.Label($"Frame Time: {metrics.FrameTime:F1}ms", _labelStyle);
                    GUILayout.Label($"Draw Calls: {metrics.DrawCalls}", _labelStyle);
                    GUILayout.Label($"Memory: {metrics.TotalMemory:F1}MB", _labelStyle);
                }
                else
                {
                    GUILayout.Label("Collecting data...", _labelStyle);
                }
            }
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private static void InitializeStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(5, 5, 2, 2)
                };
                _labelStyle.normal.textColor = _textColor;
            }
        }

        private static Color GetFPSColor(float fps)
        {
            if (fps >= _targetFPS) return Color.green;
            if (fps >= _targetFPS * 0.5f) return Color.yellow;
            return Color.red;
        }

        // Make the window draggable
        private void DragWindow(int windowID)
        {
            GUI.DragWindow();
        }
    }
} 