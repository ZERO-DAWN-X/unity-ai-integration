using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
            if (_headerStyle != null) return; // Styles already initialized

            _backgroundColor = EditorGUIUtility.isProSkin ? 
                new Color(0.22f, 0.22f, 0.22f) : 
                new Color(0.85f, 0.85f, 0.85f);

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(10, 10, 15, 5),
                normal = { textColor = EditorGUIUtility.isProSkin ? 
                    new Color(0.9f, 0.9f, 0.9f) : 
                    new Color(0.2f, 0.2f, 0.2f) }
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(5, 5, 10, 5)
            };
            
            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.textColor = Color.white;
            
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                margin = new RectOffset(10, 10, 5, 5)
            };
            
            _toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                margin = new RectOffset(10, 10, 5, 5)
            };
            
            _toolbarStyle = new GUIStyle(EditorStyles.toolbar)
            {
                fixedHeight = 35,
                padding = new RectOffset(5, 5, 5, 5)
            };

            _toggleLabelStyle = new GUIStyle(EditorStyles.label);
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

            if (_toggleLabelStyle == null)
            {
                _toggleLabelStyle = new GUIStyle(EditorStyles.label);
            }
            _toggleLabelStyle.normal.textColor = value || isHover ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            
            GUI.Label(new Rect(rect.x + 5, rect.y + 4, 16, 16), icon);
            GUI.Label(new Rect(rect.x + 25, rect.y + 4, rect.width - 30, 16), label, _toggleLabelStyle);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = !value;
                Event.current.Use();
                Repaint();
            }

            return value;
        }

        private GUIStyle _toggleLabelStyle;

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

                DrawAssetEntry(asset.Key, false);
                EditorGUI.indentLevel++;
                
                if (asset.Value.Count > 0)
                {
                    foreach (var dependency in asset.Value)
                    {
                        EditorGUILayout.LabelField($"→ {Path.GetFileName(dependency)}", 
                            new GUIStyle(EditorStyles.label) { fontSize = 10 });
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No dependencies", 
                        new GUIStyle(EditorStyles.label) { fontSize = 10 });
                }
                
                EditorGUI.indentLevel--;
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

        private void DrawAssetEntry(string assetPath, bool isUnused)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Draw icon based on asset type
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            }
            
            // Draw asset name and path
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(Path.GetFileName(assetPath), 
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            EditorGUILayout.LabelField(assetPath, 
                new GUIStyle(EditorStyles.miniLabel) { fontSize = 9 });
            EditorGUILayout.EndVertical();

            if (isUnused)
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_TreeEditor.Trash"), 
                    GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Asset",
                        $"Are you sure you want to delete {Path.GetFileName(assetPath)}?",
                        "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        AssetDatabase.Refresh();
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState(string title, string message, string iconName = "d_console.infoicon")
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            var centeredStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(EditorGUIUtility.IconContent(iconName).image, 
                GUILayout.Width(32), GUILayout.Height(32));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, new GUIStyle(centeredStyle) { fontSize = 14, fontStyle = FontStyle.Bold });
            EditorGUILayout.LabelField(message, new GUIStyle(centeredStyle) { fontSize = 12 });
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(20);
        }

        private void DrawAnalysisProgress()
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            rect.x += 10;
            rect.width -= 20;
            
            EditorGUI.ProgressBar(rect, _analysisProgress, "Analyzing Assets...");
        }

        private void RunAnalysis()
        {
            _isAnalyzing = true;
            _analysisProgress = 0f;
            
            EditorApplication.update += UpdateAnalysis;
            AssetAnalyzer.AnalyzeProjectAssets();
            
            _assetUsageStatus = AssetAnalyzer.GetAssetUsageStatus();
            _assetDependencies = AssetAnalyzer.GetAssetDependencies();
        }

        private void UpdateAnalysis()
        {
            if (_isAnalyzing)
            {
                _analysisProgress += 0.01f;
                if (_analysisProgress >= 1f)
                {
                    _isAnalyzing = false;
                    _analysisProgress = 0f;
                    EditorApplication.update -= UpdateAnalysis;
                }
                Repaint();
            }
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

        private void OnDisable()
        {
            if (_gradientTexture != null)
            {
                DestroyImmediate(_gradientTexture);
                _gradientTexture = null;
            }

            // Clear styles
            _headerStyle = null;
            _sectionStyle = null;
            _cardStyle = null;
            _buttonStyle = null;
            _searchStyle = null;
            _toggleStyle = null;
            _toolbarStyle = null;
            _toggleLabelStyle = null;
        }
    }
} 