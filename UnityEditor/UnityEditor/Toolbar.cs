using System;
using UnityEditor.Connect;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	internal class Toolbar : GUIView
	{
		private static GUIContent[] s_ToolIcons;

		private static GUIContent[] s_ViewToolIcons;

		private static GUIContent[] s_PivotIcons;

		private static GUIContent[] s_PivotRotation;

		private static GUIContent s_LayerContent;

		private static GUIContent[] s_PlayIcons;

		private static GUIContent s_CloudIcon;

		private bool t1;

		private bool t2;

		private bool t3;

		private static GUIContent[] s_ShownToolIcons = new GUIContent[5];

		public static Toolbar get = null;

		[SerializeField]
		private string m_LastLoadedLayoutName;

		internal static string lastLoadedLayoutName
		{
			get
			{
				return (!string.IsNullOrEmpty(Toolbar.get.m_LastLoadedLayoutName)) ? Toolbar.get.m_LastLoadedLayoutName : "Layout";
			}
			set
			{
				Toolbar.get.m_LastLoadedLayoutName = value;
				Toolbar.get.Repaint();
			}
		}

		private static void InitializeToolIcons()
		{
			if (Toolbar.s_ToolIcons != null)
			{
				return;
			}
			Toolbar.s_ToolIcons = new GUIContent[]
			{
				EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects."),
				EditorGUIUtility.IconContent("RotateTool", "|Rotate the selected objects."),
				EditorGUIUtility.IconContent("ScaleTool", "|Scale the selected objects."),
				EditorGUIUtility.IconContent("RectTool"),
				EditorGUIUtility.IconContent("MoveTool On"),
				EditorGUIUtility.IconContent("RotateTool On"),
				EditorGUIUtility.IconContent("ScaleTool On"),
				EditorGUIUtility.IconContent("RectTool On")
			};
			Toolbar.s_ViewToolIcons = new GUIContent[]
			{
				EditorGUIUtility.IconContent("ViewToolOrbit", "|Orbit the Scene view."),
				EditorGUIUtility.IconContent("ViewToolMove"),
				EditorGUIUtility.IconContent("ViewToolZoom"),
				EditorGUIUtility.IconContent("ViewToolOrbit", "|Orbit the Scene view."),
				EditorGUIUtility.IconContent("ViewToolOrbit On"),
				EditorGUIUtility.IconContent("ViewToolMove On"),
				EditorGUIUtility.IconContent("ViewToolZoom On"),
				EditorGUIUtility.IconContent("ViewToolOrbit On")
			};
			Toolbar.s_PivotIcons = new GUIContent[]
			{
				EditorGUIUtility.TextContentWithIcon("Center|The tool handle is placed at the center of the selection.", "ToolHandleCenter"),
				EditorGUIUtility.TextContentWithIcon("Pivot|The tool handle is placed at the active object's pivot point.", "ToolHandlePivot")
			};
			Toolbar.s_PivotRotation = new GUIContent[]
			{
				EditorGUIUtility.TextContentWithIcon("Local|Tool handles are in active object's rotation.", "ToolHandleLocal"),
				EditorGUIUtility.TextContentWithIcon("Global|Tool handles are in global rotation.", "ToolHandleGlobal")
			};
			Toolbar.s_LayerContent = EditorGUIUtility.TextContent("Layers|Which layers are visible in the Scene views.");
			Toolbar.s_PlayIcons = new GUIContent[]
			{
				EditorGUIUtility.IconContent("PlayButton"),
				EditorGUIUtility.IconContent("PauseButton"),
				EditorGUIUtility.IconContent("StepButton"),
				EditorGUIUtility.IconContent("PlayButtonProfile"),
				EditorGUIUtility.IconContent("PlayButton On"),
				EditorGUIUtility.IconContent("PauseButton On"),
				EditorGUIUtility.IconContent("StepButton On"),
				EditorGUIUtility.IconContent("PlayButtonProfile On"),
				EditorGUIUtility.IconContent("PlayButton Anim"),
				EditorGUIUtility.IconContent("PauseButton Anim"),
				EditorGUIUtility.IconContent("StepButton Anim"),
				EditorGUIUtility.IconContent("PlayButtonProfile Anim")
			};
			Toolbar.s_CloudIcon = EditorGUIUtility.IconContent("CloudConnect");
		}

		public void OnEnable()
		{
			EditorApplication.modifierKeysChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.modifierKeysChanged, new EditorApplication.CallbackFunction(base.Repaint));
			Undo.undoRedoPerformed = (Undo.UndoRedoCallback)Delegate.Combine(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.OnSelectionChange));
			UnityConnect.instance.StateChanged += new StateChangedDelegate(this.OnUnityConnectStateChanged);
			Toolbar.get = this;
		}

		public void OnDisable()
		{
			EditorApplication.modifierKeysChanged = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.modifierKeysChanged, new EditorApplication.CallbackFunction(base.Repaint));
			Undo.undoRedoPerformed = (Undo.UndoRedoCallback)Delegate.Remove(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.OnSelectionChange));
			UnityConnect.instance.StateChanged -= new StateChangedDelegate(this.OnUnityConnectStateChanged);
		}

		protected override bool OnFocus()
		{
			return false;
		}

		private void OnSelectionChange()
		{
			Tools.OnSelectionChange();
			base.Repaint();
		}

		protected void OnUnityConnectStateChanged(ConnectInfo state)
		{
			base.Repaint();
		}

		private Rect GetThinArea(Rect pos)
		{
			return new Rect(pos.x, 7f, pos.width, 18f);
		}

		private Rect GetThickArea(Rect pos)
		{
			return new Rect(pos.x, 5f, pos.width, 24f);
		}

		private void ReserveWidthLeft(float width, ref Rect pos)
		{
			pos.x -= width;
			pos.width = width;
		}

		private void ReserveWidthRight(float width, ref Rect pos)
		{
			pos.x += pos.width;
			pos.width = width;
		}

		private void OnGUI()
		{
			float width = 10f;
			float width2 = 20f;
			float num = 32f;
			float num2 = 64f;
			float width3 = 80f;
			Toolbar.InitializeToolIcons();
			bool isPlayingOrWillChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode;
			if (isPlayingOrWillChangePlaymode)
			{
				GUI.color = HostView.kPlayModeDarken;
			}
			GUIStyle gUIStyle = "AppToolbar";
			if (Event.current.type == EventType.Repaint)
			{
				gUIStyle.Draw(new Rect(0f, 0f, base.position.width, base.position.height), false, false, false, false);
			}
			Rect pos = new Rect(0f, 0f, 0f, 0f);
			this.ReserveWidthRight(width, ref pos);
			this.ReserveWidthRight(num * 5f, ref pos);
			this.DoToolButtons(this.GetThickArea(pos));
			this.ReserveWidthRight(width2, ref pos);
			this.ReserveWidthRight(num2 * 2f, ref pos);
			this.DoPivotButtons(this.GetThinArea(pos));
			float num3 = 100f;
			pos = new Rect((base.position.width - num3) / 2f, 0f, 140f, 0f);
			GUILayout.BeginArea(this.GetThickArea(pos));
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			this.DoPlayButtons(isPlayingOrWillChangePlaymode);
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
			pos = new Rect(base.position.width, 0f, 0f, 0f);
			this.ReserveWidthLeft(width, ref pos);
			this.ReserveWidthLeft(width3, ref pos);
			this.DoLayoutDropDown(this.GetThinArea(pos));
			this.ReserveWidthLeft(width, ref pos);
			this.ReserveWidthLeft(width3, ref pos);
			this.DoLayersDropDown(this.GetThinArea(pos));
			this.ReserveWidthLeft(width2, ref pos);
			this.ReserveWidthLeft(width3, ref pos);
			if (EditorGUI.ButtonMouseDown(this.GetThinArea(pos), new GUIContent("Account"), FocusType.Passive, "Dropdown"))
			{
				this.ShowUserMenu(this.GetThinArea(pos));
			}
			this.ReserveWidthLeft(width, ref pos);
			this.ReserveWidthLeft(32f, ref pos);
			if (GUI.Button(this.GetThinArea(pos), Toolbar.s_CloudIcon, "Button"))
			{
				UnityConnectServiceCollection.instance.ShowService("Hub", true);
			}
			EditorGUI.ShowRepaints();
			Highlighter.ControlHighlightGUI(this);
		}

		private void ShowUserMenu(Rect dropDownRect)
		{
			GenericMenu genericMenu = new GenericMenu();
			if (!UnityConnect.instance.online)
			{
				genericMenu.AddDisabledItem(new GUIContent("Go to account"));
				genericMenu.AddDisabledItem(new GUIContent("Sign in..."));
				if (!Application.HasProLicense())
				{
					genericMenu.AddSeparator(string.Empty);
					genericMenu.AddDisabledItem(new GUIContent("Upgrade to Pro"));
				}
			}
			else
			{
				string accountUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudWebauth);
				if (UnityConnect.instance.loggedIn)
				{
					genericMenu.AddItem(new GUIContent("Go to account"), false, delegate
					{
						UnityConnect.instance.OpenAuthorizedURLInWebBrowser(accountUrl);
					});
				}
				else
				{
					genericMenu.AddDisabledItem(new GUIContent("Go to account"));
				}
				if (UnityConnect.instance.loggedIn)
				{
					string text = "Sign out " + UnityConnect.instance.userInfo.displayName;
					genericMenu.AddItem(new GUIContent(text), false, delegate
					{
						UnityConnect.instance.Logout();
					});
				}
				else
				{
					genericMenu.AddItem(new GUIContent("Sign in..."), false, delegate
					{
						UnityConnect.instance.ShowLogin();
					});
				}
				if (!Application.HasProLicense())
				{
					genericMenu.AddSeparator(string.Empty);
					genericMenu.AddItem(new GUIContent("Upgrade to Pro"), false, delegate
					{
						Application.OpenURL("https://store.unity3d.com/");
					});
				}
			}
			genericMenu.DropDown(dropDownRect);
		}

		private void DoToolButtons(Rect rect)
		{
			GUI.changed = false;
			int num = (int)((!Tools.viewToolActive) ? Tools.current : Tool.View);
			for (int i = 1; i < 5; i++)
			{
				Toolbar.s_ShownToolIcons[i] = Toolbar.s_ToolIcons[i - 1 + ((i != num) ? 0 : 4)];
				Toolbar.s_ShownToolIcons[i].tooltip = Toolbar.s_ToolIcons[i - 1].tooltip;
			}
			Toolbar.s_ShownToolIcons[0] = Toolbar.s_ViewToolIcons[(int)(Tools.viewTool + ((num != 0) ? 0 : 4))];
			num = GUI.Toolbar(rect, num, Toolbar.s_ShownToolIcons, "Command");
			if (GUI.changed)
			{
				Tools.current = (Tool)num;
			}
		}

		private void DoPivotButtons(Rect rect)
		{
			Tools.pivotMode = (PivotMode)EditorGUI.CycleButton(new Rect(rect.x, rect.y, rect.width / 2f, rect.height), (int)Tools.pivotMode, Toolbar.s_PivotIcons, "ButtonLeft");
			if (Tools.current == Tool.Scale && Selection.transforms.Length < 2)
			{
				GUI.enabled = false;
			}
			PivotRotation pivotRotation = (PivotRotation)EditorGUI.CycleButton(new Rect(rect.x + rect.width / 2f, rect.y, rect.width / 2f, rect.height), (int)Tools.pivotRotation, Toolbar.s_PivotRotation, "ButtonRight");
			if (Tools.pivotRotation != pivotRotation)
			{
				Tools.pivotRotation = pivotRotation;
				if (pivotRotation == PivotRotation.Global)
				{
					Tools.ResetGlobalHandleRotation();
				}
			}
			if (Tools.current == Tool.Scale)
			{
				GUI.enabled = true;
			}
			if (GUI.changed)
			{
				Tools.RepaintAllToolViews();
			}
		}

		private void DoPlayButtons(bool isOrWillEnterPlaymode)
		{
			bool isPlaying = EditorApplication.isPlaying;
			GUI.changed = false;
			int num = (!isPlaying) ? 0 : 4;
			if (AnimationMode.InAnimationMode())
			{
				num = 8;
			}
			Color color = GUI.color + new Color(0.01f, 0.01f, 0.01f, 0.01f);
			GUI.contentColor = new Color(1f / color.r, 1f / color.g, 1f / color.g, 1f / color.a);
			GUILayout.Toggle(isOrWillEnterPlaymode, Toolbar.s_PlayIcons[num], "CommandLeft", new GUILayoutOption[0]);
			GUI.backgroundColor = Color.white;
			if (GUI.changed)
			{
				Toolbar.TogglePlaying();
				GUIUtility.ExitGUI();
			}
			GUI.changed = false;
			bool isPaused = GUILayout.Toggle(EditorApplication.isPaused, Toolbar.s_PlayIcons[num + 1], "CommandMid", new GUILayoutOption[0]);
			if (GUI.changed)
			{
				EditorApplication.isPaused = isPaused;
				GUIUtility.ExitGUI();
			}
			if (GUILayout.Button(Toolbar.s_PlayIcons[num + 2], "CommandRight", new GUILayoutOption[0]))
			{
				EditorApplication.Step();
				GUIUtility.ExitGUI();
			}
		}

		private void DoLayersDropDown(Rect rect)
		{
			GUIStyle style = "DropDown";
			if (EditorGUI.ButtonMouseDown(rect, Toolbar.s_LayerContent, FocusType.Passive, style) && LayerVisibilityWindow.ShowAtPosition(rect))
			{
				GUIUtility.ExitGUI();
			}
		}

		private void DoLayoutDropDown(Rect rect)
		{
			if (EditorGUI.ButtonMouseDown(rect, GUIContent.Temp(Toolbar.lastLoadedLayoutName), FocusType.Passive, "DropDown"))
			{
				Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
				rect.x = vector.x;
				rect.y = vector.y;
				EditorUtility.Internal_DisplayPopupMenu(rect, "Window/Layouts", this, 0);
			}
		}

		private static void InternalWillTogglePlaymode()
		{
			InternalEditorUtility.RepaintAllViews();
		}

		private static void TogglePlaying()
		{
			bool isPlaying = !EditorApplication.isPlaying;
			EditorApplication.isPlaying = isPlaying;
			Toolbar.InternalWillTogglePlaymode();
		}

		internal static void RepaintToolbar()
		{
			if (Toolbar.get != null)
			{
				Toolbar.get.Repaint();
			}
		}

		public float CalcHeight()
		{
			return 30f;
		}
	}
}
