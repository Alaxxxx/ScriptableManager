using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Controllers
{
      public sealed class SelectionHandler
      {
            public IEnumerable<ScriptableObjectData> CurrentSelectionData => _currentSelectionData;

            private readonly HashSet<string> _selectedSoGuids = new();
            private List<ScriptableObjectData> _currentSelectionData = new();

            private readonly ScriptableObjectRepository _soRepository;
            private List<ScriptableObjectData> _filteredList;

            public SelectionHandler(ScriptableObjectRepository soRepository)
            {
                  _soRepository = soRepository;
            }

            public void LoadSelection()
            {
                  _selectedSoGuids.Clear();

                  foreach (string guid in SettingsManager.GetLastSelection())
                  {
                        _selectedSoGuids.Add(guid);
                  }

                  RebuildSelectionDataList();
            }

            public void SaveSelection()
            {
                  SettingsManager.SetLastSelection(_selectedSoGuids);
            }

            public void SetFilteredList(List<ScriptableObjectData> filteredList)
            {
                  _filteredList = filteredList;
            }

            public void HandleSelectionChange(ScriptableObjectData soData, bool isCtrl, bool isShift)
            {
                  if (isShift)
                  {
                        SelectRange(soData);
                  }
                  else if (isCtrl)
                  {
                        Toggle(soData);
                        SettingsManager.SetLastClickedGuid(soData.guid);
                  }
                  else
                  {
                        Select(soData);
                        SettingsManager.SetLastClickedGuid(soData.guid);
                  }
            }

            private void Select(ScriptableObjectData soData)
            {
                  _selectedSoGuids.Clear();
                  _selectedSoGuids.Add(soData.guid);
                  RebuildSelectionDataList();
            }

            public void SelectFromGuid(ScriptableObjectData soData)
            {
                  Select(soData);
                  SettingsManager.SetLastClickedGuid(soData.guid);
            }

            private void Toggle(ScriptableObjectData soData)
            {
                  if (!_selectedSoGuids.Remove(soData.guid))
                  {
                        _selectedSoGuids.Add(soData.guid);
                  }

                  RebuildSelectionDataList();
            }

            private void SelectRange(ScriptableObjectData soData)
            {
                  if (_filteredList == null || _filteredList.Count == 0)
                  {
                        Select(soData);

                        return;
                  }

                  string lastSelectedGuid = SettingsManager.GetLastClickedGuid();

                  if (string.IsNullOrEmpty(lastSelectedGuid))
                  {
                        Select(soData);

                        return;
                  }

                  int lastIndex = _filteredList.FindIndex(so => so.guid == lastSelectedGuid);
                  int currentIndex = _filteredList.FindIndex(so => so.guid == soData.guid);

                  if (lastIndex == -1 || currentIndex == -1)
                  {
                        Select(soData);

                        return;
                  }

                  _selectedSoGuids.Clear();
                  int start = Mathf.Min(lastIndex, currentIndex);
                  int end = Mathf.Max(lastIndex, currentIndex);

                  for (int i = start; i <= end; i++)
                  {
                        if (i < _filteredList.Count)
                        {
                              _selectedSoGuids.Add(_filteredList[i].guid);
                        }
                  }

                  RebuildSelectionDataList();
            }

            public void Clear()
            {
                  _selectedSoGuids.Clear();
                  RebuildSelectionDataList();
            }


            public void RemoveFromSelection(IEnumerable<string> guids)
            {
                  foreach (string guid in guids)
                  {
                        _selectedSoGuids.Remove(guid);
                  }

                  RebuildSelectionDataList();
            }

            public void RebuildSelectionDataList()
            {
                  if (_selectedSoGuids.Count > 0)
                  {
                        _currentSelectionData = _soRepository.AllScriptableObjects.Where(so => _selectedSoGuids.Contains(so.guid)).ToList();
                  }
                  else
                  {
                        _currentSelectionData.Clear();
                  }
            }
      }
}