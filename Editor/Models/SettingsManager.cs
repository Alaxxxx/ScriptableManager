using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public enum SortOption
      {
            ByName,
            ByType,
            ByDate,
            ByDateOldest,
      }

      public sealed class SettingsManager
      {
            private const string SortOptionKey = "SOManager_SortOption";
            private const string ExcludedPathsKey = "SOManager_ExcludedPaths";
            private const string LastSelectionKey = "SOManager_LastSelection";
            private const string LastClickedKey = "SOManager_LastClickedGuid";
            private const string ScanPackagesKey = "SOManager_ScanPackages";

            private readonly static List<string> DefaultExcludedPaths = new() { "Assets/Plugins/" };
            private const bool DefaultScanPackages = false;

            public SortOption CurrentSortOption { get; private set; }
            public List<string> ExcludedPaths { get; private set; }
            public bool ScanPackages { get; set; }

            public void LoadSettings()
            {
                  CurrentSortOption = (SortOption)EditorPrefs.GetInt(SortOptionKey, (int)SortOption.ByName);
                  ScanPackages = EditorPrefs.GetBool(ScanPackagesKey, DefaultScanPackages);

                  string excluded = EditorPrefs.GetString(ExcludedPathsKey, "");

                  ExcludedPaths = string.IsNullOrEmpty(excluded)
                              ? new List<string>(DefaultExcludedPaths)
                              : new List<string>(excluded.Split(';').Where(static s => !string.IsNullOrEmpty(s)));
            }

            public void SaveSettings()
            {
                  EditorPrefs.SetInt(SortOptionKey, (int)CurrentSortOption);
                  EditorPrefs.SetBool(ScanPackagesKey, ScanPackages);
                  string excluded = string.Join(";", ExcludedPaths);
                  EditorPrefs.SetString(ExcludedPathsKey, excluded);
            }

            public void ResetToDefaults()
            {
                  CurrentSortOption = SortOption.ByName;
                  ScanPackages = DefaultScanPackages;
                  ExcludedPaths = new List<string>(DefaultExcludedPaths);
                  SaveSettings();
            }

            public static float GetFloat(string key, float defaultValue) => EditorPrefs.GetFloat(key, defaultValue);

            public static void SetFloat(string key, float value) => EditorPrefs.SetFloat(key, value);

            public static string GetString(string key, string defaultValue) => EditorPrefs.GetString(key, defaultValue);

            public static void SetString(string key, string value) => EditorPrefs.SetString(key, value);

            public void SetSortOption(SortOption option)
            {
                  CurrentSortOption = option;
                  SaveSettings();
            }

            public static IEnumerable<string> GetLastSelection() =>
                        new List<string>(EditorPrefs.GetString(LastSelectionKey, "").Split(';').Where(static s => !string.IsNullOrEmpty(s)));

            public static void SetLastSelection(IEnumerable<string> guids) => EditorPrefs.SetString(LastSelectionKey, string.Join(";", guids));

            public static string GetLastClickedGuid() => EditorPrefs.GetString(LastClickedKey, "");

            public static void SetLastClickedGuid(string guid) => EditorPrefs.SetString(LastClickedKey, guid);
      }
}