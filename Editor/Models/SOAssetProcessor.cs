using System;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public sealed class SoAssetProcessor : AssetPostprocessor
      {
            public static event Action OnAssetsChanged;

            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                  bool hasSoChanges = false;

                  foreach (string path in importedAssets)
                  {
                        if (IsScriptableObjectAsset(path))
                        {
                              hasSoChanges = true;

                              break;
                        }
                  }

                  if (!hasSoChanges)
                  {
                        foreach (string path in deletedAssets)
                        {
                              if (IsScriptableObjectAsset(path))
                              {
                                    hasSoChanges = true;

                                    break;
                              }
                        }
                  }

                  if (!hasSoChanges)
                  {
                        foreach (string path in movedAssets)
                        {
                              if (IsScriptableObjectAsset(path))
                              {
                                    hasSoChanges = true;

                                    break;
                              }
                        }
                  }

                  if (hasSoChanges)
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