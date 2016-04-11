using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.Audio
{
	internal sealed class MixerEffectDefinitions
	{
		private static readonly List<MixerEffectDefinition> s_MixerEffectDefinitions = new List<MixerEffectDefinition>();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void ClearDefinitionsRuntime();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void AddDefinitionRuntime(string name, MixerParameterDefinition[] parameters);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern string[] GetAudioEffectNames();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern MixerParameterDefinition[] GetAudioEffectParameterDesc(string effectName);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool EffectCanBeSidechainTarget(AudioMixerEffectController effect);

		public static void Refresh()
		{
			MixerEffectDefinitions.ClearDefinitions();
			MixerEffectDefinitions.RegisterAudioMixerEffect("Attenuation", new MixerParameterDefinition[0]);
			MixerEffectDefinitions.RegisterAudioMixerEffect("Send", new MixerParameterDefinition[0]);
			MixerEffectDefinitions.RegisterAudioMixerEffect("Receive", new MixerParameterDefinition[0]);
			MixerParameterDefinition[] array = new MixerParameterDefinition[]
			{
				new MixerParameterDefinition
				{
					name = "Threshold",
					units = "dB",
					displayScale = 1f,
					displayExponent = 1f,
					minRange = -80f,
					maxRange = 0f,
					defaultValue = -10f,
					description = "Threshold of side-chain level detector"
				},
				new MixerParameterDefinition
				{
					name = "Ratio",
					units = "%",
					displayScale = 100f,
					displayExponent = 1f,
					minRange = 0.2f,
					maxRange = 10f,
					defaultValue = 2f,
					description = "Ratio of compression applied when side-chain signal exceeds threshold"
				},
				new MixerParameterDefinition
				{
					name = "Attack Time",
					units = "ms",
					displayScale = 1000f,
					displayExponent = 3f,
					minRange = 0f,
					maxRange = 10f,
					defaultValue = 0.1f,
					description = "Level detector attack time"
				},
				new MixerParameterDefinition
				{
					name = "Release Time",
					units = "ms",
					displayScale = 1000f,
					displayExponent = 3f,
					minRange = 0f,
					maxRange = 10f,
					defaultValue = 0.1f,
					description = "Level detector release time"
				},
				new MixerParameterDefinition
				{
					name = "Make-up Gain",
					units = "dB",
					displayScale = 1f,
					displayExponent = 1f,
					minRange = -80f,
					maxRange = 40f,
					defaultValue = 0f,
					description = "Make-up gain"
				},
				new MixerParameterDefinition
				{
					name = "Knee",
					units = "dB",
					displayScale = 1f,
					displayExponent = 1f,
					minRange = 0f,
					maxRange = 50f,
					defaultValue = 10f,
					description = "Sharpness of compression curve knee"
				},
				new MixerParameterDefinition
				{
					name = "Sidechain Mix",
					units = "%",
					displayScale = 100f,
					displayExponent = 1f,
					minRange = 0f,
					maxRange = 1f,
					defaultValue = 1f,
					description = "Sidechain/source mix. If set to 100% the compressor detects level entirely from sidechain signal."
				}
			};
			MixerEffectDefinitions.RegisterAudioMixerEffect("Duck Volume", array);
			MixerEffectDefinitions.AddDefinitionRuntime("Duck Volume", array);
			string[] audioEffectNames = MixerEffectDefinitions.GetAudioEffectNames();
			string[] array2 = audioEffectNames;
			for (int i = 0; i < array2.Length; i++)
			{
				string text = array2[i];
				MixerParameterDefinition[] audioEffectParameterDesc = MixerEffectDefinitions.GetAudioEffectParameterDesc(text);
				MixerEffectDefinitions.RegisterAudioMixerEffect(text, audioEffectParameterDesc);
			}
		}

		public static bool EffectExists(string name)
		{
			foreach (MixerEffectDefinition current in MixerEffectDefinitions.s_MixerEffectDefinitions)
			{
				if (current.name == name)
				{
					return true;
				}
			}
			return false;
		}

		public static string[] GetEffectList()
		{
			string[] array = new string[MixerEffectDefinitions.s_MixerEffectDefinitions.Count];
			for (int i = 0; i < MixerEffectDefinitions.s_MixerEffectDefinitions.Count; i++)
			{
				array[i] = MixerEffectDefinitions.s_MixerEffectDefinitions[i].name;
			}
			return array;
		}

		public static void ClearDefinitions()
		{
			MixerEffectDefinitions.s_MixerEffectDefinitions.Clear();
			MixerEffectDefinitions.ClearDefinitionsRuntime();
		}

		public static MixerParameterDefinition[] GetEffectParameters(string effect)
		{
			foreach (MixerEffectDefinition current in MixerEffectDefinitions.s_MixerEffectDefinitions)
			{
				if (current.name == effect)
				{
					return current.parameters;
				}
			}
			return new MixerParameterDefinition[0];
		}

		public static bool RegisterAudioMixerEffect(string name, MixerParameterDefinition[] definitions)
		{
			foreach (MixerEffectDefinition current in MixerEffectDefinitions.s_MixerEffectDefinitions)
			{
				if (current.name == name)
				{
					return false;
				}
			}
			MixerEffectDefinition item = new MixerEffectDefinition(name, definitions);
			MixerEffectDefinitions.s_MixerEffectDefinitions.Add(item);
			MixerEffectDefinitions.ClearDefinitionsRuntime();
			foreach (MixerEffectDefinition current2 in MixerEffectDefinitions.s_MixerEffectDefinitions)
			{
				MixerEffectDefinitions.AddDefinitionRuntime(current2.name, current2.parameters);
			}
			return true;
		}
	}
}
