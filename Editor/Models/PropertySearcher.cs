using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OpalStudio.ScriptableManager.Editor.Models
{
      public sealed class PropertyFilter
      {
            public string PropertyName;

            public enum Operator
            {
                  Equals,
                  NotEquals,
                  GreaterThan,
                  LessThan,
                  Contains
            }

            public Operator SelectedOperator;
            public string Value;
            public SerializedPropertyType PropertyType;
      }

      public static class PropertySearcher
      {
            private readonly static Dictionary<Type, FieldInfo[]> FieldsCache = new();

            public static FieldInfo[] GetSerializableFields(Type type)
            {
                  if (FieldsCache.TryGetValue(type, out FieldInfo[] cachedFields))
                  {
                        return cachedFields;
                  }

                  FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                           .Where(static field => (field.IsPublic && !Attribute.IsDefined(field, typeof(NonSerializedAttribute))) ||
                                                                  Attribute.IsDefined(field, typeof(SerializeField)))
                                           .ToArray();

                  FieldsCache[type] = fields;

                  return fields;
            }

            public static List<ScriptableObjectData> FilterByProperties(List<ScriptableObjectData> data, List<PropertyFilter> filters)
            {
                  if (filters == null || filters.Count == 0)
                  {
                        return data;
                  }

                  return data.Where(soData => MatchesAllFilters(soData.scriptableObject, filters)).ToList();
            }

            private static bool MatchesAllFilters(ScriptableObject so, List<PropertyFilter> filters)
            {
                  if (so == null)
                  {
                        return false;
                  }

                  var serializedObject = new SerializedObject(so);

                  foreach (PropertyFilter filter in filters)
                  {
                        SerializedProperty property = serializedObject.FindProperty(filter.PropertyName);

                        if (property == null || !Matches(property, filter))
                        {
                              return false;
                        }
                  }

                  return true;
            }

            private static bool Matches(SerializedProperty property, PropertyFilter filter)
            {
                  try
                  {
                        switch (property.propertyType)
                        {
                              case SerializedPropertyType.Integer:
                              case SerializedPropertyType.Enum:
                                    long propValueInt = property.propertyType == SerializedPropertyType.Integer ? property.longValue : property.enumValueIndex;

                                    if (!long.TryParse(filter.Value, out long filterValueInt))
                                    {
                                          return false;
                                    }

                                    return Compare(propValueInt, filterValueInt, filter.SelectedOperator);

                              case SerializedPropertyType.Float:
                                    double propValueFloat = property.doubleValue;

                                    if (!double.TryParse(filter.Value, out double filterValueFloat))
                                    {
                                          return false;
                                    }

                                    return Compare(propValueFloat, filterValueFloat, filter.SelectedOperator);

                              case SerializedPropertyType.String:
                                    string propValueStr = property.stringValue;

                                    return Compare(propValueStr, filter.Value, filter.SelectedOperator);

                              case SerializedPropertyType.Boolean:
                                    if (!bool.TryParse(filter.Value, out bool filterValueBool))
                                    {
                                          return false;
                                    }

                                    return Compare(property.boolValue, filterValueBool, filter.SelectedOperator);

                              default:
                                    return false;
                        }
                  }
                  catch (Exception)
                  {
                        return false;
                  }
            }

            private static bool Compare<T>(IComparable<T> a, T b, PropertyFilter.Operator op)
            {
                  int comparison = a.CompareTo(b);

                  return op switch
                  {
                              PropertyFilter.Operator.Equals => comparison == 0,
                              PropertyFilter.Operator.NotEquals => comparison != 0,
                              PropertyFilter.Operator.GreaterThan => comparison > 0,
                              PropertyFilter.Operator.LessThan => comparison < 0,
                              _ => false
                  };
            }

            private static bool Compare(string a, string b, PropertyFilter.Operator op)
            {
                  return op switch
                  {
                              PropertyFilter.Operator.Equals => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
                              PropertyFilter.Operator.NotEquals => !string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
                              PropertyFilter.Operator.Contains => a != null && a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0,
                              _ => false
                  };
            }

            private static bool Compare(bool a, bool b, PropertyFilter.Operator op)
            {
                  return op switch
                  {
                              PropertyFilter.Operator.Equals => a == b,
                              PropertyFilter.Operator.NotEquals => a != b,
                              _ => false
                  };
            }
      }
}