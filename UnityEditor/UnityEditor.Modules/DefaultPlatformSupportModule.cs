using System;
using UnityEngine;

namespace UnityEditor.Modules
{
	internal abstract class DefaultPlatformSupportModule : IPlatformSupportModule
	{
		protected ICompilationExtension compilationExtension;

		public abstract string TargetName
		{
			get;
		}

		public abstract string JamTarget
		{
			get;
		}

		public virtual string ExtensionVersion
		{
			get
			{
				return null;
			}
		}

		public virtual string[] AssemblyReferencesForUserScripts
		{
			get
			{
				return new string[0];
			}
		}

		public virtual GUIContent[] GetDisplayNames()
		{
			return null;
		}

		public abstract IBuildPostprocessor CreateBuildPostprocessor();

		public virtual IScriptingImplementations CreateScriptingImplementations()
		{
			return null;
		}

		public virtual ISettingEditorExtension CreateSettingsEditorExtension()
		{
			return null;
		}

		public virtual IPreferenceWindowExtension CreatePreferenceWindowExtension()
		{
			return null;
		}

		public virtual IBuildWindowExtension CreateBuildWindowExtension()
		{
			return null;
		}

		public virtual ICompilationExtension CreateCompilationExtension()
		{
			ICompilationExtension arg_26_0;
			if (this.compilationExtension != null)
			{
				ICompilationExtension compilationExtension = this.compilationExtension;
				arg_26_0 = compilationExtension;
			}
			else
			{
				arg_26_0 = (this.compilationExtension = new DefaultCompilationExtension());
			}
			return arg_26_0;
		}

		public virtual IPluginImporterExtension CreatePluginImporterExtension()
		{
			return null;
		}

		public virtual IUserAssembliesValidator CreateUserAssembliesValidatorExtension()
		{
			return null;
		}

		public virtual IDevice CreateDevice(string id)
		{
			throw new NotSupportedException();
		}

		public virtual void OnActivate()
		{
		}

		public virtual void OnDeactivate()
		{
		}

		public virtual void OnLoad()
		{
		}

		public virtual void OnUnload()
		{
		}
	}
}
