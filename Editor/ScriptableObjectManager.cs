using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Controllers;
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

            private SelectionHandler _selectionHandler;
            private PanelResizer _panelResizer;
            private AssetOperationsController _assetOperationsController;
            private DataFilterController _dataFilterController;

            private FilterPanelView _filterPanel;
            private SoListView _soListPanel;
            private EditorPanelView _editorPanel;
            private SettingsPanelView _settingsPanelView;

            private bool _showSettings;

            private void OnEnable()
            {
                  _settingsManager = new SettingsManager();
                  _settingsManager.LoadSettings();

                  _soRepository = new ScriptableObjectRepository(_settingsManager);
                  _soRepository.RefreshData();

                  _favoritesManager = new FavoritesManager();
                  _favoritesManager.LoadFavorites();

                  _selectionHandler = new SelectionHandler(_soRepository, _settingsManager);
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

                  SubscribeToEvents();
                  ApplyFiltersAndSort();

                  this.wantsMouseMove = true;
                  EditorApplication.update += OnEditorUpdate;
                  SoAssetProcessor.OnAssetsChanged += OnSOAssetsChanged;
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
                  UnsubscribeFromEvents();
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
                  _filterPanel.OnRequestToggleFavorite += HandleToggleFavoriteFromFilterPanel;
                  _filterPanel.OnRequestDeleteFavorite += HandleDeleteFromFilterPanel;
                  _filterPanel.OnRequestPingFavorite += HandlePingFromFilterPanel;

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

                  _settingsPanelView.OnSettingsChanged += RefreshAll;
            }

            private void UnsubscribeFromEvents()
            {
                  _filterPanel.OnFiltersChanged -= ApplyFiltersAndSort;
                  _filterPanel.OnToggleFavoritesFilter -= ApplyFiltersAndSort;
                  _filterPanel.OnRequestToggleFavorite -= HandleToggleFavoriteFromFilterPanel;
                  _filterPanel.OnRequestDeleteFavorite -= HandleDeleteFromFilterPanel;
                  _filterPanel.OnRequestPingFavorite -= HandlePingFromFilterPanel;
                  _settingsPanelView.OnSettingsChanged -= RefreshAll;
            }

            private void HandleToggleFavoriteFromFilterPanel(ScriptableObjectData soData)
            {
                  _assetOperationsController.ToggleFavorites(new[] { soData.guid });
                  Repaint();
            }

            private void HandleDeleteFromFilterPanel(ScriptableObjectData soData)
            {
                  if (_assetOperationsController.DeleteAssets(new[] { soData.guid }))
                  {
                        RefreshAll();
                  }
            }

            private static void HandlePingFromFilterPanel(ScriptableObjectData soData)
            {
                  if (soData?.scriptableObject != null)
                  {
                        EditorGUIUtility.PingObject(soData.scriptableObject);
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

                  EditorGUILayout.BeginVertical(GUILayout.Width(_panelResizer.LeftPanelWidth));

                  List<ScriptableObjectData> favoriteSOs = _soRepository.AllScriptableObjects.Where(so => _favoritesManager.favoriteSoGuids.Contains(so.guid))
                                                                        .OrderBy(static so => so.name)
                                                                        .ToList();
                  _filterPanel.DrawFiltersAndFavorites(favoriteSOs, _soRepository.AllScriptableObjects);
                  GUILayout.FlexibleSpace();
                  _filterPanel.DrawStatistics(_soRepository.AllScriptableObjects.Count);
                  EditorGUILayout.EndVertical();

                  // Center Panel
                  EditorGUILayout.BeginVertical(GUILayout.Width(_panelResizer.CenterPanelWidth));
                  List<ScriptableObjectData> filteredData = _dataFilterController.GetFilteredAndSortedData(_filterPanel);
                  _soListPanel.Draw(filteredData, _selectionHandler.CurrentSelectionData.ToList(), _settingsManager.CurrentSortOption, _favoritesManager.favoriteSoGuids);
                  EditorGUILayout.EndVertical();

                  // Right Panel
                  _editorPanel.Draw();

                  EditorGUILayout.EndHorizontal();
            }

            private void OnSOAssetsChanged() => RefreshAll();

            private void RefreshAll()
            {
                  AssetDatabase.Refresh();
                  _soRepository.RefreshData();
                  _favoritesManager.CleanFavorites(_soRepository.AllScriptableObjects.Select(static so => so.guid));
                  _filterPanel.UpdateAllSoTypes(_soRepository.GetAllSoTypes());
                  ApplyFiltersAndSort();
            }

            private void ApplyFiltersAndSort()
            {
                  List<ScriptableObjectData> filteredData = _dataFilterController.GetFilteredAndSortedData(_filterPanel);
                  _selectionHandler.SetFilteredList(filteredData);
                  _selectionHandler.RebuildSelectionDataList();
                  _editorPanel.SetTargets(_selectionHandler.CurrentSelectionData.ToList());
                  Repaint();
            }
      }
}