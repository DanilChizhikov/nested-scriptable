using System;
using System.Collections.Generic;
using UnityEngine;

namespace MBSCore.Scriptable
{
    internal sealed partial class NestedScriptableEditor
    {
        private static bool ValidateGenericType(Type checkedType, out Type genericTypes)
        {
            genericTypes = null;
            while (checkedType != null)
            {
                if (checkedType.IsArray)
                {
                    genericTypes = checkedType.GetElementType();
                }
                else if (checkedType.IsGenericType && checkedType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    genericTypes = checkedType.GetGenericArguments()[0];
                }
                
                if(typeof(ScriptableObject).IsAssignableFrom(genericTypes))
                {
                    return true;
                }

                checkedType = genericTypes?.BaseType;
            }

            return false;
        }
    }
}