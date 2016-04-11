using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEditor.Modules;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
	[EditorWindowTitle(title = "Game", useTypeNameAsIconName = true)]
	internal class GameView : EditorWindow, IHasCustomMenu
	{
		private const int kToolbarHeight = 17;

		private const int kBorderSize = 5;

		[SerializeField]
		private bool m_MaximizeOnPlay;

		[SerializeField]
		private bool m_Gizmos;

		[SerializeField]
		private bool m_Stats;

		[SerializeField]
		private int[] m_SelectedSizes = new int[0];

		[SerializeField]
		private int m_TargetDisplay;

		private int m_SizeChangeID = -2147483648;

		private GUIContent gizmosContent = new GUIContent("Gizmos");

		private GUIContent renderdocContent;

		private static GUIStyle s_GizmoButtonStyle;

		private static GUIStyle s_ResolutionWarningStyle;

		private static List<GameView> s_GameViews = new List<GameView>();

		private static GameView s_LastFocusedGameView = null;

		private static Rect s_MainGameViewRect = new Rect(0f, 0f, 640f, 480f);

		private Vector2 m_ShownResolution = Vector2.zero;

		private AnimBool m_ResolutionTooLargeWarning = new AnimBool(false);

		public bool maximizeOnPlay
		{
			get
			{
				return this.m_MaximizeOnPlay;
			}
			set
			{
				this.m_MaximizeOnPlay = value;
			}
		}

		private int selectedSizeIndex
		{
			get
			{
				return this.m_SelectedSizes[(int)GameView.currentSizeGroupType];
			}
			set
			{
				this.m_SelectedSizes[(int)GameView.currentSizeGroupType] = value;
			}
		}

		private static GameViewSizeGroupType currentSizeGroupType
		{
			get
			{
				return ScriptableSingleton<GameViewSizes>.instance.currentGroupType;
			}
		}

		private GameViewSize currentGameViewSize
		{
			get
			{
				return ScriptableSingleton<GameViewSizes>.instance.currentGroup.GetGameViewSize(this.selectedSizeIndex);
			}
		}

		private Rect gameViewRenderRect
		{
			get
			{
				return new Rect(0f, 17f, base.position.width, base.position.height - 17f);
			}
		}

		public GameView()
		{
			base.depthBufferBits = 32;
			base.antiAlias = -1;
			base.autoRepaintOnSceneChange = true;
			this.m_TargetDisplay = 0;
		}

		public void OnValidate()
		{
			this.EnsureSelectedSizeAreValid();
		}

		public void OnEnable()
		{
			base.depthBufferBits = 32;
			base.titleContent = base.GetLocalizedTitleContent();
			this.EnsureSelectedSizeAreValid();
			this.renderdocContent = EditorGUIUtility.IconContent("renderdoc", "Capture|Capture the current view and open in RenderDoc");
			base.dontClearBackground = true;
			GameView.s_GameViews.Add(this);
			this.m_ResolutionTooLargeWarning.valueChanged.AddListener(new UnityAction(base.Repaint));
			this.m_ResolutionTooLargeWarning.speed = 0.3f;
		}

		public void OnDisable()
		{
			GameView.s_GameViews.Remove(this);
			this.m_ResolutionTooLargeWarning.valueChanged.RemoveListener(new UnityAction(base.Repaint));
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.DoDelayedGameViewChanged));
		}

		internal static GameView GetMainGameView()
		{
			if (GameView.s_LastFocusedGameView == null && GameView.s_GameViews != null && GameView.s_GameViews.Count > 0)
			{
				GameView.s_LastFocusedGameView = GameView.s_GameViews[0];
			}
			return GameView.s_LastFocusedGameView;
		}

		public static void RepaintAll()
		{
			if (GameView.s_GameViews == null)
			{
				return;
			}
			foreach (GameView current in GameView.s_GameViews)
			{
				current.Repaint();
			}
		}

		internal static Vector2 GetSizeOfMainGameView()
		{
			Rect mainGameViewRenderRect = GameView.GetMainGameViewRenderRect();
			return new Vector2(mainGameViewRenderRect.width, mainGameViewRenderRect.height);
		}

		internal static Rect GetMainGameViewRenderRect()
		{
			GameView mainGameView = GameView.GetMainGameView();
			if (mainGameView != null)
			{
				GameView.s_MainGameViewRect = mainGameView.GetConstrainedGameViewRenderRect();
			}
			return GameView.s_MainGameViewRect;
		}

		private void GameViewAspectWasChanged()
		{
			base.SetInternalGameViewRect(GameView.GetConstrainedGameViewRenderRect(this.gameViewRenderRect, this.selectedSizeIndex));
			EditorApplication.SetSceneRepaintDirty();
		}

		private void AllowCursorLockAndHide(bool enable)
		{
			Unsupported.SetAllowCursorLock(enable);
			Unsupported.SetAllowCursorHide(enable);
		}

		private void OnFocus()
		{
			this.AllowCursorLockAndHide(true);
			GameView.s_LastFocusedGameView = this;
			InternalEditorUtility.OnGameViewFocus(true);
		}

		private void OnLostFocus()
		{
			if (!EditorApplicationLayout.IsInitializingPlaymodeLayout())
			{
				this.AllowCursorLockAndHide(false);
			}
			InternalEditorUtility.OnGameViewFocus(false);
		}

		private void DelayedGameViewChanged()
		{
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(this.DoDelayedGameViewChanged));
		}

		private void DoDelayedGameViewChanged()
		{
			this.GameViewAspectWasChanged();
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.DoDelayedGameViewChanged));
		}

		internal override void OnResized()
		{
			this.DelayedGameViewChanged();
		}

		private void EnsureSelectedSizeAreValid()
		{
			int num = Enum.GetNames(typeof(GameViewSizeGroupType)).Length;
			if (this.m_SelectedSizes.Length != num)
			{
				Array.Resize<int>(ref this.m_SelectedSizes, num);
			}
			using (IEnumerator enumerator = Enum.GetValues(typeof(GameViewSizeGroupType)).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					GameViewSizeGroupType gameViewSizeGroupType = (GameViewSizeGroupType)((int)enumerator.Current);
					GameViewSizeGroup group = ScriptableSingleton<GameViewSizes>.instance.GetGroup(gameViewSizeGroupType);
					int num2 = (int)gameViewSizeGroupType;
					this.m_SelectedSizes[num2] = Mathf.Clamp(this.m_SelectedSizes[num2], 0, group.GetTotalCount() - 1);
				}
			}
		}

		public bool IsShowingGizmos()
		{
			return this.m_Gizmos;
		}

		private void OnSelectionChange()
		{
			if (this.m_Gizmos)
			{
				base.Repaint();
			}
		}

		private void LoadRenderDoc()
		{
			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				RenderDoc.Load();
				ShaderUtil.RecreateGfxDevice();
			}
		}

		public virtual void AddItemsToMenu(GenericMenu menu)
		{
			if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
			{
				menu.AddItem(new GUIContent("Load RenderDoc"), false, new GenericMenu.MenuFunction(this.LoadRenderDoc));
			}
		}

		private bool ShouldShowMultiDisplayOption()
		{
			GUIContent[] displayNames = ModuleManager.GetDisplayNames(EditorUserBuildSettings.activeBuildTarget.ToString());
			return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == BuildTargetGroup.Standalone || displayNames != null;
		}

		internal static Rect GetConstrainedGameViewRenderRect(Rect renderRect, int sizeIndex)
		{
			bool flag;
			return GameView.GetConstrainedGameViewRenderRect(renderRect, sizeIndex, out flag);
		}

		internal static Rect GetConstrainedGameViewRenderRect(Rect renderRect, int sizeIndex, out bool fitsInsideRect)
		{
			return GameViewSizes.GetConstrainedRect(renderRect, GameView.currentSizeGroupType, sizeIndex, out fitsInsideRect);
		}

		internal Rect GetConstrainedGameViewRenderRect()
		{
			if (this.m_Parent == null)
			{
				return GameView.s_MainGameViewRect;
			}
			this.m_Pos = this.m_Parent.borderSize.Remove(this.m_Parent.position);
			Rect renderRect = EditorGUIUtility.PointsToPixels(this.gameViewRenderRect);
			Rect constrainedGameViewRenderRect = GameView.GetConstrainedGameViewRenderRect(renderRect, this.selectedSizeIndex);
			return EditorGUIUtility.PixelsToPoints(constrainedGameViewRenderRect);
		}

		private void SelectionCallback(int indexClicked, object objectSelected)
		{
			if (indexClicked != this.selectedSizeIndex)
			{
				this.selectedSizeIndex = indexClicked;
				base.dontClearBackground = true;
				this.GameViewAspectWasChanged();
			}
		}

		private void DoToolbarGUI()
		{
			ScriptableSingleton<GameViewSizes>.instance.RefreshStandaloneAndWebplayerDefaultSizes();
			if (ScriptableSingleton<GameViewSizes>.instance.GetChangeID() != this.m_SizeChangeID)
			{
				this.EnsureSelectedSizeAreValid();
				this.m_SizeChangeID = ScriptableSingleton<GameViewSizes>.instance.GetChangeID();
			}
			GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
			if (this.ShouldShowMultiDisplayOption())
			{
				int num = EditorGUILayout.Popup(this.m_TargetDisplay, DisplayUtility.GetDisplayNames(), EditorStyles.toolbarPopup, new GUILayoutOption[]
				{
					GUILayout.Width(80f)
				});
				EditorGUILayout.Space();
				if (num != this.m_TargetDisplay)
				{
					this.m_TargetDisplay = num;
					this.GameViewAspectWasChanged();
				}
			}
			EditorGUILayout.GameViewSizePopup(GameView.currentSizeGroupType, this.selectedSizeIndex, new Action<int, object>(this.SelectionCallback), EditorStyles.toolbarDropDown, new GUILayoutOption[]
			{
				GUILayout.Width(160f)
			});
			if (FrameDebuggerUtility.IsLocalEnabled())
			{
				GUILayout.FlexibleSpace();
				Color color = GUI.color;
				GUI.color *= AnimationMode.animatedPropertyColor;
				GUILayout.Label("Frame Debugger on", EditorStyles.miniLabel, new GUILayoutOption[0]);
				GUI.color = color;
				if (Event.current.type == EventType.Repaint)
				{
					FrameDebuggerWindow.RepaintAll();
				}
			}
			GUILayout.FlexibleSpace();
			if (RenderDoc.IsLoaded())
			{
				EditorGUI.BeginDisabledGroup(!RenderDoc.IsSupported());
				if (GUILayout.Button(this.renderdocContent, EditorStyles.toolbarButton, new GUILayoutOption[0]))
				{
					this.m_Parent.CaptureRenderDoc();
					GUIUtility.ExitGUI();
				}
				EditorGUI.EndDisabledGroup();
			}
			this.m_MaximizeOnPlay = GUILayout.Toggle(this.m_MaximizeOnPlay, "Maximize on Play", EditorStyles.toolbarButton, new GUILayoutOption[0]);
			EditorUtility.audioMasterMute = GUILayout.Toggle(EditorUtility.audioMasterMute, "Mute audio", EditorStyles.toolbarButton, new GUILayoutOption[0]);
			this.m_Stats = GUILayout.Toggle(this.m_Stats, "Stats", EditorStyles.toolbarButton, new GUILayoutOption[0]);
			Rect rect = GUILayoutUtility.GetRect(this.gizmosContent, GameView.s_GizmoButtonStyle);
			Rect position = new Rect(rect.xMax - (float)GameView.s_GizmoButtonStyle.border.right, rect.y, (float)GameView.s_GizmoButtonStyle.border.right, rect.height);
			if (EditorGUI.ButtonMouseDown(position, GUIContent.none, FocusType.Passive, GUIStyle.none))
			{
				Rect last = GUILayoutUtility.topLevel.GetLast();
				if (AnnotationWindow.ShowAtPosition(last, true))
				{
					GUIUtility.ExitGUI();
				}
			}
			this.m_Gizmos = GUI.Toggle(rect, this.m_Gizmos, this.gizmosContent, GameView.s_GizmoButtonStyle);
			GUILayout.EndHorizontal();
		}

		private void OnGUI()
		{
			if (GameView.s_GizmoButtonStyle == null)
			{
				GameView.s_GizmoButtonStyle = "GV Gizmo DropDown";
				GameView.s_ResolutionWarningStyle = new GUIStyle("PreOverlayLabel");
				GameView.s_ResolutionWarningStyle.alignment = TextAnchor.UpperLeft;
				GameView.s_ResolutionWarningStyle.padding = new RectOffset(6, 6, 1, 1);
			}
			this.DoToolbarGUI();
			Rect gameViewRenderRect = this.gameViewRenderRect;
			Rect renderRect = EditorGUIUtility.PointsToPixels(gameViewRenderRect);
			bool fitsInsideRect;
			Rect constrainedGameViewRenderRect = GameView.GetConstrainedGameViewRenderRect(renderRect, this.selectedSizeIndex, out fitsInsideRect);
			Rect rect = EditorGUIUtility.PixelsToPoints(constrainedGameViewRenderRect);
			Rect rect2 = GUIClip.Unclip(rect);
			Rect cameraRect = EditorGUIUtility.PointsToPixels(rect2);
			base.SetInternalGameViewRect(rect2);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.CustomCursor);
			EventType type = Event.current.type;
			if (type == EventType.MouseDown && gameViewRenderRect.Contains(Event.current.mousePosition))
			{
				this.AllowCursorLockAndHide(true);
			}
			else if (type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
			{
				Unsupported.SetAllowCursorLock(false);
			}
			if (type == EventType.Repaint)
			{
				bool flag = EditorGUIUtility.IsDisplayReferencedByCameras(this.m_TargetDisplay);
				if (!this.currentGameViewSize.isFreeAspectRatio || !InternalEditorUtility.HasFullscreenCamera() || !flag)
				{
					GUI.Box(gameViewRenderRect, GUIContent.none, "GameViewBackground");
					if (!InternalEditorUtility.HasFullscreenCamera())
					{
						float[] array = new float[]
						{
							30f,
							gameViewRenderRect.height / 2f - 10f,
							gameViewRenderRect.height - 10f
						};
						for (int i = 0; i < array.Length; i++)
						{
							int num = (int)array[i];
							GUI.Label(new Rect(gameViewRenderRect.width / 2f - 100f, (float)num, 300f, 20f), "Scene is missing a fullscreen camera", "WhiteLargeLabel");
						}
					}
				}
				Vector2 s_EditorScreenPointOffset = GUIUtility.s_EditorScreenPointOffset;
				GUIUtility.s_EditorScreenPointOffset = Vector2.zero;
				SavedGUIState savedGUIState = SavedGUIState.Create();
				if (this.ShouldShowMultiDisplayOption())
				{
					EditorGUIUtility.RenderGameViewCamerasInternal(cameraRect, this.m_TargetDisplay, this.m_Gizmos, true);
				}
				else
				{
					EditorGUIUtility.RenderGameViewCamerasInternal(cameraRect, 0, this.m_Gizmos, true);
				}
				GL.sRGBWrite = false;
				savedGUIState.ApplyAndForget();
				GUIUtility.s_EditorScreenPointOffset = s_EditorScreenPointOffset;
			}
			else if (type != EventType.Layout && type != EventType.Used)
			{
				if (WindowLayout.s_MaximizeKey.activated && (!EditorApplication.isPlaying || EditorApplication.isPaused))
				{
					return;
				}
				bool flag2 = rect.Contains(Event.current.mousePosition);
				if (Event.current.rawType == EventType.MouseDown && !flag2)
				{
					return;
				}
				Vector2 mousePosition = Event.current.mousePosition;
				Vector2 vector = mousePosition - rect.position;
				vector = EditorGUIUtility.PointsToPixels(vector);
				Event.current.mousePosition = vector;
				Event.current.displayIndex = this.m_TargetDisplay;
				EditorGUIUtility.QueueGameViewInputEvent(Event.current);
				bool flag3 = true;
				if (Event.current.rawType == EventType.MouseUp && !flag2)
				{
					flag3 = false;
				}
				if (type == EventType.ExecuteCommand || type == EventType.ValidateCommand)
				{
					flag3 = false;
				}
				if (flag3)
				{
					Event.current.Use();
				}
				else
				{
					Event.current.mousePosition = mousePosition;
				}
			}
			this.ShowResolutionWarning(new Rect(gameViewRenderRect.x, gameViewRenderRect.y, 200f, 20f), fitsInsideRect, constrainedGameViewRenderRect.size);
			if (this.m_Stats)
			{
				GameViewGUI.GameViewStatsGUI();
			}
		}

		private void ShowResolutionWarning(Rect position, bool fitsInsideRect, Vector2 shownSize)
		{
			if (!fitsInsideRect && shownSize != this.m_ShownResolution)
			{
				this.m_ShownResolution = shownSize;
				this.m_ResolutionTooLargeWarning.value = true;
			}
			if (fitsInsideRect && this.m_ShownResolution != Vector2.zero)
			{
				this.m_ShownResolution = Vector2.zero;
				this.m_ResolutionTooLargeWarning.value = false;
			}
			this.m_ResolutionTooLargeWarning.target = (!fitsInsideRect && !EditorApplication.isPlaying);
			if (this.m_ResolutionTooLargeWarning.faded > 0f)
			{
				Color color = GUI.color;
				GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(this.m_ResolutionTooLargeWarning.faded * 2f));
				EditorGUI.DropShadowLabel(position, string.Format("Using resolution {0}x{1}", shownSize.x, shownSize.y), GameView.s_ResolutionWarningStyle);
				GUI.color = color;
			}
		}
	}
}
