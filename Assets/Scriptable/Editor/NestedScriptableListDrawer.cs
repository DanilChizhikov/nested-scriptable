using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MBSCore.Scriptable
{
    [CustomPropertyDrawer(typeof(NestedScriptableList<>))]
    internal sealed class NestedScriptableListDrawer : PropertyDrawer
    {
        private const string ErrorMessage = "NestedScriptableList can only be used within the inheritors of ScriptableObject";
        private const float ErrorMessageHeight = 25f;

        private ReorderableList _list;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            if (_list != null)
            {
                return _list.GetHeight() + EditorGUIUtility.standardVerticalSpacing;
            }

            return ErrorMessageHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property);
            if (_list == null)
            {
                EditorGUI.HelpBox(position, ErrorMessage, MessageType.Error);
                return;
            }
            
            _list.DoList(position);
        }

        private void Initialize(SerializedProperty property)
        {
            if (_list != null ||
                property.serializedObject.targetObject is not ScriptableObject targetObject)
            {
                return;
            }
            
            ValidateGenericType(fieldInfo.FieldType, out Type listArgumentType);
            IList list = fieldInfo.GetValue(property.serializedObject.targetObject) as IList;
            _list = new ReorderableList(list, listArgumentType, true, true, true, true);
            ScriptableDrawerMediator.CreateMediator(property.displayName, fieldInfo, listArgumentType, _list, targetObject);
        }
        
        private static void ValidateGenericType(Type checkedType, out Type genericTypes)
        {
            while (checkedType != null)
            {
                if (checkedType.IsGenericType && checkedType.GetGenericTypeDefinition() == typeof(NestedScriptableList<>))
                {
                    genericTypes = checkedType.GetGenericArguments()[0];
                    return;
                }

                checkedType = checkedType.BaseType;
            }

            genericTypes = null;
        }
    }
}