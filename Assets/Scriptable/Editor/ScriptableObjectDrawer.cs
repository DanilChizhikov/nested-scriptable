using UnityEditor;
using UnityEngine;

namespace MBSCore.Scriptable
{
    [CustomPropertyDrawer(typeof(ScriptableObject), true)]
    internal sealed class ScriptableObjectDrawer : PropertyDrawer
    {
        private Editor _editor;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(position, property.isExpanded, label);
            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;
            EditorGUI.ObjectField(position, property.objectReferenceValue, typeof(ScriptableObject), false);
            if (property.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new GUILayout.VerticalScope("box"))
                    {
                        if (!_editor)
                        {
                            Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                        }

                        var checkScope = new EditorGUI.ChangeCheckScope();
                        using (checkScope)
                        {
                            if (_editor)
                            {
                                _editor.OnInspectorGUI();
                            }
                        }
                    
                        if (checkScope.changed)
                        {
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
            
            EditorGUI.EndFoldoutHeaderGroup();
        }
    }
}