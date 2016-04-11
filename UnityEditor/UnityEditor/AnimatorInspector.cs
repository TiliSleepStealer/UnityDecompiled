using System;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(Animator))]
	internal class AnimatorInspector : Editor
	{
		private class Styles
		{
			public GUIContent applyRootMotion = new GUIContent(EditorGUIUtility.TextContent("Apply Root Motion"));

			public GUIContent updateMode = new GUIContent(EditorGUIUtility.TextContent("Update Mode"));

			public GUIContent cullingMode = new GUIContent(EditorGUIUtility.TextContent("Culling Mode"));

			public Styles()
			{
				this.applyRootMotion.tooltip = "Automatically move the object using the root motion from the animations";
				this.updateMode.tooltip = "Controls when and how often the Animator is updated";
				this.cullingMode.tooltip = "Controls what is updated when the object has been culled";
			}
		}

		private SerializedProperty m_Avatar;

		private SerializedProperty m_ApplyRootMotion;

		private SerializedProperty m_CullingMode;

		private SerializedProperty m_WarningMessage;

		private AnimBool m_ShowWarningMessage = new AnimBool();

		private bool m_IsRootPositionOrRotationControlledByCurves;

		private static AnimatorInspector.Styles styles;

		private bool IsWarningMessageEmpty
		{
			get
			{
				return this.m_WarningMessage != null && this.m_WarningMessage.stringValue.Length > 0;
			}
		}

		private string WarningMessage
		{
			get
			{
				return (this.m_WarningMessage == null) ? string.Empty : this.m_WarningMessage.stringValue;
			}
		}

		private void Init()
		{
			if (AnimatorInspector.styles == null)
			{
				AnimatorInspector.styles = new AnimatorInspector.Styles();
			}
			this.InitShowOptions();
		}

		private void InitShowOptions()
		{
			this.m_ShowWarningMessage.value = this.IsWarningMessageEmpty;
			this.m_ShowWarningMessage.valueChanged.AddListener(new UnityAction(base.Repaint));
		}

		private void UpdateShowOptions()
		{
			this.m_ShowWarningMessage.target = this.IsWarningMessageEmpty;
		}

		private void OnEnable()
		{
			this.m_Avatar = base.serializedObject.FindProperty("m_Avatar");
			this.m_ApplyRootMotion = base.serializedObject.FindProperty("m_ApplyRootMotion");
			this.m_CullingMode = base.serializedObject.FindProperty("m_CullingMode");
			this.m_WarningMessage = base.serializedObject.FindProperty("m_WarningMessage");
			this.Init();
		}

		public override void OnInspectorGUI()
		{
			bool flag = base.targets.Length > 1;
			Animator animator = this.target as Animator;
			base.serializedObject.UpdateIfDirtyOrScript();
			this.UpdateShowOptions();
			EditorGUI.BeginChangeCheck();
			RuntimeAnimatorController runtimeAnimatorController = EditorGUILayout.ObjectField("Controller", animator.runtimeAnimatorController, typeof(RuntimeAnimatorController), false, new GUILayoutOption[0]) as RuntimeAnimatorController;
			bool flag2 = EditorGUI.EndChangeCheck();
			EditorGUILayout.PropertyField(this.m_Avatar, new GUILayoutOption[0]);
			if (animator.supportsOnAnimatorMove && !flag)
			{
				EditorGUILayout.LabelField("Apply Root Motion", "Handled by Script", new GUILayoutOption[0]);
			}
			else
			{
				EditorGUILayout.PropertyField(this.m_ApplyRootMotion, AnimatorInspector.styles.applyRootMotion, new GUILayoutOption[0]);
				if (Event.current.type == EventType.Layout)
				{
					this.m_IsRootPositionOrRotationControlledByCurves = animator.isRootPositionOrRotationControlledByCurves;
				}
				if (!this.m_ApplyRootMotion.boolValue && this.m_IsRootPositionOrRotationControlledByCurves)
				{
					EditorGUILayout.HelpBox("Root position or rotation are controlled by curves", MessageType.Info, true);
				}
			}
			animator.updateMode = (AnimatorUpdateMode)EditorGUILayout.EnumPopup("Update Mode", animator.updateMode, new GUILayoutOption[0]);
			EditorGUILayout.PropertyField(this.m_CullingMode, AnimatorInspector.styles.cullingMode, new GUILayoutOption[0]);
			if (!flag)
			{
				EditorGUILayout.HelpBox(animator.GetStats(), MessageType.Info, true);
			}
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowWarningMessage.faded))
			{
				EditorGUILayout.HelpBox(this.WarningMessage, MessageType.Warning, true);
			}
			EditorGUILayout.EndFadeGroup();
			base.serializedObject.ApplyModifiedProperties();
			if (flag2)
			{
				Undo.RecordObject(animator, "Changed AnimatorController");
				animator.runtimeAnimatorController = runtimeAnimatorController;
				AnimationWindowUtility.ControllerChanged();
			}
		}
	}
}
