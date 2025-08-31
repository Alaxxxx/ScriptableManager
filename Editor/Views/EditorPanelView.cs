using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class EditorPanelView
      {
            public event Action OnRequestBulkDelete;
            public event Action OnRequestBulkAddToFavorites;

            private SerializedObject _serializedObject;
            private Vector2 _scrollPos;
            private List<ScriptableObjectData> _currentSelection;
            private bool _isMultiEditingDifferentTypes;
            private Texture2D _assetPreview;

            private bool _dependenciesFoldout = true;
            private Vector2 _dependencyScrollPos;
            private List<Object> _dependencies = new();
            private List<Object> _referencers = new();
            private bool _isSearchingDependencies;

            private readonly Dictionary<string, List<GameObject>> _quickScanResults = new();
            private bool _isScanning;

            public void SetTargets(List<ScriptableObjectData> selection)
            {
                  _currentSelection = selection;
                  _isMultiEditingDifferentTypes = false;
                  _serializedObject = null;
                  _assetPreview = null;

                  _dependencies.Clear();
                  _referencers.Clear();
                  _isSearchingDependencies = false;
                  _isScanning = false;

                  ClearQuickScanResults();

                  if (selection == null || selection.Count == 0)
                  {
                        return;
                  }

                  if (selection.Count == 1)
                  {
                        _assetPreview = AssetPreview.GetAssetPreview(selection[0].scriptableObject);
                        SearchForDependencies(selection[0].path);
                  }

                  ScriptableObject[] objects = selection.Select(static s => s.scriptableObject).Where(static s => s).ToArray();

                  if (objects.Length == 0)
                  {
                        return;
                  }

                  if (selection.Count > 1)
                  {
                        string firstType = selection[0].type;

                        if (selection.Any(s => s.type != firstType))
                        {
                              _isMultiEditingDifferentTypes = true;

                              return;
                        }
                  }

                  _serializedObject = new SerializedObject(objects);
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
                  foreach (List<GameObject> results in _quickScanResults.Values)
                  {
                        foreach (GameObject go in results)
                        {
                              if (go && go.hideFlags == HideFlags.DontSave)
                              {
                                    GameObject.DestroyImmediate(go);
                              }
                        }
                  }

                  _quickScanResults.Clear();
            }

            public void Draw()
            {
                  EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
                  _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                  if (_currentSelection == null || _currentSelection.Count == 0)
                  {
                        DrawNoSelectionMessage();
                  }
                  else if (_isMultiEditingDifferentTypes)
                  {
                        DrawMultiEditDifferentTypes();
                  }
                  else if (_currentSelection.Count > 1)
                  {
                        DrawMultiEditSameType();
                  }
                  else
                  {
                        DrawSingleEdit();
                  }

                  EditorGUILayout.EndScrollView();

                  if (_currentSelection is { Count: 1 })
                  {
                        DrawDependencyPanel();
                  }

                  EditorGUILayout.EndVertical();
            }

            private void DrawSingleEdit()
            {
                  ScriptableObjectData soData = _currentSelection[0];

                  EditorGUILayout.LabelField($"Editing: {soData.name}", EditorStyles.boldLabel);
                  EditorGUILayout.LabelField($"Type: {soData.type}", EditorStyles.miniLabel);

                  DrawDetailedInfo(soData);

                  DrawAssetPreview();
                  EditorGUILayout.Space(10);
                  DrawSerializedObjectEditor();
            }

            private void DrawDetailedInfo(ScriptableObjectData soData)
            {
                  EditorGUILayout.BeginVertical("box");
                  EditorGUILayout.LabelField("📁 File Information", EditorStyles.boldLabel);

                  EditorGUILayout.BeginHorizontal();
                  EditorGUILayout.LabelField("Path:", GUILayout.Width(60));
                  EditorGUILayout.SelectableLabel(soData.path, EditorStyles.miniLabel, GUILayout.Height(16));
                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.BeginHorizontal();
                  EditorGUILayout.LabelField("Modified:", GUILayout.Width(60));
                  EditorGUILayout.LabelField($"{soData.FormattedDate} ({soData.RelativeDate})", EditorStyles.miniLabel);
                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.EndVertical();
                  EditorGUILayout.Space(5);
            }

            private void DrawMultiEditSameType()
            {
                  EditorGUILayout.LabelField($"Editing: {_currentSelection.Count} objects", EditorStyles.boldLabel);
                  EditorGUILayout.LabelField($"Type: {_currentSelection[0].type}", EditorStyles.miniLabel);
                  EditorGUILayout.Space(10);
                  DrawSerializedObjectEditor();
            }

            private void DrawMultiEditDifferentTypes()
            {
                  EditorGUILayout.LabelField($"{_currentSelection.Count} objects selected", EditorStyles.boldLabel);
                  EditorGUILayout.HelpBox("Multi-editing is not supported for objects of different types.", MessageType.Info);
                  EditorGUILayout.Space(10);
                  EditorGUILayout.LabelField("Group Actions", EditorStyles.boldLabel);

                  if (GUILayout.Button("Add to Favorites"))
                  {
                        OnRequestBulkAddToFavorites?.Invoke();
                  }

                  if (GUILayout.Button("Delete Selected Objects"))
                  {
                        OnRequestBulkDelete?.Invoke();
                  }
            }

            private void DrawAssetPreview()
            {
                  if (_currentSelection == null || _currentSelection.Count != 1)
                  {
                        return;
                  }

                  GUILayout.Space(10);
                  GUILayout.BeginHorizontal();
                  GUILayout.FlexibleSpace();
                  Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

                  if (_assetPreview)
                  {
                        GUI.DrawTexture(previewRect, _assetPreview, ScaleMode.ScaleToFit);
                  }
                  else if (AssetPreview.IsLoadingAssetPreview(_currentSelection[0].scriptableObject.GetInstanceID()))
                  {
                        EditorGUI.LabelField(previewRect, "Loading Preview...", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        Texture2D fallbackIcon = AssetPreview.GetMiniThumbnail(_currentSelection[0].scriptableObject);

                        if (fallbackIcon)
                        {
                              GUI.DrawTexture(previewRect, fallbackIcon, ScaleMode.ScaleToFit);
                        }
                  }

                  GUILayout.FlexibleSpace();
                  GUILayout.EndHorizontal();
            }

            private void DrawSerializedObjectEditor()
            {
                  if (_serializedObject != null && _serializedObject.targetObject != null)
                  {
                        _serializedObject.Update();
                        EditorGUI.BeginChangeCheck();
                        SerializedProperty prop = _serializedObject.GetIterator();

                        if (prop.NextVisible(true))
                        {
                              do
                              {
                                    if (prop.name == "m_Script")
                                    {
                                          continue;
                                    }

                                    EditorGUILayout.PropertyField(prop, true);
                              } while (prop.NextVisible(false));
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                              _serializedObject.ApplyModifiedProperties();
                        }
                  }
            }

            private static void DrawNoSelectionMessage()
            {
                  GUILayout.FlexibleSpace();
                  EditorGUILayout.LabelField("Select a ScriptableObject to edit", EditorStyles.centeredGreyMiniLabel);
                  GUILayout.FlexibleSpace();
            }

            private void DrawDependencyPanel()
            {
                  GUILayout.Box(GUIContent.none, SoManagerStyles.Separator);
                  _dependenciesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_dependenciesFoldout, $"Dependencies ({_dependencies.Count + _referencers.Count})");

                  if (_dependenciesFoldout)
                  {
                        if (_isSearchingDependencies)
                        {
                              EditorGUILayout.LabelField("Searching for dependencies...", EditorStyles.centeredGreyMiniLabel);
                        }
                        else
                        {
                              float contentHeight = EstimateDependencyPanelHeight();
                              float panelHeight = Mathf.Clamp(contentHeight, 100, 300);
                              _dependencyScrollPos = EditorGUILayout.BeginScrollView(_dependencyScrollPos, GUILayout.Height(panelHeight));

                              EditorGUILayout.BeginVertical(SoManagerStyles.DependencyBox);
                              DrawSimpleDependencyList("Uses (Dependencies)", _dependencies);
                              EditorGUILayout.EndVertical();

                              EditorGUILayout.Space(5);

                              EditorGUILayout.BeginVertical(SoManagerStyles.DependencyBox);
                              DrawCategorizedReferencerList("Used By (Referencers)", _referencers);
                              EditorGUILayout.EndVertical();

                              EditorGUILayout.EndScrollView();
                        }
                  }

                  EditorGUILayout.EndFoldoutHeaderGroup();
            }

            private float EstimateDependencyPanelHeight()
            {
                  float height = 50;
                  height += _dependencies.Count * 22;
                  List<IGrouping<string, Object>> groupedReferencers = _referencers.GroupBy(GetAssetCategory).ToList();
                  height += groupedReferencers.Count * 20;
                  height += _referencers.Count * 22;

                  foreach (List<GameObject> result in _quickScanResults.Values)
                  {
                        height += result.Count * 22;
                  }

                  return height;
            }

            private static void DrawSimpleDependencyList(string label, List<Object> assets)
            {
                  EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                  if (assets.Count == 0)
                  {
                        EditorGUILayout.LabelField("   None found.", EditorStyles.miniLabel);

                        return;
                  }

                  DrawAssetList(assets);
            }

            private void DrawCategorizedReferencerList(string label, List<Object> assets)
            {
                  EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                  if (assets.Count == 0)
                  {
                        EditorGUILayout.LabelField("   None found.", EditorStyles.miniLabel);

                        return;
                  }

                  IOrderedEnumerable<IGrouping<string, Object>> groupedAssets = assets.GroupBy(GetAssetCategory).OrderBy(static g => g.Key);

                  foreach (IGrouping<string, Object> group in groupedAssets)
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
                  if (asset is SceneAsset)
                  {
                        return "Scenes";
                  }

                  if (asset is GameObject)
                  {
                        return "Prefabs";
                  }

                  if (asset is ScriptableObject)
                  {
                        return "Scriptable Objects";
                  }

                  return "Other Assets";
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
                        var sceneAsset = asset as SceneAsset;

                        if (!sceneAsset)
                        {
                              continue;
                        }

                        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                        bool isScanning = _isScanning;

                        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, SoManagerStyles.DependencyItemStyle);
                        var scanButtonRect = new Rect(rowRect.xMax - 24, rowRect.y + 2, 22, 16);
                        var pingAreaRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 28, rowRect.height);

                        Event currentEvent = Event.current;

                        if (currentEvent.type == EventType.MouseDown && rowRect.Contains(currentEvent.mousePosition))
                        {
                              if (scanButtonRect.Contains(currentEvent.mousePosition) && !isScanning)
                              {
                                    PerformQuickScan(sceneAsset);
                                    currentEvent.Use();
                              }
                              else if (pingAreaRect.Contains(currentEvent.mousePosition))
                              {
                                    EditorGUIUtility.PingObject(sceneAsset);
                                    currentEvent.Use();
                              }
                        }

                        if (currentEvent.type == EventType.Repaint)
                        {
                              SoManagerStyles.DependencyItemStyle.Draw(rowRect, false, false, false, false);
                              Texture2D icon = AssetPreview.GetMiniThumbnail(sceneAsset);
                              var labelRect = new Rect(pingAreaRect.x + 4, pingAreaRect.y + 2, pingAreaRect.width - 8, pingAreaRect.height - 4);

                              if (icon)
                              {
                                    GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, 16, 16), icon);
                              }

                              GUI.Label(new Rect(labelRect.x + 20, labelRect.y, labelRect.width - 20, 16), sceneAsset.name, EditorStyles.label);

                              GUI.Label(scanButtonRect,
                                          isScanning
                                                      ? new GUIContent("⏳", "Scanning in progress...")
                                                      : new GUIContent("🔍", "Quick Scan: Find GameObjects that reference this asset"));
                        }

                        if (_quickScanResults.TryGetValue(scenePath, out List<GameObject> gameObjects))
                        {
                              DrawQuickScanResults(gameObjects);
                        }
                  }
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

                        foreach (GameObject go in gameObjects)
                        {
                              if (go)
                              {
                                    DrawGameObjectReference(go);
                              }
                        }
                  }

                  EditorGUI.indentLevel--;
            }

            private static void DrawGameObjectReference(GameObject go)
            {
                  if (!go)
                  {
                        return;
                  }

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
                  if (_currentSelection == null || _currentSelection.Count == 0 || _isScanning)
                  {
                        return;
                  }

                  string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                  string targetGuid = _currentSelection[0].guid;

                  _isScanning = true;

                  try
                  {
                        EditorUtility.DisplayProgressBar("Quick Scan", $"Analyzing {sceneAsset.name}...", 0.5f);

                        List<GameObject> results = DependencyFinder.FindGameObjectReferencersInScene(scenePath, targetGuid);
                        _quickScanResults[scenePath] = results ?? new List<GameObject>();
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error during quick scan of {sceneAsset.name}: {e.Message}");
                        _quickScanResults[scenePath] = new List<GameObject>();
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