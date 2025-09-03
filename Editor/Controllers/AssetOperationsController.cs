using System.Collections.Generic;
using System.Linq;
using OpalStudio.ScriptableManager.Editor.Models;
using UnityEditor;

namespace OpalStudio.ScriptableManager.Editor.Controllers
{
      public sealed class AssetOperationsController
      {
            private readonly ScriptableObjectRepository _repository;
            private readonly FavoritesManager _favoritesManager;
            private readonly SelectionHandler _selectionHandler;

            public AssetOperationsController(ScriptableObjectRepository repository, FavoritesManager favoritesManager, SelectionHandler selectionHandler)
            {
                  _repository = repository;
                  _favoritesManager = favoritesManager;
                  _selectionHandler = selectionHandler;
            }

            public void ToggleFavorites(IEnumerable<string> guids)
            {
                  _favoritesManager.ToggleFavoritesGroup(guids);
            }

            public bool DeleteAssets(IEnumerable<string> guids)
            {
                  List<string> guidList = guids.ToList();
                  List<ScriptableObjectData> itemsToDelete = _repository.AllScriptableObjects.Where(s => guidList.Contains(s.guid)).ToList();

                  if (itemsToDelete.Count == 0)
                  {
                        return false;
                  }

                  if (EditorUtility.DisplayDialog("Delete Selected Objects", $"Are you sure you want to delete {itemsToDelete.Count} objects? This cannot be undone.",
                                  "Delete", "Cancel"))
                  {
                        foreach (ScriptableObjectData soData in itemsToDelete)
                        {
                              AssetDatabase.MoveAssetToTrash(soData.path);
                        }

                        _selectionHandler.RemoveFromSelection(guidList);

                        return true;
                  }

                  return false;
            }
      }
}