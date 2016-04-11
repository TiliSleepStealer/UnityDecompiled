using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

namespace UnityEditor
{
	internal class CodeStrippingUtils
	{
		public static readonly string[] NativeClassBlackList = new string[]
		{
			"PreloadData",
			"Material",
			"Cubemap",
			"Texture3D",
			"RenderTexture",
			"Mesh"
		};

		public static readonly Dictionary<string, string> NativeClassDependencyBlackList = new Dictionary<string, string>
		{
			{
				"ParticleSystemRenderer",
				"ParticleSystem"
			}
		};

		private static readonly string[] s_UserAssemblies = new string[]
		{
			"Assembly-CSharp.dll",
			"Assembly-CSharp-firstpass.dll",
			"Assembly-UnityScript.dll",
			"Assembly-UnityScript-firstpass.dll",
			"UnityEngine.Analytics.dll"
		};

		public static string[] UserAssemblies
		{
			get
			{
				return CodeStrippingUtils.s_UserAssemblies;
			}
		}

		public static HashSet<string> GetModulesFromICalls(string icallsListFile)
		{
			string[] array = File.ReadAllLines(icallsListFile);
			HashSet<string> hashSet = new HashSet<string>();
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string icall = array2[i];
				string iCallModule = ModuleMetadata.GetICallModule(icall);
				if (!string.IsNullOrEmpty(iCallModule))
				{
					hashSet.Add(iCallModule);
				}
			}
			return hashSet;
		}

		public static void GenerateDependencies(string strippedAssemblyDir, string icallsListFile, RuntimeClassRegistry rcr, out HashSet<string> nativeClasses, out HashSet<string> nativeModules)
		{
			string[] userAssemblies = CodeStrippingUtils.GetUserAssemblies(strippedAssemblyDir);
			nativeClasses = ((!PlayerSettings.stripEngineCode) ? null : CodeStrippingUtils.GenerateNativeClassList(rcr, strippedAssemblyDir, userAssemblies));
			if (nativeClasses != null)
			{
				CodeStrippingUtils.ExcludeModuleManagers(ref nativeClasses);
			}
			nativeModules = CodeStrippingUtils.GetNativeModulesToRegister(nativeClasses);
			if (nativeClasses != null && icallsListFile != null)
			{
				HashSet<string> modulesFromICalls = CodeStrippingUtils.GetModulesFromICalls(icallsListFile);
				int derivedFromClassID = BaseObjectTools.StringToClassID("GlobalGameManager");
				foreach (string current in modulesFromICalls)
				{
					int[] moduleClasses = ModuleMetadata.GetModuleClasses(current);
					int[] array = moduleClasses;
					for (int i = 0; i < array.Length; i++)
					{
						int num = array[i];
						if (BaseObjectTools.IsDerivedFromClassID(num, derivedFromClassID))
						{
							nativeClasses.Add(BaseObjectTools.ClassIDToString(num));
						}
					}
				}
				nativeModules.UnionWith(modulesFromICalls);
			}
			AssemblyReferenceChecker assemblyReferenceChecker = new AssemblyReferenceChecker();
			assemblyReferenceChecker.CollectReferencesFromRoots(strippedAssemblyDir, userAssemblies, true, 0f, true);
		}

		public static void WriteModuleAndClassRegistrationFile(string strippedAssemblyDir, string icallsListFile, string outputDir, RuntimeClassRegistry rcr, IEnumerable<string> classesToSkip)
		{
			HashSet<string> nativeClasses;
			HashSet<string> nativeModules;
			CodeStrippingUtils.GenerateDependencies(strippedAssemblyDir, icallsListFile, rcr, out nativeClasses, out nativeModules);
			string file = Path.Combine(outputDir, "UnityClassRegistration.cpp");
			CodeStrippingUtils.WriteModuleAndClassRegistrationFile(file, nativeModules, nativeClasses, new HashSet<string>(classesToSkip));
		}

		public static HashSet<string> GetNativeModulesToRegister(HashSet<string> nativeClasses)
		{
			return (nativeClasses != null) ? CodeStrippingUtils.GetRequiredStrippableModules(nativeClasses) : CodeStrippingUtils.GetAllStrippableModules();
		}

		private static HashSet<string> GetClassNames(IEnumerable<int> classIds)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (int current in classIds)
			{
				hashSet.Add(BaseObjectTools.ClassIDToString(current));
			}
			return hashSet;
		}

		private static HashSet<string> GetAllStrippableModules()
		{
			HashSet<string> hashSet = new HashSet<string>();
			string[] moduleNames = ModuleMetadata.GetModuleNames();
			for (int i = 0; i < moduleNames.Length; i++)
			{
				string text = moduleNames[i];
				if (ModuleMetadata.GetModuleStrippable(text))
				{
					hashSet.Add(text);
				}
			}
			return hashSet;
		}

		private static HashSet<string> GetRequiredStrippableModules(HashSet<string> nativeClasses)
		{
			HashSet<string> hashSet = new HashSet<string>();
			string[] moduleNames = ModuleMetadata.GetModuleNames();
			for (int i = 0; i < moduleNames.Length; i++)
			{
				string text = moduleNames[i];
				if (ModuleMetadata.GetModuleStrippable(text))
				{
					HashSet<string> classNames = CodeStrippingUtils.GetClassNames(ModuleMetadata.GetModuleClasses(text));
					if (nativeClasses.Overlaps(classNames))
					{
						hashSet.Add(text);
					}
				}
			}
			return hashSet;
		}

		private static void ExcludeModuleManagers(ref HashSet<string> nativeClasses)
		{
			string[] moduleNames = ModuleMetadata.GetModuleNames();
			int derivedFromClassID = BaseObjectTools.StringToClassID("GlobalGameManager");
			string[] array = moduleNames;
			for (int i = 0; i < array.Length; i++)
			{
				string moduleName = array[i];
				if (ModuleMetadata.GetModuleStrippable(moduleName))
				{
					int[] moduleClasses = ModuleMetadata.GetModuleClasses(moduleName);
					HashSet<int> hashSet = new HashSet<int>();
					HashSet<string> hashSet2 = new HashSet<string>();
					int[] array2 = moduleClasses;
					for (int j = 0; j < array2.Length; j++)
					{
						int num = array2[j];
						if (BaseObjectTools.IsDerivedFromClassID(num, derivedFromClassID))
						{
							hashSet.Add(num);
						}
						else
						{
							hashSet2.Add(BaseObjectTools.ClassIDToString(num));
						}
					}
					if (hashSet2.Count != 0)
					{
						if (!nativeClasses.Overlaps(hashSet2))
						{
							foreach (int current in hashSet)
							{
								nativeClasses.Remove(BaseObjectTools.ClassIDToString(current));
							}
						}
					}
				}
			}
		}

		private static HashSet<string> GenerateNativeClassList(RuntimeClassRegistry rcr, string directory, string[] rootAssemblies)
		{
			HashSet<string> hashSet = CodeStrippingUtils.CollectNativeClassListFromRoots(directory, rootAssemblies);
			string[] nativeClassBlackList = CodeStrippingUtils.NativeClassBlackList;
			for (int i = 0; i < nativeClassBlackList.Length; i++)
			{
				string item = nativeClassBlackList[i];
				hashSet.Add(item);
			}
			foreach (string current in CodeStrippingUtils.NativeClassDependencyBlackList.Keys)
			{
				if (hashSet.Contains(current))
				{
					string item2 = CodeStrippingUtils.NativeClassDependencyBlackList[current];
					hashSet.Add(item2);
				}
			}
			foreach (string current2 in rcr.GetAllNativeClassesIncludingManagersAsString())
			{
				int num = BaseObjectTools.StringToClassID(current2);
				if (num != -1 && !BaseObjectTools.IsBaseObject(num))
				{
					hashSet.Add(current2);
				}
			}
			HashSet<string> hashSet2 = new HashSet<string>();
			foreach (string current3 in hashSet)
			{
				int iD = BaseObjectTools.StringToClassID(current3);
				while (!BaseObjectTools.IsBaseObject(iD))
				{
					hashSet2.Add(BaseObjectTools.ClassIDToString(iD));
					iD = BaseObjectTools.GetSuperClassID(iD);
				}
			}
			return hashSet2;
		}

		private static HashSet<string> CollectNativeClassListFromRoots(string directory, string[] rootAssemblies)
		{
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> hashSet2 = CodeStrippingUtils.CollectManagedTypeReferencesFromRoots(directory, rootAssemblies);
			foreach (string current in hashSet2)
			{
				int num = BaseObjectTools.StringToClassID(current);
				if (num != -1 && !BaseObjectTools.IsBaseObject(num))
				{
					hashSet.Add(current);
				}
			}
			return hashSet;
		}

		private static HashSet<string> CollectManagedTypeReferencesFromRoots(string directory, string[] rootAssemblies)
		{
			HashSet<string> hashSet = new HashSet<string>();
			AssemblyReferenceChecker assemblyReferenceChecker = new AssemblyReferenceChecker();
			bool withMethods = false;
			bool ignoreSystemDlls = false;
			assemblyReferenceChecker.CollectReferencesFromRoots(directory, rootAssemblies, withMethods, 0f, ignoreSystemDlls);
			string[] assemblyFileNames = assemblyReferenceChecker.GetAssemblyFileNames();
			AssemblyDefinition[] assemblyDefinitions = assemblyReferenceChecker.GetAssemblyDefinitions();
			AssemblyDefinition[] array = assemblyDefinitions;
			for (int i = 0; i < array.Length; i++)
			{
				AssemblyDefinition assemblyDefinition = array[i];
				foreach (TypeDefinition current in assemblyDefinition.MainModule.Types)
				{
					if (current.Namespace.StartsWith("UnityEngine") && (current.Fields.Count > 0 || current.Methods.Count > 0 || current.Properties.Count > 0))
					{
						string name = current.Name;
						hashSet.Add(name);
					}
				}
			}
			AssemblyDefinition assemblyDefinition2 = null;
			for (int j = 0; j < assemblyFileNames.Length; j++)
			{
				if (assemblyFileNames[j] == "UnityEngine.dll")
				{
					assemblyDefinition2 = assemblyDefinitions[j];
				}
			}
			AssemblyDefinition[] array2 = assemblyDefinitions;
			for (int k = 0; k < array2.Length; k++)
			{
				AssemblyDefinition assemblyDefinition3 = array2[k];
				if (assemblyDefinition3 != assemblyDefinition2)
				{
					foreach (TypeReference current2 in assemblyDefinition3.MainModule.GetTypeReferences())
					{
						if (current2.Namespace.StartsWith("UnityEngine"))
						{
							string name2 = current2.Name;
							hashSet.Add(name2);
						}
					}
				}
			}
			return hashSet;
		}

		private static void WriteStaticallyLinkedModuleRegistration(TextWriter w, HashSet<string> nativeModules, HashSet<string> nativeClasses)
		{
			w.WriteLine("struct ClassRegistrationContext;");
			w.WriteLine("void InvokeRegisterStaticallyLinkedModuleClasses(ClassRegistrationContext& context)");
			w.WriteLine("{");
			if (nativeClasses == null)
			{
				w.WriteLine("\tvoid RegisterStaticallyLinkedModuleClasses(ClassRegistrationContext&);");
				w.WriteLine("\tRegisterStaticallyLinkedModuleClasses(context);");
			}
			else
			{
				w.WriteLine("\t// Do nothing (we're in stripping mode)");
			}
			w.WriteLine("}");
			w.WriteLine();
			w.WriteLine("void RegisterStaticallyLinkedModulesGranular()");
			w.WriteLine("{");
			foreach (string current in nativeModules)
			{
				w.WriteLine("\tvoid RegisterModule_" + current + "();");
				w.WriteLine("\tRegisterModule_" + current + "();");
				w.WriteLine();
			}
			w.WriteLine("}");
		}

		private static void WriteModuleAndClassRegistrationFile(string file, HashSet<string> nativeModules, HashSet<string> nativeClasses, HashSet<string> classesToSkip)
		{
			using (TextWriter textWriter = new StreamWriter(file))
			{
				CodeStrippingUtils.WriteStaticallyLinkedModuleRegistration(textWriter, nativeModules, nativeClasses);
				textWriter.WriteLine();
				textWriter.WriteLine("void RegisterAllClasses()");
				textWriter.WriteLine("{");
				if (nativeClasses == null)
				{
					textWriter.WriteLine("\tvoid RegisterAllClassesGranular();");
					textWriter.WriteLine("\tRegisterAllClassesGranular();");
				}
				else
				{
					textWriter.WriteLine("\t//Total: {0} classes", nativeClasses.Count);
					int num = 0;
					foreach (string current in nativeClasses)
					{
						textWriter.WriteLine("\t//{0}. {1}", num, current);
						if (classesToSkip.Contains(current))
						{
							textWriter.WriteLine("\t//Skipping {0}", current);
						}
						else
						{
							textWriter.WriteLine("\tvoid RegisterClass_{0}();", current);
							textWriter.WriteLine("\tRegisterClass_{0}();", current);
						}
						textWriter.WriteLine();
						num++;
					}
				}
				textWriter.WriteLine("}");
				textWriter.Close();
			}
		}

		private static string[] GetUserAssemblies(string strippedAssemblyDir)
		{
			List<string> list = new List<string>();
			string[] array = CodeStrippingUtils.s_UserAssemblies;
			for (int i = 0; i < array.Length; i++)
			{
				string assemblyName = array[i];
				list.AddRange(CodeStrippingUtils.GetAssembliesInDirectory(strippedAssemblyDir, assemblyName));
			}
			return list.ToArray();
		}

		private static IEnumerable<string> GetAssembliesInDirectory(string strippedAssemblyDir, string assemblyName)
		{
			return Directory.GetFiles(strippedAssemblyDir, assemblyName, SearchOption.TopDirectoryOnly);
		}
	}
}
