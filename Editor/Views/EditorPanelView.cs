using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Views
{
      public sealed class EditorPanelView : IDisposable
      {
            public event Action OnRequestBulkDelete;
            public event Action OnRequestBulkAddToFavorites;

            private Vector2 _scrollPos;
            private List<ScriptableObjectData> _currentSelection;
            private SerializedObject _serializedObject;
            private bool _isMultiEditingDifferentTypes;

            private readonly AssetDetailsView _assetDetailsView;
            private readonly DependencyPanelView _dependencyPanelView;

            public EditorPanelView()
            {
                  _assetDetailsView = new AssetDetailsView();
                  _dependencyPanelView = new DependencyPanelView();
            }

            public void Dispose()
            {
                  _serializedObject?.Dispose();
                  _serializedObject = null;
            }

            public void SetTargets(List<ScriptableObjectData> selection)
            {
                  _currentSelection = selection;
                  _isMultiEditingDifferentTypes = false;
                  _serializedObject = null;

                  if (selection == null || selection.Count == 0)
                  {
                        _dependencyPanelView.ClearTarget();
                        _assetDetailsView.SetTarget(null);

                        return;
                  }

                  if (selection.Count == 1)
                  {
                        ScriptableObjectData singleTarget = selection[0];
                        _assetDetailsView.SetTarget(singleTarget);
                        _dependencyPanelView.SetTarget(singleTarget);
                  }
                  else
                  {
                        _dependencyPanelView.ClearTarget();
                        string firstType = selection[0].type;

                        if (selection.Exists(s => s.type != firstType))
                        {
                              _isMultiEditingDifferentTypes = true;

                              return;
                        }
                  }

                  ScriptableObject[] objects = selection.Select(static s => s.scriptableObject).Where(static s => s).ToArray();

                  if (objects.Length > 0)
                  {
                        _serializedObject = new SerializedObject(objects);
                  }
            }

            public void Draw()
            {
                  EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
                  _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

                  if (_currentSelection == null || _currentSelection.Count == 0)
                  {
                        DrawNoSelectionMessage();
                  }
                  else if (_isMultiEditingDifferentTypes)
                  {
                        DrawMultiEditDifferentTypes();
                  }
                  else if (_currentSelection.Count > 1)
                  {
                        DrawMultiEditSameType();
                  }
                  else
                  {
                        DrawSingleEdit();
                  }

                  EditorGUILayout.EndScrollView();

                  if (_currentSelection is { Count: 1 })
                  {
                        _dependencyPanelView.Draw();
                  }

                  EditorGUILayout.EndVertical();
            }

            private void DrawSingleEdit()
            {
                  ScriptableObjectData soData = _currentSelection[0];
                  EditorGUILayout.LabelField($"Editing: {soData.name}", EditorStyles.boldLabel);
                  EditorGUILayout.LabelField($"Type: {soData.type}", EditorStyles.miniLabel);

                  _assetDetailsView.Draw(soData);

                  EditorGUILayout.Space(10);
                  DrawSerializedObjectEditor();
            }

            private void DrawMultiEditSameType()
            {
                  EditorGUILayout.LabelField($"Editing: {_currentSelection.Count} objects", EditorStyles.boldLabel);
                  EditorGUILayout.LabelField($"Type: {_currentSelection[0].type}", EditorStyles.miniLabel);
                  EditorGUILayout.Space(10);
                  DrawSerializedObjectEditor();
            }

            private void DrawMultiEditDifferentTypes()
            {
                  EditorGUILayout.LabelField($"{_currentSelection.Count} objects selected", EditorStyles.boldLabel);
                  EditorGUILayout.HelpBox("Multi-editing is not supported for objects of different types.", MessageType.Info);
                  EditorGUILayout.Space(10);
                  EditorGUILayout.LabelField("Group Actions", EditorStyles.boldLabel);

                  if (GUILayout.Button("Add to Favorites"))
                  {
                        OnRequestBulkAddToFavorites?.Invoke();
                  }

                  if (GUILayout.Button("Delete Selected Objects"))
                  {
                        OnRequestBulkDelete?.Invoke();
                  }
            }

            private void DrawSerializedObjectEditor()
            {
                  if (_serializedObject == null || _serializedObject.targetObject == null)
                  {
                        return;
                  }

                  _serializedObject.Update();
                  EditorGUI.BeginChangeCheck();

                  SerializedProperty prop = _serializedObject.GetIterator();

                  if (prop.NextVisible(true))
                  {
                        do
                        {
                              if (prop.name == "m_Script")
                              {
                                    continue;
                              }

                              EditorGUILayout.PropertyField(prop, true);
                        } while (prop.NextVisible(false));
                  }

                  if (EditorGUI.EndChangeCheck())
                  {
                        _serializedObject.ApplyModifiedProperties();
                  }
            }

            private static void DrawNoSelectionMessage()
            {
                  GUILayout.FlexibleSpace();
                  EditorGUILayout.LabelField("Select a ScriptableObject to edit", EditorStyles.centeredGreyMiniLabel);
                  GUILayout.FlexibleSpace();
            }
      }
}