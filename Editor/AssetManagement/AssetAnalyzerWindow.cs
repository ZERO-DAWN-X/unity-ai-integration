using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor
{
    public class AssetAnalyzerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showUnusedAssets = true;
        private bool _showDependencies = true;
        private bool _showNamingIssues = true;
        private string _searchFilter = "";
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private List<string> _invalidNamedAssets;
        private bool _showOptimizationSettings = false;

        [MenuItem("Window/Asset Management/Asset Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetAnalyzerWindow>();
            window.titleContent = new GUIContent("Asset Analyzer");
            window.Show();
        }

        private void OnEnable()
        {
            _headerStyle = new GUIStyle();
            _headerStyle.fontSize = 14;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.margin = new RectOffset(5, 5, 5, 5);

            _sectionStyle = new GUIStyle();
            _sectionStyle.fontSize = 12;
            _sectionStyle.fontStyle = FontStyle.Bold;
            _sectionStyle.margin = new RectOffset(5, 5, 10, 5);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawOptions();
            DrawMainContent();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Analyze Assets", EditorStyles.toolbarButton))
            {
                AssetAnalyzer.AnalyzeProjectAssets();
                _invalidNamedAssets = AssetOptimizer.ValidateAssetNames();
            }

            if (GUILayout.Button("Optimize Assets", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("Optimize Assets",
                    "This will modify import settings for assets in your project. Continue?",
                    "Optimize", "Cancel"))
                {
                    AssetOptimizer.OptimizeAssetImportSettings();
                }
            }

            GUILayout.FlexibleSpace();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal();
            _showUnusedAssets = EditorGUILayout.ToggleLeft("Show Unused Assets", _showUnusedAssets);
            _showDependencies = EditorGUILayout.ToggleLeft("Show Dependencies", _showDependencies);
            _showNamingIssues = EditorGUILayout.ToggleLeft("Show Naming Issues", _showNamingIssues);
            EditorGUILayout.EndHorizontal();

            _showOptimizationSettings = EditorGUILayout.Foldout(_showOptimizationSettings, "Optimization Settings", true);
            if (_showOptimizationSettings)
            {
                EditorGUI.indentLevel++;
                DrawOptimizationSettings();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawMainContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_showUnusedAssets)
            {
                DrawUnusedAssets();
            }

            if (_showDependencies)
            {
                DrawDependencies();
            }

            if (_showNamingIssues && _invalidNamedAssets != null && _invalidNamedAssets.Count > 0)
            {
                DrawNamingIssues();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawOptimizationSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Texture Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Max Texture Size: 2048");
            EditorGUILayout.LabelField("Compression: Enabled for non-UI textures");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Model Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Mesh Compression: Medium (High for mobile)");
            EditorGUILayout.LabelField("Optimize Mesh: Enabled for mobile");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Short Clips (<1s): ADPCM, Decompress on Load");
            EditorGUILayout.LabelField("Long Clips (>1s): Vorbis, Streaming");
            EditorGUI.indentLevel--;
        }

        private void DrawUnusedAssets()
        {
            EditorGUILayout.LabelField("Unused Assets", _headerStyle);
            EditorGUILayout.Space();

            // Implementation will be added when we have data
            EditorGUILayout.HelpBox("Run 'Analyze Assets' to find unused assets", MessageType.Info);
        }

        private void DrawDependencies()
        {
            EditorGUILayout.LabelField("Asset Dependencies", _headerStyle);
            EditorGUILayout.Space();

            // Implementation will be added when we have data
            EditorGUILayout.HelpBox("Run 'Analyze Assets' to view dependencies", MessageType.Info);
        }

        private void DrawNamingIssues()
        {
            EditorGUILayout.LabelField("Naming Convention Issues", _headerStyle);
            EditorGUILayout.Space();

            foreach (var assetPath in _invalidNamedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !assetPath.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginHorizontal();

                // Asset icon and path
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var icon = AssetDatabase.GetCachedIcon(assetPath);
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(assetPath);

                // Suggested name
                string suggestedName = AssetOptimizer.SuggestAssetName(assetPath);
                if (GUILayout.Button("Fix Name", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Rename Asset",
                        $"Rename to: {suggestedName}?",
                        "Rename", "Cancel"))
                    {
                        string directory = System.IO.Path.GetDirectoryName(assetPath);
                        string extension = System.IO.Path.GetExtension(assetPath);
                        string newPath = System.IO.Path.Combine(directory ?? "", suggestedName + extension);
                        
                        AssetDatabase.MoveAsset(assetPath, newPath);
                        AssetDatabase.Refresh();
                        _invalidNamedAssets = AssetOptimizer.ValidateAssetNames();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAssetEntry(string assetPath, bool isUnused = false)
        {
            EditorGUILayout.BeginHorizontal();

            // Asset icon
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

            // Asset name and path
            EditorGUILayout.LabelField(assetPath);

            // Actions
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            if (isUnused && GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Asset",
                    $"Are you sure you want to delete {assetPath}?",
                    "Delete", "Cancel"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.Refresh();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
} 