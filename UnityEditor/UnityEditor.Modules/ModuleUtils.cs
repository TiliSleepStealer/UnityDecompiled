using System;
using System.Collections.Generic;

namespace UnityEditor.Modules
{
	internal static class ModuleUtils
	{
		internal static string[] GetAdditionalReferencesForUserScripts()
		{
			List<string> list = new List<string>();
			foreach (IPlatformSupportModule current in ModuleManager.platformSupportModules)
			{
				list.AddRange(current.AssemblyReferencesForUserScripts);
			}
			return list.ToArray();
		}
	}
}
