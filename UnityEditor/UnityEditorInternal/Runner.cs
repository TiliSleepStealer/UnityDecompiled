using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditorInternal
{
	internal class Runner
	{
		internal static void RunManagedProgram(string exe, string args)
		{
			Runner.RunManagedProgram(exe, args, Application.dataPath + "/..", null);
		}

		internal static void RunManagedProgram(string exe, string args, string workingDirectory, CompilerOutputParserBase parser)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Program program;
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				ProcessStartInfo si = new ProcessStartInfo
				{
					Arguments = args,
					CreateNoWindow = true,
					FileName = exe
				};
				program = new Program(si);
			}
			else
			{
				program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), "4.0", exe, args);
			}
			using (program)
			{
				program.GetProcessStartInfo().WorkingDirectory = workingDirectory;
				program.Start();
				program.WaitForExit();
				stopwatch.Stop();
				Console.WriteLine("{0} exited after {1} ms.", exe, stopwatch.ElapsedMilliseconds);
				if (program.ExitCode != 0)
				{
					if (parser != null)
					{
						string[] errorOutput = program.GetErrorOutput();
						string[] standardOutput = program.GetStandardOutput();
						IEnumerable<CompilerMessage> enumerable = parser.Parse(errorOutput, standardOutput, true);
						foreach (CompilerMessage current in enumerable)
						{
							UnityEngine.Debug.LogPlayerBuildError(current.message, current.file, current.line, current.column);
						}
					}
					UnityEngine.Debug.LogError(string.Concat(new string[]
					{
						"Failed running ",
						exe,
						" ",
						args,
						"\n\n",
						program.GetAllOutput()
					}));
					throw new Exception(string.Format("{0} did not run properly!", exe));
				}
			}
		}

		public static void RunNativeProgram(string exe, string args)
		{
			using (NativeProgram nativeProgram = new NativeProgram(exe, args))
			{
				nativeProgram.Start();
				nativeProgram.WaitForExit();
				if (nativeProgram.ExitCode != 0)
				{
					UnityEngine.Debug.LogError(string.Concat(new string[]
					{
						"Failed running ",
						exe,
						" ",
						args,
						"\n\n",
						nativeProgram.GetAllOutput()
					}));
					throw new Exception(string.Format("{0} did not run properly!", exe));
				}
			}
		}
	}
}
