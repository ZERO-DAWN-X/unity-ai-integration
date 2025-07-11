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
        private Dictionary<string, bool> _assetUsageStatus;
        private Dictionary<string, List<string>> _assetDependencies;

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
                RunAnalysis();
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

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshData();
            }

            GUILayout.FlexibleSpace();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();
        }

        private void RunAnalysis()
        {
            AssetAnalyzer.AnalyzeProjectAssets();
            RefreshData();
        }

        private void RefreshData()
        {
            _invalidNamedAssets = AssetOptimizer.ValidateAssetNames();
            _assetUsageStatus = AssetAnalyzer.GetAssetUsageStatus();
            _assetDependencies = AssetAnalyzer.GetAssetDependencies();
            Repaint();
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

            if (_assetUsageStatus == null || _assetUsageStatus.Count == 0)
            {
                EditorGUILayout.HelpBox("Run 'Analyze Assets' to find unused assets", MessageType.Info);
                return;
            }

            var unusedAssets = _assetUsageStatus.Where(x => !x.Value).ToList();
            if (unusedAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("No unused assets found", MessageType.Info);
                return;
            }

            foreach (var asset in unusedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !asset.Key.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                DrawAssetEntry(asset.Key, true);
            }
        }

        private void DrawDependencies()
        {
            EditorGUILayout.LabelField("Asset Dependencies", _headerStyle);
            EditorGUILayout.Space();

            if (_assetDependencies == null || _assetDependencies.Count == 0)
            {
                EditorGUILayout.HelpBox("Run 'Analyze Assets' to view dependencies", MessageType.Info);
                return;
            }

            foreach (var asset in _assetDependencies.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !asset.Key.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                var obj = AssetDatabase.LoadAssetAtPath<Object>(asset.Key);
                var icon = AssetDatabase.GetCachedIcon(asset.Key);
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(asset.Key, EditorStyles.boldLabel);
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                if (asset.Value.Any())
                {
                    foreach (var dependency in asset.Value)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var depObj = AssetDatabase.LoadAssetAtPath<Object>(dependency);
                        var depIcon = AssetDatabase.GetCachedIcon(dependency);
                        GUILayout.Space(20);
                        GUILayout.Label(depIcon, GUILayout.Width(20), GUILayout.Height(20));
                        EditorGUILayout.LabelField(dependency);
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = depObj;
                            EditorGUIUtility.PingObject(depObj);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No dependencies");
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
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
                    RefreshData();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
} 