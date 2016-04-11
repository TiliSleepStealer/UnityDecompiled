using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor.Scripting;
using UnityEditorInternal;

namespace UnityEditor.VisualStudioIntegration
{
	internal class SolutionSynchronizer
	{
		private enum Mode
		{
			UnityScriptAsUnityProj,
			UntiyScriptAsPrecompiledAssembly
		}

		public static readonly ISolutionSynchronizationSettings DefaultSynchronizationSettings = new DefaultSolutionSynchronizationSettings();

		private static readonly string WindowsNewline = "\r\n";

		internal static readonly Dictionary<string, ScriptingLanguage> BuiltinSupportedExtensions = new Dictionary<string, ScriptingLanguage>
		{
			{
				"cs",
				ScriptingLanguage.CSharp
			},
			{
				"js",
				ScriptingLanguage.UnityScript
			},
			{
				"boo",
				ScriptingLanguage.Boo
			},
			{
				"shader",
				ScriptingLanguage.None
			},
			{
				"compute",
				ScriptingLanguage.None
			},
			{
				"cginc",
				ScriptingLanguage.None
			},
			{
				"glslinc",
				ScriptingLanguage.None
			}
		};

		private string[] ProjectSupportedExtensions = new string[0];

		private static readonly Dictionary<ScriptingLanguage, string> ProjectExtensions = new Dictionary<ScriptingLanguage, string>
		{
			{
				ScriptingLanguage.Boo,
				".booproj"
			},
			{
				ScriptingLanguage.CSharp,
				".csproj"
			},
			{
				ScriptingLanguage.UnityScript,
				".unityproj"
			},
			{
				ScriptingLanguage.None,
				".csproj"
			}
		};

		private static readonly Regex _MonoDevelopPropertyHeader = new Regex("^\\s*GlobalSection\\(MonoDevelopProperties.*\\)");

		public static readonly string MSBuildNamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

		private readonly string _projectDirectory;

		private readonly ISolutionSynchronizationSettings _settings;

		private readonly string _projectName;

		private static readonly string DefaultMonoDevelopSolutionProperties = string.Join("\r\n", new string[]
		{
			"    GlobalSection(MonoDevelopProperties) = preSolution",
			"        StartupItem = Assembly-CSharp.csproj",
			"    EndGlobalSection"
		}).Replace("    ", "\t");

		public static readonly Regex scriptReferenceExpression = new Regex("^Library.ScriptAssemblies.(?<project>Assembly-(?<language>[^-]+)(?<editor>-Editor)?(?<firstpass>-firstpass)?).dll$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static string[] InternalAssembliesIncludedIntoEditorProject = new string[]
		{
			"/UnityEditor.iOS.Extensions.Common.dll",
			"/UnityEditor.iOS.Extensions.Xcode.dll"
		};

		public SolutionSynchronizer(string projectDirectory, ISolutionSynchronizationSettings settings)
		{
			this._projectDirectory = projectDirectory;
			this._settings = settings;
			this._projectName = Path.GetFileName(this._projectDirectory);
		}

		public SolutionSynchronizer(string projectDirectory) : this(projectDirectory, SolutionSynchronizer.DefaultSynchronizationSettings)
		{
		}

		private void SetupProjectSupportedExtensions()
		{
			this.ProjectSupportedExtensions = EditorSettings.projectGenerationUserExtensions;
		}

		public bool ShouldFileBePartOfSolution(string file)
		{
			string extension = Path.GetExtension(file);
			return extension == ".dll" || this.IsSupportedExtension(extension);
		}

		private bool IsSupportedExtension(string extension)
		{
			extension = extension.TrimStart(new char[]
			{
				'.'
			});
			return SolutionSynchronizer.BuiltinSupportedExtensions.ContainsKey(extension) || this.ProjectSupportedExtensions.Contains(extension);
		}

		private static ScriptingLanguage ScriptingLanguageFor(MonoIsland island)
		{
			return SolutionSynchronizer.ScriptingLanguageFor(island.GetExtensionOfSourceFiles());
		}

		private static ScriptingLanguage ScriptingLanguageFor(string extension)
		{
			ScriptingLanguage result;
			if (SolutionSynchronizer.BuiltinSupportedExtensions.TryGetValue(extension.TrimStart(new char[]
			{
				'.'
			}), out result))
			{
				return result;
			}
			return ScriptingLanguage.None;
		}

		public bool ProjectExists(MonoIsland island)
		{
			return File.Exists(this.ProjectFile(island));
		}

		public bool SolutionExists()
		{
			return File.Exists(this.SolutionFile());
		}

		private static void DumpIsland(MonoIsland island)
		{
			Console.WriteLine("{0} ({1})", island._output, island._classlib_profile);
			Console.WriteLine("Files: ");
			Console.WriteLine(string.Join("\n", island._files));
			Console.WriteLine("References: ");
			Console.WriteLine(string.Join("\n", island._references));
			Console.WriteLine(string.Empty);
		}

		public bool SyncIfNeeded(IEnumerable<string> affectedFiles)
		{
			this.SetupProjectSupportedExtensions();
			if (this.SolutionExists() && affectedFiles.Any(new Func<string, bool>(this.ShouldFileBePartOfSolution)))
			{
				this.Sync();
				return true;
			}
			return false;
		}

		public void Sync()
		{
			this.SetupProjectSupportedExtensions();
			bool flag = AssetPostprocessingInternal.OnPreGeneratingCSProjectFiles();
			if (flag)
			{
				return;
			}
			IEnumerable<MonoIsland> islands = from i in InternalEditorUtility.GetMonoIslands()
			where 0 < i._files.Length
			select i;
			string otherAssetsProjectPart = this.GenerateAllAssetProjectPart();
			this.SyncSolution(islands);
			foreach (MonoIsland current in SolutionSynchronizer.RelevantIslandsForMode(islands, SolutionSynchronizer.ModeForCurrentExternalEditor()))
			{
				this.SyncProject(current, otherAssetsProjectPart);
			}
			AssetPostprocessingInternal.CallOnGeneratedCSProjectFiles();
		}

		private string GenerateAllAssetProjectPart()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
			for (int i = 0; i < allAssetPaths.Length; i++)
			{
				string text = allAssetPaths[i];
				string extension = Path.GetExtension(text);
				if (this.IsSupportedExtension(extension) && SolutionSynchronizer.ScriptingLanguageFor(extension) == ScriptingLanguage.None)
				{
					stringBuilder.AppendFormat("     <None Include=\"{0}\" />{1}", this.EscapedRelativePathFor(text), SolutionSynchronizer.WindowsNewline);
				}
			}
			return stringBuilder.ToString();
		}

		private void SyncProject(MonoIsland island, string otherAssetsProjectPart)
		{
			SolutionSynchronizer.SyncFileIfNotChanged(this.ProjectFile(island), this.ProjectText(island, SolutionSynchronizer.ModeForCurrentExternalEditor(), otherAssetsProjectPart));
		}

		private static void SyncFileIfNotChanged(string filename, string newContents)
		{
			if (File.Exists(filename) && newContents == File.ReadAllText(filename))
			{
				return;
			}
			File.WriteAllText(filename, newContents);
		}

		private static bool IsInternalAssemblyThatShouldBeReferenced(bool isBuildingEditorProject, string reference)
		{
			if (!isBuildingEditorProject)
			{
				return false;
			}
			string[] internalAssembliesIncludedIntoEditorProject = SolutionSynchronizer.InternalAssembliesIncludedIntoEditorProject;
			for (int i = 0; i < internalAssembliesIncludedIntoEditorProject.Length; i++)
			{
				string value = internalAssembliesIncludedIntoEditorProject[i];
				if (reference.EndsWith(value))
				{
					return true;
				}
			}
			return false;
		}

		private string ProjectText(MonoIsland island, SolutionSynchronizer.Mode mode, string allAssetsProject)
		{
			StringBuilder stringBuilder = new StringBuilder(this.ProjectHeader(island));
			List<string> list = new List<string>();
			List<Match> list2 = new List<Match>();
			bool isBuildingEditorProject = island._output.EndsWith("-Editor.dll");
			string[] files = island._files;
			for (int i = 0; i < files.Length; i++)
			{
				string text = files[i];
				string b = Path.GetExtension(text).ToLower();
				string text2 = (!Path.IsPathRooted(text)) ? Path.Combine(this._projectDirectory, text) : text;
				if (".dll" != b)
				{
					string arg = "Compile";
					stringBuilder.AppendFormat("     <{0} Include=\"{1}\" />{2}", arg, this.EscapedRelativePathFor(text2), SolutionSynchronizer.WindowsNewline);
				}
				else
				{
					list.Add(text2);
				}
			}
			stringBuilder.Append(allAssetsProject);
			foreach (string current in list.Union(island._references))
			{
				if (!current.EndsWith("/UnityEditor.dll") && !current.EndsWith("/UnityEngine.dll") && !current.EndsWith("\\UnityEditor.dll") && !current.EndsWith("\\UnityEngine.dll"))
				{
					Match match = SolutionSynchronizer.scriptReferenceExpression.Match(current);
					if (match.Success && (mode == SolutionSynchronizer.Mode.UnityScriptAsUnityProj || (int)Enum.Parse(typeof(ScriptingLanguage), match.Groups["language"].Value, true) == 2))
					{
						list2.Add(match);
					}
					else
					{
						string text3 = (!Path.IsPathRooted(current)) ? Path.Combine(this._projectDirectory, current) : current;
						if (AssemblyHelper.IsManagedAssembly(text3))
						{
							if (!AssemblyHelper.IsInternalAssembly(text3) || SolutionSynchronizer.IsInternalAssemblyThatShouldBeReferenced(isBuildingEditorProject, text3))
							{
								text3 = text3.Replace("\\", "/");
								text3 = text3.Replace("\\\\", "/");
								stringBuilder.AppendFormat(" <Reference Include=\"{0}\">{1}", Path.GetFileNameWithoutExtension(text3), SolutionSynchronizer.WindowsNewline);
								stringBuilder.AppendFormat(" <HintPath>{0}</HintPath>{1}", text3, SolutionSynchronizer.WindowsNewline);
								stringBuilder.AppendFormat(" </Reference>{0}", SolutionSynchronizer.WindowsNewline);
							}
						}
					}
				}
			}
			if (0 < list2.Count)
			{
				stringBuilder.AppendLine("  </ItemGroup>");
				stringBuilder.AppendLine("  <ItemGroup>");
				foreach (Match current2 in list2)
				{
					string value = current2.Groups["project"].Value;
					stringBuilder.AppendFormat("    <ProjectReference Include=\"{0}{1}\">{2}", value, SolutionSynchronizer.GetProjectExtension((ScriptingLanguage)((int)Enum.Parse(typeof(ScriptingLanguage), current2.Groups["language"].Value, true))), SolutionSynchronizer.WindowsNewline);
					stringBuilder.AppendFormat("      <Project>{{{0}}}</Project>", this.ProjectGuid(Path.Combine("Temp", current2.Groups["project"].Value + ".dll")), SolutionSynchronizer.WindowsNewline);
					stringBuilder.AppendFormat("      <Name>{0}</Name>", value, SolutionSynchronizer.WindowsNewline);
					stringBuilder.AppendLine("    </ProjectReference>");
				}
			}
			stringBuilder.Append(this.ProjectFooter(island));
			return stringBuilder.ToString();
		}

		public string ProjectFile(MonoIsland island)
		{
			ScriptingLanguage key = SolutionSynchronizer.ScriptingLanguageFor(island);
			return Path.Combine(this._projectDirectory, string.Format("{0}{1}", Path.GetFileNameWithoutExtension(island._output), SolutionSynchronizer.ProjectExtensions[key]));
		}

		internal string SolutionFile()
		{
			return Path.Combine(this._projectDirectory, string.Format("{0}.sln", this._projectName));
		}

		private string ProjectHeader(MonoIsland island)
		{
			string text = "4.0";
			string text2 = "10.0.20506";
			ScriptingLanguage language = SolutionSynchronizer.ScriptingLanguageFor(island);
			if (this._settings.VisualStudioVersion == 9)
			{
				text = "3.5";
				text2 = "9.0.21022";
			}
			object[] array = new object[]
			{
				text,
				text2,
				this.ProjectGuid(island._output),
				this._settings.EngineAssemblyPath,
				this._settings.EditorAssemblyPath,
				string.Join(";", new string[]
				{
					"DEBUG",
					"TRACE"
				}.Concat(this._settings.Defines).Concat(island._defines).Distinct<string>().ToArray<string>()),
				SolutionSynchronizer.MSBuildNamespaceUri,
				Path.GetFileNameWithoutExtension(island._output),
				EditorSettings.projectGenerationRootNamespace
			};
			string result;
			try
			{
				result = string.Format(this._settings.GetProjectHeaderTemplate(language), array);
			}
			catch (Exception)
			{
				throw new NotSupportedException("Failed creating c# project because the c# project header did not have the correct amount of arguments, which is " + array.Length);
			}
			return result;
		}

		private void SyncSolution(IEnumerable<MonoIsland> islands)
		{
			SolutionSynchronizer.SyncFileIfNotChanged(this.SolutionFile(), this.SolutionText(islands, SolutionSynchronizer.ModeForCurrentExternalEditor()));
		}

		private static SolutionSynchronizer.Mode ModeForCurrentExternalEditor()
		{
			if (SolutionSynchronizer.IsSelectedEditorVisualStudio())
			{
				return SolutionSynchronizer.Mode.UntiyScriptAsPrecompiledAssembly;
			}
			if (SolutionSynchronizer.IsSelectedEditorInternalMonoDevelop())
			{
				return SolutionSynchronizer.Mode.UnityScriptAsUnityProj;
			}
			return (!EditorPrefs.GetBool("kExternalEditorSupportsUnityProj", false)) ? SolutionSynchronizer.Mode.UntiyScriptAsPrecompiledAssembly : SolutionSynchronizer.Mode.UnityScriptAsUnityProj;
		}

		private static bool IsSelectedEditorVisualStudio()
		{
			string externalScriptEditor = InternalEditorUtility.GetExternalScriptEditor();
			return externalScriptEditor.EndsWith("devenv.exe") || externalScriptEditor.EndsWith("vcsexpress.exe");
		}

		private static bool IsSelectedEditorInternalMonoDevelop()
		{
			return InternalEditorUtility.GetExternalScriptEditor() == "internal";
		}

		private string SolutionText(IEnumerable<MonoIsland> islands, SolutionSynchronizer.Mode mode)
		{
			string text = "11.00";
			if (this._settings.VisualStudioVersion == 9)
			{
				text = "10.00";
			}
			IEnumerable<MonoIsland> enumerable = SolutionSynchronizer.RelevantIslandsForMode(islands, mode);
			string projectEntries = this.GetProjectEntries(enumerable);
			string text2 = string.Join(SolutionSynchronizer.WindowsNewline, (from i in enumerable
			select this.GetProjectActiveConfigurations(this.ProjectGuid(i._output))).ToArray<string>());
			return string.Format(this._settings.SolutionTemplate, new object[]
			{
				text,
				projectEntries,
				text2,
				this.ReadExistingMonoDevelopSolutionProperties()
			});
		}

		private static IEnumerable<MonoIsland> RelevantIslandsForMode(IEnumerable<MonoIsland> islands, SolutionSynchronizer.Mode mode)
		{
			return from i in islands
			where mode == SolutionSynchronizer.Mode.UnityScriptAsUnityProj || ScriptingLanguage.CSharp == SolutionSynchronizer.ScriptingLanguageFor(i)
			select i;
		}

		private string GetProjectEntries(IEnumerable<MonoIsland> islands)
		{
			IEnumerable<string> source = from i in islands
			select string.Format(SolutionSynchronizer.DefaultSynchronizationSettings.SolutionProjectEntryTemplate, new object[]
			{
				this.SolutionGuid(),
				this._projectName,
				Path.GetFileName(this.ProjectFile(i)),
				this.ProjectGuid(i._output)
			});
			return string.Join(SolutionSynchronizer.WindowsNewline, source.ToArray<string>());
		}

		private string GetProjectActiveConfigurations(string projectGuid)
		{
			return string.Format(SolutionSynchronizer.DefaultSynchronizationSettings.SolutionProjectConfigurationTemplate, projectGuid);
		}

		private string EscapedRelativePathFor(string file)
		{
			string value = this._projectDirectory.Replace("/", "\\");
			file = file.Replace("/", "\\");
			return SecurityElement.Escape((!file.StartsWith(value)) ? file : file.Substring(this._projectDirectory.Length + 1));
		}

		private string ProjectGuid(string assembly)
		{
			return SolutionGuidGenerator.GuidForProject(this._projectName + Path.GetFileNameWithoutExtension(assembly));
		}

		private string SolutionGuid()
		{
			return SolutionGuidGenerator.GuidForSolution(this._projectName);
		}

		private string ProjectFooter(MonoIsland island)
		{
			return string.Format(this._settings.GetProjectFooterTemplate(SolutionSynchronizer.ScriptingLanguageFor(island)), this.ReadExistingMonoDevelopProjectProperties(island));
		}

		private string ReadExistingMonoDevelopSolutionProperties()
		{
			if (!this.SolutionExists())
			{
				return SolutionSynchronizer.DefaultMonoDevelopSolutionProperties;
			}
			string[] array;
			try
			{
				array = File.ReadAllLines(this.SolutionFile());
			}
			catch (IOException)
			{
				string defaultMonoDevelopSolutionProperties = SolutionSynchronizer.DefaultMonoDevelopSolutionProperties;
				return defaultMonoDevelopSolutionProperties;
			}
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string text = array2[i];
				if (SolutionSynchronizer._MonoDevelopPropertyHeader.IsMatch(text))
				{
					flag = true;
				}
				if (flag)
				{
					if (text.Contains("EndGlobalSection"))
					{
						stringBuilder.Append(text);
						flag = false;
					}
					else
					{
						stringBuilder.AppendFormat("{0}{1}", text, SolutionSynchronizer.WindowsNewline);
					}
				}
			}
			if (0 < stringBuilder.Length)
			{
				return stringBuilder.ToString();
			}
			return SolutionSynchronizer.DefaultMonoDevelopSolutionProperties;
		}

		private string ReadExistingMonoDevelopProjectProperties(MonoIsland island)
		{
			if (!this.ProjectExists(island))
			{
				return string.Empty;
			}
			XmlDocument xmlDocument = new XmlDocument();
			XmlNamespaceManager xmlNamespaceManager;
			try
			{
				xmlDocument.Load(this.ProjectFile(island));
				xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
				xmlNamespaceManager.AddNamespace("msb", SolutionSynchronizer.MSBuildNamespaceUri);
			}
			catch (Exception ex)
			{
				if (ex is IOException || ex is XmlException)
				{
					string empty = string.Empty;
					return empty;
				}
				throw;
			}
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/msb:Project/msb:ProjectExtensions", xmlNamespaceManager);
			if (xmlNodeList.Count == 0)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (XmlNode xmlNode in xmlNodeList)
			{
				stringBuilder.AppendLine(xmlNode.OuterXml);
			}
			return stringBuilder.ToString();
		}

		[Obsolete("Use AssemblyHelper.IsManagedAssembly")]
		public static bool IsManagedAssembly(string file)
		{
			return AssemblyHelper.IsManagedAssembly(file);
		}

		public static string GetProjectExtension(ScriptingLanguage language)
		{
			if (!SolutionSynchronizer.ProjectExtensions.ContainsKey(language))
			{
				throw new ArgumentException("Unsupported language", "language");
			}
			return SolutionSynchronizer.ProjectExtensions[language];
		}
	}
}
