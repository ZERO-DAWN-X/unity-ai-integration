using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.SceneManagement
{
    public class SceneAnalyzerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private SceneAnalyzer.SceneAnalysisResult _analysisResult;
        private bool _showHierarchy = true;
        private bool _showDependencies = true;
        private bool _showMissingRefs = true;
        private bool _showPrefabVariants = true;
        private bool _showOptimizations = true;
        private string _searchFilter = "";
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _criticalStyle;
        private GUIStyle _buttonStyle;

        [MenuItem("Window/Scene Management/Scene Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneAnalyzerWindow>();
            window.titleContent = new GUIContent("Scene Analyzer", EditorGUIUtility.IconContent("SceneAsset Icon").image);
            window.minSize = new Vector2(450, 300);
        }

        private void OnEnable()
        {
            InitializeStyles();
            if (_analysisResult == null)
            {
                AnalyzeScene();
            }
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _sectionStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 12,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.7f, 0f) },
                fontSize = 11
            };

            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 1f) },
                fontSize = 11
            };

            _criticalStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
                fontSize = 11
            };

            _buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(2, 2, 2, 2)
            };
        }

        private void OnGUI()
        {
            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_analysisResult != null)
            {
                if (_showHierarchy)
                    DrawHierarchySection();
                
                if (_showDependencies)
                    DrawDependenciesSection();
                
                if (_showMissingRefs)
                    DrawMissingReferencesSection();
                
                if (_showPrefabVariants)
                    DrawPrefabVariantsSection();
                
                if (_showOptimizations)
                    DrawOptimizationSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Analyze", EditorStyles.toolbarButton))
            {
                AnalyzeScene();
            }

            GUILayout.Space(10);

            _showHierarchy = GUILayout.Toggle(_showHierarchy, "Hierarchy", EditorStyles.toolbarButton);
            _showDependencies = GUILayout.Toggle(_showDependencies, "Dependencies", EditorStyles.toolbarButton);
            _showMissingRefs = GUILayout.Toggle(_showMissingRefs, "Missing Refs", EditorStyles.toolbarButton);
            _showPrefabVariants = GUILayout.Toggle(_showPrefabVariants, "Prefab Variants", EditorStyles.toolbarButton);
            _showOptimizations = GUILayout.Toggle(_showOptimizations, "Optimization", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            GUILayout.Label("Filter:", EditorStyles.toolbarButton);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHierarchySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Hierarchy Analysis", _headerStyle);

            if (_analysisResult.UnusedGameObjects.Count > 0)
            {
                EditorGUILayout.LabelField($"Unused GameObjects: {_analysisResult.UnusedGameObjects.Count}", _warningStyle);
                foreach (var obj in _analysisResult.UnusedGameObjects)
                {
                    if (obj == null) continue;
                    if (!string.IsNullOrEmpty(_searchFilter) && !obj.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    if (GUILayout.Button("Select", _buttonStyle, GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = obj;
                    }
                    if (GUILayout.Button("Delete", _buttonStyle, GUILayout.Width(60)))
                    {
                        DestroyImmediate(obj);
                        AnalyzeScene();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No unused GameObjects found", _infoStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDependenciesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dependencies", _headerStyle);

            foreach (var kvp in _analysisResult.Dependencies)
            {
                if (kvp.Key == null) continue;
                if (!string.IsNullOrEmpty(_searchFilter) && !kvp.Key.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true);
                
                EditorGUI.indentLevel++;
                foreach (var dependency in kvp.Value)
                {
                    if (dependency == null) continue;
                    EditorGUILayout.ObjectField("Depends on:", dependency, typeof(GameObject), true);
                }
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMissingReferencesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Missing References", _headerStyle);

            if (_analysisResult.MissingComponents.Count > 0 || _analysisResult.NullReferences.Count > 0)
            {
                // Missing Components
                foreach (var obj in _analysisResult.MissingComponents)
                {
                    if (obj == null) continue;
                    if (!string.IsNullOrEmpty(_searchFilter) && !obj.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                    if (GUILayout.Button("Select", _buttonStyle, GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = obj;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Null References
                foreach (var kvp in _analysisResult.NullReferences)
                {
                    if (kvp.Key == null) continue;
                    if (!string.IsNullOrEmpty(_searchFilter) && !kvp.Key.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.ObjectField(kvp.Key, typeof(GameObject), true);
                    
                    EditorGUI.indentLevel++;
                    foreach (var component in kvp.Value)
                    {
                        if (component == null) continue;
                        EditorGUILayout.ObjectField("Component with null refs:", component, typeof(Component), true);
                    }
                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No missing references found", _infoStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPrefabVariantsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Prefab Variants Analysis", _headerStyle);

            foreach (var analysis in _analysisResult.PrefabVariants)
            {
                if (analysis.Prefab == null) continue;
                if (!string.IsNullOrEmpty(_searchFilter) && !analysis.Prefab.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.ObjectField("Variant:", analysis.Prefab, typeof(GameObject), true);
                EditorGUILayout.ObjectField("Base:", analysis.BaseObject, typeof(GameObject), true);

                EditorGUI.indentLevel++;
                foreach (var diff in analysis.Differences)
                {
                    EditorGUILayout.LabelField($"{diff.Component.GetType().Name} - {diff.PropertyPath}");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"Base: {diff.BaseValue}", _infoStyle);
                    EditorGUILayout.LabelField($"Variant: {diff.VariantValue}", _infoStyle);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOptimizationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Optimization Tips", _headerStyle);

            foreach (var tip in _analysisResult.OptimizationTips)
            {
                if (tip.Target != null && !string.IsNullOrEmpty(_searchFilter) && 
                    !tip.Target.name.ToLower().Contains(_searchFilter.ToLower())) continue;

                GUIStyle style = tip.Severity switch
                {
                    SceneAnalyzer.TipSeverity.Info => _infoStyle,
                    SceneAnalyzer.TipSeverity.Warning => _warningStyle,
                    SceneAnalyzer.TipSeverity.Critical => _criticalStyle,
                    _ => _infoStyle
                };

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(tip.Description, style);
                if (tip.Target != null)
                {
                    if (GUILayout.Button("Select", _buttonStyle, GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = tip.Target;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void AnalyzeScene()
        {
            _analysisResult = SceneAnalyzer.AnalyzeActiveScene();
            Repaint();
        }
    }
} 