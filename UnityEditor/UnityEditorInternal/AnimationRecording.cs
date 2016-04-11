using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
	internal class AnimationRecording
	{
		private static bool HasAnyRecordableModifications(GameObject root, UndoPropertyModification[] modifications)
		{
			for (int i = 0; i < modifications.Length; i++)
			{
				if (modifications[i].currentValue == null || !(modifications[i].currentValue.target is Animator))
				{
					EditorCurveBinding editorCurveBinding;
					if (AnimationUtility.PropertyModificationToEditorCurveBinding(modifications[i].previousValue, root, out editorCurveBinding) != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static PropertyModification FindPropertyModification(GameObject root, UndoPropertyModification[] modifications, EditorCurveBinding binding)
		{
			for (int i = 0; i < modifications.Length; i++)
			{
				if (modifications[i].currentValue == null || !(modifications[i].currentValue.target is Animator))
				{
					EditorCurveBinding lhs;
					AnimationUtility.PropertyModificationToEditorCurveBinding(modifications[i].previousValue, root, out lhs);
					if (lhs == binding)
					{
						return modifications[i].previousValue;
					}
				}
			}
			return null;
		}

		public static UndoPropertyModification[] Process(AnimationWindowState state, UndoPropertyModification[] modifications)
		{
			GameObject activeRootGameObject = state.activeRootGameObject;
			AnimationClip activeAnimationClip = state.activeAnimationClip;
			Animator component = activeRootGameObject.GetComponent<Animator>();
			if (!AnimationRecording.HasAnyRecordableModifications(activeRootGameObject, modifications))
			{
				return modifications;
			}
			List<UndoPropertyModification> list = new List<UndoPropertyModification>();
			for (int i = 0; i < modifications.Length; i++)
			{
				EditorCurveBinding binding = default(EditorCurveBinding);
				PropertyModification previousValue = modifications[i].previousValue;
				Type type = AnimationUtility.PropertyModificationToEditorCurveBinding(previousValue, activeRootGameObject, out binding);
				if (type != null && type != typeof(Animator))
				{
					if (component != null && component.isHuman && binding.type == typeof(Transform) && component.IsBoneTransform(previousValue.target as Transform))
					{
						Debug.LogWarning("Keyframing for humanoid rig is not supported!", previousValue.target as Transform);
					}
					else
					{
						AnimationMode.AddPropertyModification(binding, previousValue, modifications[i].keepPrefabOverride);
						EditorCurveBinding[] array = RotationCurveInterpolation.RemapAnimationBindingForAddKey(binding, activeAnimationClip);
						if (array != null)
						{
							for (int j = 0; j < array.Length; j++)
							{
								AnimationRecording.AddKey(state, array[j], type, AnimationRecording.FindPropertyModification(activeRootGameObject, modifications, array[j]));
							}
						}
						else
						{
							AnimationRecording.AddKey(state, binding, type, previousValue);
						}
					}
				}
				else
				{
					list.Add(modifications[i]);
				}
			}
			return list.ToArray();
		}

		private static bool ValueFromPropertyModification(PropertyModification modification, EditorCurveBinding binding, out object outObject)
		{
			if (modification == null)
			{
				outObject = null;
				return false;
			}
			if (binding.isPPtrCurve)
			{
				outObject = modification.objectReference;
				return true;
			}
			float num;
			if (float.TryParse(modification.value, out num))
			{
				outObject = num;
				return true;
			}
			outObject = null;
			return false;
		}

		private static void AddKey(AnimationWindowState state, EditorCurveBinding binding, Type type, PropertyModification modification)
		{
			GameObject activeRootGameObject = state.activeRootGameObject;
			AnimationClip activeAnimationClip = state.activeAnimationClip;
			AnimationWindowCurve animationWindowCurve = new AnimationWindowCurve(activeAnimationClip, binding, type);
			object currentValue = CurveBindingUtility.GetCurrentValue(activeRootGameObject, binding);
			if (animationWindowCurve.length == 0)
			{
				object value = null;
				if (!AnimationRecording.ValueFromPropertyModification(modification, binding, out value))
				{
					value = currentValue;
				}
				if (state.frame != 0)
				{
					AnimationWindowUtility.AddKeyframeToCurve(animationWindowCurve, value, type, AnimationKeyTime.Frame(0, activeAnimationClip.frameRate));
				}
			}
			AnimationWindowUtility.AddKeyframeToCurve(animationWindowCurve, currentValue, type, AnimationKeyTime.Frame(state.frame, activeAnimationClip.frameRate));
			state.SaveCurve(animationWindowCurve);
		}
	}
}
