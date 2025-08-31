using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using OpalStudio.ScriptableManager.Editor.Views;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor
{
      public sealed class ScriptableObjectManager : EditorWindow
      {
            [MenuItem("Tools/ScriptableObject Manager")]
            public static void ShowWindow()
            {
                  var window = GetWindow<ScriptableObjectManager>();
                  window.titleContent = new GUIContent("SO Manager", EditorGUIUtility.IconContent("ScriptableObject Icon").image);
                  window.Show();
            }

            private ScriptableObjectRepository _soRepository;
            private FavoritesManager _favoritesManager;
            private SettingsManager _settingsManager;

            private FilterPanelView _filterPanel;
            private SoListView _soListPanel;
            private EditorPanelView _editorPanel;

            private List<ScriptableObjectData> _filteredScriptableObjects = new();

            private HashSet<string> _selectedSoGuids = new();
            private List<ScriptableObjectData> _currentSelectionData = new();

            private float _leftPanelWidth;
            private float _centerPanelWidth;
            private bool _isResizingLeft;
            private bool _isResizingRight;
            private bool _showSettings;

            private void OnEnable()
            {
                  _settingsManager = new SettingsManager();
                  _settingsManager.LoadSettings();

                  _selectedSoGuids = new HashSet<string>(_settingsManager.GetLastSelection());
                  _leftPanelWidth = SettingsManager.GetFloat("SOManager_LeftPanelWidth", 250f);
                  _centerPanelWidth = SettingsManager.GetFloat("SOManager_CenterPanelWidth", 400f);

                  _soRepository = new ScriptableObjectRepository(_settingsManager);
                  _soRepository.RefreshData();

                  _favoritesManager = new FavoritesManager();
                  _favoritesManager.LoadFavorites();

                  string searchText = _settingsManager.GetString("SOManager_SearchText", "");
                  string typeFilter = _settingsManager.GetString("SOManager_TypeFilter", "All Types");
                  _filterPanel = new FilterPanelView(searchText, typeFilter, _soRepository.GetAllSoTypes(), _favoritesManager.favoriteSoGuids);

                  _soListPanel = new SoListView();
                  _editorPanel = new EditorPanelView();

                  SubscribeToEvents();

                  RefreshAll();
                  this.wantsMouseMove = true;
                  EditorApplication.update += OnEditorUpdate;
                  SOAssetProcessor.OnAssetsChanged += OnSOAssetsChanged;
            }

            private void OnDisable()
            {
                  _settingsManager.SetLastSelection(_selectedSoGuids.ToList());
                  _settingsManager.SetFloat("SOManager_LeftPanelWidth", _leftPanelWidth);
                  _settingsManager.SetFloat("SOManager_CenterPanelWidth", _centerPanelWidth);
                  _settingsManager.SetString("SOManager_SearchText", _filterPanel.SearchText);
                  _settingsManager.SetString("SOManager_TypeFilter", _filterPanel.SelectedTypeFilter);
                  _settingsManager.SaveSettings();

                  EditorApplication.update -= OnEditorUpdate;
                  SOAssetProcessor.OnAssetsChanged -= OnSOAssetsChanged;
            }

            private void OnEditorUpdate()
            {
                  if (SoManagerStyles.NeedsRepaint)
                  {
                        Repaint();
                        SoManagerStyles.NeedsRepaint = false;
                  }
            }

            private void SubscribeToEvents()
            {
                  _filterPanel.OnFiltersChanged += ApplyFiltersAndSort;
                  _filterPanel.OnToggleFavoritesFilter += ApplyFiltersAndSort;
                  _filterPanel.OnFavoriteSelected += SelectSo;

                  _soListPanel.OnSelectionChanged += OnSelectionChanged;
                  _soListPanel.OnClearSelection += ClearSelection;

                  _soListPanel.OnSortChanged += sortOption =>
                  {
                        _settingsManager.SetSortOption(sortOption);
                        ApplyFiltersAndSort();
                  };
                  _soListPanel.OnRequestBulkDelete += HandleBulkDelete;
                  _soListPanel.OnRequestBulkToggleFavorites += HandleBulkToggleFavorites;

                  _editorPanel.OnRequestBulkDelete += () => HandleBulkDelete(_currentSelectionData.Select(static s => s.guid));
                  _editorPanel.OnRequestBulkAddToFavorites += () => HandleBulkToggleFavorites(_currentSelectionData.Select(static s => s.guid));
            }

            private void OnGUI()
            {
                  HandleResize();

                  RebuildSelectionDataList();

                  DrawHeader();

                  if (_showSettings)
                  {
                        DrawSettingsPanel();
                  }
                  else
                  {
                        DrawMainLayout();
                  }

                  DrawResizeHandles();
            }

            private void OnSOAssetsChanged()
            {
                  RefreshAll();
            }

            private void DrawHeader()
            {
                  EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                  if (GUILayout.Button(new GUIContent("Refresh", EditorGUIUtility.IconContent("Refresh").image), EditorStyles.toolbarButton, GUILayout.Width(80)))
                  {
                        RefreshAll();
                  }

                  GUILayout.FlexibleSpace();

                  var settingsIcon = new GUIContent("⚙️", "Settings");

                  if (GUILayout.Button(settingsIcon, _showSettings ? SoManagerStyles.ToolbarButtonSelected : EditorStyles.toolbarButton, GUILayout.Width(30)))
                  {
                        _showSettings = !_showSettings;
                  }

                  EditorGUILayout.EndHorizontal();
            }

            private void DrawMainLayout()
            {
                  EditorGUILayout.BeginHorizontal();

                  EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));

                  {
                        List<ScriptableObjectData> favoriteSOs = _soRepository.AllScriptableObjects.Where(so => _favoritesManager.favoriteSoGuids.Contains(so.guid))
                                                                              .OrderBy(static so => so.name)
                                                                              .ToList();

                        _filterPanel.DrawFiltersAndFavorites(favoriteSOs, _soRepository.AllScriptableObjects);


                        GUILayout.FlexibleSpace();

                        _filterPanel.DrawStatistics(_soRepository.AllScriptableObjects.Count);
                  }
                  EditorGUILayout.EndVertical();

                  EditorGUILayout.BeginVertical(GUILayout.Width(_centerPanelWidth));
                  _soListPanel.Draw(_filteredScriptableObjects, _currentSelectionData, _settingsManager.CurrentSortOption, _favoritesManager.favoriteSoGuids);
                  EditorGUILayout.EndVertical();

                  _editorPanel.Draw();

                  EditorGUILayout.EndHorizontal();
            }

            private void DrawSettingsPanel()
            {
                  EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                  EditorGUILayout.HelpBox("Here you can configure paths to exclude from the search.", MessageType.Info);

                  List<string> excludedPaths = _settingsManager.ExcludedPaths;

                  for (int i = 0; i < excludedPaths.Count; i++)
                  {
                        EditorGUILayout.BeginHorizontal();
                        excludedPaths[i] = EditorGUILayout.TextField(excludedPaths[i]);

                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                              excludedPaths.RemoveAt(i);
                              _settingsManager.SaveSettings();
                              RefreshAll();
                        }

                        EditorGUILayout.EndHorizontal();
                  }

                  if (GUILayout.Button("Add Excluded Path"))
                  {
                        excludedPaths.Add("Assets/NewExcludePath/");
                        _settingsManager.SaveSettings();
                  }
            }

            private void HandleResize()
            {
                  Event e = Event.current;

                  if (e.type == EventType.MouseDown && e.button == 0)
                  {
                        CheckResizeStart(e);
                  }

                  if (_isResizingLeft || _isResizingRight)
                  {
                        switch (e.type)
                        {
                              case EventType.MouseDrag:
                              case EventType.MouseMove:
                                    PerformResize(e);
                                    e.Use();
                                    Repaint();

                                    break;

                              case EventType.MouseUp:
                              case EventType.MouseLeaveWindow:
                              case EventType.Ignore:
                                    EndResize(e);

                                    break;

                              case EventType.KeyDown when e.keyCode == KeyCode.Escape:
                                    EndResize(e);

                                    break;
                        }
                  }
            }

            private void CheckResizeStart(Event e)
            {
                  float leftResizeX = _leftPanelWidth;
                  float rightResizeX = _leftPanelWidth + _centerPanelWidth;

                  var leftResizeRect = new Rect(leftResizeX - 2.5f, 0, 5, this.position.height);
                  var rightResizeRect = new Rect(rightResizeX - 2.5f, 0, 5, this.position.height);

                  if (leftResizeRect.Contains(e.mousePosition))
                  {
                        _isResizingLeft = true;
                        e.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                  }
                  else if (rightResizeRect.Contains(e.mousePosition))
                  {
                        _isResizingRight = true;
                        e.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                  }
            }

            private void PerformResize(Event e)
            {
                  if (_isResizingLeft)
                  {
                        _leftPanelWidth = Mathf.Clamp(e.mousePosition.x, 150, this.position.width - _centerPanelWidth - 50);
                  }

                  if (_isResizingRight)
                  {
                        _centerPanelWidth = Mathf.Clamp(e.mousePosition.x - _leftPanelWidth, 200, this.position.width - _leftPanelWidth - 200);
                  }
            }

            private void EndResize(Event e)
            {
                  if (_isResizingLeft || _isResizingRight)
                  {
                        _isResizingLeft = false;
                        _isResizingRight = false;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        e.Use();
                        Repaint();
                  }
            }

            private void DrawResizeHandles()
            {
                  float leftResizeX = _leftPanelWidth;
                  var leftResizeRect = new Rect(leftResizeX - 2.5f, 0, 5, this.position.height);
                  EditorGUIUtility.AddCursorRect(leftResizeRect, MouseCursor.ResizeHorizontal);

                  float rightResizeX = _leftPanelWidth + _centerPanelWidth;
                  var rightResizeRect = new Rect(rightResizeX - 2.5f, 0, 5, this.position.height);
                  EditorGUIUtility.AddCursorRect(rightResizeRect, MouseCursor.ResizeHorizontal);

                  if (_isResizingLeft || _isResizingRight)
                  {
                        Color oldColor = GUI.color;
                        GUI.color = new Color(0.5f, 0.8f, 1f, 0.8f);

                        if (_isResizingLeft)
                        {
                              GUI.DrawTexture(new Rect(leftResizeX - 1f, 0, 2, this.position.height), EditorGUIUtility.whiteTexture);
                        }

                        if (_isResizingRight)
                        {
                              GUI.DrawTexture(new Rect(rightResizeX - 1f, 0, 2, this.position.height), EditorGUIUtility.whiteTexture);
                        }

                        GUI.color = oldColor;
                  }
            }

            private void RefreshAll()
            {
                  AssetDatabase.SaveAssets();
                  AssetDatabase.Refresh();

                  _soRepository.RefreshData();
                  _filterPanel.UpdateAllSoTypes(_soRepository.GetAllSoTypes());
                  ApplyFiltersAndSort();
            }

            private void ApplyFiltersAndSort()
            {
                  _filteredScriptableObjects = _soRepository.AllScriptableObjects.Where(so =>
                                                            {
                                                                  bool matchesSearch = string.IsNullOrEmpty(_filterPanel.SearchText) ||
                                                                                       so.name.ToLower()
                                                                                         .Contains(_filterPanel.SearchText.ToLower(),
                                                                                                     StringComparison.OrdinalIgnoreCase) ||
                                                                                       so.type.ToLower()
                                                                                         .Contains(_filterPanel.SearchText.ToLower(), StringComparison.OrdinalIgnoreCase);

                                                                  bool matchesType = _filterPanel.SelectedTypeFilter == "All Types" ||
                                                                                     so.type == _filterPanel.SelectedTypeFilter;

                                                                  bool matchesFavorites = !_filterPanel.FavoritesOnly ||
                                                                                          _favoritesManager.favoriteSoGuids.Contains(so.guid);

                                                                  return matchesSearch && matchesType && matchesFavorites;
                                                            })
                                                            .ToList();

                  SortFilteredList();
                  RebuildSelectionDataList();
                  _editorPanel.SetTargets(_currentSelectionData);
                  Repaint();
            }

            private void SortFilteredList()
            {
                  switch (_settingsManager.CurrentSortOption)
                  {
                        case SortOption.ByName:
                              _filteredScriptableObjects = _filteredScriptableObjects.OrderBy(static so => so.name).ToList();

                              break;
                        case SortOption.ByType:
                              _filteredScriptableObjects = _filteredScriptableObjects.OrderBy(static so => so.type).ThenBy(static so => so.name).ToList();

                              break;
                        case SortOption.ByDate:
                              _filteredScriptableObjects = _filteredScriptableObjects.OrderByDescending(static so => so.LastModified).ToList();

                              break;
                        case SortOption.ByDateOldest:
                              _filteredScriptableObjects = _filteredScriptableObjects.OrderBy(static so => so.LastModified).ToList();

                              break;
                  }
            }

            private void RebuildSelectionDataList()
            {
                  if (_selectedSoGuids.Count > 0)
                  {
                        _currentSelectionData = _soRepository.AllScriptableObjects.Where(so => _selectedSoGuids.Contains(so.guid)).ToList();
                  }
                  else
                  {
                        _currentSelectionData.Clear();
                  }
            }

            private void SelectSo(ScriptableObjectData soData)
            {
                  _selectedSoGuids.Clear();
                  _selectedSoGuids.Add(soData.guid);
                  RebuildSelectionDataList();
                  _editorPanel.SetTargets(_currentSelectionData);
            }


            private void ToggleSoInSelection(ScriptableObjectData soData)
            {
                  if (!_selectedSoGuids.Remove(soData.guid))
                  {
                        _selectedSoGuids.Add(soData.guid);
                  }

                  RebuildSelectionDataList();
                  _editorPanel.SetTargets(_currentSelectionData);
            }

            private void SelectRange(ScriptableObjectData soData)
            {
                  if (_currentSelectionData.Count == 0)
                  {
                        SelectSo(soData);

                        return;
                  }

                  string lastSelectedGuid = _settingsManager.GetLastClickedGuid();

                  if (string.IsNullOrEmpty(lastSelectedGuid))
                  {
                        SelectSo(soData);

                        return;
                  }

                  int lastIndex = _filteredScriptableObjects.FindIndex(so => so.guid == lastSelectedGuid);
                  int currentIndex = _filteredScriptableObjects.FindIndex(so => so.guid == soData.guid);

                  if (lastIndex == -1 || currentIndex == -1)
                  {
                        SelectSo(soData);

                        return;
                  }

                  _selectedSoGuids.Clear();
                  int start = Mathf.Min(lastIndex, currentIndex);
                  int end = Mathf.Max(lastIndex, currentIndex);

                  for (int i = start; i <= end; i++)
                  {
                        _selectedSoGuids.Add(_filteredScriptableObjects[i].guid);
                  }

                  RebuildSelectionDataList();
                  _editorPanel.SetTargets(_currentSelectionData);
            }

            private void OnSelectionChanged(ScriptableObjectData soData, bool isCtrl, bool isShift)
            {
                  _settingsManager.SetLastClickedGuid(soData.guid);

                  if (isShift)
                  {
                        SelectRange(soData);
                  }
                  else if (isCtrl)
                  {
                        ToggleSoInSelection(soData);
                  }
                  else
                  {
                        SelectSo(soData);
                  }
            }

            private void ClearSelection()
            {
                  _selectedSoGuids.Clear();
                  RebuildSelectionDataList();
                  _editorPanel.SetTargets(_currentSelectionData);
            }

            private void HandleBulkDelete(IEnumerable<string> guids)
            {
                  List<ScriptableObjectData> itemsToDelete = _soRepository.AllScriptableObjects.Where(s => guids.Contains(s.guid)).ToList();

                  if (itemsToDelete.Count > 0 && EditorUtility.DisplayDialog("Delete Selected Objects",
                                  $"Are you sure you want to delete {itemsToDelete.Count} objects? This cannot be undone.", "Delete", "Cancel"))
                  {
                        foreach (ScriptableObjectData soData in itemsToDelete)
                        {
                              AssetDatabase.MoveAssetToTrash(soData.path);
                        }

                        foreach (string guid in guids)
                        {
                              _selectedSoGuids.Remove(guid);
                        }

                        RefreshAll();
                  }
            }

            private void HandleBulkToggleFavorites(IEnumerable<string> guids)
            {
                  _favoritesManager.ToggleFavoritesGroup(guids);
                  Repaint();
            }
      }
}