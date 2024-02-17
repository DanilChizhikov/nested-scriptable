using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MBSCore.Scriptable
{
    [CustomEditor(typeof(ScriptableObject), true)]
    internal sealed partial class NestedScriptableEditor : Editor
    {
        private readonly List<FieldInfo> _fieldInfos = new List<FieldInfo>();
        private readonly Dictionary<FieldInfo, ReorderableList> _listMap = new Dictionary<FieldInfo, ReorderableList>();
        
        private ScriptableObject _target;

        public override void OnInspectorGUI()
        {
            if (_target == null)
            {
                return;
            }

            serializedObject.Update();
            int fieldInfosCount = _fieldInfos.Count;
            for (int i = 0; i < fieldInfosCount; i++)
            {
                FieldInfo fieldInfo = _fieldInfos[i];
                if(!fieldInfo.IsPublic &&
                   fieldInfo.GetCustomAttributes(typeof(SerializeField), true).Length <= 0)
                {
                    continue;
                }

                SerializedProperty property = serializedObject.FindProperty(fieldInfo.Name);
                if (_listMap.TryGetValue(fieldInfo, out ReorderableList list))
                {
                    list.DoLayoutList();
                }
                else
                {
                    EditorGUILayout.PropertyField(property);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        private ReorderableList CreateList(SerializedProperty property, FieldInfo fieldInfo)
        {
            if (!ValidateGenericType(fieldInfo.FieldType, out Type listArgumentType))
            {
                return null;
            }
            
            IList list = fieldInfo.GetValue(_target) as IList;
            var reorderableList = new ReorderableList(list, listArgumentType, true, true, true, true); 
            ScriptableDrawerMediator.CreateMediator(property.displayName, fieldInfo, listArgumentType, reorderableList, _target);
            return reorderableList;
        }

        private void OnEnable()
        {
            _target = target as ScriptableObject;
            if (_target == null)
            {
                return;
            }
            
            _fieldInfos.Clear();
            _listMap.Clear();
            FieldInfo[] fields = _target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance |
                                                             BindingFlags.Public | BindingFlags.Static);
            _fieldInfos.AddRange(fields);
            int fieldInfosCount = _fieldInfos.Count;
            for (int i = 0; i < fieldInfosCount; i++)
            {
                FieldInfo fieldInfo = _fieldInfos[i];
                if(!fieldInfo.IsPublic &&
                   fieldInfo.GetCustomAttributes(typeof(SerializeField), true).Length <= 0)
                {
                    continue;
                }

                SerializedProperty property = serializedObject.FindProperty(fieldInfo.Name);
                if (fieldInfo.GetCustomAttributes(typeof(NestedScriptableAttribute), true).Length > 0)
                {
                    _listMap.Add(fieldInfo, CreateList(property, fieldInfo));
                }
            }
        }

        private void OnDisable()
        {
            _target = null;
        }
    }
}