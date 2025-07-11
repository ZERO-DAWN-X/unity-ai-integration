using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Microsoft.Unity.VisualStudio.Editor
{
    public class AssetAnalyzer
    {
        private static Dictionary<string, List<string>> _assetDependencies = new Dictionary<string, List<string>>();
        private static Dictionary<string, bool> _assetUsageStatus = new Dictionary<string, bool>();

        public static void AnalyzeProjectAssets()
        {
            EditorUtility.DisplayProgressBar("Asset Analysis", "Scanning project assets...", 0f);
            try
            {
                // Get all assets in the project
                string[] allAssets = AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.StartsWith("Assets/") && 
                           !path.StartsWith("Assets/AssetReports/"))
                    .ToArray();

                if (allAssets.Length == 0)
                {
                    Debug.Log("No assets found to analyze. Please add some assets to your project first.");
                    return;
                }

                int totalAssets = allAssets.Length;
                Debug.Log($"Found {totalAssets} assets to analyze.");
                
                _assetDependencies.Clear();
                _assetUsageStatus.Clear();

                // Analyze each asset
                for (int i = 0; i < allAssets.Length; i++)
                {
                    string assetPath = allAssets[i];
                    float progress = (float)i / totalAssets;
                    EditorUtility.DisplayProgressBar("Asset Analysis", 
                        $"Analyzing {Path.GetFileName(assetPath)} ({i + 1}/{totalAssets})", 
                        progress);

                    AnalyzeAssetDependencies(assetPath);
                    CheckAssetUsage(assetPath);
                }

                // Generate report
                GenerateAssetReport();
                
                Debug.Log("Asset analysis completed successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during asset analysis: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void AnalyzeAssetDependencies(string assetPath)
        {
            try
            {
                string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive: true)
                    .Where(dep => dep != assetPath && dep.StartsWith("Assets/"))
                    .ToArray();
                _assetDependencies[assetPath] = new List<string>(dependencies);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to analyze dependencies for {assetPath}: {e.Message}");
            }
        }

        private static void CheckAssetUsage(string assetPath)
        {
            try
            {
                // Skip checking usage for certain file types
                if (assetPath.EndsWith(".meta") || assetPath.EndsWith(".cs"))
                {
                    _assetUsageStatus[assetPath] = true;
                    return;
                }

                // Check if asset is referenced by any scene or prefab
                string[] allScenes = AssetDatabase.FindAssets("t:Scene");
                string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
                
                bool isUsed = false;

                // Check scenes
                foreach (string sceneGuid in allScenes)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    string[] sceneDependencies = AssetDatabase.GetDependencies(scenePath, recursive: true);
                    if (sceneDependencies.Contains(assetPath))
                    {
                        isUsed = true;
                        break;
                    }
                }

                // Check prefabs if not used in scenes
                if (!isUsed)
                {
                    foreach (string prefabGuid in allPrefabs)
                    {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                        string[] prefabDependencies = AssetDatabase.GetDependencies(prefabPath, recursive: true);
                        if (prefabDependencies.Contains(assetPath))
                        {
                            isUsed = true;
                            break;
                        }
                    }
                }

                _assetUsageStatus[assetPath] = isUsed;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to check usage for {assetPath}: {e.Message}");
                _assetUsageStatus[assetPath] = true; // Assume used in case of error
            }
        }

        private static void GenerateAssetReport()
        {
            // Create report directory if it doesn't exist
            string reportDir = "Assets/AssetReports";
            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            string reportPath = $"{reportDir}/AssetAnalysisReport_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine("Asset Analysis Report");
                writer.WriteLine("===================");
                writer.WriteLine($"Generated: {System.DateTime.Now}\n");

                // Summary
                int totalAssets = _assetUsageStatus.Count;
                int unusedAssets = _assetUsageStatus.Count(x => !x.Value);
                writer.WriteLine($"Total Assets Analyzed: {totalAssets}");
                writer.WriteLine($"Unused Assets Found: {unusedAssets}\n");

                // Unused Assets
                writer.WriteLine("Unused Assets:");
                writer.WriteLine("-------------");
                var unusedList = _assetUsageStatus.Where(x => !x.Value).ToList();
                if (unusedList.Any())
                {
                    foreach (var asset in unusedList)
                    {
                        writer.WriteLine($"- {asset.Key}");
                    }
                }
                else
                {
                    writer.WriteLine("No unused assets found.");
                }
                writer.WriteLine();

                // Asset Dependencies
                writer.WriteLine("Asset Dependencies:");
                writer.WriteLine("------------------");
                foreach (var asset in _assetDependencies.OrderBy(x => x.Key))
                {
                    writer.WriteLine($"\n{asset.Key}:");
                    if (asset.Value.Any())
                    {
                        foreach (var dependency in asset.Value)
                        {
                            writer.WriteLine($"  - {dependency}");
                        }
                    }
                    else
                    {
                        writer.WriteLine("  No dependencies");
                    }
                }
            }

            Debug.Log($"Asset analysis report generated at: {reportPath}");
            AssetDatabase.Refresh();
        }

        public static Dictionary<string, List<string>> GetAssetDependencies()
        {
            return _assetDependencies;
        }

        public static Dictionary<string, bool> GetAssetUsageStatus()
        {
            return _assetUsageStatus;
        }
    }
} 