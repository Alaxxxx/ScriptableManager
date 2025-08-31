using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class FilterPanelView
      {
            public event Action OnFiltersChanged;
            public event Action OnToggleFavoritesFilter;
            public event Action<ScriptableObjectData> OnFavoriteSelected;

            public string SearchText { get; private set; }
            public string SelectedTypeFilter { get; private set; }
            public bool FavoritesOnly { get; private set; }

            private string[] _allTypeOptions;
            private readonly HashSet<string> _favoriteGuids;
            private Vector2 _favoritesScrollPos;
            private int _hoveredFavoriteIndex = -1;

            private Vector2 _recentlyModifiedScrollPos;
            private int _hoveredRecentIndex = -1;
            private const int MaxRecentItems = 10;
            private const int RecentDaysThreshold = 1;

            public FilterPanelView(string initialSearch, string initialType, string[] allTypes, HashSet<string> favoriteGuids)
            {
                  SearchText = initialSearch;
                  SelectedTypeFilter = initialType;
                  _allTypeOptions = allTypes;
                  _favoriteGuids = favoriteGuids;
            }

            public void UpdateAllSoTypes(string[] allTypes)
            {
                  _allTypeOptions = allTypes;
            }

            public void DrawFiltersAndFavorites(List<ScriptableObjectData> favorites, List<ScriptableObjectData> allSOs)
            {
                  EditorGUILayout.LabelField("🔍 Search & Filters", EditorStyles.boldLabel);

                  string newSearchText = EditorGUILayout.TextField(SearchText);

                  if (newSearchText != SearchText)
                  {
                        SearchText = newSearchText;
                        OnFiltersChanged?.Invoke();
                  }

                  EditorGUILayout.BeginHorizontal();

                  {
                        EditorGUILayout.LabelField(new GUIContent("Type Filter", "Filter by a specific ScriptableObject type."), GUILayout.Width(75));
                        int selectedIndex = Array.IndexOf(_allTypeOptions, SelectedTypeFilter);

                        if (selectedIndex == -1)
                        {
                              selectedIndex = 0;
                        }

                        int newIndex = EditorGUILayout.Popup(selectedIndex, _allTypeOptions);

                        if (newIndex != selectedIndex)
                        {
                              SelectedTypeFilter = _allTypeOptions[newIndex];
                              OnFiltersChanged?.Invoke();
                        }
                  }
                  EditorGUILayout.EndHorizontal();

                  bool newFavoritesOnly = GUILayout.Toggle(FavoritesOnly, new GUIContent(" Favorites Only", "⭐"));

                  if (newFavoritesOnly != FavoritesOnly)
                  {
                        FavoritesOnly = newFavoritesOnly;
                        OnToggleFavoritesFilter?.Invoke();
                  }

                  EditorGUILayout.Space(10);
                  DrawFavoritesSection(favorites);

                  EditorGUILayout.Space(10);
                  DrawRecentlyModifiedSection(allSOs);
            }

            private void DrawFavoritesSection(List<ScriptableObjectData> favorites)
            {
                  EditorGUILayout.LabelField("⭐ Favorites", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical("box");
                  _favoritesScrollPos = EditorGUILayout.BeginScrollView(_favoritesScrollPos, GUILayout.MinHeight(100), GUILayout.MaxHeight(250));

                  Event currentEvent = Event.current;

                  if (favorites.Count == 0)
                  {
                        EditorGUILayout.LabelField("No favorites added.", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        int index = 0;

                        foreach (ScriptableObjectData fav in favorites.Take(20))
                        {
                              DrawFavoriteItem(fav, index++, currentEvent);
                        }
                  }

                  EditorGUILayout.EndScrollView();
                  EditorGUILayout.EndVertical();
            }

            private void DrawRecentlyModifiedSection(List<ScriptableObjectData> allSOs)
            {
                  List<ScriptableObjectData> recentSOs = GetRecentlyModifiedSOs(allSOs);

                  EditorGUILayout.LabelField("🕒 Recently Modified", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical("box");
                  _recentlyModifiedScrollPos = EditorGUILayout.BeginScrollView(_recentlyModifiedScrollPos, GUILayout.MinHeight(100), GUILayout.MaxHeight(250));

                  Event currentEvent = Event.current;

                  if (recentSOs.Count == 0)
                  {
                        EditorGUILayout.LabelField("No recent modifications.", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        int index = 0;

                        foreach (ScriptableObjectData recentSo in recentSOs)
                        {
                              DrawRecentlyModifiedItem(recentSo, index++, currentEvent);
                        }
                  }

                  EditorGUILayout.EndScrollView();
                  EditorGUILayout.EndVertical();
            }

            private void DrawRecentlyModifiedItem(ScriptableObjectData recentSo, int index, Event currentEvent)
            {
                  Rect itemRect = GUILayoutUtility.GetRect(GUIContent.none, SoManagerStyles.FavoriteItemStyle, GUILayout.Height(22));

                  if (itemRect.Contains(currentEvent.mousePosition))
                  {
                        if (currentEvent.type == EventType.MouseMove && _hoveredRecentIndex != index)
                        {
                              _hoveredRecentIndex = index;
                              SoManagerStyles.NeedsRepaint = true;
                        }

                        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                        {
                              OnFavoriteSelected?.Invoke(recentSo);
                              currentEvent.Use();
                        }
                  }
                  else if (_hoveredRecentIndex == index && currentEvent.type == EventType.MouseMove)
                  {
                        _hoveredRecentIndex = -1;
                        SoManagerStyles.NeedsRepaint = true;
                  }

                  if (currentEvent.type == EventType.Repaint)
                  {
                        GUIStyle style = (_hoveredRecentIndex == index) ? SoManagerStyles.FavoriteItemHoverStyle : SoManagerStyles.FavoriteItemStyle;
                        style.Draw(itemRect, false, false, false, false);

                        Texture2D icon = AssetPreview.GetMiniThumbnail(recentSo.scriptableObject);
                        var iconRect = new Rect(itemRect.x + 2, itemRect.y + 3, 16, 16);

                        if (icon)
                        {
                              GUI.DrawTexture(iconRect, icon);
                        }

                        var labelRect = new Rect(itemRect.x + 22, itemRect.y, itemRect.width - 50, itemRect.height);
                        var timeRect = new Rect(itemRect.x + itemRect.width - 45, itemRect.y + 2, 40, 16);

                        GUI.Label(labelRect, new GUIContent(recentSo.name, $"{recentSo.type} - {recentSo.FormattedDate}"), style);

                        GUI.Label(timeRect, recentSo.RelativeDate, EditorStyles.miniLabel);
                  }
            }

            private void DrawFavoriteItem(ScriptableObjectData favorite, int index, Event currentEvent)
            {
                  Rect itemRect = GUILayoutUtility.GetRect(GUIContent.none, SoManagerStyles.FavoriteItemStyle, GUILayout.Height(22));

                  if (itemRect.Contains(currentEvent.mousePosition))
                  {
                        if (currentEvent.type == EventType.MouseMove && _hoveredFavoriteIndex != index)
                        {
                              _hoveredFavoriteIndex = index;
                              SoManagerStyles.NeedsRepaint = true;
                        }

                        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                        {
                              OnFavoriteSelected?.Invoke(favorite);
                              currentEvent.Use();
                        }
                  }
                  else if (_hoveredFavoriteIndex == index && currentEvent.type == EventType.MouseMove)
                  {
                        _hoveredFavoriteIndex = -1;
                        SoManagerStyles.NeedsRepaint = true;
                  }

                  // --- Drawing ---
                  if (currentEvent.type == EventType.Repaint)
                  {
                        GUIStyle style = (_hoveredFavoriteIndex == index) ? SoManagerStyles.FavoriteItemHoverStyle : SoManagerStyles.FavoriteItemStyle;
                        style.Draw(itemRect, false, false, false, false);

                        Texture2D icon = AssetPreview.GetMiniThumbnail(favorite.scriptableObject);
                        var iconRect = new Rect(itemRect.x + 2, itemRect.y + 3, 16, 16);

                        if (icon)
                        {
                              GUI.DrawTexture(iconRect, icon);
                        }

                        var labelRect = new Rect(itemRect.x + 22, itemRect.y, itemRect.width - 22, itemRect.height);
                        GUI.Label(labelRect, new GUIContent(favorite.name, favorite.type), style);
                  }
            }

            private List<ScriptableObjectData> GetRecentlyModifiedSOs(IEnumerable<ScriptableObjectData> allSOs)
            {
                  DateTime cutoffDate = DateTime.Now.AddDays(-RecentDaysThreshold);

                  return allSOs.Where(so => so.LastModified >= cutoffDate).OrderByDescending(static so => so.LastModified).Take(MaxRecentItems).ToList();
            }

            public void DrawStatistics(int totalCount)
            {
                  EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);
                  EditorGUILayout.BeginVertical("box");
                  EditorGUILayout.LabelField($"Total Objects: {totalCount}", EditorStyles.miniLabel);
                  EditorGUILayout.LabelField($"Favorites: {_favoriteGuids.Count}", EditorStyles.miniLabel);
                  EditorGUILayout.EndVertical();
            }
      }
}