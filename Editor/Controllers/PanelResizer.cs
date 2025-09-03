using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Controllers
{
      public sealed class PanelResizer
      {
            public float LeftPanelWidth { get; private set; }
            public float CenterPanelWidth { get; private set; }

            private bool _isResizingLeft;
            private bool _isResizingRight;

            public PanelResizer(float defaultLeftWidth, float defaultCenterWidth)
            {
                  LeftPanelWidth = defaultLeftWidth;
                  CenterPanelWidth = defaultCenterWidth;
            }

            public void LoadState()
            {
                  LeftPanelWidth = SettingsManager.GetFloat("SOManager_LeftPanelWidth", LeftPanelWidth);
                  CenterPanelWidth = SettingsManager.GetFloat("SOManager_CenterPanelWidth", CenterPanelWidth);
            }

            public void SaveState(SettingsManager settings)
            {
                  SettingsManager.SetFloat("SOManager_LeftPanelWidth", LeftPanelWidth);
                  SettingsManager.SetFloat("SOManager_CenterPanelWidth", CenterPanelWidth);
            }

            public void HandleResizeEvents(Event e, Rect windowPosition)
            {
                  if (e.type == EventType.MouseDown && e.button == 0)
                  {
                        CheckResizeStart(e, windowPosition);
                  }

                  if (_isResizingLeft || _isResizingRight)
                  {
                        switch (e.type)
                        {
                              case EventType.MouseDrag:
                              case EventType.MouseMove:
                                    PerformResize(e, windowPosition);
                                    e.Use();

                                    break;

                              case EventType.MouseUp:
                              case EventType.MouseLeaveWindow:
                              case EventType.Ignore:
                              case EventType.KeyDown when e.keyCode == KeyCode.Escape:
                                    EndResize(e);

                                    break;
                        }
                  }
            }

            public void DrawResizeHandles(Rect windowPosition)
            {
                  var leftResizeRect = new Rect(LeftPanelWidth - 2.5f, 0, 5, windowPosition.height);
                  EditorGUIUtility.AddCursorRect(leftResizeRect, MouseCursor.ResizeHorizontal);

                  var rightResizeRect = new Rect(LeftPanelWidth + CenterPanelWidth - 2.5f, 0, 5, windowPosition.height);
                  EditorGUIUtility.AddCursorRect(rightResizeRect, MouseCursor.ResizeHorizontal);

                  if (!_isResizingLeft && !_isResizingRight)
                  {
                        return;
                  }

                  Color oldColor = GUI.color;
                  GUI.color = new Color(0.5f, 0.8f, 1f, 0.8f);

                  if (_isResizingLeft)
                  {
                        GUI.DrawTexture(new Rect(LeftPanelWidth - 1f, 0, 2, windowPosition.height), EditorGUIUtility.whiteTexture);
                  }

                  if (_isResizingRight)
                  {
                        GUI.DrawTexture(new Rect(LeftPanelWidth + CenterPanelWidth - 1f, 0, 2, windowPosition.height), EditorGUIUtility.whiteTexture);
                  }

                  GUI.color = oldColor;
            }

            private void CheckResizeStart(Event e, Rect windowPosition)
            {
                  var leftResizeRect = new Rect(LeftPanelWidth - 2.5f, 0, 5, windowPosition.height);
                  var rightResizeRect = new Rect(LeftPanelWidth + CenterPanelWidth - 2.5f, 0, 5, windowPosition.height);

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

            private void PerformResize(Event e, Rect windowPosition)
            {
                  if (_isResizingLeft)
                  {
                        LeftPanelWidth = Mathf.Clamp(e.mousePosition.x, 150, windowPosition.width - CenterPanelWidth - 50);
                  }

                  if (_isResizingRight)
                  {
                        CenterPanelWidth = Mathf.Clamp(e.mousePosition.x - LeftPanelWidth, 200, windowPosition.width - LeftPanelWidth - 200);
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
                  }
            }
      }
}