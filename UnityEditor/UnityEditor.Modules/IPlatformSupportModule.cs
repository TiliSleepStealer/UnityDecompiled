using System;
using UnityEngine;

namespace UnityEditor.Modules
{
	internal interface IPlatformSupportModule
	{
		string TargetName
		{
			get;
		}

		string JamTarget
		{
			get;
		}

		string[] AssemblyReferencesForUserScripts
		{
			get;
		}

		string ExtensionVersion
		{
			get;
		}

		GUIContent[] GetDisplayNames();

		IBuildPostprocessor CreateBuildPostprocessor();

		IScriptingImplementations CreateScriptingImplementations();

		ISettingEditorExtension CreateSettingsEditorExtension();

		IPreferenceWindowExtension CreatePreferenceWindowExtension();

		IBuildWindowExtension CreateBuildWindowExtension();

		ICompilationExtension CreateCompilationExtension();

		IPluginImporterExtension CreatePluginImporterExtension();

		IUserAssembliesValidator CreateUserAssembliesValidatorExtension();

		IDevice CreateDevice(string id);

		void OnActivate();

		void OnDeactivate();

		void OnLoad();

		void OnUnload();
	}
}
