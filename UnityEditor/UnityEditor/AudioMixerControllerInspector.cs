using System;
using UnityEditor.Audio;
using UnityEngine;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(AudioMixerController))]
	internal class AudioMixerControllerInspector : Editor
	{
		private static class Texts
		{
			public static GUIContent m_EnableSuspendLabel = new GUIContent("Auto Mixer Suspend", "Enables/disables suspending of processing in order to save CPU when the RMS signal level falls under the defined threshold (in dB). Mixers resume processing when an AudioSource referencing them starts playing again.");

			public static GUIContent m_SuspendThresholdLabel = new GUIContent("    Threshold Volume", "The level of the Master Group at which the mixer suspends processing in order to save CPU. Mixers resume processing when an AudioSource referencing them starts playing again.");

			public static string dB = "dB";
		}

		private SerializedProperty m_EnableSuspend;

		private SerializedProperty m_SuspendThreshold;

		public void OnEnable()
		{
			this.m_SuspendThreshold = base.serializedObject.FindProperty("m_SuspendThreshold");
			this.m_EnableSuspend = base.serializedObject.FindProperty("m_EnableSuspend");
		}

		public override void OnInspectorGUI()
		{
			base.serializedObject.Update();
			EditorGUILayout.PropertyField(this.m_EnableSuspend, AudioMixerControllerInspector.Texts.m_EnableSuspendLabel, new GUILayoutOption[0]);
			EditorGUI.BeginDisabledGroup(!this.m_EnableSuspend.boolValue || this.m_EnableSuspend.hasMultipleDifferentValues);
			EditorGUI.BeginChangeCheck();
			EditorGUI.s_UnitString = AudioMixerControllerInspector.Texts.dB;
			float num = this.m_SuspendThreshold.floatValue;
			num = EditorGUILayout.PowerSlider(AudioMixerControllerInspector.Texts.m_SuspendThresholdLabel, num, AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume(), 1f, new GUILayoutOption[0]);
			EditorGUI.s_UnitString = null;
			if (EditorGUI.EndChangeCheck())
			{
				this.m_SuspendThreshold.floatValue = num;
			}
			EditorGUI.EndDisabledGroup();
			base.serializedObject.ApplyModifiedProperties();
		}
	}
}
