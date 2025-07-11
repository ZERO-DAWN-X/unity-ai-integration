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
        private GUIStyle _buttonStyle;
        private GUIStyle _searchStyle;
        private GUIStyle _assetBoxStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIStyle _toggleStyle;
        private List<string> _invalidNamedAssets;
        private bool _showOptimizationSettings = false;
        private Dictionary<string, bool> _assetUsageStatus;
        private Dictionary<string, List<string>> _assetDependencies;
        private Color _proColor = new Color(0.2f, 0.2f, 0.2f);
        private Color _personalColor = new Color(0.75f, 0.75f, 0.75f);
        private Color _accentColor = new Color(0.2f, 0.4f, 0.7f);
        private Texture2D _backgroundTexture;

        [MenuItem("Window/Asset Management/Asset Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetAnalyzerWindow>();
            window.titleContent = new GUIContent("Asset Analyzer", EditorGUIUtility.IconContent("d_PreMatCube").image);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            // Header style
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _headerStyle.fontSize = 16;
            _headerStyle.margin = new RectOffset(10, 10, 10, 10);
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // Section style
            _sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionStyle.fontSize = 13;
            _sectionStyle.margin = new RectOffset(5, 5, 10, 5);
            _sectionStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            _buttonStyle.hover.textColor = _accentColor;
            _buttonStyle.fixedWidth = 60;
            _buttonStyle.margin = new RectOffset(2, 2, 2, 2);

            // Search style
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            _searchStyle.fixedWidth = 200;
            _searchStyle.margin = new RectOffset(5, 5, 5, 5);

            // Asset box style
            _assetBoxStyle = new GUIStyle(EditorStyles.helpBox);
            _assetBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            _assetBoxStyle.margin = new RectOffset(5, 5, 5, 5);

            // Toolbar button style
            _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
            _toolbarButtonStyle.fontSize = 12;
            _toolbarButtonStyle.fixedHeight = 25;
            _toolbarButtonStyle.margin = new RectOffset(5, 5, 5, 5);
            _toolbarButtonStyle.padding = new RectOffset(10, 10, 5, 5);

            // Toggle style
            _toggleStyle = new GUIStyle(EditorStyles.toggle);
            _toggleStyle.fontSize = 12;
            _toggleStyle.margin = new RectOffset(10, 10, 5, 5);

            // Create background texture
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? _proColor : _personalColor);
            _backgroundTexture.Apply();
        }

        private void OnGUI()
        {
            DrawBackground();
            DrawToolbar();
            DrawOptions();
            DrawMainContent();
        }

        private void DrawBackground()
        {
            if (_backgroundTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, position.width, position.height), _backgroundTexture, ScaleMode.StretchToFill);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button(new GUIContent(" Analyze", EditorGUIUtility.IconContent("d_Refresh").image), _toolbarButtonStyle))
            {
                RunAnalysis();
            }

            if (GUILayout.Button(new GUIContent(" Optimize", EditorGUIUtility.IconContent("d_Settings").image), _toolbarButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Optimize Assets",
                    "This will modify import settings for assets in your project. Continue?",
                    "Optimize", "Cancel"))
                {
                    AssetOptimizer.OptimizeAssetImportSettings();
                }
            }

            if (GUILayout.Button(new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image), _toolbarButtonStyle))
            {
                RefreshData();
            }

            GUILayout.FlexibleSpace();
            
            // Search bar with icon
            EditorGUILayout.BeginHorizontal(_searchStyle);
            GUILayout.Label(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, _searchStyle);
            if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("×", EditorStyles.label, GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            
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
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showUnusedAssets = EditorGUILayout.ToggleLeft(new GUIContent(" Show Unused", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), _showUnusedAssets, _toggleStyle);
            _showDependencies = EditorGUILayout.ToggleLeft(new GUIContent(" Show Dependencies", EditorGUIUtility.IconContent("d_BuildSettings.Web.Small").image), _showDependencies, _toggleStyle);
            _showNamingIssues = EditorGUILayout.ToggleLeft(new GUIContent(" Show Naming Issues", EditorGUIUtility.IconContent("d_FilterByLabel").image), _showNamingIssues, _toggleStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            
            if (GUILayout.Button(new GUIContent(" Optimization Settings", EditorGUIUtility.IconContent("d_Settings").image), 
                _showOptimizationSettings ? EditorStyles.toolbarButton : EditorStyles.miniButton))
            {
                _showOptimizationSettings = !_showOptimizationSettings;
            }
            
            if (_showOptimizationSettings)
            {
                EditorGUILayout.BeginVertical(_assetBoxStyle);
                DrawOptimizationSettings();
                EditorGUILayout.EndVertical();
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
            EditorGUILayout.Space(5);
            
            // Texture Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent(" Texture Settings", EditorGUIUtility.IconContent("d_Texture Icon").image), _sectionStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Max Texture Size: 2048", EditorStyles.label);
            EditorGUILayout.LabelField("Compression: Enabled for non-UI textures", EditorStyles.label);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Model Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent(" Model Settings", EditorGUIUtility.IconContent("d_Mesh Icon").image), _sectionStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Mesh Compression: Medium (High for mobile)", EditorStyles.label);
            EditorGUILayout.LabelField("Optimize Mesh: Enabled for mobile", EditorStyles.label);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Audio Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent(" Audio Settings", EditorGUIUtility.IconContent("d_AudioSource Icon").image), _sectionStyle);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Short Clips (<1s): ADPCM, Decompress on Load", EditorStyles.label);
            EditorGUILayout.LabelField("Long Clips (>1s): Vorbis, Streaming", EditorStyles.label);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawUnusedAssets()
        {
            EditorGUILayout.BeginVertical(_assetBoxStyle);
            EditorGUILayout.LabelField(new GUIContent(" Unused Assets", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), _headerStyle);
            EditorGUILayout.Space(5);

            if (_assetUsageStatus == null || _assetUsageStatus.Count == 0)
            {
                EditorGUILayout.HelpBox("Run 'Analyze Assets' to find unused assets", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            var unusedAssets = _assetUsageStatus.Where(x => !x.Value).ToList();
            if (unusedAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("No unused assets found", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var asset in unusedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !asset.Key.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                DrawAssetEntry(asset.Key, true);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawDependencies()
        {
            EditorGUILayout.BeginVertical(_assetBoxStyle);
            EditorGUILayout.LabelField(new GUIContent(" Asset Dependencies", EditorGUIUtility.IconContent("d_BuildSettings.Web.Small").image), _headerStyle);
            EditorGUILayout.Space(5);

            if (_assetDependencies == null || _assetDependencies.Count == 0)
            {
                EditorGUILayout.HelpBox("Run 'Analyze Assets' to view dependencies", MessageType.Info);
                EditorGUILayout.EndVertical();
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
                
                if (GUILayout.Button("Select", _buttonStyle))
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
                        
                        if (GUILayout.Button("Select", _buttonStyle))
                        {
                            Selection.activeObject = depObj;
                            EditorGUIUtility.PingObject(depObj);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No dependencies", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawNamingIssues()
        {
            EditorGUILayout.BeginVertical(_assetBoxStyle);
            EditorGUILayout.LabelField(new GUIContent(" Naming Convention Issues", EditorGUIUtility.IconContent("d_FilterByLabel").image), _headerStyle);
            EditorGUILayout.Space(5);

            foreach (var assetPath in _invalidNamedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !assetPath.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // Asset icon and path
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var icon = AssetDatabase.GetCachedIcon(assetPath);
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(assetPath);

                // Suggested name
                string suggestedName = AssetOptimizer.SuggestAssetName(assetPath);
                EditorGUILayout.LabelField($"→ {suggestedName}", EditorStyles.miniLabel, GUILayout.Width(200));
                
                if (GUILayout.Button("Fix", _buttonStyle))
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
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawAssetEntry(string assetPath, bool isUnused = false)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Asset icon
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

            // Asset name and path
            EditorGUILayout.LabelField(assetPath);

            // Actions
            if (GUILayout.Button("Select", _buttonStyle))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            if (isUnused && GUILayout.Button("Delete", _buttonStyle))
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
            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                DestroyImmediate(_backgroundTexture);
            }
        }
    }
} 