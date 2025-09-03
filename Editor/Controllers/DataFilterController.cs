using System;
using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using OpalStudio.ScriptableManager.Editor.Views;

namespace OpalStudio.ScriptableManager.Editor.Controllers
{
      public sealed class DataFilterController
      {
            private readonly ScriptableObjectRepository _repository;
            private readonly SettingsManager _settingsManager;
            private readonly FavoritesManager _favoritesManager;

            public DataFilterController(ScriptableObjectRepository repository, SettingsManager settingsManager, FavoritesManager favoritesManager)
            {
                  _repository = repository;
                  _settingsManager = settingsManager;
                  _favoritesManager = favoritesManager;
            }

            public List<ScriptableObjectData> GetFilteredAndSortedData(FilterPanelView filterPanel)
            {
                  List<ScriptableObjectData> filteredList = _repository.AllScriptableObjects.Where(so =>
                                                                       {
                                                                             bool matchesSearch = string.IsNullOrEmpty(filterPanel.SearchText) ||
                                                                                                  so.name.ToLower()
                                                                                                    .Contains(filterPanel.SearchText.ToLower(),
                                                                                                                StringComparison.OrdinalIgnoreCase) ||
                                                                                                  so.type.ToLower()
                                                                                                    .Contains(filterPanel.SearchText.ToLower(),
                                                                                                                StringComparison.OrdinalIgnoreCase);

                                                                             bool matchesType = filterPanel.SelectedTypeFilter == "All Types" ||
                                                                                                so.type == filterPanel.SelectedTypeFilter;

                                                                             bool matchesFavorites = !filterPanel.FavoritesOnly ||
                                                                                                     _favoritesManager.favoriteSoGuids.Contains(so.guid);

                                                                             return matchesSearch && matchesType && matchesFavorites;
                                                                       })
                                                                       .ToList();

                  return SortData(filteredList);
            }

            private List<ScriptableObjectData> SortData(List<ScriptableObjectData> data)
            {
                  return _settingsManager.CurrentSortOption switch
                  {
                              SortOption.ByName => data.OrderBy(static so => so.name).ToList(),
                              SortOption.ByType => data.OrderBy(static so => so.type).ThenBy(static so => so.name).ToList(),
                              SortOption.ByDate => data.OrderByDescending(static so => so.LastModified).ToList(),
                              SortOption.ByDateOldest => data.OrderBy(static so => so.LastModified).ToList(),
                              _ => data
                  };
            }
      }
}