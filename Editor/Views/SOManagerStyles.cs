using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public static class SoManagerStyles
      {
            public static bool NeedsRepaint { get; set; }

            private static GUIStyle listItemBackground;
            private static GUIStyle listItemBackgroundHover;
            private static GUIStyle listItemBackgroundSelected;
            private static GUIStyle dependencyItemStyle;
            private static GUIStyle toolbarButtonSelected;
            private static GUIStyle separator;
            private static GUIStyle dependencyBox;
            private static GUIStyle favoriteItemStyle;
            private static GUIStyle favoriteItemHoverStyle;


            public static GUIStyle ListItemBackground =>
                        listItemBackground ??= new GUIStyle("box")
                        {
                                    margin = new RectOffset(5, 5, 1, 1),
                                    padding = new RectOffset(5, 5, 5, 5)
                        };

            public static GUIStyle ListItemBackgroundHover
            {
                  get
                  {
                        listItemBackgroundHover ??= new GUIStyle(ListItemBackground)
                        {
                                    normal =
                                    {
                                                background = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, 0.2f))
                                    }
                        };

                        return listItemBackgroundHover;
                  }
            }

            public static GUIStyle ListItemBackgroundSelected
            {
                  get
                  {
                        listItemBackgroundSelected ??= new GUIStyle(ListItemBackground)
                        {
                                    normal =
                                    {
                                                background = MakeTex(1, 1, new Color(0.24f, 0.5f, 0.87f, 1f))
                                    }
                        };

                        return listItemBackgroundSelected;
                  }
            }

            public static GUIStyle ListItemBackgroundDragging
            {
                  get
                  {
                        listItemBackgroundDragging ??= new GUIStyle(ListItemBackground)
                        {
                                    normal = { background = MakeTex(1, 1, new Color(0.8f, 0.8f, 0.2f, 0.3f)) }
                        };

                        return listItemBackgroundDragging;
                  }
            }

            private static GUIStyle listItemBackgroundDragging;

            public static GUIStyle DependencyItemStyle =>
                        dependencyItemStyle ??= new GUIStyle(EditorStyles.label)
                        {
                                    padding = new RectOffset(4, 4, 4, 4),
                                    margin = new RectOffset(10, 0, 1, 1),
                        };

            public static GUIStyle DependencyBox =>
                        dependencyBox ??= new GUIStyle("box")
                        {
                                    margin = new RectOffset(4, 4, 4, 4),
                                    padding = new RectOffset(5, 5, 5, 5)
                        };

            public static GUIStyle ToolbarButtonSelected
            {
                  get
                  {
                        if (toolbarButtonSelected == null)
                        {
                              toolbarButtonSelected = new GUIStyle(EditorStyles.toolbarButton);

                              if (EditorStyles.toolbarButton.onNormal.background != null)
                              {
                                    toolbarButtonSelected.normal.background = EditorStyles.toolbarButton.onNormal.background;
                              }
                        }

                        return toolbarButtonSelected;
                  }
            }

            public static GUIStyle Separator
            {
                  get
                  {
                        separator ??= new GUIStyle("box")
                        {
                                    border = new RectOffset(0, 0, 0, 0),
                                    margin = new RectOffset(4, 4, 8, 8),
                                    padding = new RectOffset(0, 0, 0, 0),
                                    fixedHeight = 1,
                        };
                        separator.normal.background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 0.8f));

                        return separator;
                  }
            }

            public static GUIStyle FavoriteItemStyle =>
                        favoriteItemStyle ??= new GUIStyle(EditorStyles.label)
                        {
                                    padding = new RectOffset(4, 4, 2, 2),
                                    margin = new RectOffset(4, 4, 1, 1),
                                    alignment = TextAnchor.MiddleLeft,
                        };

            public static GUIStyle FavoriteItemHoverStyle
            {
                  get
                  {
                        favoriteItemHoverStyle ??= new GUIStyle(FavoriteItemStyle)
                        {
                                    normal = { background = MakeTex(1, 1, new Color(0.5f, 0.5f, 0.5f, 0.15f)) }
                        };

                        return favoriteItemHoverStyle;
                  }
            }


            private static Texture2D MakeTex(int width, int height, Color col)
            {
                  var pix = new Color[width * height];

                  for (int i = 0; i < pix.Length; ++i)
                  {
                        pix[i] = col;
                  }

                  var result = new Texture2D(width, height);
                  result.SetPixels(pix);
                  result.Apply();

                  return result;
            }
      }
}