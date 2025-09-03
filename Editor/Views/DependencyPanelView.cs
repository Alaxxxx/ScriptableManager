using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class DependencyPanelView
      {
            private bool _dependenciesFoldout = true;
            private Vector2 _dependencyScrollPos;
            private List<Object> _dependencies = new();
            private List<Object> _referencers = new();
            private bool _isSearchingDependencies;
            private readonly Dictionary<string, List<GameObject>> _quickScanResults = new();
            private bool _isScanning;
            private ScriptableObjectData _currentTarget;
            private int _currentTab;
            private readonly string[] _tabs = { "Uses (Dependencies)", "Used By (Referencers)" };

            public void SetTarget(ScriptableObjectData target)
            {
                  if (target == null || _currentTarget.Equals(target))
                  {
                        return;
                  }

                  _currentTarget = target;

                  _dependencies.Clear();
                  _referencers.Clear();
                  ClearQuickScanResults();
                  SearchForDependencies(target.path);
            }

            public void ClearTarget()
            {
                  _currentTarget = null;
                  _dependencies.Clear();
                  _referencers.Clear();
                  ClearQuickScanResults();
            }

            private void SearchForDependencies(string path)
            {
                  _isSearchingDependencies = true;
                  _dependencies = DependencyFinder.FindDependencies(path);
                  _referencers = DependencyFinder.FindReferencers(path);
                  _isSearchingDependencies = false;
            }

            private void ClearQuickScanResults()
            {
                  if (_quickScanResults.Count == 0)
                  {
                        return;
                  }

                  List<GameObject> objectsToDestroy = _quickScanResults.Values.SelectMany(static list => list)
                                                                       .Where(static go => go && go.hideFlags == HideFlags.DontSave)
                                                                       .ToList();

                  if (objectsToDestroy.Any())
                  {
                        EditorApplication.delayCall += () =>
                        {
                              foreach (GameObject go in objectsToDestroy)
                              {
                                    if (go)
                                    {
                                          GameObject.DestroyImmediate(go);
                                    }
                              }
                        };
                  }

                  _quickScanResults.Clear();
            }

            public void Draw()
            {
                  GUILayout.FlexibleSpace();

                  GUILayout.Box(GUIContent.none, SoManagerStyles.Separator);

                  string foldoutLabel = $"Dependencies ({_dependencies.Count + _referencers.Count})";
                  _dependenciesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_dependenciesFoldout, foldoutLabel);

                  if (_dependenciesFoldout)
                  {
                        if (_isSearchingDependencies)
                        {
                              EditorGUILayout.LabelField("Searching for dependencies...", EditorStyles.centeredGreyMiniLabel);
                        }
                        else
                        {
                              _currentTab = GUILayout.Toolbar(_currentTab, _tabs);

                              _dependencyScrollPos = EditorGUILayout.BeginScrollView(_dependencyScrollPos, GUILayout.MinHeight(150), GUILayout.ExpandHeight(true));

                              EditorGUILayout.BeginVertical(SoManagerStyles.DependencyBox);

                              if (_currentTab == 0)
                              {
                                    DrawSimpleDependencyList(_dependencies);
                              }
                              else if (_currentTab == 1)
                              {
                                    DrawCategorizedReferencerList(_referencers);
                              }

                              EditorGUILayout.EndVertical();
                              EditorGUILayout.EndScrollView();
                        }
                  }

                  EditorGUILayout.EndFoldoutHeaderGroup();
            }

            private static void DrawSimpleDependencyList(List<Object> assets)
            {
                  if (assets.Count == 0)
                  {
                        EditorGUILayout.LabelField("   None found.", EditorStyles.miniLabel);

                        return;
                  }

                  DrawAssetList(assets);
            }

            private void DrawCategorizedReferencerList(List<Object> assets)
            {
                  if (assets.Count == 0)
                  {
                        EditorGUILayout.LabelField("   None found.", EditorStyles.miniLabel);

                        return;
                  }

                  foreach (IGrouping<string, Object> group in assets.GroupBy(GetAssetCategory).OrderBy(static g => g.Key))
                  {
                        EditorGUILayout.LabelField($"   {group.Key}", EditorStyles.boldLabel);

                        if (group.Key == "Scenes")
                        {
                              DrawSceneAssetList(group.ToList());
                        }
                        else
                        {
                              DrawAssetList(group.ToList());
                        }
                  }
            }

            private static string GetAssetCategory(Object asset)
            {
                  return asset switch
                  {
                              SceneAsset => "Scenes",
                              GameObject => "Prefabs",
                              ScriptableObject => "Scriptable Objects",
                              _ => "Other Assets"
                  };
            }

            private static void DrawAssetList(IEnumerable<Object> assets)
            {
                  foreach (Object asset in assets)
                  {
                        DrawSingleAsset(asset);
                  }
            }

            private void DrawSceneAssetList(IEnumerable<Object> assets)
            {
                  foreach (Object asset in assets)
                  {
                        if (asset is not SceneAsset sceneAsset)
                        {
                              continue;
                        }

                        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, SoManagerStyles.DependencyItemStyle);
                        var scanButtonRect = new Rect(rowRect.xMax - 24, rowRect.y + 2, 22, 16);
                        var pingAreaRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 28, rowRect.height);

                        HandleSceneAssetEvents(pingAreaRect, scanButtonRect, sceneAsset, Event.current);

                        if (Event.current.type == EventType.Repaint)
                        {
                              DrawSceneAssetRow(pingAreaRect, scanButtonRect, sceneAsset);
                        }

                        if (_quickScanResults.TryGetValue(scenePath, out List<GameObject> gameObjects))
                        {
                              DrawQuickScanResults(gameObjects);
                        }
                  }
            }

            private void HandleSceneAssetEvents(Rect pingRect, Rect scanRect, SceneAsset sceneAsset, Event e)
            {
                  if (e.type == EventType.MouseDown && (pingRect.Contains(e.mousePosition) || scanRect.Contains(e.mousePosition)))
                  {
                        if (scanRect.Contains(e.mousePosition) && !_isScanning)
                        {
                              PerformQuickScan(sceneAsset);
                        }
                        else
                        {
                              EditorGUIUtility.PingObject(sceneAsset);
                        }

                        e.Use();
                  }
            }

            private void DrawSceneAssetRow(Rect pingRect, Rect scanRect, SceneAsset sceneAsset)
            {
                  SoManagerStyles.DependencyItemStyle.Draw(pingRect, false, false, false, false);
                  Texture2D icon = AssetPreview.GetMiniThumbnail(sceneAsset);
                  var labelRect = new Rect(pingRect.x + 4, pingRect.y + 2, pingRect.width - 8, pingRect.height - 4);

                  if (icon)
                  {
                        GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, 16, 16), icon);
                  }

                  GUI.Label(new Rect(labelRect.x + 20, labelRect.y, labelRect.width - 20, 16), sceneAsset.name, EditorStyles.label);
                  GUI.Label(scanRect, _isScanning ? new GUIContent("⏳", "Scanning...") : new GUIContent("🔍", "Quick Scan"));
            }

            private static void DrawSingleAsset(Object asset)
            {
                  if (asset == null)
                  {
                        return;
                  }

                  if (GUILayout.Button(GUIContent.none, SoManagerStyles.DependencyItemStyle))
                  {
                        EditorGUIUtility.PingObject(asset);
                  }

                  Rect buttonRect = GUILayoutUtility.GetLastRect();
                  Texture2D icon = AssetPreview.GetMiniThumbnail(asset);
                  var contentRect = new Rect(buttonRect.x + 4, buttonRect.y + 2, buttonRect.width - 8, buttonRect.height - 4);

                  if (icon)
                  {
                        GUI.DrawTexture(new Rect(contentRect.x, contentRect.y, 16, 16), icon);
                  }

                  GUI.Label(new Rect(contentRect.x + 20, contentRect.y, contentRect.width - 20, 16), asset.name, EditorStyles.label);
            }

            private static void DrawQuickScanResults(List<GameObject> gameObjects)
            {
                  EditorGUI.indentLevel++;

                  if (gameObjects.Count == 0)
                  {
                        EditorGUILayout.LabelField("   └ ✅ No references found", EditorStyles.miniLabel);
                  }
                  else
                  {
                        EditorGUILayout.LabelField($"   └ ✅ {gameObjects.Count} reference(s) found:", EditorStyles.miniLabel);

                        foreach (GameObject go in gameObjects.Where(static go => go))
                        {
                              DrawGameObjectReference(go);
                        }
                  }

                  EditorGUI.indentLevel--;
            }

            private static void DrawGameObjectReference(GameObject go)
            {
                  EditorGUI.indentLevel++;

                  if (GUILayout.Button(GUIContent.none, SoManagerStyles.DependencyItemStyle))
                  {
                        EditorGUIUtility.PingObject(go);
                        Selection.activeGameObject = go;
                  }

                  Rect buttonRect = GUILayoutUtility.GetLastRect();
                  Texture icon = EditorGUIUtility.IconContent("d_GameObject Icon").image;
                  var contentRect = new Rect(buttonRect.x + 4, buttonRect.y + 2, buttonRect.width - 8, buttonRect.height - 4);

                  if (icon)
                  {
                        GUI.DrawTexture(new Rect(contentRect.x, contentRect.y, 16, 16), icon);
                  }

                  GUI.Label(new Rect(contentRect.x + 20, contentRect.y, contentRect.width - 20, 16), $"└ {go.name}", EditorStyles.label);
                  EditorGUI.indentLevel--;
            }

            private void PerformQuickScan(SceneAsset sceneAsset)
            {
                  if (_currentTarget == null || _isScanning)
                  {
                        return;
                  }

                  string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                  string targetGuid = _currentTarget.guid;

                  _isScanning = true;

                  try
                  {
                        EditorUtility.DisplayProgressBar("Quick Scan", $"Analyzing {sceneAsset.name}...", 0.5f);
                        List<GameObject> results = DependencyFinder.FindGameObjectReferencersInScene(scenePath, targetGuid);
                        _quickScanResults[scenePath] = results ?? new List<GameObject>();
                  }
                  finally
                  {
                        _isScanning = false;
                        EditorUtility.ClearProgressBar();

                        if (EditorWindow.focusedWindow)
                        {
                              EditorWindow.focusedWindow.Repaint();
                        }
                  }
            }
      }
}