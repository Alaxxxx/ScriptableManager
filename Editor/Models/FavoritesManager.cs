using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace OpalStudio.ScriptableManager.Models
{
      public sealed class FavoritesManager
      {
            private const string FavoritesKey = "SOManager_Favorites";
            public HashSet<string> favoriteSoGuids { get; private set; }

            public FavoritesManager()
            {
                  favoriteSoGuids = new HashSet<string>();
            }

            public void LoadFavorites()
            {
                  string favs = EditorPrefs.GetString(FavoritesKey, "");
                  favoriteSoGuids = new HashSet<string>(favs.Split(';').Where(static s => !string.IsNullOrEmpty(s)));
            }

            private void SaveFavorites()
            {
                  EditorPrefs.SetString(FavoritesKey, string.Join(";", favoriteSoGuids));
            }

            public void ToggleFavorite(string guid)
            {
                  if (!favoriteSoGuids.Add(guid))
                  {
                        favoriteSoGuids.Remove(guid);
                  }

                  SaveFavorites();
            }

            public void ToggleFavoritesGroup(IEnumerable<string> guids)
            {
                  IEnumerable<string> enumerable = guids as string[] ?? guids.ToArray();
                  bool allAreFavorites = enumerable.All(guid => favoriteSoGuids.Contains(guid));

                  foreach (string guid in enumerable)
                  {
                        if (allAreFavorites)
                        {
                              favoriteSoGuids.Remove(guid);
                        }
                        else
                        {
                              favoriteSoGuids.Add(guid);
                        }
                  }

                  SaveFavorites();
            }
      }
}