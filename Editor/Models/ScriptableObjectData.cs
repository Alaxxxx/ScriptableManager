using System;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      [Serializable]
      public sealed class ScriptableObjectData : IEquatable<ScriptableObjectData>
      {
            public ScriptableObject scriptableObject;
            public string name;
            public string type;
            public string path;
            public string guid;
            public DateTime LastModified;

            public bool Equals(ScriptableObjectData other)
            {
                  if (ReferenceEquals(null, other))
                  {
                        return false;
                  }

                  if (ReferenceEquals(this, other))
                  {
                        return true;
                  }

                  return guid == other.guid;
            }

            public override bool Equals(object obj)
            {
                  if (ReferenceEquals(null, obj))
                  {
                        return false;
                  }

                  if (ReferenceEquals(this, obj))
                  {
                        return true;
                  }

                  if (obj.GetType() != this.GetType())
                  {
                        return false;
                  }

                  return Equals((ScriptableObjectData)obj);
            }

            public override int GetHashCode()
            {
                  return guid != null ? guid.GetHashCode() : 0;
            }
      }
}