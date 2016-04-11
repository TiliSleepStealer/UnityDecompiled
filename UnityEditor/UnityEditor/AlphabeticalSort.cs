using System;
using UnityEngine;

namespace UnityEditor
{
	public class AlphabeticalSort : BaseHierarchySort
	{
		private readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("AlphabeticalSorting"), "Alphabetical Order");

		public override GUIContent content
		{
			get
			{
				return this.m_Content;
			}
		}
	}
}
