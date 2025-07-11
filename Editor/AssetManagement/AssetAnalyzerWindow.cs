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
        private bool _stylesInitialized;

        private const float MIN_WINDOW_WIDTH = 450f;
        private const float MIN_WINDOW_HEIGHT = 300f;
        private const float TOOLBAR_HEIGHT = 30f;
        private const float SECTION_SPACING = 10f;
        private const float ITEM_SPACING = 5f;

        [MenuItem("Window/Asset Management/Asset Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetAnalyzerWindow>();
            window.titleContent = new GUIContent("Asset Analyzer", EditorGUIUtility.IconContent("d_PreMatCube").image);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            _stylesInitialized = false;
        }

        private void OnDisable()
        {
            if (_backgroundTexture != null)
            {
                DestroyImmediate(_backgroundTexture);
                _backgroundTexture = null;
            }
            _stylesInitialized = false;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Header style
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(10, 10, 10, 10)
            };
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // Section style
            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(5, 5, 10, 5)
            };
            _sectionStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // Button style
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 60,
                margin = new RectOffset(2, 2, 2, 2)
            };
            _buttonStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            _buttonStyle.hover.textColor = _accentColor;

            // Search style
            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                fixedWidth = 200,
                margin = new RectOffset(5, 5, 5, 5)
            };

            // Asset box style
            _assetBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            // Toolbar button style
            _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fixedHeight = 25,
                margin = new RectOffset(5, 5, 5, 5),
                padding = new RectOffset(10, 10, 5, 5)
            };

            // Toggle style
            _toggleStyle = new GUIStyle(EditorStyles.toggle)
            {
                fontSize = 12,
                margin = new RectOffset(10, 10, 5, 5)
            };

            // Create background texture
            if (_backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(1, 1);
                _backgroundTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? _proColor : _personalColor);
                _backgroundTexture.Apply();
            }

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            // Initialize styles if needed
            if (!_stylesInitialized || Event.current.type == EventType.Layout)
            {
                InitializeStyles();
            }

            // Handle window resizing
            HandleWindowResize();

            // Draw window content
            DrawWindowContent();

            // Handle repaint requests
            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void HandleWindowResize()
        {
            if (Event.current.type == EventType.Repaint)
            {
                Rect windowRect = position;
                bool needsResize = false;

                if (windowRect.width < MIN_WINDOW_WIDTH)
                {
                    windowRect.width = MIN_WINDOW_WIDTH;
                    needsResize = true;
                }
                if (windowRect.height < MIN_WINDOW_HEIGHT)
                {
                    windowRect.height = MIN_WINDOW_HEIGHT;
                    needsResize = true;
                }

                if (needsResize)
                {
                    position = windowRect;
                }
            }
        }

        private void DrawWindowContent()
        {
            DrawBackground();

            EditorGUILayout.BeginVertical();
            {
                // Fixed toolbar at top
                GUILayout.Space(2);
                DrawToolbar();
                GUILayout.Space(2);
                DrawOptions();

                // Scrollable content area
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true));
                {
                    DrawMainContent();
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
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
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
            {
                // Left side buttons
                GUILayout.BeginHorizontal(GUILayout.Width(position.width * 0.6f));
                {
                    if (GUILayout.Button(new GUIContent(" Analyze", EditorGUIUtility.IconContent("d_Refresh").image),
                        _toolbarButtonStyle, GUILayout.Width(80)))
                    {
                        RunAnalysis();
                    }

                    if (GUILayout.Button(new GUIContent(" Optimize", EditorGUIUtility.IconContent("d_Settings").image),
                        _toolbarButtonStyle, GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("Optimize Assets",
                            "This will modify import settings for assets in your project. Continue?",
                            "Optimize", "Cancel"))
                        {
                            AssetOptimizer.OptimizeAssetImportSettings();
                        }
                    }

                    if (GUILayout.Button(new GUIContent(" Refresh", EditorGUIUtility.IconContent("d_Refresh").image),
                        _toolbarButtonStyle, GUILayout.Width(80)))
                    {
                        RefreshData();
                    }
                }
                GUILayout.EndHorizontal();

                // Right side search
                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal(GUILayout.Width(220));
                {
                    GUILayout.Label(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter, _searchStyle);
                    if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("×", EditorStyles.label, GUILayout.Width(20)))
                    {
                        _searchFilter = "";
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndHorizontal();
            }
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Toggle row
                EditorGUILayout.BeginHorizontal();
                {
                    _showUnusedAssets = EditorGUILayout.ToggleLeft(
                        new GUIContent(" Show Unused", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image),
                        _showUnusedAssets,
                        _toggleStyle,
                        GUILayout.Width(position.width / 3 - 10)
                    );

                    _showDependencies = EditorGUILayout.ToggleLeft(
                        new GUIContent(" Dependencies", EditorGUIUtility.IconContent("d_BuildSettings.Web.Small").image),
                        _showDependencies,
                        _toggleStyle,
                        GUILayout.Width(position.width / 3 - 10)
                    );

                    _showNamingIssues = EditorGUILayout.ToggleLeft(
                        new GUIContent(" Naming Issues", EditorGUIUtility.IconContent("d_FilterByLabel").image),
                        _showNamingIssues,
                        _toggleStyle,
                        GUILayout.Width(position.width / 3 - 10)
                    );
                }
                EditorGUILayout.EndHorizontal();

                // Optimization Settings button
                if (GUILayout.Button(
                    new GUIContent(" Optimization Settings", EditorGUIUtility.IconContent("d_Settings").image),
                    _showOptimizationSettings ? EditorStyles.toolbarButton : EditorStyles.miniButton))
                {
                    _showOptimizationSettings = !_showOptimizationSettings;
                }

                if (_showOptimizationSettings)
                {
                    DrawOptimizationSettings();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMainContent()
        {
            GUILayout.Space(SECTION_SPACING);

            if (_showUnusedAssets)
            {
                DrawUnusedAssets();
                GUILayout.Space(SECTION_SPACING);
            }

            if (_showDependencies)
            {
                DrawDependencies();
                GUILayout.Space(SECTION_SPACING);
            }

            if (_showNamingIssues && _invalidNamedAssets != null && _invalidNamedAssets.Count > 0)
            {
                DrawNamingIssues();
                GUILayout.Space(SECTION_SPACING);
            }
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

                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                }

                EditorGUILayout.LabelField(asset.Key, EditorStyles.boldLabel);

                if (obj != null && GUILayout.Button("Select", _buttonStyle))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                if (asset.Value != null && asset.Value.Any())
                {
                    foreach (var dependency in asset.Value)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var depObj = AssetDatabase.LoadAssetAtPath<Object>(dependency);
                        var depIcon = AssetDatabase.GetCachedIcon(dependency);
                        GUILayout.Space(20);

                        if (depIcon != null)
                        {
                            GUILayout.Label(depIcon, GUILayout.Width(16), GUILayout.Height(16));
                        }

                        EditorGUILayout.LabelField(dependency);

                        if (depObj != null && GUILayout.Button("Select", _buttonStyle))
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
            EditorGUILayout.Space(10);
        }

        private void DrawNamingIssues()
        {
            EditorGUILayout.BeginVertical(_assetBoxStyle);
            EditorGUILayout.LabelField(new GUIContent(" Naming Issues", EditorGUIUtility.IconContent("d_FilterByLabel").image), _headerStyle);
            EditorGUILayout.Space(5);

            foreach (var assetPath in _invalidNamedAssets)
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !assetPath.ToLower().Contains(_searchFilter.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                var icon = AssetDatabase.GetCachedIcon(assetPath);

                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                }

                EditorGUILayout.LabelField(assetPath, EditorStyles.boldLabel);

                if (obj != null && GUILayout.Button("Select", _buttonStyle))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();

                string suggestedName = AssetOptimizer.SuggestAssetName(assetPath);
                EditorGUILayout.LabelField($"Suggested name: {suggestedName}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawAssetEntry(string assetPath, bool showDelete = false)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    // Icon
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    var icon = AssetDatabase.GetCachedIcon(assetPath);

                    GUILayout.BeginHorizontal(GUILayout.Width(24));
                    if (icon != null)
                    {
                        GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    }
                    GUILayout.EndHorizontal();

                    // Path (with word wrap and flexible width)
                    EditorGUILayout.LabelField(assetPath, EditorStyles.wordWrappedLabel,
                        GUILayout.ExpandWidth(true));

                    // Buttons
                    GUILayout.BeginHorizontal(GUILayout.Width(130));
                    if (obj != null)
                    {
                        if (GUILayout.Button("Select", _buttonStyle))
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }

                        if (showDelete && GUILayout.Button("Delete", _buttonStyle))
                        {
                            if (EditorUtility.DisplayDialog("Delete Asset",
                                $"Are you sure you want to delete {assetPath}?",
                                "Delete", "Cancel"))
                            {
                                AssetDatabase.DeleteAsset(assetPath);
                                RefreshData();
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(ITEM_SPACING);
        }
    }
} 