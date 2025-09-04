using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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

                        try
                        {
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
                        catch (Exception e)
                        {
                              Debug.LogWarning($"Could not read asset file at path '{path}'. It might be corrupted or locked. Error: {e.Message}");
                        }
                  }

                  return referencers.OrderBy(static obj => obj.name).ToList();
            }

            public static List<GameObject> FindGameObjectReferencersInScene(string scenePath, string targetGuid, Action<float, string> progressCallback = null)
            {
                  var foundGameObjects = new List<GameObject>();

                  if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(targetGuid))
                  {
                        return foundGameObjects;
                  }

                  try
                  {
                        Scene currentScene = SceneManager.GetActiveScene();

                        if (currentScene.IsValid() && currentScene.path == scenePath)
                        {
                              return ScanSceneForReferences(currentScene, targetGuid, progressCallback);
                        }

                        List<GameObject> fileReferences = AnalyzeSceneFileForReferences(scenePath, targetGuid);

                        if (fileReferences.Count > 0)
                        {
                              return fileReferences;
                        }

                        return foundGameObjects;
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error during scan {scenePath}: {e.Message}");

                        return foundGameObjects;
                  }
                  finally
                  {
                        progressCallback?.Invoke(1f, "Finished");
                        EditorUtility.ClearProgressBar();
                  }
            }

            private static List<GameObject> ScanSceneForReferences(Scene scene, string targetGuid, Action<float, string> progressCallback = null)
            {
                  var foundGameObjects = new List<GameObject>();
                  var allGameObjects = new List<GameObject>();

                  foreach (GameObject rootGo in scene.GetRootGameObjects())
                  {
                        CollectAllGameObjects(rootGo, allGameObjects);
                  }

                  int totalObjects = allGameObjects.Count;
                  progressCallback?.Invoke(0f, $"Scan of {totalObjects} GameObjects...");

                  for (int i = 0; i < totalObjects; i++)
                  {
                        GameObject go = allGameObjects[i];

                        if (!go)
                        {
                              continue;
                        }

                        float progress = (float)i / totalObjects;
                        progressCallback?.Invoke(progress, $"Analyse of {go.name}...");

                        if (GameObjectReferencesAsset(go, targetGuid))
                        {
                              foundGameObjects.Add(go);
                        }

                        if (i % 10 == 0 && EditorUtility.DisplayCancelableProgressBar("Deep Scan", $"Analyse {i + 1}/{totalObjects}: {go.name}", progress))
                        {
                              break;
                        }
                  }

                  return foundGameObjects.Where(static go => go != null).OrderBy(static go => go.name).ToList();
            }

            private static void CollectAllGameObjects(GameObject parent, List<GameObject> collection)
            {
                  collection.Add(parent);

                  foreach (Transform child in parent.transform)
                  {
                        CollectAllGameObjects(child.gameObject, collection);
                  }
            }

            private static bool GameObjectReferencesAsset(GameObject gameObject, string targetGuid)
            {
                  try
                  {
                        Component[] components = gameObject.GetComponents<Component>();

                        foreach (Component component in components)
                        {
                              if (!component)
                              {
                                    continue;
                              }

                              using var serializedObject = new SerializedObject(component);

                              SerializedProperty prop = serializedObject.GetIterator();

                              while (prop.NextVisible(true))
                              {
                                    if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue != null)
                                    {
                                          string assetPath = AssetDatabase.GetAssetPath(prop.objectReferenceValue);

                                          if (!string.IsNullOrEmpty(assetPath))
                                          {
                                                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                                                if (guid == targetGuid)
                                                {
                                                      return true;
                                                }
                                          }
                                    }
                              }
                        }
                  }
                  catch (Exception e)
                  {
                        Debug.LogWarning($"Error during analysis of {gameObject.name}: {e.Message}");
                  }

                  return false;
            }

            private static List<GameObject> AnalyzeSceneFileForReferences(string scenePath, string targetGuid)
            {
                  var foundObjects = new List<GameObject>();

                  try
                  {
                        if (!File.Exists(scenePath))
                        {
                              return foundObjects;
                        }

                        string sceneContent = File.ReadAllText(scenePath);

                        if (!sceneContent.Contains(targetGuid, StringComparison.OrdinalIgnoreCase))
                        {
                              return foundObjects;
                        }

                        List<string> gameObjectNames = ParseGameObjectNamesFromScene(sceneContent, targetGuid);

                        foreach (string goName in gameObjectNames)
                        {
                              var dummyObject = new GameObject(goName)
                              {
                                          hideFlags = HideFlags.DontSave
                              };
                              foundObjects.Add(dummyObject);
                        }

                        Debug.Log($"Quick scan found {foundObjects.Count} GameObject references in {Path.GetFileNameWithoutExtension(scenePath)}");
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error during analysis {scenePath}: {e.Message}");
                  }

                  return foundObjects;
            }

            private static List<string> ParseGameObjectNamesFromScene(string sceneContent, string targetGuid)
            {
                  var gameObjectNames = new List<string>();
                  string[] lines = sceneContent.Split('\n');

                  string currentGameObjectName = "";
                  bool foundReferenceInCurrentGo = false;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i].Trim();

                        if (line.StartsWith("GameObject:", StringComparison.OrdinalIgnoreCase))
                        {
                              if (foundReferenceInCurrentGo && !string.IsNullOrEmpty(currentGameObjectName))
                              {
                                    gameObjectNames.Add(currentGameObjectName);
                              }

                              foundReferenceInCurrentGo = false;
                              currentGameObjectName = "Unknown GameObject";

                              for (int j = i + 1; j < Math.Min(i + 15, lines.Length); j++)
                              {
                                    string nextLine = lines[j].Trim();

                                    if (nextLine.StartsWith("m_Name:", StringComparison.OrdinalIgnoreCase))
                                    {
                                          string nameValue = nextLine[7..].Trim();

                                          if (nameValue.StartsWith("\"", StringComparison.OrdinalIgnoreCase) &&
                                              nameValue.EndsWith("\"", StringComparison.OrdinalIgnoreCase))
                                          {
                                                nameValue = nameValue.Substring(1, nameValue.Length - 2);
                                          }

                                          nameValue = nameValue.Trim();

                                          if (!string.IsNullOrEmpty(nameValue))
                                          {
                                                currentGameObjectName = nameValue;
                                          }

                                          break;
                                    }
                              }
                        }

                        if (line.Contains(targetGuid, StringComparison.OrdinalIgnoreCase))
                        {
                              foundReferenceInCurrentGo = true;
                        }
                  }

                  if (foundReferenceInCurrentGo && !string.IsNullOrEmpty(currentGameObjectName))
                  {
                        gameObjectNames.Add(currentGameObjectName);
                  }

                  return gameObjectNames.Where(static name => name != "Unknown GameObject").Distinct().OrderBy(static name => name).ToList();
            }
      }
}