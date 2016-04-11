using System;
using UnityEngine;

namespace UnityEditor
{
	internal class EmissionModuleUI : ModuleUI
	{
		private enum EmissionTypes
		{
			Time,
			Distance
		}

		private class Texts
		{
			public GUIContent rate = new GUIContent("Rate", "The number of particles emitted per second (Time) or per distance unit (Distance)");

			public GUIContent burst = new GUIContent("Bursts", "Emission of extra particles at specific times during the duration of the system.");
		}

		private const int k_MaxNumBursts = 4;

		private SerializedProperty m_Type;

		public SerializedMinMaxCurve m_Rate;

		private SerializedProperty[] m_BurstTime = new SerializedProperty[4];

		private SerializedProperty[] m_BurstParticleMinCount = new SerializedProperty[4];

		private SerializedProperty[] m_BurstParticleMaxCount = new SerializedProperty[4];

		private SerializedProperty m_BurstCount;

		private string[] m_GuiNames = new string[]
		{
			"Time",
			"Distance"
		};

		private static EmissionModuleUI.Texts s_Texts;

		public EmissionModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName) : base(owner, o, "EmissionModule", displayName)
		{
			this.m_ToolTip = "Emission of the emitter. This controls the rate at which particles are emitted as well as burst emissions.";
		}

		protected override void Init()
		{
			if (EmissionModuleUI.s_Texts == null)
			{
				EmissionModuleUI.s_Texts = new EmissionModuleUI.Texts();
			}
			if (this.m_BurstCount != null)
			{
				return;
			}
			this.m_Type = base.GetProperty("m_Type");
			this.m_Rate = new SerializedMinMaxCurve(this, EmissionModuleUI.s_Texts.rate, "rate");
			this.m_BurstTime[0] = base.GetProperty("time0");
			this.m_BurstTime[1] = base.GetProperty("time1");
			this.m_BurstTime[2] = base.GetProperty("time2");
			this.m_BurstTime[3] = base.GetProperty("time3");
			this.m_BurstParticleMinCount[0] = base.GetProperty("cnt0");
			this.m_BurstParticleMinCount[1] = base.GetProperty("cnt1");
			this.m_BurstParticleMinCount[2] = base.GetProperty("cnt2");
			this.m_BurstParticleMinCount[3] = base.GetProperty("cnt3");
			this.m_BurstParticleMaxCount[0] = base.GetProperty("cntmax0");
			this.m_BurstParticleMaxCount[1] = base.GetProperty("cntmax1");
			this.m_BurstParticleMaxCount[2] = base.GetProperty("cntmax2");
			this.m_BurstParticleMaxCount[3] = base.GetProperty("cntmax3");
			this.m_BurstCount = base.GetProperty("m_BurstCount");
		}

		public override void OnInspectorGUI(ParticleSystem s)
		{
			ModuleUI.GUIMinMaxCurve(EmissionModuleUI.s_Texts.rate, this.m_Rate);
			ModuleUI.GUIPopup(GUIContent.none, this.m_Type, this.m_GuiNames);
			if (this.m_Type.intValue != 1)
			{
				this.DoBurstGUI(s);
			}
		}

		private void DoBurstGUI(ParticleSystem s)
		{
			EditorGUILayout.Space();
			Rect controlRect = ModuleUI.GetControlRect(13);
			GUI.Label(controlRect, EmissionModuleUI.s_Texts.burst, ParticleSystemStyles.Get().label);
			float num = 20f;
			float num2 = 40f;
			float num3 = (num2 + num) * 3f + num - 1f;
			float num4 = controlRect.width - num3;
			num4 = Mathf.Min(num4, EditorGUIUtility.labelWidth);
			int num5 = this.m_BurstCount.intValue;
			Rect position = new Rect(controlRect.x + num4, controlRect.y, num3, 3f);
			GUI.Label(position, GUIContent.none, ParticleSystemStyles.Get().line);
			Rect controlRect2 = new Rect(controlRect.x + num + num4, controlRect.y, num2 + num, controlRect.height);
			GUI.Label(controlRect2, "Time", ParticleSystemStyles.Get().label);
			controlRect2.x += num + num2;
			GUI.Label(controlRect2, "Min", ParticleSystemStyles.Get().label);
			controlRect2.x += num + num2;
			GUI.Label(controlRect2, "Max", ParticleSystemStyles.Get().label);
			position.y += 12f;
			GUI.Label(position, GUIContent.none, ParticleSystemStyles.Get().line);
			float duration = s.duration;
			int num6 = num5;
			for (int i = 0; i < num5; i++)
			{
				SerializedProperty serializedProperty = this.m_BurstTime[i];
				SerializedProperty serializedProperty2 = this.m_BurstParticleMinCount[i];
				SerializedProperty serializedProperty3 = this.m_BurstParticleMaxCount[i];
				controlRect = ModuleUI.GetControlRect(13);
				controlRect2 = new Rect(controlRect.x + num4, controlRect.y, num + num2, controlRect.height);
				float num7 = ModuleUI.FloatDraggable(controlRect2, serializedProperty, 1f, num, "n2");
				if (num7 < 0f)
				{
					serializedProperty.floatValue = 0f;
				}
				if (num7 > duration)
				{
					serializedProperty.floatValue = duration;
				}
				int intValue = serializedProperty2.intValue;
				int intValue2 = serializedProperty3.intValue;
				controlRect2.x += controlRect2.width;
				serializedProperty2.intValue = ModuleUI.IntDraggable(controlRect2, null, intValue, num);
				controlRect2.x += controlRect2.width;
				serializedProperty3.intValue = ModuleUI.IntDraggable(controlRect2, null, intValue2, num);
				if (i == num5 - 1)
				{
					controlRect2.x = position.xMax - 12f;
					if (ModuleUI.MinusButton(controlRect2))
					{
						num5--;
					}
				}
			}
			if (num5 < 4)
			{
				controlRect2 = ModuleUI.GetControlRect(13);
				controlRect2.xMin = controlRect2.xMax - 12f;
				if (ModuleUI.PlusButton(controlRect2))
				{
					num5++;
				}
			}
			if (num5 != num6)
			{
				this.m_BurstCount.intValue = num5;
			}
		}

		public override void UpdateCullingSupportedString(ref string text)
		{
			this.Init();
			if (this.m_Type.intValue == 1)
			{
				text += "\n\tEmission is distance based.";
			}
		}
	}
}
