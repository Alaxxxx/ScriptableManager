using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Controllers;
using OpalStudio.ScriptableManager.Editor.Models;
using OpalStudio.ScriptableManager.Editor.Views;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor
{
      public sealed class ScriptableObjectManager : EditorWindow, IDisposable
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

            private SelectionHandler _selectionHandler;
            private PanelResizer _panelResizer;
            private AssetOperationsController _assetOperationsController;
            private DataFilterController _dataFilterController;

            private FilterPanelView _filterPanel;
            private SoListView _soListPanel;
            private EditorPanelView _editorPanel;
            private SettingsPanelView _settingsPanelView;

            private bool _showSettings;
            private bool _isScanning;
            private Action _settingsChangedHandler;

            private void OnEnable()
            {
                  _settingsManager = new SettingsManager();
                  _settingsManager.LoadSettings();

                  _soRepository = new ScriptableObjectRepository(_settingsManager);
                  _favoritesManager = new FavoritesManager();
                  _favoritesManager.LoadFavorites();

                  _selectionHandler = new SelectionHandler(_soRepository);
                  _selectionHandler.LoadSelection();

                  _panelResizer = new PanelResizer(250f, 400f);
                  _panelResizer.LoadState();

                  _assetOperationsController = new AssetOperationsController(_soRepository, _favoritesManager, _selectionHandler);
                  _dataFilterController = new DataFilterController(_soRepository, _settingsManager, _favoritesManager);

                  string searchText = SettingsManager.GetString("SOManager_SearchText", "");
                  string typeFilter = SettingsManager.GetString("SOManager_TypeFilter", "All Types");
                  _filterPanel = new FilterPanelView(searchText, typeFilter, _soRepository.GetAllSoTypes(), _favoritesManager.favoriteSoGuids);

                  _soListPanel = new SoListView();
                  _editorPanel = new EditorPanelView();
                  _settingsPanelView = new SettingsPanelView(_settingsManager);

                  _settingsChangedHandler = () => RefreshAll();

                  SubscribeToEvents();

                  RefreshAll();

                  this.wantsMouseMove = true;
                  EditorApplication.update += OnEditorUpdate;
                  SoAssetProcessor.OnAssetsChanged += OnSOAssetsChanged;
                  CreateSoWindow.OnAssetCreated += HandleAssetCreated;
            }

            private void OnDisable()
            {
                  _selectionHandler.SaveSelection();
                  _panelResizer.SaveState(_settingsManager);
                  SettingsManager.SetString("SOManager_SearchText", _filterPanel.SearchText);
                  SettingsManager.SetString("SOManager_TypeFilter", _filterPanel.SelectedTypeFilter);
                  _settingsManager.SaveSettings();

                  EditorApplication.update -= OnEditorUpdate;
                  SoAssetProcessor.OnAssetsChanged -= OnSOAssetsChanged;
                  CreateSoWindow.OnAssetCreated -= HandleAssetCreated;
                  UnsubscribeFromEvents();
            }

            public void Dispose()
            {
                  _soRepository = null;
                  _favoritesManager = null;
                  _settingsManager = null;
                  _selectionHandler = null;
                  _panelResizer = null;
                  _assetOperationsController = null;
                  _dataFilterController = null;
                  _filterPanel = null;
                  _soListPanel = null;
                  _editorPanel = null;
                  _settingsPanelView = null;
                  _settingsChangedHandler = null;
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

                  _filterPanel.OnFavoriteSelected += soData =>
                  {
                        _selectionHandler.SelectFromGuid(soData);
                        _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                  };

                  _soListPanel.OnSelectionChanged += (soData, isCtrl, isShift) =>
                  {
                        _selectionHandler.HandleSelectionChange(soData, isCtrl, isShift);
                        _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                  };

                  _soListPanel.OnClearSelection += () =>
                  {
                        _selectionHandler.Clear();
                        _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                  };

                  _soListPanel.OnSortChanged += sortOption =>
                  {
                        _settingsManager.SetSortOption(sortOption);
                        ApplyFiltersAndSort();
                  };

                  _soListPanel.OnRequestBulkDelete += guids =>
                  {
                        if (_assetOperationsController.DeleteAssets(guids))
                        {
                              RefreshAll();
                        }
                  };

                  _soListPanel.OnRequestBulkToggleFavorites += guids =>
                  {
                        _assetOperationsController.ToggleFavorites(guids);
                        Repaint();
                  };

                  _soListPanel.OnRequestDuplicate += guid =>
                  {
                        string newGuid = _assetOperationsController.DuplicateAsset(guid);

                        if (!string.IsNullOrEmpty(newGuid))
                        {
                              HandleAssetCreated(newGuid);
                        }
                  };

                  _editorPanel.OnRequestBulkDelete += () =>
                  {
                        if (_assetOperationsController.DeleteAssets(_selectionHandler.CurrentSelectionData.Select(static s => s.guid)))
                        {
                              RefreshAll();
                        }
                  };

                  _editorPanel.OnRequestBulkAddToFavorites += () =>
                  {
                        _assetOperationsController.ToggleFavorites(_selectionHandler.CurrentSelectionData.Select(static s => s.guid));
                        Repaint();
                  };

                  _settingsPanelView.OnSettingsChanged += _settingsChangedHandler;
            }

            private void UnsubscribeFromEvents()
            {
                  _filterPanel.OnFiltersChanged -= ApplyFiltersAndSort;
                  _filterPanel.OnToggleFavoritesFilter -= ApplyFiltersAndSort;

                  if (_settingsPanelView != null)
                  {
                        _settingsPanelView.OnSettingsChanged -= _settingsChangedHandler;
                  }
            }

            private void OnGUI()
            {
                  _panelResizer.HandleResizeEvents(Event.current, position);

                  DrawHeader();

                  if (_showSettings)
                  {
                        _settingsPanelView.Draw();
                  }
                  else
                  {
                        DrawMainLayout();
                  }

                  _panelResizer.DrawResizeHandles(position);

                  if (Event.current.type == EventType.Used)
                  {
                        Repaint();
                  }
            }

            private void DrawHeader()
            {
                  EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                  EditorGUI.BeginDisabledGroup(_isScanning);

                  if (GUILayout.Button(new GUIContent("Refresh", EditorGUIUtility.IconContent("Refresh").image), EditorStyles.toolbarButton, GUILayout.Width(80)))
                  {
                        RefreshAll();
                  }

                  EditorGUI.EndDisabledGroup();

                  if (GUILayout.Button(new GUIContent("New +", "Create a new ScriptableObject"), EditorStyles.toolbarButton, GUILayout.Width(60)))
                  {
                        CreateSoWindow.ShowWindow();
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
                  if (_isScanning)
                  {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Scanning project for ScriptableObjects...", EditorStyles.centeredGreyMiniLabel);
                        GUILayout.FlexibleSpace();

                        return;
                  }

                  EditorGUILayout.BeginHorizontal();

                  EditorGUILayout.BeginVertical(GUILayout.Width(_panelResizer.LeftPanelWidth));

                  List<ScriptableObjectData> favoriteSOs = _soRepository.AllScriptableObjects.Where(so => _favoritesManager.favoriteSoGuids.Contains(so.guid))
                                                                        .OrderBy(static so => so.name)
                                                                        .ToList();
                  _filterPanel.DrawFiltersAndFavorites(favoriteSOs, _soRepository.AllScriptableObjects);
                  GUILayout.FlexibleSpace();
                  _filterPanel.DrawStatistics(_soRepository.AllScriptableObjects.Count);
                  EditorGUILayout.EndVertical();

                  EditorGUILayout.BeginVertical(GUILayout.Width(_panelResizer.CenterPanelWidth));
                  List<ScriptableObjectData> filteredData = _dataFilterController.GetFilteredAndSortedData(_filterPanel);
                  _soListPanel.Draw(filteredData, _selectionHandler.CurrentSelectionData.ToList(), _settingsManager.CurrentSortOption, _favoritesManager.favoriteSoGuids);
                  EditorGUILayout.EndVertical();

                  _editorPanel.Draw();

                  EditorGUILayout.EndHorizontal();
            }

            private void OnSOAssetsChanged() => RefreshAll();

            private void RefreshAll(string guidToSelect = null)
            {
                  if (_isScanning)
                  {
                        return;
                  }

                  _isScanning = true;
                  Repaint();

                  AssetDatabase.Refresh();

                  _soRepository.StartScan(() =>
                  {
                        _isScanning = false;
                        _filterPanel.UpdateAllSoTypes(_soRepository.GetAllSoTypes());
                        ApplyFiltersAndSort();

                        if (!string.IsNullOrEmpty(guidToSelect))
                        {
                              ScriptableObjectData newData = _soRepository.AllScriptableObjects.Find(so => so.guid == guidToSelect);

                              if (newData != null)
                              {
                                    _selectionHandler.SelectFromGuid(newData);
                                    _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                              }
                        }

                        Repaint();
                  });
            }

            private void HandleAssetCreated(string guid)
            {
                  RefreshAll(guid);
            }

            private void ApplyFiltersAndSort()
            {
                  if (_isScanning)
                  {
                        return;
                  }

                  List<ScriptableObjectData> filteredData = _dataFilterController.GetFilteredAndSortedData(_filterPanel);
                  _selectionHandler.SetFilteredList(filteredData);
                  _selectionHandler.RebuildSelectionDataList();
                  _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                  Repaint();
            }
      }
}