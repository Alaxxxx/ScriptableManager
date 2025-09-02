using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class SoListView
      {
            public event Action<ScriptableObjectData, bool, bool> OnSelectionChanged;
            public event Action OnClearSelection;
            public event Action<SortOption> OnSortChanged;
            public event Action<IEnumerable<string>> OnRequestBulkDelete;
            public event Action<IEnumerable<string>> OnRequestBulkToggleFavorites;

            private Vector2 _centerPanelScrollPos;
            private List<ScriptableObjectData> _filteredList;
            private List<ScriptableObjectData> _currentSelection;
            private int _hoveredItemIndex = -1;
            private HashSet<string> _favoriteGuids;

            private bool _isDragging;
            private ScriptableObjectData _draggedItem;
            private Vector2 _dragStartPos;
            private int _lastDragFrame = -1;

            public void Draw(List<ScriptableObjectData> filteredList, List<ScriptableObjectData> currentSelection, SortOption sortOption, HashSet<string> favoriteGuids)
            {
                  _filteredList = filteredList;
                  _currentSelection = currentSelection;
                  _favoriteGuids = favoriteGuids;

                  CheckDragStatus();

                  DrawHeader(sortOption);

                  _centerPanelScrollPos = EditorGUILayout.BeginScrollView(_centerPanelScrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);

                  GUILayout.Space(5);

                  if (_filteredList == null || _filteredList.Count == 0)
                  {
                        EditorGUILayout.LabelField("No ScriptableObjects found.", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        Event currentEvent = Event.current;
                        int itemIndex = 0;

                        foreach (ScriptableObjectData soData in _filteredList)
                        {
                              bool isSelected = _currentSelection != null && _currentSelection.Contains(soData);
                              bool isFavorite = _favoriteGuids != null && _favoriteGuids.Contains(soData.guid);
                              DrawSoListItem(soData, isSelected, isFavorite, currentEvent, itemIndex++);
                        }
                  }

                  HandleEmptySpaceClick(Event.current);

                  EditorGUILayout.EndScrollView();
            }

            private void CheckDragStatus()
            {
                  if (_isDragging)
                  {
                        if (_lastDragFrame != -1 && Time.frameCount > _lastDragFrame + 2 &&
                            (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0))
                        {
                              EndDrag();
                              SoManagerStyles.NeedsRepaint = true;
                        }

                        _lastDragFrame = Time.frameCount;

                        Event currentEvent = Event.current;

                        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                        {
                              EndDrag();
                              SoManagerStyles.NeedsRepaint = true;
                        }
                  }
            }

            private void DrawHeader(SortOption currentSort)
            {
                  EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                  EditorGUILayout.LabelField($"ScriptableObjects ({_filteredList?.Count ?? 0})", EditorStyles.boldLabel);

                  GUILayout.FlexibleSpace();

                  GUIStyle selectedStyle = SoManagerStyles.ToolbarButtonSelected;
                  GUIStyle defaultStyle = EditorStyles.toolbarButton;

                  if (GUILayout.Button(new GUIContent("A-Z", "Sort by Name"), currentSort == SortOption.ByName ? selectedStyle : defaultStyle))
                  {
                        OnSortChanged?.Invoke(SortOption.ByName);
                  }

                  if (GUILayout.Button(new GUIContent("Type", "Sort by Type"), currentSort == SortOption.ByType ? selectedStyle : defaultStyle))
                  {
                        OnSortChanged?.Invoke(SortOption.ByType);
                  }

                  Rect dateButtonRect = GUILayoutUtility.GetRect(new GUIContent("Date"), defaultStyle);
                  bool isDateSortActive = currentSort == SortOption.ByDate || currentSort == SortOption.ByDateOldest;

                  if (GUI.Button(dateButtonRect, new GUIContent("Date", "Sort by Date"), isDateSortActive ? selectedStyle : defaultStyle))
                  {
                        ShowDateSortMenu(dateButtonRect);
                  }

                  EditorGUILayout.EndHorizontal();
            }

            private void ShowDateSortMenu(Rect buttonRect)
            {
                  var menu = new GenericMenu();
                  menu.AddItem(new GUIContent("Recent First"), false, () => OnSortChanged?.Invoke(SortOption.ByDate));
                  menu.AddItem(new GUIContent("Oldest First"), false, () => OnSortChanged?.Invoke(SortOption.ByDateOldest));
                  menu.DropDown(buttonRect);
            }

            private void DrawSoListItem(ScriptableObjectData soData, bool isSelected, bool isFavorite, Event currentEvent, int itemIndex)
            {
                  bool isDraggedItem = _isDragging && _draggedItem.Equals(soData);

                  GUIStyle style = isDraggedItem ? SoManagerStyles.ListItemBackgroundDragging :
                              isSelected ? SoManagerStyles.ListItemBackgroundSelected :
                              _hoveredItemIndex == itemIndex ? SoManagerStyles.ListItemBackgroundHover : SoManagerStyles.ListItemBackground;

                  Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.Height(40), GUILayout.ExpandWidth(true));

                  if (currentEvent.type == EventType.Repaint)
                  {
                        style.Draw(rect, false, false, isSelected, false);

                        var iconRect = new Rect(rect.x + 5, rect.y + 5, 30, 30);
                        Texture2D icon = AssetPreview.GetMiniThumbnail(soData.scriptableObject);

                        if (icon)
                        {
                              GUI.DrawTexture(iconRect, icon);
                        }

                        var nameRect = new Rect(rect.x + 40, rect.y + 5, rect.width - 75, 16);
                        var typeRect = new Rect(rect.x + 40, rect.y + 21, rect.width - 75, 16);
                        GUI.Label(nameRect, soData.name, EditorStyles.boldLabel);
                        GUI.Label(typeRect, soData.type, EditorStyles.miniLabel);

                        GUIContent starContent = new GUIContent(isFavorite ? "⭐" : "☆", "Toggle Favorite");
                        var starRect = new Rect(rect.x + rect.width - 30, rect.y + (rect.height / 2f) - 8, 20, 20);
                        GUIStyle starStyle = new GUIStyle(EditorStyles.label) { fontSize = 14 };
                        GUI.Label(starRect, starContent, starStyle);
                  }

                  HandleItemEvents(rect, soData, isSelected, currentEvent, itemIndex);
            }

            private void HandleItemEvents(Rect rect, ScriptableObjectData soData, bool isSelected, Event currentEvent, int itemIndex)
            {
                  if (rect.Contains(currentEvent.mousePosition))
                  {
                        if (currentEvent.type == EventType.MouseMove)
                        {
                              _hoveredItemIndex = itemIndex;
                              SoManagerStyles.NeedsRepaint = true;
                        }

                        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                        {
                              var starRect = new Rect(rect.x + rect.width - 30, rect.y, 25, rect.height);

                              if (starRect.Contains(currentEvent.mousePosition))
                              {
                                    OnRequestBulkToggleFavorites?.Invoke(new[] { soData.guid });
                                    currentEvent.Use();

                                    return;
                              }

                              _dragStartPos = currentEvent.mousePosition;
                              _draggedItem = soData;

                              OnSelectionChanged?.Invoke(soData, currentEvent.control || currentEvent.command, currentEvent.shift);

                              if (currentEvent.clickCount == 2)
                              {
                                    EditorGUIUtility.PingObject(soData.scriptableObject);
                              }

                              currentEvent.Use();
                        }

                        else if (currentEvent.type == EventType.MouseDrag && _draggedItem != null && !_isDragging)
                        {
                              float dragDistance = Vector2.Distance(currentEvent.mousePosition, _dragStartPos);

                              if (dragDistance > 10f)
                              {
                                    StartDrag();
                                    currentEvent.Use();
                              }
                        }

                        else if (currentEvent.type == EventType.MouseUp)
                        {
                              EndDrag();
                        }

                        else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
                        {
                              ShowContextMenu(soData, isSelected);
                              currentEvent.Use();
                        }
                  }
                  else if (_hoveredItemIndex == itemIndex && currentEvent.type == EventType.MouseMove)
                  {
                        _hoveredItemIndex = -1;
                        SoManagerStyles.NeedsRepaint = true;
                  }
            }

            private void StartDrag()
            {
                  _isDragging = true;

                  var objectsToDrag = new List<Object>();

                  if (_currentSelection is { Count: > 1 } && _currentSelection.Contains(_draggedItem))
                  {
                        objectsToDrag.AddRange(_currentSelection.Select(static s => s.scriptableObject).Where(static so => so));
                  }
                  else
                  {
                        if (_draggedItem.scriptableObject)
                        {
                              objectsToDrag.Add(_draggedItem.scriptableObject);
                        }
                  }

                  if (objectsToDrag.Count > 0)
                  {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = objectsToDrag.ToArray();

                        string dragTitle = objectsToDrag.Count == 1 ? _draggedItem.name : $"{objectsToDrag.Count} ScriptableObjects";
                        DragAndDrop.StartDrag(dragTitle);
                  }
            }

            private void EndDrag()
            {
                  _isDragging = false;
                  _draggedItem = null;
                  _lastDragFrame = -1;
            }

            private void HandleEmptySpaceClick(Event currentEvent)
            {
                  if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition))
                  {
                        OnClearSelection?.Invoke();
                        currentEvent.Use();
                  }
            }

            private void ShowContextMenu(ScriptableObjectData clickedItem, bool isClickedItemSelected)
            {
                  var menu = new GenericMenu();
                  var contextSelection = new List<ScriptableObjectData>();

                  if (isClickedItemSelected && _currentSelection is { Count: > 1 })
                  {
                        contextSelection.AddRange(_currentSelection);
                  }
                  else
                  {
                        contextSelection.Add(clickedItem);
                  }

                  List<string> guids = contextSelection.Select(static s => s.guid).ToList();
                  string selectionCount = contextSelection.Count > 1 ? $" ({contextSelection.Count})" : "";

                  menu.AddItem(new GUIContent($"Ping Asset{selectionCount}"), false, () =>
                  {
                        foreach (ScriptableObjectData item in contextSelection)
                        {
                              EditorGUIUtility.PingObject(item.scriptableObject);
                        }
                  });

                  menu.AddSeparator("");

                  menu.AddItem(new GUIContent($"Toggle Favorite{selectionCount}"), false, () => OnRequestBulkToggleFavorites?.Invoke(guids));
                  menu.AddItem(new GUIContent($"Delete{selectionCount}"), false, () => OnRequestBulkDelete?.Invoke(guids));

                  menu.ShowAsContext();
            }
      }
}