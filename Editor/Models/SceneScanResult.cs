using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      [Serializable]
      public class SceneScanResult
      {
            public List<GameObject> gameObjects = new();
            public bool isComplete = false;
            public bool hasError = false;
            public string errorMessage = "";
            public DateTime scanTime = DateTime.Now;

            public void Clear()
            {
                  foreach (GameObject go in gameObjects)
                  {
                        if (go != null && go.hideFlags == HideFlags.DontSave)
                        {
                              GameObject.DestroyImmediate(go);
                        }
                  }

                  gameObjects.Clear();
                  isComplete = false;
                  hasError = false;
                  errorMessage = "";
            }
      }
}