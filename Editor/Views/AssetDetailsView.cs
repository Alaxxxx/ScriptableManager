using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class AssetDetailsView
      {
            private Texture2D _assetPreview;

            public void SetTarget(ScriptableObjectData soData)
            {
                  _assetPreview = soData?.scriptableObject ? AssetPreview.GetAssetPreview(soData.scriptableObject) : null;
            }

            public void Draw(ScriptableObjectData soData)
            {
                  DrawDetailedInfo(soData);
                  DrawAssetPreview(soData);
            }

            private static void DrawDetailedInfo(ScriptableObjectData soData)
            {
                  EditorGUILayout.BeginVertical("box");
                  EditorGUILayout.LabelField("📁 File Information", EditorStyles.boldLabel);

                  EditorGUILayout.BeginHorizontal();
                  EditorGUILayout.LabelField("Path:", GUILayout.Width(60));
                  EditorGUILayout.SelectableLabel(soData.path, EditorStyles.miniLabel, GUILayout.Height(16));
                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.BeginHorizontal();
                  EditorGUILayout.LabelField("Modified:", GUILayout.Width(60));
                  EditorGUILayout.LabelField($"{soData.FormattedDate} ({soData.RelativeDate})", EditorStyles.miniLabel);
                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.EndVertical();
                  EditorGUILayout.Space(5);
            }

            private void DrawAssetPreview(ScriptableObjectData soData)
            {
                  if (!soData?.scriptableObject)
                  {
                        return;
                  }

                  GUILayout.Space(10);
                  GUILayout.BeginHorizontal();
                  GUILayout.FlexibleSpace();
                  Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

                  if (_assetPreview)
                  {
                        GUI.DrawTexture(previewRect, _assetPreview, ScaleMode.ScaleToFit);
                  }
                  else if (AssetPreview.IsLoadingAssetPreview(soData.scriptableObject.GetInstanceID()))
                  {
                        EditorGUI.LabelField(previewRect, "Loading Preview...", EditorStyles.centeredGreyMiniLabel);
                  }
                  else
                  {
                        Texture2D fallbackIcon = AssetPreview.GetMiniThumbnail(soData.scriptableObject);

                        if (fallbackIcon)
                        {
                              GUI.DrawTexture(previewRect, fallbackIcon, ScaleMode.ScaleToFit);
                        }
                  }

                  GUILayout.FlexibleSpace();
                  GUILayout.EndHorizontal();
            }
      }
}