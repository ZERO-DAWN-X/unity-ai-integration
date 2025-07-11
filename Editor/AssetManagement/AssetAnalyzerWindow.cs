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
        private GUIStyle _cardStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _searchStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _toolbarStyle;
        private List<string> _invalidNamedAssets;
        private bool _showOptimizationSettings = false;
        private Dictionary<string, bool> _assetUsageStatus;
        private Dictionary<string, List<string>> _assetDependencies;
        private Color _accentColor = new Color(0.2f, 0.6f, 1f);
        private Color _backgroundColor;
        private Texture2D _gradientTexture;
        private bool _isAnalyzing = false;
        private float _analysisProgress = 0f;

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
            CreateGradientTexture();
        }

        private void CreateGradientTexture()
        {
            _gradientTexture = new Texture2D(1, 2);
            _gradientTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f));
            _gradientTexture.SetPixel(0, 1, new Color(0.3f, 0.3f, 0.3f));
            _gradientTexture.Apply();
        }

        private void InitializeStyles()
        {
            _backgroundColor = EditorGUIUtility.isProSkin ? 
                new Color(0.22f, 0.22f, 0.22f) : 
                new Color(0.85f, 0.85f, 0.85f);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _headerStyle.fontSize = 16;
            _headerStyle.margin = new RectOffset(10, 10, 15, 5);
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? 
                new Color(0.9f, 0.9f, 0.9f) : 
                new Color(0.2f, 0.2f, 0.2f);

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionStyle.fontSize = 13;
            _sectionStyle.margin = new RectOffset(5, 5, 10, 5);
            
            _cardStyle = new GUIStyle(EditorStyles.helpBox);
            _cardStyle.padding = new RectOffset(10, 10, 10, 10);
            _cardStyle.margin = new RectOffset(5, 5, 5, 5);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.textColor = Color.white;
            
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            _searchStyle.margin = new RectOffset(10, 10, 5, 5);
            
            _toggleStyle = new GUIStyle(EditorStyles.toggle);
            _toggleStyle.margin = new RectOffset(10, 10, 5, 5);
            
            _toolbarStyle = new GUIStyle(EditorStyles.toolbar);
            _toolbarStyle.fixedHeight = 35;
            _toolbarStyle.padding = new RectOffset(5, 5, 5, 5);
        }

        private void OnGUI()
        {
            DrawBackground();
            DrawToolbar();
            DrawOptions();
            
            EditorGUILayout.Space(5);
            DrawMainContent();
            
            if (_isAnalyzing)
            {
                DrawAnalysisProgress();
            }
        }

        private void DrawBackground()
        {
            if (Event.current.type == EventType.Repaint)
            {
                var rect = position;
                EditorGUI.DrawRect(rect, _backgroundColor);
                
                // Draw gradient header
                var headerRect = new Rect(0, 0, position.width, 35);
                GUI.DrawTexture(headerRect, _gradientTexture, ScaleMode.StretchToFill);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(_toolbarStyle);
            
            if (GUILayout.Button(new GUIContent(" Analyze", EditorGUIUtility.IconContent("d_Refresh").image), 
                GUILayout.Width(100), GUILayout.Height(25)))
            {
                RunAnalysis();
            }

            if (GUILayout.Button(new GUIContent(" Optimize", EditorGUIUtility.IconContent("d_Settings").image),
                GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Optimize Assets",
                    "This will modify import settings for assets in your project. Continue?",
                    "Optimize", "Cancel"))
                {
                    AssetOptimizer.OptimizeAssetImportSettings();
                }
            }

            GUILayout.FlexibleSpace();

            // Search bar with icon
            EditorGUILayout.BeginHorizontal(_searchStyle);
            GUILayout.Label(EditorGUIUtility.IconContent("d_Search Icon").image, GUILayout.Width(20));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, _searchStyle, GUILayout.Width(200));
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_win_close").image, 
                    GUILayout.Width(20), GUILayout.Height(20)))
                {
                    _searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var toggleWidth = position.width / 4;
            _showUnusedAssets = DrawCustomToggle(" Unused Assets", _showUnusedAssets, 
                EditorGUIUtility.IconContent("d_TreeEditor.Trash").image, toggleWidth);
            _showDependencies = DrawCustomToggle(" Dependencies", _showDependencies,
                EditorGUIUtility.IconContent("d_BuildSettings.Web.Small").image, toggleWidth);
            _showNamingIssues = DrawCustomToggle(" Naming Issues", _showNamingIssues,
                EditorGUIUtility.IconContent("d_FilterByLabel").image, toggleWidth);
            _showOptimizationSettings = DrawCustomToggle(" Settings", _showOptimizationSettings,
                EditorGUIUtility.IconContent("d_Settings").image, toggleWidth);
            
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawCustomToggle(string label, bool value, Texture icon, float width)
        {
            var rect = GUILayoutUtility.GetRect(width, 25);
            var isHover = rect.Contains(Event.current.mousePosition);
            var color = value ? _accentColor : (isHover ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.3f, 0.3f, 0.3f));
            
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, color);
            }

            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = value || isHover ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            
            GUI.Label(new Rect(rect.x + 5, rect.y + 4, 16, 16), icon);
            GUI.Label(new Rect(rect.x + 25, rect.y + 4, rect.width - 30, 16), label, style);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = !value;
                Event.current.Use();
                Repaint();
            }

            return value;
        }

        private void DrawMainContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_showOptimizationSettings)
            {
                DrawOptimizationSettings();
            }

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
            EditorGUILayout.BeginVertical(_cardStyle);
            
            EditorGUILayout.LabelField("Optimization Settings", _headerStyle);
            EditorGUILayout.Space(5);

            DrawSettingsSection("Texture Settings", new string[] {
                "Max Texture Size: 2048",
                "Compression: Enabled for non-UI textures"
            }, "d_PreTexture");

            DrawSettingsSection("Model Settings", new string[] {
                "Mesh Compression: Medium (High for mobile)",
                "Optimize Mesh: Enabled for mobile"
            }, "d_PreMatCube");

            DrawSettingsSection("Audio Settings", new string[] {
                "Short Clips (<1s): ADPCM, Decompress on Load",
                "Long Clips (>1s): Vorbis, Streaming"
            }, "d_AudioSource Icon");

            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsSection(string title, string[] items, string iconName)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.IconContent(iconName).image, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.LabelField(title, _sectionStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            foreach (var item in items)
            {
                EditorGUILayout.LabelField(item);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        private void DrawUnusedAssets()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("Unused Assets", _headerStyle);

            if (_assetUsageStatus == null || _assetUsageStatus.Count == 0)
            {
                DrawEmptyState("No analysis data", "Run 'Analyze' to find unused assets");
                EditorGUILayout.EndVertical();
                return;
            }

            var unusedAssets = _assetUsageStatus.Where(x => !x.Value).ToList();
            if (unusedAssets.Count == 0)
            {
                DrawEmptyState("All Clear!", "No unused assets found", "d_Valid");
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);
            foreach (var asset in unusedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !asset.Key.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                DrawAssetEntry(asset.Key, true);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDependencies()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("Asset Dependencies", _headerStyle);

            if (_assetDependencies == null || _assetDependencies.Count == 0)
            {
                DrawEmptyState("No dependency data", "Run 'Analyze' to view dependencies");
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);
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
                
                if (GUILayout.Button(new GUIContent(" Select", EditorGUIUtility.IconContent("d_ViewToolZoom").image),
                    GUILayout.Width(80)))
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
                        
                        if (GUILayout.Button(new GUIContent(" Select", EditorGUIUtility.IconContent("d_ViewToolZoom").image),
                            GUILayout.Width(80)))
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
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNamingIssues()
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("Naming Convention Issues", _headerStyle);
            EditorGUILayout.Space(5);

            foreach (var assetPath in _invalidNamedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) && 
                    !assetPath.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Asset icon and path
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var icon = AssetDatabase.GetCachedIcon(assetPath);
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUILayout.LabelField(assetPath);

                // Suggested name
                string suggestedName = AssetOptimizer.SuggestAssetName(assetPath);
                if (GUILayout.Button(new GUIContent(" Fix", EditorGUIUtility.IconContent("d_SaveAs").image),
                    GUILayout.Width(80)))
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

            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState(string title, string message, string iconName = "d_console.infoicon")
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            
            var icon = EditorGUIUtility.IconContent(iconName).image;
            var iconRect = GUILayoutUtility.GetRect(32, 32);
            iconRect.x = (position.width - 32) / 2;
            GUI.DrawTexture(iconRect, icon);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 14;
            
            var messageStyle = new GUIStyle(EditorStyles.label);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.normal.textColor = Color.gray;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, titleStyle);
            EditorGUILayout.LabelField(message, messageStyle);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetEntry(string assetPath, bool isUnused = false)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Asset icon and info
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(System.IO.Path.GetFileName(assetPath), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(assetPath, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Buttons
            EditorGUILayout.BeginHorizontal(GUILayout.Width(160));
            if (GUILayout.Button(new GUIContent(" Select", EditorGUIUtility.IconContent("d_ViewToolZoom").image),
                GUILayout.Width(80)))
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }

            if (isUnused && GUILayout.Button(new GUIContent(" Delete", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image),
                GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete Asset",
                    $"Are you sure you want to delete {System.IO.Path.GetFileName(assetPath)}?",
                    "Delete", "Cancel"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    AssetDatabase.Refresh();
                    RefreshData();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAnalysisProgress()
        {
            var rect = new Rect(10, position.height - 30, position.width - 20, 20);
            EditorGUI.ProgressBar(rect, _analysisProgress, "Analyzing Assets...");
            Repaint();
        }

        private void RunAnalysis()
        {
            _isAnalyzing = true;
            _analysisProgress = 0f;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    AssetAnalyzer.AnalyzeProjectAssets();
                    RefreshData();
                }
                finally
                {
                    _isAnalyzing = false;
                    _analysisProgress = 1f;
                    Repaint();
                }
            };
        }

        private void RefreshData()
        {
            _invalidNamedAssets = AssetOptimizer.ValidateAssetNames();
            _assetUsageStatus = AssetAnalyzer.GetAssetUsageStatus();
            _assetDependencies = AssetAnalyzer.GetAssetDependencies();
            Repaint();
        }

        private void OnDestroy()
        {
            if (_gradientTexture != null)
            {
                DestroyImmediate(_gradientTexture);
            }
        }
    }
} 