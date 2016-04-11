using System;
using UnityEngine;

namespace UnityEditor
{
	internal class ForceModuleUI : ModuleUI
	{
		private class Texts
		{
			public GUIContent x = new GUIContent("X");

			public GUIContent y = new GUIContent("Y");

			public GUIContent z = new GUIContent("Z");

			public GUIContent randomizePerFrame = new GUIContent("Randomize", "Randomize force every frame. Only available when using random between two constants or random between two curves.");

			public GUIContent space = new GUIContent("Space", "Specifies if the force values are in local space (rotated with the transform) or world space.");

			public string[] spaces = new string[]
			{
				"Local",
				"World"
			};
		}

		private SerializedMinMaxCurve m_X;

		private SerializedMinMaxCurve m_Y;

		private SerializedMinMaxCurve m_Z;

		private SerializedProperty m_RandomizePerFrame;

		private SerializedProperty m_InWorldSpace;

		private static ForceModuleUI.Texts s_Texts;

		public ForceModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName) : base(owner, o, "ForceModule", displayName)
		{
			this.m_ToolTip = "Controls the force of each particle during its lifetime.";
		}

		protected override void Init()
		{
			if (this.m_X != null)
			{
				return;
			}
			if (ForceModuleUI.s_Texts == null)
			{
				ForceModuleUI.s_Texts = new ForceModuleUI.Texts();
			}
			this.m_X = new SerializedMinMaxCurve(this, ForceModuleUI.s_Texts.x, "x", ModuleUI.kUseSignedRange);
			this.m_Y = new SerializedMinMaxCurve(this, ForceModuleUI.s_Texts.y, "y", ModuleUI.kUseSignedRange);
			this.m_Z = new SerializedMinMaxCurve(this, ForceModuleUI.s_Texts.z, "z", ModuleUI.kUseSignedRange);
			this.m_RandomizePerFrame = base.GetProperty("randomizePerFrame");
			this.m_InWorldSpace = base.GetProperty("inWorldSpace");
		}

		public override void OnInspectorGUI(ParticleSystem s)
		{
			if (ForceModuleUI.s_Texts == null)
			{
				ForceModuleUI.s_Texts = new ForceModuleUI.Texts();
			}
			MinMaxCurveState state = this.m_X.state;
			base.GUITripleMinMaxCurve(GUIContent.none, ForceModuleUI.s_Texts.x, this.m_X, ForceModuleUI.s_Texts.y, this.m_Y, ForceModuleUI.s_Texts.z, this.m_Z, this.m_RandomizePerFrame);
			ModuleUI.GUIBoolAsPopup(ForceModuleUI.s_Texts.space, this.m_InWorldSpace, ForceModuleUI.s_Texts.spaces);
			EditorGUI.BeginDisabledGroup(state != MinMaxCurveState.k_TwoScalars && state != MinMaxCurveState.k_TwoCurves);
			ModuleUI.GUIToggle(ForceModuleUI.s_Texts.randomizePerFrame, this.m_RandomizePerFrame);
			EditorGUI.EndDisabledGroup();
		}

		public override void UpdateCullingSupportedString(ref string text)
		{
			this.Init();
			if (!this.m_X.SupportsProcedural() || !this.m_Y.SupportsProcedural() || !this.m_Z.SupportsProcedural())
			{
				text += "\n\tLifetime force curves use too many keys.";
			}
			if (this.m_RandomizePerFrame.boolValue)
			{
				text += "\n\tLifetime force curves use random per frame.";
			}
		}
	}
}
