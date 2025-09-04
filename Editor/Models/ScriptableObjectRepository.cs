using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public sealed class ScriptableObjectRepository
      {
            public List<ScriptableObjectData> AllScriptableObjects { get; }
            private bool IsScanning { get; set; }
            private readonly SettingsManager _settingsManager;
            private IEnumerator _scanCoroutine;

            public ScriptableObjectRepository(SettingsManager settingsManager)
            {
                  _settingsManager = settingsManager;
                  AllScriptableObjects = new List<ScriptableObjectData>();
            }

            public void StartScan(Action onComplete)
            {
                  if (IsScanning)
                  {
                        return;
                  }

                  _scanCoroutine = ScanCoroutine(onComplete);
                  EditorApplication.update += UpdateScan;
            }

            private void UpdateScan()
            {
                  if (_scanCoroutine != null)
                  {
                        if (!_scanCoroutine.MoveNext())
                        {
                              EditorApplication.update -= UpdateScan;
                              _scanCoroutine = null;
                        }
                  }
                  else
                  {
                        EditorApplication.update -= UpdateScan;
                  }
            }

            private IEnumerator ScanCoroutine(Action onComplete)
            {
                  IsScanning = true;
                  AllScriptableObjects.Clear();

                  yield return null;

                  var searchInFolders = new List<string> { "Assets" };

                  if (_settingsManager.ScanPackages)
                  {
                        searchInFolders.Add("Packages");
                  }

                  string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", searchInFolders.ToArray());

                  yield return null;

                  HashSet<Type> userMadeTypes = null;

                  if (_settingsManager.ScanOnlyUserMadeSOs)
                  {
                        userMadeTypes = new HashSet<Type>(TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>()
                                                                   .Where(static t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t)));
                  }

                  const int batchSize = 50;

                  for (int i = 0; i < guids.Length; i++)
                  {
                        string guid = guids[i];
                        string path = AssetDatabase.GUIDToAssetPath(guid);

                        if (_settingsManager.ExcludedPaths.Any(excludedPath => path.StartsWith(excludedPath, StringComparison.Ordinal)))
                        {
                              continue;
                        }

                        var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                        if (so)
                        {
                              if (userMadeTypes != null && !userMadeTypes.Contains(so.GetType()))
                              {
                                    continue;
                              }

                              AllScriptableObjects.Add(new ScriptableObjectData
                              {
                                          scriptableObject = so,
                                          name = so.name,
                                          type = so.GetType().Name,
                                          path = path,
                                          guid = guid,
                                          LastModified = File.GetLastWriteTime(path)
                              });
                        }

                        if (i > 0 && i % batchSize == 0)
                        {
                              yield return null;
                        }
                  }

                  IsScanning = false;
                  onComplete?.Invoke();
                  _scanCoroutine = null;
            }

            public string[] GetAllSoTypes()
            {
                  var types = new List<string> { "All Types" };
                  types.AddRange(AllScriptableObjects.Select(static so => so.type).Distinct().OrderBy(static t => t));

                  return types.ToArray();
            }
      }
}