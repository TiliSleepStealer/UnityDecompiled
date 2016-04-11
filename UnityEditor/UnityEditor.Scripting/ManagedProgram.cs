using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting
{
	internal class ManagedProgram : Program
	{
		public ManagedProgram(string monodistribution, string profile, string executable, string arguments) : this(monodistribution, profile, executable, arguments, true)
		{
		}

		public ManagedProgram(string monodistribution, string profile, string executable, string arguments, bool setMonoEnvironmentVariables)
		{
			string text = ManagedProgram.PathCombine(new string[]
			{
				monodistribution,
				"bin",
				"mono"
			});
			string value = ManagedProgram.PathCombine(new string[]
			{
				monodistribution,
				"lib",
				"mono",
				profile
			});
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				text = CommandLineFormatter.PrepareFileName(text + ".exe");
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				Arguments = CommandLineFormatter.PrepareFileName(executable) + " " + arguments,
				CreateNoWindow = true,
				FileName = text,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = Application.dataPath + "/..",
				UseShellExecute = false
			};
			if (setMonoEnvironmentVariables)
			{
				processStartInfo.EnvironmentVariables["MONO_PATH"] = value;
				processStartInfo.EnvironmentVariables["MONO_CFG_DIR"] = ManagedProgram.PathCombine(new string[]
				{
					monodistribution,
					"etc"
				});
			}
			this._process.StartInfo = processStartInfo;
		}

		private static string PathCombine(params string[] parts)
		{
			string text = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{
				text = Path.Combine(text, parts[i]);
			}
			return text;
		}
	}
}
