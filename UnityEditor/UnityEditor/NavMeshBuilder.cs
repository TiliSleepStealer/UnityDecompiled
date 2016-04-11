using System;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
	public sealed class NavMeshBuilder
	{
		public static extern UnityEngine.Object navMeshSettingsObject
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static extern bool isRunning
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		internal static extern UnityEngine.Object sceneNavMeshData
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void BuildNavMesh();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void BuildNavMeshAsync();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void ClearAllNavMeshes();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void Cancel();

		public static void BuildNavMeshForMultipleScenes(string[] paths)
		{
			if (paths.Length == 0)
			{
				return;
			}
			for (int i = 0; i < paths.Length; i++)
			{
				for (int j = i + 1; j < paths.Length; j++)
				{
					if (paths[i] == paths[j])
					{
						throw new Exception("No duplicate scene names are allowed");
					}
				}
			}
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}
			if (!EditorSceneManager.OpenScene(paths[0]).IsValid())
			{
				throw new Exception("Could not open scene: " + paths[0]);
			}
			for (int k = 1; k < paths.Length; k++)
			{
				EditorSceneManager.OpenScene(paths[k], OpenSceneMode.Additive);
			}
			NavMeshBuilder.BuildNavMesh();
			UnityEngine.Object sceneNavMeshData = NavMeshBuilder.sceneNavMeshData;
			for (int l = 0; l < paths.Length; l++)
			{
				if (EditorSceneManager.OpenScene(paths[l]).IsValid())
				{
					NavMeshBuilder.sceneNavMeshData = sceneNavMeshData;
					EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
				}
			}
		}
	}
}
