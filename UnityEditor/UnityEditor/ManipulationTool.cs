using System;
using UnityEngine;

namespace UnityEditor
{
	internal abstract class ManipulationTool
	{
		protected virtual void OnToolGUI(SceneView view)
		{
			if (!Selection.activeTransform || Tools.s_Hidden)
			{
				return;
			}
			bool flag = !Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects);
			EditorGUI.BeginDisabledGroup(flag);
			Vector3 handlePosition = Tools.handlePosition;
			this.ToolGUI(view, handlePosition, flag);
			Handles.ShowStaticLabelIfNeeded(handlePosition);
			EditorGUI.EndDisabledGroup();
		}

		public abstract void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic);
	}
}
