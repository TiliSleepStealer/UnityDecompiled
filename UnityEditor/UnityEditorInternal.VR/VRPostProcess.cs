using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

namespace UnityEditorInternal.VR
{
	internal static class VRPostProcess
	{
		[RegisterPlugins]
		private static IEnumerable<PluginDesc> RegisterPlugins(BuildTarget target)
		{
			if (target == BuildTarget.Android && PlayerSettings.virtualRealitySupported)
			{
				PluginDesc pluginDesc = default(PluginDesc);
				string path = EditorApplication.applicationContentsPath + "/VR/oculus/" + BuildPipeline.GetBuildTargetName(target);
				pluginDesc.pluginPath = Path.Combine(path, "ovrplugin.aar");
				return new PluginDesc[]
				{
					pluginDesc
				};
			}
			return new PluginDesc[0];
		}
	}
}
