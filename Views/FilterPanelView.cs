using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Views
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

            public void Draw(Dictionary<string, int> topTypes, List<ScriptableObjectData> favorites)
            {
                  EditorGUILayout.LabelField("🔍 Search & Filters", EditorStyles.boldLabel);

                  string newSearchText = EditorGUILayout.TextField(SearchText);

                  if (newSearchText != SearchText)
                  {
                        SearchText = newSearchText;
                        OnFiltersChanged?.Invoke();
                  }

                  int selectedIndex = Array.IndexOf(_allTypeOptions, SelectedTypeFilter);

                  if (selectedIndex == -1)
                  {
                        selectedIndex = 0;
                  }

                  int newIndex = EditorGUILayout.Popup("Type Filter", selectedIndex, _allTypeOptions);

                  if (newIndex != selectedIndex)
                  {
                        SelectedTypeFilter = _allTypeOptions[newIndex];
                        OnFiltersChanged?.Invoke();
                  }

                  bool newFavoritesOnly = GUILayout.Toggle(FavoritesOnly, new GUIContent("Favorites Only", "⭐"));

                  if (newFavoritesOnly != FavoritesOnly)
                  {
                        FavoritesOnly = newFavoritesOnly;
                        OnToggleFavoritesFilter?.Invoke();
                  }

                  EditorGUILayout.Space(10);
                  DrawFavoritesSection(favorites);

                  EditorGUILayout.Space(10);
                  DrawStatistics(topTypes);
            }

            private void DrawFavoritesSection(List<ScriptableObjectData> favorites)
            {
                  EditorGUILayout.LabelField("⭐ Favorites", EditorStyles.boldLabel);

                  if (favorites.Count == 0)
                  {
                        EditorGUILayout.LabelField("No favorites added.", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        foreach (ScriptableObjectData fav in favorites.Take(10))
                        {
                              if (GUILayout.Button(new GUIContent(fav.name, fav.type), EditorStyles.miniButton))
                              {
                                    OnFavoriteSelected?.Invoke(fav);
                              }
                        }
                  }
            }

            private void DrawStatistics(Dictionary<string, int> topTypes)
            {
                  EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);
                  EditorGUILayout.LabelField($"Favorites: {_favoriteGuids.Count}", EditorStyles.miniLabel);

                  if (topTypes.Count > 0)
                  {
                        EditorGUILayout.LabelField("Top Types:", EditorStyles.miniLabel);
                        EditorGUI.indentLevel++;

                        foreach (KeyValuePair<string, int> topType in topTypes)
                        {
                              EditorGUILayout.LabelField($"{topType.Key}: {topType.Value}", EditorStyles.miniLabel);
                        }

                        EditorGUI.indentLevel--;
                  }
            }
      }
}