using System;
using UnityEngine;

namespace UnityEditor
{
	internal class RotationByVelocityModuleUI : ModuleUI
	{
		private class Texts
		{
			public GUIContent velocityRange = new GUIContent("Speed Range", "Remaps speed in the defined range to an angular velocity.");

			public GUIContent rotation = new GUIContent("Angular Velocity", "Controls the angular velocity of each particle based on its speed.");

			public GUIContent separateAxes = new GUIContent("Separate Axes", "If enabled, you can control the angular velocity limit separately for each axis.");

			public GUIContent x = new GUIContent("X");

			public GUIContent y = new GUIContent("Y");

			public GUIContent z = new GUIContent("Z");
		}

		private static RotationByVelocityModuleUI.Texts s_Texts;

		private SerializedMinMaxCurve m_X;

		private SerializedMinMaxCurve m_Y;

		private SerializedMinMaxCurve m_Z;

		private SerializedProperty m_SeparateAxes;

		private SerializedProperty m_Range;

		public RotationByVelocityModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName) : base(owner, o, "RotationBySpeedModule", displayName)
		{
			this.m_ToolTip = "Controls the angular velocity of each particle based on its speed.";
		}

		protected override void Init()
		{
			if (this.m_Z != null)
			{
				return;
			}
			if (RotationByVelocityModuleUI.s_Texts == null)
			{
				RotationByVelocityModuleUI.s_Texts = new RotationByVelocityModuleUI.Texts();
			}
			this.m_X = new SerializedMinMaxCurve(this, RotationByVelocityModuleUI.s_Texts.x, "x", ModuleUI.kUseSignedRange);
			this.m_Y = new SerializedMinMaxCurve(this, RotationByVelocityModuleUI.s_Texts.y, "y", ModuleUI.kUseSignedRange);
			this.m_Z = new SerializedMinMaxCurve(this, RotationByVelocityModuleUI.s_Texts.z, "curve", ModuleUI.kUseSignedRange);
			this.m_X.m_RemapValue = 57.29578f;
			this.m_Y.m_RemapValue = 57.29578f;
			this.m_Z.m_RemapValue = 57.29578f;
			this.m_SeparateAxes = base.GetProperty("separateAxes");
			this.m_Range = base.GetProperty("range");
		}

		public override void OnInspectorGUI(ParticleSystem s)
		{
			if (RotationByVelocityModuleUI.s_Texts == null)
			{
				RotationByVelocityModuleUI.s_Texts = new RotationByVelocityModuleUI.Texts();
			}
			EditorGUI.BeginChangeCheck();
			bool flag = ModuleUI.GUIToggle(RotationByVelocityModuleUI.s_Texts.separateAxes, this.m_SeparateAxes);
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
				this.m_Z.m_DisplayName = RotationByVelocityModuleUI.s_Texts.z;
				base.GUITripleMinMaxCurve(GUIContent.none, RotationByVelocityModuleUI.s_Texts.x, this.m_X, RotationByVelocityModuleUI.s_Texts.y, this.m_Y, RotationByVelocityModuleUI.s_Texts.z, this.m_Z, null);
			}
			else
			{
				this.m_Z.m_DisplayName = RotationByVelocityModuleUI.s_Texts.rotation;
				ModuleUI.GUIMinMaxCurve(RotationByVelocityModuleUI.s_Texts.rotation, this.m_Z);
			}
			ModuleUI.GUIMinMaxRange(RotationByVelocityModuleUI.s_Texts.velocityRange, this.m_Range);
		}

		public override void UpdateCullingSupportedString(ref string text)
		{
			text += "\n\tRotation by Speed is enabled.";
		}
	}
}
