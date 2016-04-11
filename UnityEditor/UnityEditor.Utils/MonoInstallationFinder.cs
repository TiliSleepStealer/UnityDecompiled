using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.Utils
{
	internal class MonoInstallationFinder
	{
		public static string GetFrameWorksFolder()
		{
			string text = FileUtil.NiceWinPath(EditorApplication.applicationPath);
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				return Path.Combine(Path.GetDirectoryName(text), "Data");
			}
			if (Application.platform == RuntimePlatform.OSXEditor)
			{
				return Path.Combine(text, Path.Combine("Contents", "Frameworks"));
			}
			return Path.Combine(Path.GetDirectoryName(text), "Data");
		}

		public static string GetProfileDirectory(BuildTarget target, string profile)
		{
			string monoInstallation = MonoInstallationFinder.GetMonoInstallation();
			return Path.Combine(monoInstallation, Path.Combine("lib", Path.Combine("mono", profile)));
		}

		public static string GetMonoInstallation()
		{
			return MonoInstallationFinder.GetMonoInstallation("Mono");
		}

		public static string GetMonoInstallation(string monoName)
		{
			return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), monoName);
		}
	}
}
