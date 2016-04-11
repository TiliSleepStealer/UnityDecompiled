using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
	internal class TargetChoiceHandler
	{
		internal delegate void TargetChoiceMenuFunction(SerializedProperty property, UnityEngine.Object target);

		internal static void DuplicateArrayElement(object userData)
		{
			SerializedProperty serializedProperty = (SerializedProperty)userData;
			serializedProperty.DuplicateCommand();
			serializedProperty.serializedObject.ApplyModifiedProperties();
			EditorUtility.ForceReloadInspectors();
		}

		internal static void DeleteArrayElement(object userData)
		{
			SerializedProperty serializedProperty = (SerializedProperty)userData;
			serializedProperty.DeleteCommand();
			serializedProperty.serializedObject.ApplyModifiedProperties();
			EditorUtility.ForceReloadInspectors();
		}

		internal static void SetPrefabOverride(object userData)
		{
			SerializedProperty serializedProperty = (SerializedProperty)userData;
			serializedProperty.prefabOverride = false;
			serializedProperty.serializedObject.ApplyModifiedProperties();
			EditorUtility.ForceReloadInspectors();
		}

		internal static void SetToValueOfTarget(SerializedProperty property, UnityEngine.Object target)
		{
			property.SetToValueOfTarget(target);
			property.serializedObject.ApplyModifiedProperties();
			EditorUtility.ForceReloadInspectors();
		}

		private static void TargetChoiceForwardFunction(object userData)
		{
			PropertyAndTargetHandler propertyAndTargetHandler = (PropertyAndTargetHandler)userData;
			propertyAndTargetHandler.function(propertyAndTargetHandler.property, propertyAndTargetHandler.target);
		}

		internal static void AddSetToValueOfTargetMenuItems(GenericMenu menu, SerializedProperty property, TargetChoiceHandler.TargetChoiceMenuFunction func)
		{
			SerializedProperty property2 = property.serializedObject.FindProperty(property.propertyPath);
			UnityEngine.Object[] targetObjects = property.serializedObject.targetObjects;
			List<string> list = new List<string>();
			UnityEngine.Object[] array = targetObjects;
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object @object = array[i];
				string text = "Set to Value of " + @object.name;
				if (list.Contains(text))
				{
					int num = 1;
					while (true)
					{
						text = string.Concat(new object[]
						{
							"Set to Value of ",
							@object.name,
							" (",
							num,
							")"
						});
						if (!list.Contains(text))
						{
							break;
						}
						num++;
					}
				}
				list.Add(text);
				menu.AddItem(EditorGUIUtility.TextContent(text), false, new GenericMenu.MenuFunction2(TargetChoiceHandler.TargetChoiceForwardFunction), new PropertyAndTargetHandler(property2, @object, func));
			}
		}
	}
}
