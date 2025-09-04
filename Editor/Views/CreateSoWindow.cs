using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class CreateSoWindow : EditorWindow
      {
            public static event Action<string> OnAssetCreated;

            private List<Type> _soTypes;
            private string[] _typeNames;
            private int _selectedTypeIndex;
            private string _assetName = "New ScriptableObject";
            private string _savePath = "Assets/";
            private string _projectRootPath;

            public static void ShowWindow()
            {
                  var window = GetWindow<CreateSoWindow>(true, "Create ScriptableObject", true);
                  window.minSize = new Vector2(400, 180);
                  window.maxSize = new Vector2(400, 180);
                  window.ShowUtility();
            }

            private void OnEnable()
            {
                  _projectRootPath = Directory.GetParent(Application.dataPath)?.FullName.Replace('\\', '/');

                  _soTypes = TypeCache.GetTypesWithAttribute<CreateAssetMenuAttribute>()
                                      .Where(static t => !t.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(t))
                                      .OrderBy(static t => t.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false)
                                                            .Cast<CreateAssetMenuAttribute>()
                                                            .FirstOrDefault()
                                                            ?.menuName ?? t.Name)
                                      .ToList();

                  _typeNames = _soTypes.Select(static t =>
                                       {
                                             CreateAssetMenuAttribute attr = t.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false).Cast<CreateAssetMenuAttribute>().FirstOrDefault();

                                             return !string.IsNullOrEmpty(attr?.menuName) ? attr.menuName : t.Name;
                                       })
                                       .ToArray();
            }

            private void OnGUI()
            {
                  EditorGUILayout.LabelField("Create New ScriptableObject", EditorStyles.boldLabel);
                  EditorGUILayout.Space(10);

                  if (_soTypes == null || _soTypes.Count == 0)
                  {
                        EditorGUILayout.HelpBox("No ScriptableObject types with the [CreateAssetMenu] attribute were found in this project.", MessageType.Warning);

                        return;
                  }

                  EditorGUILayout.BeginVertical("box");

                  _selectedTypeIndex = EditorGUILayout.Popup(new GUIContent("Type", "Select the type of ScriptableObject to create."), _selectedTypeIndex, _typeNames);
                  _assetName = EditorGUILayout.TextField(new GUIContent("Name", "Enter a name for the new asset file."), _assetName);

                  EditorGUILayout.BeginHorizontal();
                  EditorGUILayout.TextField(new GUIContent("Path", "The folder where the new asset will be saved."), _savePath);

                  if (GUILayout.Button("Browse", GUILayout.Width(80)))
                  {
                        SelectSavePath();
                  }

                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.EndVertical();

                  GUILayout.FlexibleSpace();

                  GUI.enabled = !string.IsNullOrWhiteSpace(_assetName) && !string.IsNullOrWhiteSpace(_savePath);

                  if (GUILayout.Button("Create Asset", GUILayout.Height(30)))
                  {
                        CreateAsset();
                  }

                  GUI.enabled = true;
            }

            private void SelectSavePath()
            {
                  string absolutePath = EditorUtility.OpenFolderPanel("Select Save Location", _savePath, "");

                  if (!string.IsNullOrEmpty(absolutePath))
                  {
                        absolutePath = absolutePath.Replace('\\', '/');

                        if (absolutePath.StartsWith(_projectRootPath, StringComparison.Ordinal))
                        {
                              _savePath = absolutePath[(_projectRootPath.Length + 1)..] + "/";
                        }
                        else
                        {
                              Debug.LogWarning("Save path must be inside the current Unity project directory.");
                        }
                  }
            }

            private void CreateAsset()
            {
                  if (_selectedTypeIndex < 0 || _selectedTypeIndex >= _soTypes.Count)
                  {
                        return;
                  }

                  Type selectedType = _soTypes[_selectedTypeIndex];
                  ScriptableObject instance = CreateInstance(selectedType);

                  if (!Directory.Exists(_savePath))
                  {
                        Directory.CreateDirectory(_savePath);
                  }

                  string fullPath = Path.Combine(_savePath, _assetName + ".asset");
                  string uniquePath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

                  AssetDatabase.CreateAsset(instance, uniquePath);
                  AssetDatabase.SaveAssets();

                  string newAssetGuid = AssetDatabase.AssetPathToGUID(uniquePath);

                  if (!string.IsNullOrEmpty(newAssetGuid))
                  {
                        OnAssetCreated?.Invoke(newAssetGuid);
                  }

                  EditorUtility.FocusProjectWindow();
                  Selection.activeObject = instance;

                  Close();
            }
      }
}