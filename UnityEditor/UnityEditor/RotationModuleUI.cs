using System;
using UnityEngine;

namespace UnityEditor
{
	internal class RotationModuleUI : ModuleUI
	{
		private class Texts
		{
			public GUIContent rotation = new GUIContent("Angular Velocity", "Controls the angular velocity of each particle during its lifetime.");

			public GUIContent separateAxes = new GUIContent("Separate Axes", "If enabled, you can control the angular velocity limit separately for each axis.");

			public GUIContent x = new GUIContent("X");

			public GUIContent y = new GUIContent("Y");

			public GUIContent z = new GUIContent("Z");
		}

		private SerializedMinMaxCurve m_X;

		private SerializedMinMaxCurve m_Y;

		private SerializedMinMaxCurve m_Z;

		private SerializedProperty m_SeparateAxes;

		private static RotationModuleUI.Texts s_Texts;

		public RotationModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName) : base(owner, o, "RotationModule", displayName)
		{
			this.m_ToolTip = "Controls the angular velocity of each particle during its lifetime.";
		}

		protected override void Init()
		{
			if (this.m_Z != null)
			{
				return;
			}
			if (RotationModuleUI.s_Texts == null)
			{
				RotationModuleUI.s_Texts = new RotationModuleUI.Texts();
			}
			this.m_X = new SerializedMinMaxCurve(this, RotationModuleUI.s_Texts.x, "x", ModuleUI.kUseSignedRange);
			this.m_Y = new SerializedMinMaxCurve(this, RotationModuleUI.s_Texts.y, "y", ModuleUI.kUseSignedRange);
			this.m_Z = new SerializedMinMaxCurve(this, RotationModuleUI.s_Texts.z, "curve", ModuleUI.kUseSignedRange);
			this.m_X.m_RemapValue = 57.29578f;
			this.m_Y.m_RemapValue = 57.29578f;
			this.m_Z.m_RemapValue = 57.29578f;
			this.m_SeparateAxes = base.GetProperty("separateAxes");
		}

		public override void OnInspectorGUI(ParticleSystem s)
		{
			if (RotationModuleUI.s_Texts == null)
			{
				RotationModuleUI.s_Texts = new RotationModuleUI.Texts();
			}
			EditorGUI.BeginChangeCheck();
			bool flag = ModuleUI.GUIToggle(RotationModuleUI.s_Texts.separateAxes, this.m_SeparateAxes);
			if (EditorGUI.EndChangeCheck())
			{
				if (flag)
				{
					this.m_Z.RemoveCurveFromEditor();
				}
				else
				{
					this.m_X.RemoveCurveFromEditor();
					this.m_Y.RemoveCurveFromEditor();
					this.m_Z.RemoveCurveFromEditor();
				}
			}
			if (flag)
			{
				this.m_Z.m_DisplayName = RotationModuleUI.s_Texts.z;
				base.GUITripleMinMaxCurve(GUIContent.none, RotationModuleUI.s_Texts.x, this.m_X, RotationModuleUI.s_Texts.y, this.m_Y, RotationModuleUI.s_Texts.z, this.m_Z, null);
			}
			else
			{
				this.m_Z.m_DisplayName = RotationModuleUI.s_Texts.rotation;
				ModuleUI.GUIMinMaxCurve(RotationModuleUI.s_Texts.rotation, this.m_Z);
			}
		}

		public override void UpdateCullingSupportedString(ref string text)
		{
			this.Init();
			if (!this.m_X.SupportsProcedural() || !this.m_Y.SupportsProcedural() || !this.m_Z.SupportsProcedural())
			{
				text += "\n\tLifetime rotation curve uses too many keys.";
			}
		}
	}
}
