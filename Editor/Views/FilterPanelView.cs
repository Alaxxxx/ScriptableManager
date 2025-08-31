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

            public void DrawFiltersAndFavorites(List<ScriptableObjectData> favorites)
            {
                  EditorGUILayout.LabelField("🔍 Search & Filters", EditorStyles.boldLabel);

                  // Search Field
                  string newSearchText = EditorGUILayout.TextField(SearchText);

                  if (newSearchText != SearchText)
                  {
                        SearchText = newSearchText;
                        OnFiltersChanged?.Invoke();
                  }

                  // Type Filter Dropdown
                  EditorGUILayout.BeginHorizontal();

                  {
                        EditorGUILayout.LabelField(new GUIContent("Type Filter", "Filter by a specific ScriptableObject type."), GUILayout.Width(75));
                        int selectedIndex = Array.IndexOf(_allTypeOptions, SelectedTypeFilter);

                        if (selectedIndex == -1)
                              selectedIndex = 0;

                        int newIndex = EditorGUILayout.Popup(selectedIndex, _allTypeOptions);

                        if (newIndex != selectedIndex)
                        {
                              SelectedTypeFilter = _allTypeOptions[newIndex];
                              OnFiltersChanged?.Invoke();
                        }
                  }
                  EditorGUILayout.EndHorizontal();


                  // Favorites Only Toggle
                  bool newFavoritesOnly = GUILayout.Toggle(FavoritesOnly, new GUIContent(" Favorites Only", "⭐"));

                  if (newFavoritesOnly != FavoritesOnly)
                  {
                        FavoritesOnly = newFavoritesOnly;
                        OnToggleFavoritesFilter?.Invoke();
                  }

                  EditorGUILayout.Space(10);
                  DrawFavoritesSection(favorites);
            }

            private void DrawFavoritesSection(List<ScriptableObjectData> favorites)
            {
                  EditorGUILayout.LabelField("⭐ Favorites", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical("box");
                  _favoritesScrollPos = EditorGUILayout.BeginScrollView(_favoritesScrollPos, GUILayout.MinHeight(100), GUILayout.MaxHeight(300));

                  Event currentEvent = Event.current;

                  if (favorites.Count == 0)
                  {
                        EditorGUILayout.LabelField("No favorites added.", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        int index = 0;

                        foreach (ScriptableObjectData fav in favorites.Take(20)) // Limit displayed favorites for performance
                        {
                              DrawFavoriteItem(fav, index++, currentEvent);
                        }
                  }

                  EditorGUILayout.EndScrollView();
                  EditorGUILayout.EndVertical();
            }

            private void DrawFavoriteItem(ScriptableObjectData favorite, int index, Event currentEvent)
            {
                  Rect itemRect = GUILayoutUtility.GetRect(GUIContent.none, SoManagerStyles.FavoriteItemStyle, GUILayout.Height(22));

                  // --- Event Handling ---
                  if (itemRect.Contains(currentEvent.mousePosition))
                  {
                        if (currentEvent.type == EventType.MouseMove)
                        {
                              if (_hoveredFavoriteIndex != index)
                              {
                                    _hoveredFavoriteIndex = index;
                                    SoManagerStyles.NeedsRepaint = true;
                              }
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