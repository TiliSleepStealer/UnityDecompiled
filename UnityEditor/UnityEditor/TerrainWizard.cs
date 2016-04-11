using System;
using UnityEngine;

namespace UnityEditor
{
	internal class TerrainWizard : ScriptableWizard
	{
		internal const int kMaxResolution = 4097;

		protected Terrain m_Terrain;

		protected TerrainData terrainData
		{
			get
			{
				if (this.m_Terrain != null)
				{
					return this.m_Terrain.terrainData;
				}
				return null;
			}
		}

		internal virtual void OnWizardUpdate()
		{
			base.isValid = true;
			base.errorString = string.Empty;
			if (this.m_Terrain == null || this.m_Terrain.terrainData == null)
			{
				base.isValid = false;
				base.errorString = "Terrain does not exist";
			}
		}

		internal void InitializeDefaults(Terrain terrain)
		{
			this.m_Terrain = terrain;
			this.OnWizardUpdate();
		}

		internal void FlushHeightmapModification()
		{
			this.m_Terrain.Flush();
		}

		internal static T DisplayTerrainWizard<T>(string title, string button) where T : TerrainWizard
		{
			T[] array = Resources.FindObjectsOfTypeAll<T>();
			if (array.Length > 0)
			{
				T result = array[0];
				result.titleContent = EditorGUIUtility.TextContent(title);
				result.createButtonName = button;
				result.otherButtonName = string.Empty;
				result.Focus();
				return result;
			}
			return ScriptableWizard.DisplayWizard<T>(title, button);
		}
	}
}
