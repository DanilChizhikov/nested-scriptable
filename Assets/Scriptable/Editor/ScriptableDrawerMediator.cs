using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MBSCore.Scriptable
{
	internal abstract class ScriptableDrawerMediator
	{
		private static readonly MethodInfo s_createMediatorGenericMethod =
			typeof(ScriptableDrawerMediator)
				.GetMethod(nameof(CreateMediatorGeneric), BindingFlags.Static | BindingFlags.NonPublic);
		
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

		private static ScriptableDrawerMediator CreateMediatorGeneric<T>(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
			ScriptableObject scriptableObject) where T : ScriptableObject
		{
			return new ScriptableDrawerMediator<T>(label, fieldInfo, reorderableList, scriptableObject);
		}
	}

	internal sealed class ScriptableDrawerMediator<T> : ScriptableDrawerMediator where T : ScriptableObject
	{
		private const string ElementTemplate = "Element {0}";
		private const float ObjectWeight = 1f;
		private const float NameWeight = 1f;
		private const float SumWeight = ObjectWeight + NameWeight;
		private const float ColumnSpace = 6f;

		public ScriptableDrawerMediator(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
			ScriptableObject scriptableObject) : base(label, fieldInfo, reorderableList, scriptableObject)
		{
			reorderableList.drawElementCallback += DrawElement;
			reorderableList.drawHeaderCallback += DrawHeader;
			reorderableList.elementHeightCallback += CalculateElementHeight;
			reorderableList.onAddDropdownCallback += ShowDropdownElements;
			reorderableList.onRemoveCallback += RemoveElement;
		}

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			float weightWidth = (rect.width - EditorGUIUtility.labelWidth - ColumnSpace) / SumWeight;
			T value = DrawObjectField(rect, weightWidth, index, out float usedWidth);
			if (value != null)
			{
				DrawNameField(rect, weightWidth, value, usedWidth);
			}
		}
		
		private void DrawHeader(Rect rect) =>
			EditorGUI.LabelField(rect, Label);
		
		private float CalculateElementHeight(int index)
		{
			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			return height;
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

		private T DrawObjectField(Rect rect, float weightWidth, int index, out float usedWidth)
		{
			usedWidth = -1;
			T value = List.list[index] as T;
			if (value == null)
			{
				return null;
			}
			
			Rect elementRect = rect;
			elementRect.height = EditorGUIUtility.singleLineHeight;
			elementRect.width = weightWidth * ObjectWeight + EditorGUIUtility.labelWidth;
			using (new EditorGUI.DisabledScope(true))
			{
				value = EditorGUI.ObjectField(elementRect, new GUIContent(string.Format(ElementTemplate, index)),
					value, typeof(T), false) as T;
			}

			usedWidth = elementRect.width;
			return value;
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

		private void RemoveElement(ReorderableList list)
		{
			int listCount = list.list.Count;
			T removedObject = List.list[list.index] as T;
			var newList = new List<T>(listCount - 1);
			for (int i = 0; i < listCount; i++)
			{
				if (i == list.index)
				{
					continue;
				}
				
				newList.Add(list.list[i] as T);
			}
			
			if(list.list is Array)
			{
				FieldInfo.SetValue(Target, newList.ToArray());
			}
			else
			{
				FieldInfo.SetValue(Target, newList);
			}

			if (removedObject == null)
			{
				return;
			}

			Object.DestroyImmediate(removedObject, true);
			AssetDatabase.SaveAssets();
		}
	}
}