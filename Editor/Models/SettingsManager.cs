using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace OpalStudio.ScriptableManager.Models
{
      public enum SortOption
      {
            ByName,
            ByType,
            ByDate
      }

      public sealed class SettingsManager
      {
            private const string SortOptionKey = "SOManager_SortOption";
            private const string ExcludedPathsKey = "SOManager_ExcludedPaths";
            private const string LastSelectionKey = "SOManager_LastSelection";
            private const string LastClickedKey = "SOManager_LastClickedGuid";

            public SortOption CurrentSortOption { get; private set; }
            public List<string> ExcludedPaths { get; private set; }

            public void LoadSettings()
            {
                  CurrentSortOption = (SortOption)EditorPrefs.GetInt(SortOptionKey, (int)SortOption.ByName);
                  string excluded = EditorPrefs.GetString(ExcludedPathsKey, "Assets/Plugins");
                  ExcludedPaths = new List<string>(excluded.Split(';').Where(static s => !string.IsNullOrEmpty(s)));
            }

            public void SaveSettings()
            {
                  EditorPrefs.SetInt(SortOptionKey, (int)CurrentSortOption);
                  string excluded = string.Join(";", ExcludedPaths);
                  EditorPrefs.SetString(ExcludedPathsKey, excluded);
            }

            public float GetFloat(string key, float defaultValue) => EditorPrefs.GetFloat(key, defaultValue);

            public void SetFloat(string key, float value) => EditorPrefs.SetFloat(key, value);

            public string GetString(string key, string defaultValue) => EditorPrefs.GetString(key, defaultValue);

            public void SetString(string key, string value) => EditorPrefs.SetString(key, value);

            public void SetSortOption(SortOption option)
            {
                  CurrentSortOption = option;
                  SaveSettings();
            }

            public List<string> GetLastSelection()
            {
                  string selection = EditorPrefs.GetString(LastSelectionKey, "");

                  return new List<string>(selection.Split(';').Where(s => !string.IsNullOrEmpty(s)));
            }

            public void SetLastSelection(List<string> guids)
            {
                  string selection = string.Join(";", guids);
                  EditorPrefs.SetString(LastSelectionKey, selection);
            }

            public string GetLastClickedGuid()
            {
                  return EditorPrefs.GetString(LastClickedKey, "");
            }

            public void SetLastClickedGuid(string guid)
            {
                  EditorPrefs.SetString(LastClickedKey, guid);
            }
      }
}