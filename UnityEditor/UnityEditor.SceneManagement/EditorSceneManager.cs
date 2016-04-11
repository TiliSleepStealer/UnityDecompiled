using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
	public sealed class EditorSceneManager : SceneManager
	{
		public static extern int loadedSceneCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static Scene OpenScene(string scenePath, [DefaultValue("OpenSceneMode.Single")] OpenSceneMode mode)
		{
			Scene result;
			EditorSceneManager.INTERNAL_CALL_OpenScene(scenePath, mode, out result);
			return result;
		}

		[ExcludeFromDocs]
		public static Scene OpenScene(string scenePath)
		{
			OpenSceneMode mode = OpenSceneMode.Single;
			Scene result;
			EditorSceneManager.INTERNAL_CALL_OpenScene(scenePath, mode, out result);
			return result;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_OpenScene(string scenePath, OpenSceneMode mode, out Scene value);

		public static Scene NewScene(NewSceneSetup setup, [DefaultValue("NewSceneMode.Single")] NewSceneMode mode)
		{
			Scene result;
			EditorSceneManager.INTERNAL_CALL_NewScene(setup, mode, out result);
			return result;
		}

		[ExcludeFromDocs]
		public static Scene NewScene(NewSceneSetup setup)
		{
			NewSceneMode mode = NewSceneMode.Single;
			Scene result;
			EditorSceneManager.INTERNAL_CALL_NewScene(setup, mode, out result);
			return result;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_NewScene(NewSceneSetup setup, NewSceneMode mode, out Scene value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool CreateSceneAsset(string scenePath, bool createDefaultGameObjects);

		public static bool CloseScene(Scene scene, bool removeScene)
		{
			return EditorSceneManager.INTERNAL_CALL_CloseScene(ref scene, removeScene);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_CloseScene(ref Scene scene, bool removeScene);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void SetTargetSceneForNewGameObjects(int sceneHandle);

		internal static Scene GetSceneByHandle(int handle)
		{
			Scene result;
			EditorSceneManager.INTERNAL_CALL_GetSceneByHandle(handle, out result);
			return result;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_GetSceneByHandle(int handle, out Scene value);

		public static void MoveSceneBefore(Scene src, Scene dst)
		{
			EditorSceneManager.INTERNAL_CALL_MoveSceneBefore(ref src, ref dst);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_MoveSceneBefore(ref Scene src, ref Scene dst);

		public static void MoveSceneAfter(Scene src, Scene dst)
		{
			EditorSceneManager.INTERNAL_CALL_MoveSceneAfter(ref src, ref dst);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_MoveSceneAfter(ref Scene src, ref Scene dst);

		internal static bool SaveSceneAs(Scene scene)
		{
			return EditorSceneManager.INTERNAL_CALL_SaveSceneAs(ref scene);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_SaveSceneAs(ref Scene scene);

		public static bool SaveScene(Scene scene, [DefaultValue("\"\"")] string dstScenePath, [DefaultValue("false")] bool saveAsCopy)
		{
			return EditorSceneManager.INTERNAL_CALL_SaveScene(ref scene, dstScenePath, saveAsCopy);
		}

		[ExcludeFromDocs]
		public static bool SaveScene(Scene scene, string dstScenePath)
		{
			bool saveAsCopy = false;
			return EditorSceneManager.INTERNAL_CALL_SaveScene(ref scene, dstScenePath, saveAsCopy);
		}

		[ExcludeFromDocs]
		public static bool SaveScene(Scene scene)
		{
			bool saveAsCopy = false;
			string empty = string.Empty;
			return EditorSceneManager.INTERNAL_CALL_SaveScene(ref scene, empty, saveAsCopy);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_SaveScene(ref Scene scene, string dstScenePath, bool saveAsCopy);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool SaveOpenScenes();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool SaveScenes(Scene[] scenes);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool SaveCurrentModifiedScenesIfUserWantsTo();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool SaveModifiedScenesIfUserWantsTo(Scene[] scenes);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool EnsureUntitledSceneHasBeenSaved(string operation);

		public static bool MarkSceneDirty(Scene scene)
		{
			return EditorSceneManager.INTERNAL_CALL_MarkSceneDirty(ref scene);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_MarkSceneDirty(ref Scene scene);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void MarkAllScenesDirty();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern SceneSetup[] GetSceneManagerSetup();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void RestoreSceneManagerSetup(SceneSetup[] value);
	}
}
