using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Unity.VisualStudio.Editor
{
    public class AssetOptimizer
    {
        private static readonly Dictionary<string, string> NamingConventions = new Dictionary<string, string>
        {
            { "Textures", "^(tx|tex)_[a-z0-9]+(_[a-z0-9]+)*$" },
            { "Models", "^(mdl|mesh)_[a-z0-9]+(_[a-z0-9]+)*$" },
            { "Materials", "^(mat)_[a-z0-9]+(_[a-z0-9]+)*$" },
            { "Animations", "^(anim)_[a-z0-9]+(_[a-z0-9]+)*$" },
            { "Prefabs", "^(pfb)_[a-z0-9]+(_[a-z0-9]+)*$" },
            { "Scripts", "^[A-Z][a-zA-Z0-9]*$" },
            { "Scenes", "^(scn)_[a-z0-9]+(_[a-z0-9]+)*$" }
        };

        public static void OptimizeAssetImportSettings()
        {
            string[] allAssets = AssetDatabase.GetAllAssetPaths();
            int progress = 0;

            foreach (string assetPath in allAssets)
            {
                EditorUtility.DisplayProgressBar("Optimizing Assets", 
                    $"Processing {System.IO.Path.GetFileName(assetPath)}", 
                    (float)progress++ / allAssets.Length);

                if (assetPath.StartsWith("Assets/"))
                {
                    OptimizeAsset(assetPath);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        private static void OptimizeAsset(string assetPath)
        {
            // Get asset type
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null) return;

            if (importer is TextureImporter textureImporter)
            {
                OptimizeTexture(textureImporter);
            }
            else if (importer is ModelImporter modelImporter)
            {
                OptimizeModel(modelImporter);
            }
            else if (importer is AudioImporter audioImporter)
            {
                OptimizeAudio(audioImporter);
            }
        }

        private static void OptimizeTexture(TextureImporter importer)
        {
            // Check if texture is a UI element
            bool isUI = importer.assetPath.Contains("/UI/");
            
            if (isUI)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
            }
            else
            {
                // Check texture size
                var textureSettings = importer.GetPlatformTextureSettings("Standalone");
                
                if (textureSettings.maxTextureSize > 2048)
                {
                    textureSettings.maxTextureSize = 2048;
                    importer.SetPlatformTextureSettings(textureSettings);
                }

                // Enable compression for non-UI textures
                importer.textureCompression = TextureImporterCompression.Compressed;
            }

            importer.SaveAndReimport();
        }

        private static void OptimizeModel(ModelImporter importer)
        {
            // Optimize mesh compression
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            
            // Optimize for mobile if detected
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || 
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                importer.meshCompression = ModelImporterMeshCompression.High;
                importer.optimizeMeshPolygons = true;
                importer.optimizeMeshVertices = true;
            }

            importer.SaveAndReimport();
        }

        private static void OptimizeAudio(AudioImporter importer)
        {
            var settings = importer.defaultSampleSettings;
            
            // Optimize based on clip length
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(importer.assetPath);
            if (clip != null)
            {
                if (clip.length < 1f) // Short sound effects
                {
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.ADPCM;
                }
                else // Longer audio like music
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                }
            }

            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        public static List<string> ValidateAssetNames()
        {
            var invalidNames = new List<string>();
            string[] allAssets = AssetDatabase.GetAllAssetPaths();

            foreach (string assetPath in allAssets)
            {
                if (!assetPath.StartsWith("Assets/")) continue;

                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                string directory = System.IO.Path.GetDirectoryName(assetPath);

                if (directory == null) continue;

                // Determine asset type from directory
                string assetType = GetAssetTypeFromPath(directory);
                if (string.IsNullOrEmpty(assetType)) continue;

                // Check if name matches convention
                if (NamingConventions.TryGetValue(assetType, out string pattern))
                {
                    if (!Regex.IsMatch(fileName, pattern))
                    {
                        invalidNames.Add(assetPath);
                    }
                }
            }

            return invalidNames;
        }

        private static string GetAssetTypeFromPath(string path)
        {
            path = path.ToLower();
            if (path.Contains("/textures/")) return "Textures";
            if (path.Contains("/models/")) return "Models";
            if (path.Contains("/materials/")) return "Materials";
            if (path.Contains("/animations/")) return "Animations";
            if (path.Contains("/prefabs/")) return "Prefabs";
            if (path.Contains("/scripts/")) return "Scripts";
            if (path.Contains("/scenes/")) return "Scenes";
            return string.Empty;
        }

        public static string SuggestAssetName(string assetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string assetType = GetAssetTypeFromPath(directory ?? string.Empty);

            if (string.IsNullOrEmpty(assetType)) return fileName;

            // Generate prefix based on asset type
            string prefix = assetType.ToLower() switch
            {
                "textures" => "tex",
                "models" => "mdl",
                "materials" => "mat",
                "animations" => "anim",
                "prefabs" => "pfb",
                "scenes" => "scn",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(prefix)) return fileName;

            // Convert to snake_case
            string newName = Regex.Replace(fileName, @"([a-z])([A-Z])", "$1_$2").ToLower();
            newName = Regex.Replace(newName, @"[^a-z0-9_]", "");
            
            return $"{prefix}_{newName}";
        }
    }
} 