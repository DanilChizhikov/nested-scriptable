using System;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MBSCore.Scriptable
{
    internal abstract partial class ScriptableDrawerMediator
    {
        private static readonly MethodInfo s_createMediatorGenericMethod =
            typeof(ScriptableDrawerMediator)
                .GetMethod(nameof(CreateMediatorGeneric), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo s_getOptimizedGUIBlock =
            typeof(Editor).GetMethod("GetOptimizedGUIBlock", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo s_onOptimizedInspectorGUI =
            typeof(Editor).GetMethod("OnOptimizedInspectorGUI", BindingFlags.Instance | BindingFlags.NonPublic);

        protected static Type s_editorDefaultType = Assembly.Load("UnityEditor").GetType("UnityEditor.GenericInspector");
        
        protected static bool GetOptimizedGUIBlock(Editor editor, bool isDirty, bool isVisible, out float height)
        {
            var arguments = new object[3];
            arguments[0] = isDirty;
            arguments[1] = isVisible;
            bool result = (bool)s_getOptimizedGUIBlock.Invoke(editor, arguments);
            height = (float)arguments[2];
            return result;
        }
		
        protected static void OnOptimizedInspectorGUI(Editor editor, Rect contentRect)
        {
			s_onOptimizedInspectorGUI.Invoke(editor, new object[] { contentRect });
        }
        
        private static ScriptableDrawerMediator CreateMediatorGeneric<T>(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
            ScriptableObject scriptableObject, SerializedProperty rootProperty) where T : ScriptableObject
        {
            return new ScriptableDrawerMediator<T>(label, fieldInfo, reorderableList, scriptableObject, rootProperty);
        }
    }
}