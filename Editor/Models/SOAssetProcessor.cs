using System;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public class SOAssetProcessor : AssetPostprocessor
      {
            public static event Action OnAssetsChanged;

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                  bool hasSOChanges = false;

                  foreach (string path in importedAssets)
                  {
                        if (IsScriptableObjectAsset(path))
                        {
                              hasSOChanges = true;

                              break;
                        }
                  }

                  if (!hasSOChanges)
                  {
                        foreach (string path in deletedAssets)
                        {
                              if (IsScriptableObjectAsset(path))
                              {
                                    hasSOChanges = true;

                                    break;
                              }
                        }
                  }

                  if (!hasSOChanges)
                  {
                        foreach (string path in movedAssets)
                        {
                              if (IsScriptableObjectAsset(path))
                              {
                                    hasSOChanges = true;

                                    break;
                              }
                        }
                  }

                  if (hasSOChanges)
                  {
                        OnAssetsChanged?.Invoke();
                  }
            }

            private static bool IsScriptableObjectAsset(string path)
            {
                  if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                  {
                        return false;
                  }

                  var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                  return asset != null;
            }
      }
}