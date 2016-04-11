using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	internal class WindowLayout
	{
		private const string kMaximizeRestoreFile = "CurrentMaximizeLayout.dwlt";

		internal static PrefKey s_MaximizeKey = new PrefKey("Window/Maximize View", "# ");

		internal static string layoutsPreferencesPath
		{
			get
			{
				return InternalEditorUtility.unityPreferencesFolder + "/Layouts";
			}
		}

		internal static string layoutsProjectPath
		{
			get
			{
				return Directory.GetCurrentDirectory() + "/Library";
			}
		}

		private static void ShowWindowImmediate(EditorWindow win)
		{
			win.Show(true);
		}

		internal static EditorWindow FindEditorWindowOfType(Type type)
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(type);
			if (array.Length > 0)
			{
				return array[0] as EditorWindow;
			}
			return null;
		}

		[DebuggerHidden]
		private static IEnumerable<T> FindEditorWindowsOfType<T>() where T : class
		{
			WindowLayout.<FindEditorWindowsOfType>c__Iterator6<T> <FindEditorWindowsOfType>c__Iterator = new WindowLayout.<FindEditorWindowsOfType>c__Iterator6<T>();
			WindowLayout.<FindEditorWindowsOfType>c__Iterator6<T> expr_07 = <FindEditorWindowsOfType>c__Iterator;
			expr_07.$PC = -2;
			return expr_07;
		}

		internal static void CheckWindowConsistency()
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
			UnityEngine.Object[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				EditorWindow editorWindow = (EditorWindow)array2[i];
				if (editorWindow.m_Parent == null)
				{
					UnityEngine.Debug.LogError("Invalid editor window " + editorWindow.GetType());
				}
			}
		}

		internal static EditorWindow TryGetLastFocusedWindowInSameDock()
		{
			Type type = null;
			string lastWindowTypeInSameDock = WindowFocusState.instance.m_LastWindowTypeInSameDock;
			if (lastWindowTypeInSameDock != string.Empty)
			{
				type = Type.GetType(lastWindowTypeInSameDock);
			}
			GameView gameView = WindowLayout.FindEditorWindowOfType(typeof(GameView)) as GameView;
			if (type != null && gameView && gameView.m_Parent != null && gameView.m_Parent is DockArea)
			{
				object[] array = Resources.FindObjectsOfTypeAll(type);
				DockArea y = gameView.m_Parent as DockArea;
				for (int i = 0; i < array.Length; i++)
				{
					EditorWindow editorWindow = array[i] as EditorWindow;
					if (editorWindow && editorWindow.m_Parent == y)
					{
						return editorWindow;
					}
				}
			}
			return null;
		}

		internal static void SaveCurrentFocusedWindowInSameDock(EditorWindow windowToBeFocused)
		{
			if (windowToBeFocused.m_Parent != null && windowToBeFocused.m_Parent is DockArea)
			{
				DockArea dockArea = windowToBeFocused.m_Parent as DockArea;
				EditorWindow actualView = dockArea.actualView;
				if (actualView)
				{
					WindowFocusState.instance.m_LastWindowTypeInSameDock = actualView.GetType().ToString();
				}
			}
		}

		internal static void FindFirstGameViewAndSetToMaximizeOnPlay()
		{
			GameView gameView = (GameView)WindowLayout.FindEditorWindowOfType(typeof(GameView));
			if (gameView)
			{
				gameView.maximizeOnPlay = true;
			}
		}

		internal static EditorWindow TryFocusAppropriateWindow(bool enteringPlaymode)
		{
			if (enteringPlaymode)
			{
				GameView gameView = (GameView)WindowLayout.FindEditorWindowOfType(typeof(GameView));
				if (gameView)
				{
					WindowLayout.SaveCurrentFocusedWindowInSameDock(gameView);
					gameView.Focus();
				}
				return gameView;
			}
			EditorWindow editorWindow = WindowLayout.TryGetLastFocusedWindowInSameDock();
			if (editorWindow)
			{
				editorWindow.ShowTab();
			}
			return editorWindow;
		}

		internal static EditorWindow GetMaximizedWindow()
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(MaximizedHostView));
			if (array.Length != 0)
			{
				MaximizedHostView maximizedHostView = array[0] as MaximizedHostView;
				if (maximizedHostView.actualView)
				{
					return maximizedHostView.actualView;
				}
			}
			return null;
		}

		internal static EditorWindow ShowAppropriateViewOnEnterExitPlaymode(bool entering)
		{
			if (WindowFocusState.instance.m_CurrentlyInPlayMode == entering)
			{
				return null;
			}
			WindowFocusState.instance.m_CurrentlyInPlayMode = entering;
			EditorWindow maximizedWindow = WindowLayout.GetMaximizedWindow();
			if (entering)
			{
				WindowFocusState.instance.m_WasMaximizedBeforePlay = (maximizedWindow != null);
				if (maximizedWindow != null)
				{
					return maximizedWindow;
				}
			}
			else if (WindowFocusState.instance.m_WasMaximizedBeforePlay)
			{
				return maximizedWindow;
			}
			if (maximizedWindow)
			{
				WindowLayout.Unmaximize(maximizedWindow);
			}
			EditorWindow editorWindow = WindowLayout.TryFocusAppropriateWindow(entering);
			if (editorWindow)
			{
				return editorWindow;
			}
			if (entering)
			{
				EditorWindow editorWindow2 = WindowLayout.FindEditorWindowOfType(typeof(SceneView));
				GameView gameView;
				if (editorWindow2 && editorWindow2.m_Parent is DockArea)
				{
					DockArea dockArea = editorWindow2.m_Parent as DockArea;
					if (dockArea)
					{
						WindowFocusState.instance.m_LastWindowTypeInSameDock = editorWindow2.GetType().ToString();
						gameView = ScriptableObject.CreateInstance<GameView>();
						dockArea.AddTab(gameView);
						return gameView;
					}
				}
				gameView = ScriptableObject.CreateInstance<GameView>();
				gameView.Show(true);
				gameView.Focus();
				return gameView;
			}
			return editorWindow;
		}

		internal static bool IsMaximized(EditorWindow window)
		{
			return window.m_Parent is MaximizedHostView;
		}

		internal static void MaximizeKeyHandler()
		{
			if ((WindowLayout.s_MaximizeKey.activated || Event.current.type == EditorGUIUtility.magnifyGestureEventType) && GUIUtility.hotControl == 0)
			{
				EventType type = Event.current.type;
				Event.current.Use();
				EditorWindow mouseOverWindow = EditorWindow.mouseOverWindow;
				if (mouseOverWindow && !(mouseOverWindow is PreviewWindow))
				{
					if (type == EditorGUIUtility.magnifyGestureEventType)
					{
						if ((double)Event.current.delta.x < -0.05)
						{
							if (WindowLayout.IsMaximized(mouseOverWindow))
							{
								WindowLayout.Unmaximize(mouseOverWindow);
							}
						}
						else if ((double)Event.current.delta.x > 0.05 && !WindowLayout.IsMaximized(mouseOverWindow))
						{
							WindowLayout.Maximize(mouseOverWindow);
						}
					}
					else if (WindowLayout.IsMaximized(mouseOverWindow))
					{
						WindowLayout.Unmaximize(mouseOverWindow);
					}
					else
					{
						WindowLayout.Maximize(mouseOverWindow);
					}
				}
			}
		}

		public static void Unmaximize(EditorWindow win)
		{
			HostView parent = win.m_Parent;
			if (parent == null)
			{
				UnityEngine.Debug.LogError("Host view was not found");
				WindowLayout.RevertFactorySettings();
				return;
			}
			UnityEngine.Object[] array = InternalEditorUtility.LoadSerializedFileAndForget(Path.Combine(WindowLayout.layoutsProjectPath, "CurrentMaximizeLayout.dwlt"));
			if (array.Length < 2)
			{
				UnityEngine.Debug.Log("Maximized serialized file backup not found");
				WindowLayout.RevertFactorySettings();
				return;
			}
			SplitView splitView = array[0] as SplitView;
			EditorWindow editorWindow = array[1] as EditorWindow;
			if (splitView == null)
			{
				UnityEngine.Debug.Log("Maximization failed because the root split view was not found");
				WindowLayout.RevertFactorySettings();
				return;
			}
			ContainerWindow window = win.m_Parent.window;
			if (window == null)
			{
				UnityEngine.Debug.Log("Maximization failed because the root split view has no container window");
				WindowLayout.RevertFactorySettings();
				return;
			}
			try
			{
				ContainerWindow.SetFreezeDisplay(true);
				if (!parent.parent)
				{
					throw new Exception();
				}
				int idx = parent.parent.IndexOfChild(parent);
				Rect position = parent.position;
				View parent2 = parent.parent;
				parent2.RemoveChild(idx);
				parent2.AddChild(splitView, idx);
				splitView.position = position;
				DockArea dockArea = editorWindow.m_Parent as DockArea;
				int idx2 = dockArea.m_Panes.IndexOf(editorWindow);
				parent.actualView = null;
				win.m_Parent = null;
				dockArea.AddTab(idx2, win);
				dockArea.RemoveTab(editorWindow);
				UnityEngine.Object.DestroyImmediate(editorWindow);
				UnityEngine.Object[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					UnityEngine.Object @object = array2[i];
					EditorWindow editorWindow2 = @object as EditorWindow;
					if (editorWindow2 != null)
					{
						editorWindow2.MakeParentsSettingsMatchMe();
					}
				}
				parent2.Initialize(parent2.window);
				parent2.position = parent2.position;
				splitView.Reflow();
				UnityEngine.Object.DestroyImmediate(parent);
				win.Focus();
				window.DisplayAllViews();
				win.m_Parent.MakeVistaDWMHappyDance();
			}
			catch (Exception arg)
			{
				UnityEngine.Debug.Log("Maximization failed: " + arg);
				WindowLayout.RevertFactorySettings();
			}
			try
			{
				if (Application.platform == RuntimePlatform.OSXEditor && SystemInfo.operatingSystem.Contains("10.7") && SystemInfo.graphicsDeviceVendor.Contains("ATI"))
				{
					UnityEngine.Object[] array3 = Resources.FindObjectsOfTypeAll(typeof(GUIView));
					for (int j = 0; j < array3.Length; j++)
					{
						GUIView gUIView = (GUIView)array3[j];
						gUIView.Repaint();
					}
				}
			}
			finally
			{
				ContainerWindow.SetFreezeDisplay(false);
			}
		}

		public static void AddSplitViewAndChildrenRecurse(View splitview, ArrayList list)
		{
			list.Add(splitview);
			DockArea dockArea = splitview as DockArea;
			if (dockArea != null)
			{
				list.AddRange(dockArea.m_Panes);
			}
			HostView x = splitview as DockArea;
			if (x != null)
			{
				list.Add(dockArea.actualView);
			}
			View[] children = splitview.children;
			for (int i = 0; i < children.Length; i++)
			{
				View splitview2 = children[i];
				WindowLayout.AddSplitViewAndChildrenRecurse(splitview2, list);
			}
		}

		public static void SaveSplitViewAndChildren(View splitview, EditorWindow win, string path)
		{
			ArrayList arrayList = new ArrayList();
			WindowLayout.AddSplitViewAndChildrenRecurse(splitview, arrayList);
			arrayList.Remove(splitview);
			arrayList.Remove(win);
			arrayList.Insert(0, splitview);
			arrayList.Insert(1, win);
			InternalEditorUtility.SaveToSerializedFileAndForget(arrayList.ToArray(typeof(UnityEngine.Object)) as UnityEngine.Object[], path, false);
		}

		public static void Maximize(EditorWindow win)
		{
			View view = WindowLayout.MaximizePrepare(win);
			if (view)
			{
				WindowLayout.MaximizePresent(win, view);
			}
		}

		public static View MaximizePrepare(EditorWindow win)
		{
			View parent = win.m_Parent.parent;
			View view = parent;
			while (parent != null && parent is SplitView)
			{
				view = parent;
				parent = parent.parent;
			}
			DockArea dockArea = win.m_Parent as DockArea;
			if (dockArea == null)
			{
				return null;
			}
			if (parent == null)
			{
				return null;
			}
			MainWindow x = view.parent as MainWindow;
			if (x == null)
			{
				return null;
			}
			ContainerWindow window = win.m_Parent.window;
			if (window == null)
			{
				return null;
			}
			int num = dockArea.m_Panes.IndexOf(win);
			if (num == -1)
			{
				return null;
			}
			dockArea.selected = num;
			WindowLayout.SaveSplitViewAndChildren(view, win, Path.Combine(WindowLayout.layoutsProjectPath, "CurrentMaximizeLayout.dwlt"));
			dockArea.m_Panes[num] = null;
			MaximizedHostView maximizedHostView = ScriptableObject.CreateInstance<MaximizedHostView>();
			int idx = parent.IndexOfChild(view);
			Rect position = view.position;
			parent.RemoveChild(view);
			parent.AddChild(maximizedHostView, idx);
			maximizedHostView.position = position;
			maximizedHostView.actualView = win;
			return view;
		}

		public static void MaximizePresent(EditorWindow win, View rootSplit)
		{
			ContainerWindow.SetFreezeDisplay(true);
			UnityEngine.Object.DestroyImmediate(rootSplit, true);
			win.Focus();
			WindowLayout.CheckWindowConsistency();
			ContainerWindow window = win.m_Parent.window;
			window.DisplayAllViews();
			win.m_Parent.MakeVistaDWMHappyDance();
			ContainerWindow.SetFreezeDisplay(false);
		}

		public static bool LoadWindowLayout(string path, bool newProjectLayoutWasCreated)
		{
			Rect position = default(Rect);
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
			UnityEngine.Object[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				ContainerWindow containerWindow = (ContainerWindow)array2[i];
				if (containerWindow.showMode == ShowMode.MainWindow)
				{
					position = containerWindow.position;
				}
			}
			try
			{
				ContainerWindow.SetFreezeDisplay(true);
				WindowLayout.CloseWindows();
				UnityEngine.Object[] array3 = InternalEditorUtility.LoadSerializedFileAndForget(path);
				ContainerWindow containerWindow2 = null;
				ContainerWindow containerWindow3 = null;
				UnityEngine.Object[] array4 = array3;
				for (int j = 0; j < array4.Length; j++)
				{
					UnityEngine.Object @object = array4[j];
					ContainerWindow containerWindow4 = @object as ContainerWindow;
					if (containerWindow4 != null && containerWindow4.showMode == ShowMode.MainWindow)
					{
						containerWindow3 = containerWindow4;
						if ((double)position.width != 0.0)
						{
							containerWindow2 = containerWindow4;
							containerWindow2.position = position;
						}
					}
				}
				int num = 0;
				UnityEngine.Object[] array5 = array3;
				for (int k = 0; k < array5.Length; k++)
				{
					UnityEngine.Object object2 = array5[k];
					if (object2 == null)
					{
						UnityEngine.Debug.LogError("Error while reading window layout: window #" + num + " is null");
						throw new Exception();
					}
					if (object2.GetType() == null)
					{
						UnityEngine.Debug.LogError(string.Concat(new object[]
						{
							"Error while reading window layout: window #",
							num,
							" type is null, instanceID=",
							object2.GetInstanceID()
						}));
						throw new Exception();
					}
					if (newProjectLayoutWasCreated)
					{
						MethodInfo method = object2.GetType().GetMethod("OnNewProjectLayoutWasCreated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (method != null)
						{
							method.Invoke(object2, null);
						}
					}
					num++;
				}
				if (containerWindow2)
				{
					containerWindow2.position = position;
					containerWindow2.OnResize();
				}
				if (containerWindow3 == null)
				{
					UnityEngine.Debug.LogError("Error while reading window layout: no main window found");
					throw new Exception();
				}
				containerWindow3.Show(containerWindow3.showMode, true, true);
				UnityEngine.Object[] array6 = array3;
				for (int l = 0; l < array6.Length; l++)
				{
					UnityEngine.Object object3 = array6[l];
					EditorWindow editorWindow = object3 as EditorWindow;
					if (editorWindow)
					{
						editorWindow.minSize = editorWindow.minSize;
					}
					ContainerWindow containerWindow5 = object3 as ContainerWindow;
					if (containerWindow5 && containerWindow5 != containerWindow3)
					{
						containerWindow5.Show(containerWindow5.showMode, true, true);
					}
				}
				GameView gameView = WindowLayout.GetMaximizedWindow() as GameView;
				if (gameView != null && gameView.maximizeOnPlay)
				{
					WindowLayout.Unmaximize(gameView);
				}
			}
			catch (Exception arg)
			{
				UnityEngine.Debug.LogError("Failed to load window layout: " + arg);
				switch (EditorUtility.DisplayDialogComplex("Failed to load window layout", "This can happen if layout contains custom windows and there are compile errors in the project.", "Load Default Layout", "Quit", "Revert Factory Settings"))
				{
				case 0:
					WindowLayout.LoadDefaultLayout();
					break;
				case 1:
					EditorApplication.Exit(0);
					break;
				case 2:
					WindowLayout.RevertFactorySettings();
					break;
				}
				return false;
			}
			finally
			{
				ContainerWindow.SetFreezeDisplay(false);
				if (Path.GetExtension(path) == ".wlt")
				{
					Toolbar.lastLoadedLayoutName = Path.GetFileNameWithoutExtension(path);
				}
				else
				{
					Toolbar.lastLoadedLayoutName = null;
				}
			}
			return true;
		}

		private static void LoadDefaultLayout()
		{
			InternalEditorUtility.LoadDefaultLayout();
		}

		public static void CloseWindows()
		{
			try
			{
				TooltipView.Close();
			}
			catch (Exception)
			{
			}
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
			UnityEngine.Object[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				ContainerWindow containerWindow = (ContainerWindow)array2[i];
				try
				{
					containerWindow.Close();
				}
				catch (Exception)
				{
				}
			}
			UnityEngine.Object[] array3 = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
			if (array3.Length != 0)
			{
				string text = string.Empty;
				UnityEngine.Object[] array4 = array3;
				for (int j = 0; j < array4.Length; j++)
				{
					EditorWindow editorWindow = (EditorWindow)array4[j];
					text = text + "\n" + editorWindow.GetType().Name;
					UnityEngine.Object.DestroyImmediate(editorWindow, true);
				}
				UnityEngine.Debug.LogError("Failed to destroy editor windows: #" + array3.Length + text);
			}
			UnityEngine.Object[] array5 = Resources.FindObjectsOfTypeAll(typeof(View));
			if (array5.Length != 0)
			{
				string text2 = string.Empty;
				UnityEngine.Object[] array6 = array5;
				for (int k = 0; k < array6.Length; k++)
				{
					View view = (View)array6[k];
					text2 = text2 + "\n" + view.GetType().Name;
					UnityEngine.Object.DestroyImmediate(view, true);
				}
				UnityEngine.Debug.LogError("Failed to destroy views: #" + array5.Length + text2);
			}
		}

		public static void SaveWindowLayout(string path)
		{
			TooltipView.Close();
			ArrayList arrayList = new ArrayList();
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
			UnityEngine.Object[] array2 = Resources.FindObjectsOfTypeAll(typeof(ContainerWindow));
			UnityEngine.Object[] array3 = Resources.FindObjectsOfTypeAll(typeof(View));
			UnityEngine.Object[] array4 = array2;
			for (int i = 0; i < array4.Length; i++)
			{
				ContainerWindow containerWindow = (ContainerWindow)array4[i];
				if (!containerWindow.m_DontSaveToLayout)
				{
					arrayList.Add(containerWindow);
				}
			}
			UnityEngine.Object[] array5 = array3;
			for (int j = 0; j < array5.Length; j++)
			{
				View view = (View)array5[j];
				if (!(view.window != null) || !view.window.m_DontSaveToLayout)
				{
					arrayList.Add(view);
				}
			}
			UnityEngine.Object[] array6 = array;
			for (int k = 0; k < array6.Length; k++)
			{
				EditorWindow editorWindow = (EditorWindow)array6[k];
				if (!(editorWindow.m_Parent != null) || !(editorWindow.m_Parent.window != null) || !editorWindow.m_Parent.window.m_DontSaveToLayout)
				{
					arrayList.Add(editorWindow);
				}
			}
			InternalEditorUtility.SaveToSerializedFileAndForget(arrayList.ToArray(typeof(UnityEngine.Object)) as UnityEngine.Object[], path, false);
		}

		public static void EnsureMainWindowHasBeenLoaded()
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(MainWindow));
			if (array.Length == 0)
			{
				MainWindow.MakeMain();
			}
		}

		internal static MainWindow FindMainWindow()
		{
			UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(MainWindow));
			if (array.Length == 0)
			{
				UnityEngine.Debug.LogError("No Main Window found!");
				return null;
			}
			return array[0] as MainWindow;
		}

		public static void SaveGUI()
		{
			View view = WindowLayout.FindMainWindow();
			Rect screenPosition = view.screenPosition;
			SaveWindowLayout windowWithRect = EditorWindow.GetWindowWithRect<SaveWindowLayout>(new Rect(screenPosition.xMax - 180f, screenPosition.y + 20f, 200f, 48f), true, "Save Window Layout");
			windowWithRect.m_Parent.window.m_DontSaveToLayout = true;
		}

		private static void RevertFactorySettings()
		{
			InternalEditorUtility.RevertFactoryLayoutSettings(true);
		}

		public static void DeleteGUI()
		{
			View view = WindowLayout.FindMainWindow();
			Rect screenPosition = view.screenPosition;
			DeleteWindowLayout windowWithRect = EditorWindow.GetWindowWithRect<DeleteWindowLayout>(new Rect(screenPosition.xMax - 180f, screenPosition.y + 20f, 200f, 150f), true, "Delete Window Layout");
			windowWithRect.m_Parent.window.m_DontSaveToLayout = true;
		}
	}
}
