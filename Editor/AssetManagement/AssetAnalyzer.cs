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
                string[] allAssets = AssetDatabase.GetAllAssetPaths();
                int totalAssets = allAssets.Length;
                
                _assetDependencies.Clear();
                _assetUsageStatus.Clear();

                // Analyze each asset
                for (int i = 0; i < allAssets.Length; i++)
                {
                    string assetPath = allAssets[i];
                    EditorUtility.DisplayProgressBar("Asset Analysis", 
                        $"Analyzing {Path.GetFileName(assetPath)}", 
                        (float)i / totalAssets);

                    // Skip Unity packages and system files
                    if (assetPath.StartsWith("Packages/") || assetPath.StartsWith("Library/"))
                        continue;

                    AnalyzeAssetDependencies(assetPath);
                    CheckAssetUsage(assetPath);
                }

                // Generate report
                GenerateAssetReport();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void AnalyzeAssetDependencies(string assetPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive: true);
            _assetDependencies[assetPath] = new List<string>(dependencies);
        }

        private static void CheckAssetUsage(string assetPath)
        {
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

                // Unused Assets
                writer.WriteLine("Unused Assets:");
                writer.WriteLine("-------------");
                foreach (var asset in _assetUsageStatus.Where(x => !x.Value))
                {
                    writer.WriteLine($"- {asset.Key}");
                }
                writer.WriteLine();

                // Asset Dependencies
                writer.WriteLine("Asset Dependencies:");
                writer.WriteLine("------------------");
                foreach (var asset in _assetDependencies)
                {
                    writer.WriteLine($"\n{asset.Key}:");
                    foreach (var dependency in asset.Value)
                    {
                        writer.WriteLine($"  - {dependency}");
                    }
                }
            }

            Debug.Log($"Asset analysis report generated at: {reportPath}");
            AssetDatabase.Refresh();
        }
    }
} 