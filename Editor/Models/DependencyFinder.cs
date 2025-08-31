using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public static class DependencyFinder
      {
            public static List<Object> FindDependencies(string assetPath)
            {
                  if (string.IsNullOrEmpty(assetPath))
                  {
                        return new List<Object>();
                  }

                  string[] dependencyPaths = AssetDatabase.GetDependencies(assetPath, false);

                  return dependencyPaths.Where(path => path != assetPath)
                                        .Select(AssetDatabase.LoadAssetAtPath<Object>)
                                        .Where(static obj => obj != null)
                                        .OrderBy(static obj => obj.name)
                                        .ToList();
            }

            public static List<Object> FindReferencers(string assetPath)
            {
                  if (string.IsNullOrEmpty(assetPath))
                  {
                        return new List<Object>();
                  }

                  var referencers = new List<Object>();
                  string targetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                  if (string.IsNullOrEmpty(targetGuid))
                  {
                        return referencers;
                  }

                  string[] assetPathsToScan = AssetDatabase.GetAllAssetPaths()
                                                           .Where(static path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                                                                                 path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                                                                                 path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                                                           .ToArray();

                  foreach (string path in assetPathsToScan)
                  {
                        if (path == assetPath)
                        {
                              continue;
                        }

                        string content = File.ReadAllText(path);

                        if (content.Contains(targetGuid, StringComparison.OrdinalIgnoreCase))
                        {
                              var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                              if (asset != null)
                              {
                                    referencers.Add(asset);
                              }
                        }
                  }

                  return referencers.OrderBy(static obj => obj.name).ToList();
            }

            public static List<GameObject> FindGameObjectReferencersInScene(string scenePath, string targetGuid)
            {
                  var foundGameObjects = new List<GameObject>();

                  if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(targetGuid))
                  {
                        return foundGameObjects;
                  }

                  if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                  {
                        return null;
                  }

                  SceneSetup[] originalSceneSetup = EditorSceneManager.GetSceneManagerSetup();

                  try
                  {
                        Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                        if (!openedScene.IsValid())
                        {
                              return foundGameObjects;
                        }

                        var allComponents = new List<Component>();
                        GameObject[] rootGameObjects = openedScene.GetRootGameObjects();

                        foreach (GameObject go in rootGameObjects)
                        {
                              allComponents.AddRange(go.GetComponentsInChildren<Component>(true));
                        }

                        int totalComponents = allComponents.Count;

                        for (int i = 0; i < totalComponents; i++)
                        {
                              Component component = allComponents[i];

                              if (!component)
                              {
                                    continue;
                              }

                              if (EditorUtility.DisplayCancelableProgressBar("Deep Scan", $"Scanning component {i + 1}/{totalComponents}...", (float)i / totalComponents))
                              {
                                    return null;
                              }

                              var so = new SerializedObject(component);
                              SerializedProperty prop = so.GetIterator();

                              while (prop.NextVisible(true))
                              {
                                    if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue == null)
                                    {
                                          continue;
                                    }

                                    string path = AssetDatabase.GetAssetPath(prop.objectReferenceValue);

                                    if (!string.IsNullOrEmpty(path))
                                    {
                                          string guid = AssetDatabase.AssetPathToGUID(path);

                                          if (guid == targetGuid)
                                          {
                                                if (!foundGameObjects.Contains(component.gameObject))
                                                {
                                                      foundGameObjects.Add(component.gameObject);
                                                }

                                                break;
                                          }
                                    }
                              }
                        }
                  }
                  finally
                  {
                        EditorUtility.ClearProgressBar();
                        EditorSceneManager.RestoreSceneManagerSetup(originalSceneSetup);
                  }

                  return foundGameObjects.Where(static go => go).OrderBy(static go => go.name).ToList();
            }
      }
}