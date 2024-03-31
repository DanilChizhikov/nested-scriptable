using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MBSCore.Scriptable
{
	internal abstract partial class ScriptableDrawerMediator
	{
		protected string Label { get; }
		protected FieldInfo FieldInfo { get; }
		protected ReorderableList List { get; }
		protected ScriptableObject Target { get; }

		public ScriptableDrawerMediator(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
			ScriptableObject scriptableObject)
		{
			Label = label;
			FieldInfo = fieldInfo;
			List = reorderableList;
			Target = scriptableObject;
		}

		public static void CreateMediator(string label, FieldInfo fieldInfo, Type type, ReorderableList reorderableList,
			ScriptableObject scriptableObject)
		{
			s_createMediatorGenericMethod
				.MakeGenericMethod(type)
				.Invoke(null, new object[] { label, fieldInfo, reorderableList, scriptableObject });
		}
	}

	internal sealed class ScriptableDrawerMediator<T> : ScriptableDrawerMediator where T : ScriptableObject
	{
		private const float ObjectWeight = 1f;
		private const float NameWeight = 1f;
		private const float SumWeight = ObjectWeight + NameWeight;
		private const float ColumnSpace = 6f;

		private readonly Dictionary<T, Editor> _editorsMap;
		private readonly Dictionary<int, bool> _expandedMap;

		public ScriptableDrawerMediator(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
			ScriptableObject scriptableObject) :
			base(label, fieldInfo, reorderableList, scriptableObject)
		{
			_editorsMap = new Dictionary<T, Editor>();
			_expandedMap = new Dictionary<int, bool>();
			reorderableList.drawHeaderCallback += DrawHeader;
			reorderableList.elementHeightCallback += CalculateElementHeight;
			reorderableList.drawElementCallback += DrawElement;
			reorderableList.onAddDropdownCallback += ShowDropdownElements;
			reorderableList.onRemoveCallback += RemoveElement;
		}
		
		private void DrawHeader(Rect rect) =>
			EditorGUI.LabelField(rect, Label);
		
		private float CalculateElementHeight(int index)
		{
			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			bool isExpanded = _expandedMap.GetValueOrDefault(index, false);
			if (isExpanded)
			{
				var element = List.list[index] as T;
				Editor editor = GetEditor(element);
				if (GetOptimizedGUIBlock(editor, false, true, out float editorHeight))
				{
					height += editorHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}
			}
			
			return height;
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			T element = List.list[index] as T;
			if (element == null)
			{
				return;
			}
			
			bool isExpanded = _expandedMap.GetValueOrDefault(index, false);
			rect.height = EditorGUIUtility.singleLineHeight;
			isExpanded = EditorGUI.Foldout(rect, isExpanded, element.name);
			if (isExpanded)
			{
				float weightWidth = (rect.width - ColumnSpace) / SumWeight;
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				rect.height = EditorGUIUtility.singleLineHeight;
				float usedWidth = DrawObjectField(rect, weightWidth, ref element);
				DrawNameField(rect, weightWidth, element, usedWidth);
				Editor editor = GetEditor(element);
				if (!GetOptimizedGUIBlock(editor, false, true, out float height))
				{
					return;
				}

				Rect editorRect = rect;
				editorRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				editorRect.height = height + EditorGUIUtility.singleLineHeight;
				GUI.changed = true;
				OnOptimizedInspectorGUI(editor, editorRect);
			}
			
			_expandedMap[index] = isExpanded;
		}
		
		private void ShowDropdownElements(Rect rect, ReorderableList list)
		{
			var dropdownMenu = new GenericMenu();
			string[] names = ScriptableTypeController<T>.Names;
			for (int i = 0; i < names.Length; i++)
			{
				string createdTypeName = names[i];
				dropdownMenu.AddItem(new GUIContent(createdTypeName), false, () =>
				{
					T elementConfig = ScriptableTypeController<T>.CreateInstance(createdTypeName);
					elementConfig.name = createdTypeName;
					if (List.list is Array array)
					{
						var newArray = new T[array.Length + 1];
						array.CopyTo(newArray, 0);
						FieldInfo.SetValue(Target, newArray);
						List.list = newArray;
					}
					else
					{
						List.list.Add(null);
					}
			
					List.list[^1] = elementConfig;
					AssetDatabase.AddObjectToAsset(elementConfig, Target);
					AssetDatabase.SaveAssets();
				});
			}
			
			dropdownMenu.ShowAsContext();
		}
		
		private void RemoveElement(ReorderableList list)
		{
			T removedObject = List.list[list.index] as T;
			list.list.RemoveAt(list.index);
			if (removedObject == null)
			{
				return;
			}

			Object.DestroyImmediate(removedObject, true);
			AssetDatabase.SaveAssets();
		}

		private float DrawObjectField(Rect rect, float weightWidth, ref T element)
		{
			Rect elementRect = rect;
			elementRect.height = EditorGUIUtility.singleLineHeight;
			elementRect.width = weightWidth * ObjectWeight;
			using (new EditorGUI.DisabledScope(true))
			{
				element = EditorGUI.ObjectField(elementRect, string.Empty, element, typeof(T), false) as T;
			} 
			
			return elementRect.width;
		}

		private void DrawNameField(Rect rect, float weightWidth, T value, float usedWidth)
		{
			var nameRect = new Rect(
				rect.x + usedWidth + ColumnSpace,
				rect.y,
				weightWidth * NameWeight,
				EditorGUIUtility.singleLineHeight
			);

			EditorGUI.BeginChangeCheck();
			string valueName = EditorGUI.TextField(nameRect, GUIContent.none, value.name);
			if (!EditorGUI.EndChangeCheck() || value.name == valueName)
			{
				return;
			}

			value.name = valueName;
			AssetDatabase.SaveAssets();
			EditorGUIUtility.PingObject(value);
		}

		private Editor GetEditor(T element)
		{
			if (_editorsMap.TryGetValue(element, out Editor editor))
			{
				return editor;
			}
			
			editor = Editor.CreateEditor(element, s_editorDefaultType);
			_editorsMap.Add(element, editor);
			return editor;
		}
	}
}