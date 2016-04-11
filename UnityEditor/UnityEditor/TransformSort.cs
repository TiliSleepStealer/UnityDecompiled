using System;
using UnityEngine;

namespace UnityEditor
{
	public class TransformSort : BaseHierarchySort
	{
		private readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("DefaultSorting"), "Transform Child Order");

		public override GUIContent content
		{
			get
			{
				return this.m_Content;
			}
		}
	}
}
