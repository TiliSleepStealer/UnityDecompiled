using System;
using UnityEngine;

namespace UnityEditor
{
	internal sealed class EditorGUIInternal : GUI
	{
		internal static Rect GetTooltipRect()
		{
			return GUI.tooltipRect;
		}

		internal static string GetMouseTooltip()
		{
			return GUI.mouseTooltip;
		}

		internal static bool DoToggleForward(Rect position, int id, bool value, GUIContent content, GUIStyle style)
		{
			Event current = Event.current;
			if (current.MainActionKeyForControl(id))
			{
				value = !value;
				current.Use();
				GUI.changed = true;
			}
			if (EditorGUI.showMixedValue)
			{
				style = EditorStyles.toggleMixed;
			}
			EventType type = current.type;
			bool flag = current.type == EventType.MouseDown && current.button != 0;
			if (flag)
			{
				current.type = EventType.Ignore;
			}
			bool result = GUI.DoToggle(position, id, !EditorGUI.showMixedValue && value, content, style.m_Ptr);
			if (flag)
			{
				current.type = type;
			}
			else if (current.type != type)
			{
				GUIUtility.keyboardControl = id;
			}
			return result;
		}

		internal static Vector2 DoBeginScrollViewForward(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
		{
			return GUI.DoBeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
		}

		internal static void BeginWindowsForward(int skinMode, int editorWindowInstanceID)
		{
			GUI.BeginWindows(skinMode, editorWindowInstanceID);
		}

		internal static void AssetPopup<T>(SerializedProperty serializedProperty, GUIContent content, string fileExtension) where T : UnityEngine.Object, new()
		{
			AssetPopupBackend.AssetPopup<T>(serializedProperty, content, fileExtension);
		}
	}
}
