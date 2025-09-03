using System;
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
            private readonly SettingsManager _settingsManager;

            public ScriptableObjectRepository(SettingsManager settingsManager)
            {
                  _settingsManager = settingsManager;
                  AllScriptableObjects = new List<ScriptableObjectData>();
            }

            public void RefreshData()
            {
                  AllScriptableObjects.Clear();

                  var searchInFolders = new List<string> { "Assets" };

                  if (_settingsManager.ScanPackages)
                  {
                        searchInFolders.Add("Packages");
                  }

                  string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", searchInFolders.ToArray());

                  foreach (string guid in guids)
                  {
                        string path = AssetDatabase.GUIDToAssetPath(guid);

                        if (_settingsManager.ExcludedPaths.Any(excludedPath => path.StartsWith(excludedPath, StringComparison.Ordinal)))
                        {
                              continue;
                        }

                        var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                        if (so)
                        {
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
                  }
            }

            public string[] GetAllSoTypes()
            {
                  var types = new List<string> { "All Types" };
                  types.AddRange(AllScriptableObjects.Select(static so => so.type).Distinct().OrderBy(static t => t));

                  return types.ToArray();
            }
      }
}