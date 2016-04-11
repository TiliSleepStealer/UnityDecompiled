using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.VisualStudioIntegration
{
	internal class UnityVSSupport
	{
		private static bool m_ShouldUnityVSBeActive;

		public static string s_LoadedUnityVS;

		private static string s_AboutLabel;

		public static void Initialize()
		{
			UnityVSSupport.Initialize(null);
		}

		public static void Initialize(string editorPath)
		{
			if (Application.platform != RuntimePlatform.WindowsEditor)
			{
				return;
			}
			string externalEditor = editorPath ?? EditorPrefs.GetString("kScriptsDefaultApp");
			if (externalEditor.EndsWith("UnityVS.OpenFile.exe"))
			{
				externalEditor = SyncVS.FindBestVisualStudio();
				if (externalEditor != null)
				{
					EditorPrefs.SetString("kScriptsDefaultApp", externalEditor);
				}
			}
			KeyValuePair<VisualStudioVersion, string>[] array = (from kvp in SyncVS.InstalledVisualStudios
			where Paths.AreEqual(kvp.Value, externalEditor, true)
			select kvp).ToArray<KeyValuePair<VisualStudioVersion, string>>();
			bool flag = array.Length > 0;
			UnityVSSupport.m_ShouldUnityVSBeActive = flag;
			if (!flag)
			{
				return;
			}
			string vstuBridgeAssembly = UnityVSSupport.GetVstuBridgeAssembly(array[0].Key);
			if (vstuBridgeAssembly == null)
			{
				Console.WriteLine("Unable to find bridge dll in registry for Microsoft Visual Studio Tools for Unity for " + externalEditor);
				return;
			}
			if (!File.Exists(vstuBridgeAssembly))
			{
				Console.WriteLine("Unable to find bridge dll on disk for Microsoft Visual Studio Tools for Unity for " + vstuBridgeAssembly);
				return;
			}
			UnityVSSupport.s_LoadedUnityVS = vstuBridgeAssembly;
			InternalEditorUtility.SetupCustomDll(Path.GetFileNameWithoutExtension(vstuBridgeAssembly), vstuBridgeAssembly);
		}

		public static bool ShouldUnityVSBeActive()
		{
			return UnityVSSupport.m_ShouldUnityVSBeActive;
		}

		private static string GetVstuBridgeAssembly(VisualStudioVersion version)
		{
			string result;
			try
			{
				string vsTargetYear = string.Empty;
				switch (version)
				{
				case VisualStudioVersion.VisualStudio2010:
					vsTargetYear = "2010";
					break;
				case VisualStudioVersion.VisualStudio2012:
					vsTargetYear = "2012";
					break;
				case VisualStudioVersion.VisualStudio2013:
					vsTargetYear = "2013";
					break;
				case VisualStudioVersion.VisualStudio2015:
					vsTargetYear = "2015";
					break;
				}
				result = (UnityVSSupport.GetVstuBridgePathFromRegistry(vsTargetYear, true) ?? UnityVSSupport.GetVstuBridgePathFromRegistry(vsTargetYear, false));
			}
			catch (Exception)
			{
				result = null;
			}
			return result;
		}

		private static string GetVstuBridgePathFromRegistry(string vsTargetYear, bool currentUser)
		{
			string keyName = string.Format("{0}\\Software\\Microsoft\\Microsoft Visual Studio {1} Tools for Unity", (!currentUser) ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER", vsTargetYear);
			return (string)Registry.GetValue(keyName, "UnityExtensionPath", null);
		}

		public static void ScriptEditorChanged(string editorPath)
		{
			if (Application.platform != RuntimePlatform.WindowsEditor)
			{
				return;
			}
			UnityVSSupport.Initialize(editorPath);
			InternalEditorUtility.RequestScriptReload();
		}

		public static string GetAboutWindowLabel()
		{
			if (UnityVSSupport.s_AboutLabel != null)
			{
				return UnityVSSupport.s_AboutLabel;
			}
			UnityVSSupport.s_AboutLabel = UnityVSSupport.CalculateAboutWindowLabel();
			return UnityVSSupport.s_AboutLabel;
		}

		private static string CalculateAboutWindowLabel()
		{
			if (!UnityVSSupport.m_ShouldUnityVSBeActive)
			{
				return string.Empty;
			}
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.Location == UnityVSSupport.s_LoadedUnityVS);
			if (assembly == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder("Microsoft Visual Studio Tools for Unity ");
			stringBuilder.Append(assembly.GetName().Version);
			stringBuilder.Append(" enabled");
			return stringBuilder.ToString();
		}
	}
}
