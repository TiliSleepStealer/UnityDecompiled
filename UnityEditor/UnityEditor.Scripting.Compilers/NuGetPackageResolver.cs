using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor.Utils;

namespace UnityEditor.Scripting.Compilers
{
	internal sealed class NuGetPackageResolver
	{
		public string PackagesDirectory
		{
			get;
			set;
		}

		public string ProjectLockFile
		{
			get;
			set;
		}

		public string TargetMoniker
		{
			get;
			set;
		}

		public string[] ResolvedReferences
		{
			get;
			private set;
		}

		public NuGetPackageResolver()
		{
			this.TargetMoniker = "UAP,Version=v10.0";
		}

		private string ConvertToWindowsPath(string path)
		{
			return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		}

		public bool EnsureProjectLockFile(string projectFile)
		{
			string directoryName = Path.GetDirectoryName(this.ProjectLockFile);
			string text = FileUtil.NiceWinPath(Path.Combine(directoryName, Path.GetFileName(projectFile)));
			Console.WriteLine("Restoring NuGet packages from \"{0}\".", Path.GetFullPath(text));
			if (File.Exists(this.ProjectLockFile))
			{
				Console.WriteLine("Done. Reusing existing \"{0}\" file.", Path.GetFullPath(this.ProjectLockFile));
				return true;
			}
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.Copy(projectFile, text, true);
			string buildToolsDirectory = BuildPipeline.GetBuildToolsDirectory(BuildTarget.WSAPlayer);
			string fileName = FileUtil.NiceWinPath(Path.Combine(buildToolsDirectory, "nuget.exe"));
			Program program = new Program(new ProcessStartInfo
			{
				Arguments = string.Format("restore \"{0}\" -NonInteractive -Source https://api.nuget.org/v3/index.json", text),
				CreateNoWindow = true,
				FileName = fileName
			});
			using (program)
			{
				program.Start();
				for (int i = 0; i < 15; i++)
				{
					if (!program.WaitForExit(60000))
					{
						Console.WriteLine("Still restoring NuGet packages.");
					}
				}
				if (!program.HasExited)
				{
					throw new Exception(string.Format("Failed to restore NuGet packages:{0}Time out.", Environment.NewLine));
				}
				if (program.ExitCode != 0)
				{
					throw new Exception(string.Format("Failed to restore NuGet packages:{0}{1}", Environment.NewLine, program.GetAllOutput()));
				}
			}
			Console.WriteLine("Done.");
			return false;
		}

		public void Resolve()
		{
			string json = File.ReadAllText(this.ProjectLockFile);
			Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize(json);
			Dictionary<string, object> dictionary2 = (Dictionary<string, object>)dictionary["targets"];
			Dictionary<string, object> dictionary3 = (Dictionary<string, object>)dictionary2[this.TargetMoniker];
			List<string> list = new List<string>();
			string path = this.ConvertToWindowsPath(this.GetPackagesPath());
			foreach (KeyValuePair<string, object> current in dictionary3)
			{
				Dictionary<string, object> dictionary4 = (Dictionary<string, object>)current.Value;
				object obj;
				if (dictionary4.TryGetValue("compile", out obj))
				{
					Dictionary<string, object> dictionary5 = (Dictionary<string, object>)obj;
					string[] array = current.Key.Split(new char[]
					{
						'/'
					});
					string path2 = array[0];
					string path3 = array[1];
					string text = Path.Combine(Path.Combine(path, path2), path3);
					if (!Directory.Exists(text))
					{
						throw new Exception(string.Format("Package directory not found: \"{0}\".", text));
					}
					foreach (string current2 in dictionary5.Keys)
					{
						if (!string.Equals(Path.GetFileName(current2), "_._", StringComparison.InvariantCultureIgnoreCase))
						{
							string text2 = Path.Combine(text, this.ConvertToWindowsPath(current2));
							if (!File.Exists(text2))
							{
								throw new Exception(string.Format("Reference not found: \"{0}\".", text2));
							}
							list.Add(text2);
						}
					}
					if (dictionary4.ContainsKey("frameworkAssemblies"))
					{
						throw new NotImplementedException("Support for \"frameworkAssemblies\" property has not been implemented yet.");
					}
				}
			}
			this.ResolvedReferences = list.ToArray();
		}

		private string GetPackagesPath()
		{
			string text = this.PackagesDirectory;
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			text = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			string environmentVariable = Environment.GetEnvironmentVariable("USERPROFILE");
			return Path.Combine(Path.Combine(environmentVariable, ".nuget"), "packages");
		}
	}
}
