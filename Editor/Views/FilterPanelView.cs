using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            public event Action<ScriptableObjectData> OnRequestToggleFavorite;
            public event Action<ScriptableObjectData> OnRequestDeleteFavorite;
            public event Action<ScriptableObjectData> OnRequestPingFavorite;

            public string SearchText { get; private set; }
            public string SelectedTypeFilter { get; private set; }
            public bool FavoritesOnly { get; private set; }
            public List<PropertyFilter> PropertyFilters { get; } = new();

            private string[] _allTypeOptions;
            private readonly HashSet<string> _favoriteGuids;
            private Vector2 _favoritesScrollPos;
            private int _hoveredFavoriteIndex = -1;

            private Vector2 _recentlyModifiedScrollPos;
            private int _hoveredRecentIndex = -1;
            private const int MaxRecentItems = 10;
            private const int RecentDaysThreshold = 1;

            private FieldInfo[] _currentTypeFields;
            private string[] _currentTypeFieldNames;
            private Type _currentSelectedType;

            private int _newFilterFieldIndex;
            private int _newFilterOperatorIndex;
            private string _newFilterValue = "";

            public FilterPanelView(string initialSearch, string initialType, string[] allTypes, HashSet<string> favoriteGuids)
            {
                  SearchText = initialSearch;
                  SelectedTypeFilter = initialType;
                  _allTypeOptions = allTypes;
                  _favoriteGuids = favoriteGuids;
                  UpdatePropertySearchUI();
            }

            public void UpdateAllSoTypes(string[] allTypes)
            {
                  _allTypeOptions = allTypes;
            }

            public void DrawFiltersAndFavorites(List<ScriptableObjectData> favorites, IEnumerable<ScriptableObjectData> allSOs)
            {
                  EditorGUILayout.LabelField("🔍 Search & Filters", EditorStyles.boldLabel);

                  string newSearchText = EditorGUILayout.TextField(SearchText);

                  if (newSearchText != SearchText)
                  {
                        SearchText = newSearchText;
                        OnFiltersChanged?.Invoke();
                  }

                  EditorGUILayout.BeginHorizontal();
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
                        UpdatePropertySearchUI();
                        OnFiltersChanged?.Invoke();
                  }

                  EditorGUILayout.EndHorizontal();

                  bool newFavoritesOnly = GUILayout.Toggle(FavoritesOnly, new GUIContent(" Favorites Only", "⭐"));

                  if (newFavoritesOnly != FavoritesOnly)
                  {
                        FavoritesOnly = newFavoritesOnly;
                        OnToggleFavoritesFilter?.Invoke();
                  }

                  if (SelectedTypeFilter != "All Types")
                  {
                        DrawPropertySearch();
                  }

                  EditorGUILayout.Space(10);
                  DrawFavoritesSection(favorites);

                  EditorGUILayout.Space(10);
                  DrawRecentlyModifiedSection(allSOs);
            }

            private void DrawPropertySearch()
            {
                  EditorGUILayout.Space(5);
                  EditorGUILayout.LabelField("🔎 Property Search", EditorStyles.boldLabel);

                  EditorGUILayout.BeginVertical("box");

                  for (int i = PropertyFilters.Count - 1; i >= 0; i--)
                  {
                        PropertyFilter filter = PropertyFilters[i];
                        EditorGUILayout.BeginHorizontal();

                        string operatorText = filter.SelectedOperator.ToString()
                                                    .Replace("Equals", "=", StringComparison.OrdinalIgnoreCase)
                                                    .Replace("NotEquals", "≠", StringComparison.OrdinalIgnoreCase)
                                                    .Replace("GreaterThan", ">", StringComparison.OrdinalIgnoreCase)
                                                    .Replace("LessThan", "<", StringComparison.OrdinalIgnoreCase);
                        EditorGUILayout.LabelField(new GUIContent($"{filter.PropertyName} {operatorText} \"{filter.Value}\"", "Click to remove this filter."));

                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                              PropertyFilters.RemoveAt(i);
                              OnFiltersChanged?.Invoke();
                        }

                        EditorGUILayout.EndHorizontal();
                  }

                  if (PropertyFilters.Count > 0)
                  {
                        GUILayout.Box(GUIContent.none, SoManagerStyles.Separator);
                  }


                  if (_currentTypeFields is { Length: > 0 })
                  {
                        EditorGUILayout.BeginHorizontal();

                        _newFilterFieldIndex = EditorGUILayout.Popup(_newFilterFieldIndex, _currentTypeFieldNames);

                        FieldInfo selectedField = _currentTypeFields[_newFilterFieldIndex];
                        PropertyFilter.Operator[] operators = GetOperatorsForType(selectedField.FieldType);

                        _newFilterOperatorIndex = EditorGUILayout.Popup(_newFilterOperatorIndex, operators.Select(static op => op.ToString()).ToArray());

                        _newFilterValue = EditorGUILayout.TextField(_newFilterValue);

                        if (GUILayout.Button("+", GUILayout.Width(25)))
                        {
                              var newFilter = new PropertyFilter
                              {
                                          PropertyName = selectedField.Name,
                                          SelectedOperator = operators[_newFilterOperatorIndex],
                                          Value = _newFilterValue,
                                          PropertyType = GetSerializedPropertyType(selectedField.FieldType)
                              };
                              PropertyFilters.Add(newFilter);

                              _newFilterValue = "";
                              _newFilterFieldIndex = 0;
                              _newFilterOperatorIndex = 0;

                              OnFiltersChanged?.Invoke();
                        }

                        EditorGUILayout.EndHorizontal();
                  }
                  else
                  {
                        EditorGUILayout.LabelField("No serializable fields found for this type.", EditorStyles.miniLabel);
                  }

                  EditorGUILayout.EndVertical();
            }

            private void UpdatePropertySearchUI()
            {
                  PropertyFilters.Clear();

                  if (SelectedTypeFilter == "All Types")
                  {
                        _currentSelectedType = null;
                        _currentTypeFields = null;
                        _currentTypeFieldNames = null;

                        return;
                  }

                  _currentSelectedType = AppDomain.CurrentDomain.GetAssemblies()
                                                  .SelectMany(static asm => asm.GetTypes())
                                                  .FirstOrDefault(t => t.Name == SelectedTypeFilter && typeof(ScriptableObject).IsAssignableFrom(t));

                  if (_currentSelectedType != null)
                  {
                        _currentTypeFields = PropertySearcher.GetSerializableFields(_currentSelectedType);
                        _currentTypeFieldNames = _currentTypeFields.Select(static f => f.Name).ToArray();

                        _newFilterFieldIndex = 0;
                        _newFilterOperatorIndex = 0;
                  }
            }

            private static PropertyFilter.Operator[] GetOperatorsForType(Type type)
            {
                  if (type == typeof(string))
                  {
                        return new[] { PropertyFilter.Operator.Contains, PropertyFilter.Operator.Equals, PropertyFilter.Operator.NotEquals };
                  }

                  if (type == typeof(bool))
                  {
                        return new[] { PropertyFilter.Operator.Equals, PropertyFilter.Operator.NotEquals };
                  }

                  if (type.IsPrimitive || type.IsEnum)
                  {
                        return new[]
                        {
                                    PropertyFilter.Operator.Equals, PropertyFilter.Operator.NotEquals, PropertyFilter.Operator.GreaterThan,
                                    PropertyFilter.Operator.LessThan
                        };
                  }

                  return new[] { PropertyFilter.Operator.Equals, PropertyFilter.Operator.NotEquals };
            }

            private static SerializedPropertyType GetSerializedPropertyType(Type type)
            {
                  if (type == typeof(int) || type == typeof(long))
                  {
                        return SerializedPropertyType.Integer;
                  }

                  if (type == typeof(float) || type == typeof(double))
                  {
                        return SerializedPropertyType.Float;
                  }

                  if (type == typeof(bool))
                  {
                        return SerializedPropertyType.Boolean;
                  }

                  if (type == typeof(string))
                  {
                        return SerializedPropertyType.String;
                  }

                  if (type.IsEnum)
                  {
                        return SerializedPropertyType.Enum;
                  }

                  if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                  {
                        return SerializedPropertyType.ObjectReference;
                  }

                  return SerializedPropertyType.Generic;
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

            private void DrawRecentlyModifiedSection(IEnumerable<ScriptableObjectData> allSOs)
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
                        GUIStyle style = _hoveredRecentIndex == index ? SoManagerStyles.FavoriteItemHoverStyle : SoManagerStyles.FavoriteItemStyle;
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
                  var starRect = new Rect(itemRect.x + itemRect.width - 22, itemRect.y + 3, 20, 16);

                  if (itemRect.Contains(currentEvent.mousePosition))
                  {
                        if (currentEvent.type == EventType.MouseMove && _hoveredFavoriteIndex != index)
                        {
                              _hoveredFavoriteIndex = index;
                              SoManagerStyles.NeedsRepaint = true;
                        }

                        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                        {
                              if (starRect.Contains(currentEvent.mousePosition))
                              {
                                    OnRequestToggleFavorite?.Invoke(favorite);
                              }
                              else
                              {
                                    OnFavoriteSelected?.Invoke(favorite);
                              }

                              currentEvent.Use();
                        }
                        else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                        {
                              ShowFavoriteContextMenu(favorite);
                              currentEvent.Use();
                        }
                  }
                  else if (_hoveredFavoriteIndex == index && currentEvent.type == EventType.MouseMove)
                  {
                        _hoveredFavoriteIndex = -1;
                        SoManagerStyles.NeedsRepaint = true;
                  }

                  if (currentEvent.type == EventType.Repaint)
                  {
                        GUIStyle style = _hoveredFavoriteIndex == index ? SoManagerStyles.FavoriteItemHoverStyle : SoManagerStyles.FavoriteItemStyle;
                        style.Draw(itemRect, false, false, false, false);

                        Texture2D icon = AssetPreview.GetMiniThumbnail(favorite.scriptableObject);
                        var iconRect = new Rect(itemRect.x + 2, itemRect.y + 3, 16, 16);

                        if (icon)
                        {
                              GUI.DrawTexture(iconRect, icon);
                        }

                        var labelRect = new Rect(itemRect.x + 22, itemRect.y, itemRect.width - 44, itemRect.height);
                        GUI.Label(labelRect, new GUIContent(favorite.name, favorite.type), style);

                        bool isFavorite = _favoriteGuids.Contains(favorite.guid);
                        var starContent = new GUIContent(isFavorite ? "⭐" : "☆", "Toggle Favorite");
                        var starStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
                        GUI.Label(starRect, starContent, starStyle);
                  }
            }

            private void ShowFavoriteContextMenu(ScriptableObjectData favorite)
            {
                  var menu = new GenericMenu();
                  menu.AddItem(new GUIContent("Ping Asset"), false, () => OnRequestPingFavorite?.Invoke(favorite));
                  menu.AddSeparator("");
                  menu.AddItem(new GUIContent("Remove from Favorites"), false, () => OnRequestToggleFavorite?.Invoke(favorite));
                  menu.AddItem(new GUIContent("Delete Asset"), false, () => OnRequestDeleteFavorite?.Invoke(favorite));
                  menu.ShowAsContext();
            }

            private static List<ScriptableObjectData> GetRecentlyModifiedSOs(IEnumerable<ScriptableObjectData> allSOs)
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