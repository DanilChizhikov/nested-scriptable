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
		private const string CreateFieldName = "Create Type";
		private const string ElementTemplate = "Element {0}";
		private const float ObjectWeight = 1f;
		private const float NameWeight = 1f;
		private const float SumWeight = ObjectWeight + NameWeight;
		private const float ColumnSpace = 6f;

		private readonly Dictionary<T, Editor> _editors;

		private int _lastCreateIndex = -1;
		private int _lastChangedIndex = -1;

		public ScriptableDrawerMediator(string label, FieldInfo fieldInfo, ReorderableList reorderableList,
			ScriptableObject scriptableObject) : base(label, fieldInfo, reorderableList, scriptableObject)
		{
			reorderableList.drawElementCallback += DrawElement;
			reorderableList.drawHeaderCallback += DrawHeader;
			reorderableList.elementHeightCallback += CalculateElementHeight;
			reorderableList.onChangedCallback += OnChangedCallback;
			reorderableList.drawFooterCallback += DrawFooterCallback;
			_editors = new Dictionary<T, Editor>();
		}

		private void OnChangedCallback(ReorderableList list)
		{
			_lastChangedIndex = list.index;
		}

		private void AddElement()
		{
			string createdTypeName = ScriptableTypeController<T>.Names[_lastCreateIndex];
			_lastCreateIndex = -1;
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
		}

		private float CalculateElementHeight(int index)
		{
			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			return height;
		}
		
		private void DrawFooterCallback(Rect rect)
		{
			if (_lastCreateIndex > ScriptableTypeController<T>.Names.Length)
			{
				_lastCreateIndex = -1;
			}

			float buttonWidth = Mathf.Min((rect.width - 30f) / 2f, 50f);
			float popupWidth = rect.width - 30f - buttonWidth * 2f;
			var selectPopupRect = new Rect(rect.x, rect.y, popupWidth, EditorGUIUtility.singleLineHeight);
			_lastCreateIndex = EditorGUI.Popup(selectPopupRect, CreateFieldName, _lastCreateIndex, ScriptableTypeController<T>.Names);
			var addButtonRect = new Rect(rect.x + popupWidth + 10f, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
			if (GUI.Button(addButtonRect, "+") && CanAdd())
			{
				AddElement();
			}

			var removeButtonRect = new Rect(rect.x + popupWidth + 20f + buttonWidth, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight);
			if (GUI.Button(removeButtonRect, "-"))
			{
				RemoveElement();
			}
		}

		private bool CanAdd() =>
			_lastCreateIndex >= 0 && _lastCreateIndex <ScriptableTypeController<T>.Names.Length;

		private void DrawHeader(Rect rect) =>
			EditorGUI.LabelField(rect, Label);

		private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			float weightWidth = (rect.width - EditorGUIUtility.labelWidth - ColumnSpace) / SumWeight;
			T value = DrawObjectField(rect, weightWidth, index, out float usedWidth);
			if (value != null)
			{
				DrawNameField(rect, weightWidth, value, usedWidth);
			}
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

		private static void DrawNameField(Rect rect, float weightWidth, T value, float usedWidth)
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

		private void RemoveElement()
		{
			if (_lastCreateIndex < 0)
			{
				return;
			}
			
			T removedObject = List.list[_lastChangedIndex] as T;
			List.list.RemoveAt(_lastChangedIndex);
			if (removedObject == null)
			{
				return;
			}

			if (_editors.Remove(removedObject, out Editor editor))
			{
				Object.DestroyImmediate(editor);
			}

			Object.DestroyImmediate(removedObject, true);
			AssetDatabase.SaveAssets();
			_lastChangedIndex = -1;
		}
	}
}