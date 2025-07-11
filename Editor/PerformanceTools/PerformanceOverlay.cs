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
        private static PerformanceAnalyzer _analyzer;

        // Initialize the overlay
        static PerformanceOverlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnUpdate;
            _analyzer = new PerformanceAnalyzer();
        }

        [MenuItem("Window/Visual Studio/Performance Tools/Toggle FPS Overlay %#f")] // Ctrl+Shift+F or Cmd+Shift+F
        public static void ToggleOverlay()
        {
            _showOverlay = !_showOverlay;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _showOverlay = true;
                InitializeGUIStyle();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _showOverlay = false;
            }
        }

        private static void InitializeGUIStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                _labelStyle.normal.textColor = _textColor;
            }
        }

        private static void OnUpdate()
        {
            if (_showOverlay && EditorApplication.isPlaying)
            {
                // Force repaint of Game view
                var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                if (gameView != null)
                {
                    gameView.Repaint();
                }
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterGameViewCallback()
        {
            EditorApplication.update += () =>
            {
                if (_showOverlay && EditorApplication.isPlaying)
                {
                    var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                    if (gameView != null)
                    {
                        gameView.wantsMouseMove = true;
                        gameView.Repaint();
                    }
                }
            };
        }

        private static void DrawOverlay()
        {
            if (!_showOverlay || !EditorApplication.isPlaying) return;

            var metrics = _analyzer.GetCurrentMetrics();
            
            // Draw background
            EditorGUI.DrawRect(_windowRect, _backgroundColor);

            // Prepare GUI
            GUILayout.BeginArea(_windowRect);
            {
                // FPS Color based on performance
                Color fpsColor = metrics.FPS >= _targetFPS ? Color.green :
                               metrics.FPS >= _targetFPS * 0.5f ? Color.yellow :
                               Color.red;

                _labelStyle.normal.textColor = fpsColor;
                GUILayout.Label($"FPS: {metrics.FPS:F1}", _labelStyle);

                _labelStyle.normal.textColor = _textColor;
                GUILayout.Label($"Frame Time: {metrics.FrameTime:F1}ms", _labelStyle);
                GUILayout.Label($"Draw Calls: {metrics.DrawCalls}", _labelStyle);
                GUILayout.Label($"Memory: {metrics.TotalMemory:F1}MB", _labelStyle);
            }
            GUILayout.EndArea();
        }

        [InitializeOnLoadMethod]
        private static void RegisterGameViewGUI()
        {
            // Subscribe to Game view's OnGUI
            EditorApplication.update += () =>
            {
                if (_showOverlay && EditorApplication.isPlaying)
                {
                    var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    var gameView = EditorWindow.GetWindow(gameViewType);
                    if (gameView != null)
                    {
                        var onGUIMethod = gameViewType.GetMethod("OnGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (onGUIMethod != null)
                        {
                            DrawOverlay();
                            gameView.Repaint();
                        }
                    }
                }
            };
        }
    }
} 