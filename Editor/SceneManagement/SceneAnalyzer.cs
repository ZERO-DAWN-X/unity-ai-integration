using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Unity.VisualStudio.Editor.SceneManagement
{
    public class SceneAnalyzer
    {
        public class SceneAnalysisResult
        {
            public List<GameObject> UnusedGameObjects { get; set; } = new List<GameObject>();
            public List<GameObject> MissingComponents { get; set; } = new List<GameObject>();
            public Dictionary<GameObject, List<Component>> NullReferences { get; set; } = new Dictionary<GameObject, List<Component>>();
            public Dictionary<GameObject, List<GameObject>> Dependencies { get; set; } = new Dictionary<GameObject, List<GameObject>>();
            public List<PrefabVariantAnalysis> PrefabVariants { get; set; } = new List<PrefabVariantAnalysis>();
            public List<SceneOptimizationTip> OptimizationTips { get; set; } = new List<SceneOptimizationTip>();
        }

        public class PrefabVariantAnalysis
        {
            public GameObject Prefab { get; set; }
            public GameObject BaseObject { get; set; }
            public List<PropertyDifference> Differences { get; set; } = new List<PropertyDifference>();
        }

        public class PropertyDifference
        {
            public string PropertyPath { get; set; }
            public object BaseValue { get; set; }
            public object VariantValue { get; set; }
            public Component Component { get; set; }
        }

        public class SceneOptimizationTip
        {
            public string Description { get; set; }
            public TipSeverity Severity { get; set; }
            public GameObject Target { get; set; }
        }

        public enum TipSeverity
        {
            Info,
            Warning,
            Critical
        }

        public static SceneAnalysisResult AnalyzeActiveScene()
        {
            var result = new SceneAnalysisResult();
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            AnalyzeHierarchy(rootObjects, result);
            DetectMissingComponents(rootObjects, result);
            TrackDependencies(rootObjects, result);
            AnalyzePrefabVariants(rootObjects, result);
            GenerateOptimizationTips(scene, result);

            return result;
        }

        private static void AnalyzeHierarchy(GameObject[] rootObjects, SceneAnalysisResult result)
        {
            foreach (var obj in rootObjects)
            {
                // Check for unused objects (no components except Transform, no children, not referenced)
                if (IsUnusedGameObject(obj))
                {
                    result.UnusedGameObjects.Add(obj);
                }

                // Recursively analyze children
                foreach (Transform child in obj.transform)
                {
                    AnalyzeHierarchy(new[] { child.gameObject }, result);
                }
            }
        }

        private static bool IsUnusedGameObject(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            return components.Length <= 1 && // Only has Transform
                   obj.transform.childCount == 0 && // No children
                   !IsReferencedByOthers(obj); // Not referenced by other objects
        }

        private static bool IsReferencedByOthers(GameObject obj)
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var otherObj in allObjects)
            {
                if (otherObj == obj) continue;
                
                var components = otherObj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                            iterator.objectReferenceValue == obj)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void DetectMissingComponents(GameObject[] objects, SceneAnalysisResult result)
        {
            foreach (var obj in objects)
            {
                var components = obj.GetComponents<Component>();
                var hasNull = false;

                foreach (var component in components)
                {
                    if (component == null)
                    {
                        hasNull = true;
                        break;
                    }

                    // Check for null references in serialized fields
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();
                    bool hasNullRefs = false;

                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                            iterator.objectReferenceValue == null &&
                            !iterator.name.Equals("m_Script"))
                        {
                            if (!result.NullReferences.ContainsKey(obj))
                            {
                                result.NullReferences[obj] = new List<Component>();
                            }
                            if (!hasNullRefs)
                            {
                                result.NullReferences[obj].Add(component);
                                hasNullRefs = true;
                            }
                        }
                    }
                }

                if (hasNull)
                {
                    result.MissingComponents.Add(obj);
                }

                // Recursively check children
                foreach (Transform child in obj.transform)
                {
                    DetectMissingComponents(new[] { child.gameObject }, result);
                }
            }
        }

        private static void TrackDependencies(GameObject[] objects, SceneAnalysisResult result)
        {
            foreach (var obj in objects)
            {
                var dependencies = new List<GameObject>();
                var components = obj.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null) continue;

                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();

                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                            iterator.objectReferenceValue is GameObject referenced &&
                            referenced != obj &&
                            !dependencies.Contains(referenced))
                        {
                            dependencies.Add(referenced);
                        }
                    }
                }

                if (dependencies.Count > 0)
                {
                    result.Dependencies[obj] = dependencies;
                }

                // Recursively track children
                foreach (Transform child in obj.transform)
                {
                    TrackDependencies(new[] { child.gameObject }, result);
                }
            }
        }

        private static void AnalyzePrefabVariants(GameObject[] objects, SceneAnalysisResult result)
        {
            foreach (var obj in objects)
            {
                var prefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (prefabInstance != null && prefabInstance == obj)
                {
                    var baseObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (baseObject != null)
                    {
                        var analysis = new PrefabVariantAnalysis
                        {
                            Prefab = obj,
                            BaseObject = baseObject
                        };

                        // Compare properties
                        CompareProperties(obj, baseObject, analysis);
                        if (analysis.Differences.Count > 0)
                        {
                            result.PrefabVariants.Add(analysis);
                        }
                    }
                }

                // Recursively analyze children
                foreach (Transform child in obj.transform)
                {
                    AnalyzePrefabVariants(new[] { child.gameObject }, result);
                }
            }
        }

        private static void CompareProperties(GameObject variant, GameObject baseObject, PrefabVariantAnalysis analysis)
        {
            var variantComponents = variant.GetComponents<Component>();
            var baseComponents = baseObject.GetComponents<Component>();

            for (int i = 0; i < variantComponents.Length; i++)
            {
                if (i >= baseComponents.Length) break;

                var variantComponent = variantComponents[i];
                var baseComponent = baseComponents[i];
                if (variantComponent == null || baseComponent == null) continue;

                var variantSerialized = new SerializedObject(variantComponent);
                var baseSerialized = new SerializedObject(baseComponent);

                var variantIterator = variantSerialized.GetIterator();
                var baseIterator = baseSerialized.GetIterator();

                while (variantIterator.NextVisible(true) && baseIterator.NextVisible(true))
                {
                    if (variantIterator.propertyType != baseIterator.propertyType) continue;
                    if (variantIterator.name.Equals("m_Script")) continue;

                    if (!SerializedProperty.DataEquals(variantIterator, baseIterator))
                    {
                        analysis.Differences.Add(new PropertyDifference
                        {
                            PropertyPath = variantIterator.propertyPath,
                            BaseValue = GetPropertyValue(baseIterator),
                            VariantValue = GetPropertyValue(variantIterator),
                            Component = variantComponent
                        });
                    }
                }
            }
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                default:
                    return property.propertyPath;
            }
        }

        private static void GenerateOptimizationTips(Scene scene, SceneAnalysisResult result)
        {
            // Check for deep hierarchy
            var allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                int depth = 0;
                var current = obj.transform;
                while (current.parent != null)
                {
                    depth++;
                    current = current.parent;
                }

                if (depth > 7)
                {
                    result.OptimizationTips.Add(new SceneOptimizationTip
                    {
                        Description = $"Deep hierarchy detected ({depth} levels). Consider flattening for better performance.",
                        Severity = TipSeverity.Warning,
                        Target = obj
                    });
                }
            }

            // Check for large number of active objects
            if (allObjects.Length > 1000)
            {
                result.OptimizationTips.Add(new SceneOptimizationTip
                {
                    Description = $"Large number of GameObjects ({allObjects.Length}). Consider object pooling or splitting into multiple scenes.",
                    Severity = TipSeverity.Warning
                });
            }

            // Check for objects with many components
            foreach (var obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                if (components.Length > 10)
                {
                    result.OptimizationTips.Add(new SceneOptimizationTip
                    {
                        Description = $"High component count ({components.Length}). Consider splitting functionality.",
                        Severity = TipSeverity.Info,
                        Target = obj
                    });
                }
            }
        }
    }
} 