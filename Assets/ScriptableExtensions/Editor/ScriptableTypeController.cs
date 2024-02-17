using System;
using System.Linq;
using UnityEngine;

namespace MBSCore.ScriptableExtensions
{
    internal static class ScriptableTypeController<T> where T : ScriptableObject
    {
        private static readonly Type s_processingType = typeof(T);

        private static string[] s_names;
        private static Type[] s_types;

        public static string[] Names
        {
            get
            {
                if (s_names == null)
                {
                    int count = Types.Length;
                    s_names = new string[count];
                    for (var i = 0; i < count; i++)
                    {
                        s_names[i] = Types[i].Name;
                    }
                }

                return s_names;
            }
        }

        private static Type[] Types
        {
            get
            {
                if (s_types == null)
                {
                    s_types = GetAssignableTypes(s_processingType);
                }

                return s_types;
            }
        }

        public static T CreateInstance(string typeName)
        {
            foreach (Type type in Types)
            {
                if (type.Name == typeName)
                {
                    ScriptableObject asset = ScriptableObject.CreateInstance(type);
                    return asset as T;
                }
            }

            return null;
        }

        private static bool Validate(Type type)
        {
            return !type.IsAbstract && s_processingType.IsAssignableFrom(type);
        }
        
        private static Type[] GetAssignableTypes(Type baseType)
        {
            bool AssignableCondition(Type type) => baseType.IsAssignableFrom(type) && !type.IsAbstract;
            return GetAllMatchingTypes(AssignableCondition);
        }
        
        private static Type[] GetAllMatchingTypes(Func<Type, bool> predicate)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(predicate)
                .ToArray();
        }
    }
}