using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class CreateScriptableObjectWindow : EditorWindow
      {
            private List<Type> _allTypes;
            private List<Type> _filteredTypes;
            private string _searchText = "";
            private Vector2 _scrollPosition;
            private Type _selectedType;

            public static void ShowWindow(List<Type> allTypes)
            {
                  var window = GetWindow<CreateScriptableObjectWindow>(true, "Create New ScriptableObject", true);
                  window.minSize = new Vector2(300, 400);
                  window.maxSize = new Vector2(300, 800);
                  window._allTypes = allTypes;
                  window.FilterTypes();
                  window.ShowUtility();
            }

            private void OnGUI()
            {
                  EditorGUILayout.LabelField("Select a type to create:", EditorStyles.boldLabel);

                  string newSearchText = EditorGUILayout.TextField(GUIContent.none, _searchText, "SearchTextField");

                  if (newSearchText != _searchText)
                  {
                        _searchText = newSearchText;
                        FilterTypes();
                  }

                  _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box");

                  foreach (Type type in _filteredTypes)
                  {
                        if (GUILayout.Button(type.Name, EditorStyles.label))
                        {
                              _selectedType = type;
                              CreateAsset(_selectedType);
                              Close();
                        }
                  }

                  EditorGUILayout.EndScrollView();
            }

            private void FilterTypes()
            {
                  _filteredTypes = string.IsNullOrEmpty(_searchText)
                              ? _allTypes
                              : _allTypes.Where(t => t.Name.ToLower().Contains(_searchText.ToLower(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            private static void CreateAsset(Type type)
            {
                  ScriptableObject asset = CreateInstance(type);
                  string path = AssetDatabase.GetAssetPath(Selection.activeObject);

                  if (path == "")
                  {
                        path = "Assets";
                  }
                  else if (Path.GetExtension(path) != "")
                  {
                        path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "", StringComparison.OrdinalIgnoreCase);
                  }

                  string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + type.Name + ".asset");

                  AssetDatabase.CreateAsset(asset, assetPathAndName);
                  AssetDatabase.SaveAssets();
                  AssetDatabase.Refresh();
                  EditorUtility.FocusProjectWindow();
                  Selection.activeObject = asset;
            }
      }
}