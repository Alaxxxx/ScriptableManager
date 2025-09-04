using System;
using System.IO;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class SettingsPanelView
      {
            public event Action OnSettingsChanged;

            private readonly SettingsManager _settingsManager;
            private string _pathToRemove;
            private readonly string _projectRootPath;

            public SettingsPanelView(SettingsManager settingsManager)
            {
                  _settingsManager = settingsManager;
                  _projectRootPath = Directory.GetParent(Application.dataPath)?.FullName.Replace('\\', '/');
            }

            public void Draw()
            {
                  EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                  EditorGUILayout.HelpBox("Configure the ScriptableObject Manager.", MessageType.Info);

                  DrawScanSettings();
                  DrawExcludedPaths();

                  GUILayout.FlexibleSpace();

                  if (GUILayout.Button("Reset to Defaults") && EditorUtility.DisplayDialog("Reset Settings",
                                  "Are you sure you want to reset all settings to their default values?", "Reset", "Cancel"))
                  {
                        _settingsManager.ResetToDefaults();
                        OnSettingsChanged?.Invoke();
                  }
            }

            private void DrawScanSettings()
            {
                  EditorGUILayout.BeginVertical("box");
                  EditorGUILayout.LabelField("Scan Settings", EditorStyles.boldLabel);

                  EditorGUI.BeginChangeCheck();

                  bool newScanPackages =
                              EditorGUILayout.Toggle(
                                          new GUIContent("Scan 'Packages' Folder", "Include assets from the 'Packages' folder in the scan. This may impact performance."),
                                          _settingsManager.ScanPackages);

                  bool newScanOnlyUserMade =
                              EditorGUILayout.Toggle(
                                          new GUIContent("Scan Creatable SOs Only", "Only scans for ScriptableObject types that have the [CreateAssetMenu] attribute."),
                                          _settingsManager.ScanOnlyUserMadeSOs);

                  if (EditorGUI.EndChangeCheck())
                  {
                        _settingsManager.ScanPackages = newScanPackages;
                        _settingsManager.ScanOnlyUserMadeSOs = newScanOnlyUserMade;
                        _settingsManager.SaveSettings();
                        OnSettingsChanged?.Invoke();
                  }

                  EditorGUILayout.EndVertical();
            }

            private void DrawExcludedPaths()
            {
                  EditorGUILayout.BeginVertical("box");
                  EditorGUILayout.LabelField("Excluded Paths", EditorStyles.boldLabel);

                  if (_pathToRemove != null)
                  {
                        _settingsManager.ExcludedPaths.Remove(_pathToRemove);
                        _settingsManager.SaveSettings();
                        OnSettingsChanged?.Invoke();
                        _pathToRemove = null;
                        GUI.changed = true;
                  }

                  var pathsCopy = new System.Collections.Generic.List<string>(_settingsManager.ExcludedPaths);

                  foreach (string path in pathsCopy)
                  {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(path);

                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                              _pathToRemove = path;
                        }

                        EditorGUILayout.EndHorizontal();
                  }

                  if (GUILayout.Button("Add Excluded Folder"))
                  {
                        EditorApplication.delayCall += AddExcludedFolder;
                  }

                  EditorGUILayout.EndVertical();
            }

            private void AddExcludedFolder()
            {
                  string absolutePath = EditorUtility.OpenFolderPanel("Select Folder to Exclude", _projectRootPath, "");

                  if (!string.IsNullOrEmpty(absolutePath))
                  {
                        absolutePath = absolutePath.Replace('\\', '/');

                        if (absolutePath.StartsWith(_projectRootPath, StringComparison.Ordinal))
                        {
                              string relativePath = absolutePath[(_projectRootPath.Length + 1)..] + "/";

                              if (!_settingsManager.ExcludedPaths.Contains(relativePath))
                              {
                                    _settingsManager.ExcludedPaths.Add(relativePath);
                                    _settingsManager.SaveSettings();
                                    OnSettingsChanged?.Invoke();
                              }
                        }
                        else
                        {
                              Debug.LogWarning("Excluded path must be inside the current Unity project directory.");
                        }
                  }
            }
      }
}