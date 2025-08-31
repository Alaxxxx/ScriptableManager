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

            public string FormattedDate => LastModified.ToString("dd/MM/yyyy HH:mm");
            public string RelativeDate => GetRelativeDate(LastModified);

            private static string GetRelativeDate(DateTime date)
            {
                  TimeSpan diff = DateTime.Now - date;

                  if (diff.TotalDays < 1)
                  {
                        return "Today";
                  }

                  if (diff.TotalDays < 2)
                  {
                        return "Yesterday";
                  }

                  if (diff.TotalDays < 7)
                  {
                        return $"{(int)diff.TotalDays} days ago";
                  }

                  if (diff.TotalDays < 30)
                  {
                        return $"{(int)(diff.TotalDays / 7)} weeks ago";
                  }

                  if (diff.TotalDays < 365)
                  {
                        return $"{(int)(diff.TotalDays / 30)} months ago";
                  }

                  return $"{(int)(diff.TotalDays / 365)} years ago";
            }

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