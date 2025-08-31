using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Models
{
      public sealed class ScriptableObjectRepository
      {
            public List<ScriptableObjectData> AllScriptableObjects { get; private set; }
            private readonly SettingsManager _settingsManager;

            public ScriptableObjectRepository(SettingsManager settingsManager)
            {
                  _settingsManager = settingsManager;
                  AllScriptableObjects = new List<ScriptableObjectData>();
            }

            public void RefreshData()
            {
                  AllScriptableObjects.Clear();
                  string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });

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

            public Dictionary<string, int> GetTopTypes(int count = 5)
            {
                  return AllScriptableObjects.GroupBy(static so => so.type)
                                             .ToDictionary(static g => g.Key, static g => g.Count())
                                             .OrderByDescending(static kvp => kvp.Value)
                                             .Take(count)
                                             .ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value);
            }

            public static List<Type> GetAllTypesDerivedFromSo()
            {
                  return AppDomain.CurrentDomain.GetAssemblies()
                                  .SelectMany(static assembly => assembly.GetTypes())
                                  .Where(static type => type.IsSubclassOf(typeof(ScriptableObject)) && !type.IsAbstract)
                                  .OrderBy(static type => type.Name)
                                  .ToList();
            }
      }
}