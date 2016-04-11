using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	internal class AnimEditor : ScriptableObject
	{
		internal const int kSliderThickness = 15;

		internal const int kLayoutRowHeight = 18;

		internal const int kIntFieldWidth = 35;

		internal const int kHierarchyMinWidth = 300;

		internal const float kDisabledRulerAlpha = 0.12f;

		private static List<AnimEditor> s_AnimationWindows = new List<AnimEditor>();

		[SerializeField]
		private SplitterState m_HorizontalSplitter;

		[SerializeField]
		private AnimationWindowState m_State;

		[SerializeField]
		private DopeSheetEditor m_DopeSheet;

		[SerializeField]
		private AnimationWindowHierarchy m_Hierarchy;

		[SerializeField]
		private AnimationWindowClipPopup m_ClipPopup;

		[SerializeField]
		private AnimationEventTimeLine m_Events;

		[SerializeField]
		private CurveEditor m_CurveEditor;

		[SerializeField]
		private EditorWindow m_OwnerWindow;

		[NonSerialized]
		private Rect m_Position;

		[NonSerialized]
		private bool m_TriggerFraming;

		[NonSerialized]
		private bool m_StylesInitialized;

		[NonSerialized]
		private float m_PreviousUpdateTime;

		[NonSerialized]
		private bool m_Initialized;

		internal static PrefColor kEulerXColor = new PrefColor("Testing/EulerX", 1f, 0f, 1f, 1f);

		internal static PrefColor kEulerYColor = new PrefColor("Testing/EulerY", 1f, 1f, 0f, 1f);

		internal static PrefColor kEulerZColor = new PrefColor("Testing/EulerZ", 0f, 1f, 1f, 1f);

		internal static PrefKey kAnimationPrevFrame = new PrefKey("Animation/Previous Frame", ",");

		internal static PrefKey kAnimationNextFrame = new PrefKey("Animation/Next Frame", ".");

		internal static PrefKey kAnimationPrevKeyframe = new PrefKey("Animation/Previous Keyframe", "&,");

		internal static PrefKey kAnimationNextKeyframe = new PrefKey("Animation/Next Keyframe", "&.");

		internal static PrefKey kAnimationRecordKeyframe = new PrefKey("Animation/Record Keyframe", "k");

		internal static PrefKey kAnimationShowCurvesToggle = new PrefKey("Animation/Show curves", "c");

		public bool locked
		{
			get
			{
				return this.m_State.locked;
			}
			set
			{
				this.m_State.locked = value;
			}
		}

		public bool stateDisabled
		{
			get
			{
				return this.m_State.disabled;
			}
		}

		private float hierarchyWidth
		{
			get
			{
				return (float)this.m_HorizontalSplitter.realSizes[0];
			}
		}

		private float contentWidth
		{
			get
			{
				return (float)this.m_HorizontalSplitter.realSizes[1];
			}
		}

		public static List<AnimEditor> GetAllAnimationWindows()
		{
			return AnimEditor.s_AnimationWindows;
		}

		public void OnBreadcrumbGUI(EditorWindow parent, Rect position)
		{
			this.m_DopeSheet.m_Owner = parent;
			this.m_OwnerWindow = parent;
			this.m_Position = position;
			if (!this.m_Initialized)
			{
				this.Initialize();
			}
			this.m_State.OnGUI();
			this.ClampSplitterSize();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			SplitterGUILayout.BeginHorizontalSplit(this.m_HorizontalSplitter, new GUILayoutOption[0]);
			EditorGUI.BeginDisabledGroup(this.m_State.disabled);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			GUILayout.BeginHorizontal(EditorStyles.toolbarButton, new GUILayoutOption[0]);
			this.RecordButtonOnGUI();
			this.PlayButtonOnGUI();
			this.FrameNavigationOnGUI();
			this.AddKeyframeButtonOnGUI();
			this.AddEventButtonOnGUI();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(EditorStyles.toolbarButton, new GUILayoutOption[0]);
			this.ClipSelectionDropDownOnGUI();
			this.FrameRateInputFieldOnGUI();
			GUILayout.EndHorizontal();
			this.HierarchyOnGUI();
			GUILayout.BeginHorizontal(AnimationWindowStyles.miniToolbar, new GUILayoutOption[0]);
			this.TabSelectionButtonsOnGUI();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			this.TimeRulerOnGUI();
			this.EventLineOnGUI();
			this.MainContentOnGUI();
			GUILayout.EndVertical();
			SplitterGUILayout.EndHorizontalSplit();
			GUILayout.EndHorizontal();
			this.RenderEventTooltip();
			EditorGUI.EndDisabledGroup();
			this.HandleFrameNavigationHotKeys();
		}

		private void MainContentOnGUI()
		{
			if (this.m_State.animatorIsOptimized)
			{
				GUILayout.Label("Editing optimized game object hierarchy is not supported.\nPlease select a game object that does not have 'Optimize Game Objects' applied.", new GUILayoutOption[0]);
				return;
			}
			Rect contentLayoutRect = this.GetContentLayoutRect();
			if (this.m_State.disabled)
			{
				this.SetupWizardOnGUI(contentLayoutRect);
			}
			else if (this.m_State.showCurveEditor)
			{
				this.CurveEditorOnGUI(contentLayoutRect);
			}
			else
			{
				this.DopeSheetOnGUI(contentLayoutRect);
			}
			this.HandleCopyPaste();
			AnimationWindowUtility.DrawVerticalSplitLine(new Vector2(contentLayoutRect.xMin + 1f, contentLayoutRect.yMin), new Vector2(contentLayoutRect.xMin + 1f, contentLayoutRect.yMax));
		}

		private Rect GetContentLayoutRect()
		{
			return GUILayoutUtility.GetRect(this.contentWidth, this.contentWidth, 0f, 3.40282347E+38f, new GUILayoutOption[]
			{
				GUILayout.ExpandHeight(true)
			});
		}

		public void Update()
		{
			if (this.m_State == null)
			{
				return;
			}
			this.PlaybackUpdate();
		}

		public void OnEnable()
		{
			base.hideFlags = HideFlags.HideAndDontSave;
			AnimEditor.s_AnimationWindows.Add(this);
			if (this.m_State == null)
			{
				this.m_State = (ScriptableObject.CreateInstance(typeof(AnimationWindowState)) as AnimationWindowState);
				this.m_State.hideFlags = HideFlags.HideAndDontSave;
				this.m_State.animEditor = this;
				this.InitializeHorizontalSplitter();
				this.InitializeClipSelection();
				this.InitializeDopeSheet();
				this.InitializeEvents();
				this.InitializeCurveEditor();
			}
			this.InitializeNonserializedValues();
			this.m_State.timeArea = ((!this.m_State.showCurveEditor) ? this.m_DopeSheet : this.m_CurveEditor);
			this.m_DopeSheet.state = this.m_State;
			this.m_ClipPopup.state = this.m_State;
			AnimationWindowState expr_CF = this.m_State;
			expr_CF.onClipSelectionChanged = (Action)Delegate.Combine(expr_CF.onClipSelectionChanged, new Action(this.OnClipSelectionChange));
			this.m_State.OnSelectionChange();
			CurveEditor expr_101 = this.m_CurveEditor;
			expr_101.curvesUpdated = (CurveEditor.CallbackFunction)Delegate.Combine(expr_101.curvesUpdated, new CurveEditor.CallbackFunction(this.SaveChangedCurvesFromCurveEditor));
		}

		public void OnDisable()
		{
			AnimEditor.s_AnimationWindows.Remove(this);
			if (this.m_CurveEditor != null)
			{
				CurveEditor expr_1D = this.m_CurveEditor;
				expr_1D.curvesUpdated = (CurveEditor.CallbackFunction)Delegate.Remove(expr_1D.curvesUpdated, new CurveEditor.CallbackFunction(this.SaveChangedCurvesFromCurveEditor));
			}
			AnimationWindowState expr_44 = this.m_State;
			expr_44.onClipSelectionChanged = (Action)Delegate.Remove(expr_44.onClipSelectionChanged, new Action(this.OnClipSelectionChange));
			this.m_State.OnDisable();
		}

		public void OnSelectionChange()
		{
			this.m_State.OnSelectionChange();
			this.Repaint();
		}

		public void OnControllerChange()
		{
			this.m_State.OnControllerChange();
			this.Repaint();
		}

		public void OnClipSelectionChange()
		{
			this.m_TriggerFraming = true;
		}

		private void PlaybackUpdate()
		{
			if (this.m_State.playing)
			{
				float num = Time.realtimeSinceStartup - this.m_PreviousUpdateTime;
				this.m_PreviousUpdateTime = Time.realtimeSinceStartup;
				this.m_State.currentTime += num;
				if (this.m_State.currentTime > this.m_State.maxTime)
				{
					this.m_State.currentTime = this.m_State.minTime;
				}
				this.m_State.currentTime = Mathf.Clamp(this.m_State.currentTime, this.m_State.minTime, this.m_State.maxTime);
				this.m_State.ResampleAnimation();
				this.Repaint();
			}
		}

		private void SetupWizardOnGUI(Rect position)
		{
			Rect position2 = new Rect(position.x, position.y, position.width - 15f, position.height - 15f);
			GUI.BeginClip(position2);
			GUI.enabled = true;
			this.m_State.showCurveEditor = false;
			this.m_State.timeArea = this.m_DopeSheet;
			this.m_State.timeArea.SetShownHRangeInsideMargins(0f, 1f);
			bool flag = Selection.activeGameObject && !EditorUtility.IsPersistent(Selection.activeGameObject);
			if (flag)
			{
				string arg = (this.m_State.activeRootGameObject || this.m_State.activeAnimationClip) ? AnimationWindowStyles.animationClip.text : AnimationWindowStyles.animatorAndAnimationClip.text;
				string t = string.Format(AnimationWindowStyles.formatIsMissing.text, Selection.activeGameObject.name, arg);
				GUIContent content = GUIContent.Temp(t);
				Vector2 vector = GUI.skin.label.CalcSize(content);
				Rect position3 = new Rect(position2.width * 0.5f - vector.x * 0.5f, position2.height * 0.5f - vector.y * 0.5f, vector.x, vector.y);
				GUI.Label(position3, content);
				Rect position4 = new Rect(position2.width * 0.5f - 35f, position3.yMax + 3f, 70f, 20f);
				if (GUI.Button(position4, AnimationWindowStyles.create) && AnimationWindowUtility.InitializeGameobjectForAnimation(Selection.activeGameObject))
				{
					Component closestAnimationPlayerComponentInParents = AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(Selection.activeGameObject.transform);
					this.m_State.activeAnimationClip = AnimationUtility.GetAnimationClips(closestAnimationPlayerComponentInParents.gameObject)[0];
					this.m_State.recording = true;
					this.m_State.currentTime = 0f;
					this.m_State.ResampleAnimation();
				}
			}
			else
			{
				Color color = GUI.color;
				GUI.color = Color.gray;
				Vector2 vector2 = GUI.skin.label.CalcSize(AnimationWindowStyles.noAnimatableObjectSelectedText);
				Rect position5 = new Rect(position2.width * 0.5f - vector2.x * 0.5f, position2.height * 0.5f - vector2.y * 0.5f, vector2.x, vector2.y);
				GUI.Label(position5, AnimationWindowStyles.noAnimatableObjectSelectedText);
				GUI.color = color;
			}
			GUI.EndClip();
			GUI.enabled = false;
		}

		private void EventLineOnGUI()
		{
			Rect rect = GUILayoutUtility.GetRect(this.m_Position.width - this.hierarchyWidth, 18f);
			rect.width -= 15f;
			GUI.Label(rect, GUIContent.none, AnimationWindowStyles.eventBackground);
			if (!this.m_State.disabled)
			{
				this.DrawPlayHead(rect.yMin - 1f, rect.yMax);
			}
			this.m_Events.EventLineGUI(rect, this.m_State);
		}

		private void RenderEventTooltip()
		{
			this.m_Events.DrawInstantTooltip(this.m_Position);
		}

		private void TabSelectionButtonsOnGUI()
		{
			GUILayout.FlexibleSpace();
			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle(!this.m_State.showCurveEditor, AnimationWindowStyles.dopesheet, AnimationWindowStyles.miniToolbarButton, new GUILayoutOption[]
			{
				GUILayout.Width(80f)
			});
			GUILayout.Toggle(this.m_State.showCurveEditor, AnimationWindowStyles.curves, AnimationWindowStyles.miniToolbarButton, new GUILayoutOption[]
			{
				GUILayout.Width(80f)
			});
			if (EditorGUI.EndChangeCheck() || AnimEditor.kAnimationShowCurvesToggle.activated)
			{
				this.SwitchBetweenCurvesAndDopesheet();
			}
		}

		private void HierarchyOnGUI()
		{
			Rect rect = GUILayoutUtility.GetRect(this.hierarchyWidth, this.hierarchyWidth, 0f, 3.40282347E+38f, new GUILayoutOption[]
			{
				GUILayout.ExpandHeight(true)
			});
			if (!this.m_State.disabled)
			{
				this.m_Hierarchy.OnGUI(rect);
			}
		}

		private void FrameRateInputFieldOnGUI()
		{
			GUILayout.Label(AnimationWindowStyles.samples, AnimationWindowStyles.toolbarLabel, new GUILayoutOption[0]);
			EditorGUI.BeginChangeCheck();
			int num = EditorGUILayout.IntField((int)this.m_State.frameRate, EditorStyles.toolbarTextField, new GUILayoutOption[]
			{
				GUILayout.Width(35f)
			});
			if (EditorGUI.EndChangeCheck())
			{
				this.m_State.frameRate = (float)num;
			}
		}

		private void ClipSelectionDropDownOnGUI()
		{
			this.m_ClipPopup.OnGUI();
		}

		private void DopeSheetOnGUI(Rect position)
		{
			Rect rect = new Rect(position.xMin, position.yMin, position.width - 15f, position.height);
			if (Event.current.type == EventType.Repaint)
			{
				this.m_DopeSheet.rect = rect;
				this.m_DopeSheet.SetTickMarkerRanges();
				this.m_DopeSheet.RecalculateBounds();
			}
			if (this.m_State.showCurveEditor)
			{
				return;
			}
			if (this.m_TriggerFraming && Event.current.type == EventType.Repaint)
			{
				this.m_DopeSheet.FrameClip();
				this.m_TriggerFraming = false;
			}
			Rect position2 = new Rect(position.xMin, position.yMin, position.width - 15f, position.height - 15f);
			Rect position3 = new Rect(position2.xMin, position2.yMin, position2.width, 16f);
			this.m_DopeSheet.BeginViewGUI();
			if (!this.m_State.disabled)
			{
				this.m_DopeSheet.TimeRuler(position2, this.m_State.frameRate, false, true, 0.12f);
				this.m_DopeSheet.DrawMasterDopelineBackground(position3);
				this.DrawPlayHead(position2.yMin - 1f, position2.yMax);
			}
			this.m_DopeSheet.OnGUI(position2, this.m_State.hierarchyState.scrollPos * -1f);
			this.m_DopeSheet.EndViewGUI();
			Rect position4 = new Rect(rect.xMax, rect.yMin, 15f, position2.height);
			float bottomValue = Mathf.Max(this.m_DopeSheet.contentHeight, position.height);
			this.m_State.hierarchyState.scrollPos.y = GUI.VerticalScrollbar(position4, this.m_State.hierarchyState.scrollPos.y, position.height, 0f, bottomValue);
		}

		private void CurveEditorOnGUI(Rect position)
		{
			if (Event.current.type == EventType.Repaint)
			{
				this.m_CurveEditor.rect = position;
				this.m_CurveEditor.SetTickMarkerRanges();
				this.m_CurveEditor.RecalculateBounds();
			}
			if (!this.m_State.showCurveEditor)
			{
				return;
			}
			Rect position2 = new Rect(position.xMin, position.yMin, position.width - 15f, position.height - 15f);
			this.m_CurveEditor.vSlider = this.m_State.showCurveEditor;
			this.m_CurveEditor.hSlider = this.m_State.showCurveEditor;
			this.UpdateCurveEditorData();
			if (this.m_State.m_FrameCurveEditor)
			{
				this.m_CurveEditor.FrameSelected(false, true);
				this.m_State.m_FrameCurveEditor = false;
			}
			this.m_CurveEditor.BeginViewGUI();
			if (!this.m_State.disabled)
			{
				GUI.Box(position2, GUIContent.none, AnimationWindowStyles.curveEditorBackground);
				this.m_CurveEditor.GridGUI();
				this.DrawPlayHead(position2.yMin, position2.yMax);
			}
			EditorGUI.BeginDisabledGroup(this.m_State.animationIsReadOnly);
			EditorGUI.BeginChangeCheck();
			this.m_CurveEditor.CurveGUI();
			if (EditorGUI.EndChangeCheck())
			{
				this.SaveChangedCurvesFromCurveEditor();
				this.UpdateSelectedKeysFromCurveEditor();
			}
			EditorGUI.EndDisabledGroup();
			this.m_CurveEditor.EndViewGUI();
		}

		private void TimeRulerOnGUI()
		{
			Rect rect = GUILayoutUtility.GetRect(this.m_Position.width - this.hierarchyWidth, 18f);
			Rect position = new Rect(rect.xMin, rect.yMin, rect.width - 15f, rect.height);
			GUI.Box(rect, GUIContent.none, EditorStyles.toolbarButton);
			this.m_State.timeArea.TimeRuler(position, this.m_State.frameRate, true, false, 1f);
			if (!this.m_State.disabled)
			{
				this.RenderEndOfClipOverlay(position.yMin, position.yMax);
				this.DrawPlayHead(position.yMin, position.yMax);
			}
			EditorGUI.BeginChangeCheck();
			int frame = Mathf.Max(Mathf.RoundToInt(GUI.HorizontalSlider(position, (float)this.m_State.frame, this.m_State.minVisibleFrame, this.m_State.maxVisibleFrame, GUIStyle.none, GUIStyle.none)), 0);
			if (EditorGUI.EndChangeCheck())
			{
				this.m_State.frame = frame;
				this.m_State.recording = true;
				this.m_State.ResampleAnimation();
			}
		}

		private void AddEventButtonOnGUI()
		{
			if (GUILayout.Button(AnimationWindowStyles.addEventContent, EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
				AnimationEventPopup.Create(this.m_State.activeRootGameObject, this.m_State.activeAnimationClip, this.m_State.currentTime, this.m_OwnerWindow);
			}
		}

		private void AddKeyframeButtonOnGUI()
		{
			if (GUILayout.Button(AnimationWindowStyles.addKeyframeContent, EditorStyles.toolbarButton, new GUILayoutOption[0]) || AnimEditor.kAnimationRecordKeyframe.activated)
			{
				AnimationWindowUtility.AddSelectedKeyframes(this.m_State, this.m_State.time);
			}
		}

		private void FrameNavigationOnGUI()
		{
			if (GUILayout.Button(AnimationWindowStyles.prevKeyContent, EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
				this.MoveToPreviousKeyframe();
			}
			if (GUILayout.Button(AnimationWindowStyles.nextKeyContent, EditorStyles.toolbarButton, new GUILayoutOption[0]))
			{
				this.MoveToNextKeyframe();
			}
			EditorGUI.BeginChangeCheck();
			int frame = EditorGUILayout.IntField(this.m_State.frame, EditorStyles.toolbarTextField, new GUILayoutOption[]
			{
				GUILayout.Width(35f)
			});
			if (EditorGUI.EndChangeCheck())
			{
				this.m_State.frame = frame;
			}
		}

		private void HandleFrameNavigationHotKeys()
		{
			if (AnimEditor.kAnimationPrevKeyframe.activated)
			{
				this.MoveToPreviousKeyframe();
			}
			if (AnimEditor.kAnimationNextKeyframe.activated)
			{
				this.MoveToNextKeyframe();
			}
			if (AnimEditor.kAnimationNextFrame.activated)
			{
				this.m_State.frame++;
			}
			if (AnimEditor.kAnimationPrevFrame.activated)
			{
				this.m_State.frame--;
			}
			if (AnimEditor.kAnimationPrevKeyframe.activated || AnimEditor.kAnimationNextKeyframe.activated || AnimEditor.kAnimationNextFrame.activated || AnimEditor.kAnimationPrevFrame.activated)
			{
				this.Repaint();
			}
		}

		private void MoveToPreviousKeyframe()
		{
			List<AnimationWindowCurve> list = (!this.m_State.showCurveEditor) ? this.m_State.allCurves : this.m_State.activeCurves;
			this.m_State.currentTime = AnimationWindowUtility.GetPreviousKeyframeTime(list.ToArray(), this.m_State.FrameToTime((float)this.m_State.frame), this.m_State.frameRate);
			this.m_State.frame = this.m_State.TimeToFrameFloor(this.m_State.currentTime);
		}

		private void MoveToNextKeyframe()
		{
			List<AnimationWindowCurve> list = (!this.m_State.showCurveEditor) ? this.m_State.allCurves : this.m_State.activeCurves;
			this.m_State.currentTime = AnimationWindowUtility.GetNextKeyframeTime(list.ToArray(), this.m_State.FrameToTime((float)this.m_State.frame), this.m_State.frameRate);
			this.m_State.frame = this.m_State.TimeToFrameFloor(this.m_State.currentTime);
		}

		private void PlayButtonOnGUI()
		{
			EditorGUI.BeginChangeCheck();
			this.m_State.playing = GUILayout.Toggle(this.m_State.playing, AnimationWindowStyles.playContent, EditorStyles.toolbarButton, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				this.m_PreviousUpdateTime = Time.realtimeSinceStartup;
			}
		}

		private void RecordButtonOnGUI()
		{
			EditorGUI.BeginChangeCheck();
			this.m_State.recording = GUILayout.Toggle(this.m_State.recording, AnimationWindowStyles.recordContent, EditorStyles.toolbarButton, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck() && this.m_State.recording)
			{
				this.m_State.ResampleAnimation();
			}
		}

		private void SwitchBetweenCurvesAndDopesheet()
		{
			this.m_State.showCurveEditor = !this.m_State.showCurveEditor;
			if (this.m_State.showCurveEditor)
			{
				this.UpdateCurveEditorData();
				this.UpdateSelectedKeysToCurveEditor();
				AnimationWindowUtility.SyncTimeArea(this.m_DopeSheet, this.m_CurveEditor);
				this.m_State.timeArea = this.m_CurveEditor;
				this.m_CurveEditor.RecalculateBounds();
				this.m_CurveEditor.FrameSelected(false, true);
			}
			else
			{
				this.UpdateSelectedKeysFromCurveEditor();
				AnimationWindowUtility.SyncTimeArea(this.m_CurveEditor, this.m_DopeSheet);
				this.m_State.timeArea = this.m_DopeSheet;
			}
		}

		private void DrawPlayHead(float minY, float maxY)
		{
			Rect screenRect = new Rect(this.hierarchyWidth - 1f, 0f, this.m_Position.width - this.hierarchyWidth - 15f, this.m_Position.height);
			GUIClip.Push(screenRect, Vector2.zero, Vector2.zero, false);
			float positionX = this.m_State.TimeToPixel(this.m_State.currentTime);
			AnimationWindowUtility.DrawPlayHead(positionX, minY, maxY, 1f);
			GUIClip.Pop();
		}

		private void RenderEndOfClipOverlay(float minY, float maxY)
		{
			Rect rect = new Rect(this.hierarchyWidth - 1f, 0f, this.m_Position.width - this.hierarchyWidth - 15f, this.m_Position.height);
			Rect rect2 = new Rect(rect.xMin, minY, rect.width, maxY - minY);
			AnimationWindowUtility.DrawEndOfClip(rect2, this.m_State.TimeToPixel(this.m_State.maxTime) + rect2.xMin);
		}

		private void ClampSplitterSize()
		{
			this.m_HorizontalSplitter.realSizes[1] = (int)Mathf.Min(this.m_Position.width - this.hierarchyWidth, (float)this.m_HorizontalSplitter.realSizes[1]);
		}

		private void SaveChangedCurvesFromCurveEditor()
		{
			Undo.RegisterCompleteObjectUndo(this.m_State.activeAnimationClip, "Edit Curve");
			CurveWrapper[] animationCurves = this.m_CurveEditor.animationCurves;
			for (int i = 0; i < animationCurves.Length; i++)
			{
				CurveWrapper curveWrapper = animationCurves[i];
				if (curveWrapper.changed)
				{
					AnimationUtility.SetEditorCurve(this.m_State.activeAnimationClip, curveWrapper.binding, curveWrapper.curve);
					curveWrapper.changed = false;
				}
			}
			this.m_State.ResampleAnimation();
		}

		private void UpdateSelectedKeysFromCurveEditor()
		{
			this.m_State.ClearKeySelections();
			foreach (CurveSelection current in this.m_CurveEditor.selectedCurves)
			{
				AnimationWindowKeyframe animationWindowKeyframe = AnimationWindowUtility.CurveSelectionToAnimationWindowKeyframe(current, this.m_State.allCurves);
				if (animationWindowKeyframe != null)
				{
					this.m_State.SelectKey(animationWindowKeyframe);
				}
			}
		}

		private void UpdateSelectedKeysToCurveEditor()
		{
			this.m_CurveEditor.ClearSelection();
			foreach (AnimationWindowKeyframe current in this.m_State.selectedKeys)
			{
				CurveSelection curveSelection = AnimationWindowUtility.AnimationWindowKeyframeToCurveSelection(current, this.m_CurveEditor);
				if (curveSelection != null)
				{
					this.m_CurveEditor.AddSelection(curveSelection);
				}
			}
		}

		private void HandleCopyPaste()
		{
			if (Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand)
			{
				string commandName = Event.current.commandName;
				if (commandName != null)
				{
					if (AnimEditor.<>f__switch$mapB == null)
					{
						AnimEditor.<>f__switch$mapB = new Dictionary<string, int>(2)
						{
							{
								"Copy",
								0
							},
							{
								"Paste",
								1
							}
						};
					}
					int num;
					if (AnimEditor.<>f__switch$mapB.TryGetValue(commandName, out num))
					{
						if (num != 0)
						{
							if (num == 1)
							{
								if (Event.current.type == EventType.ExecuteCommand)
								{
									this.m_State.PasteKeys();
									this.UpdateCurveEditorData();
									this.UpdateSelectedKeysToCurveEditor();
								}
								Event.current.Use();
							}
						}
						else
						{
							if (Event.current.type == EventType.ExecuteCommand)
							{
								this.m_State.CopyKeys();
							}
							Event.current.Use();
						}
					}
				}
			}
		}

		private void UpdateCurveEditorData()
		{
			this.m_CurveEditor.animationCurves = this.m_State.activeCurveWrappers.ToArray();
		}

		public void Repaint()
		{
			if (this.m_OwnerWindow != null)
			{
				this.m_OwnerWindow.Repaint();
			}
		}

		private void Initialize()
		{
			AnimationWindowStyles.Initialize();
			this.InitializeHierarchy();
			this.m_CurveEditor.m_PlayHead = this.m_State;
			this.m_Initialized = true;
		}

		private void InitializeClipSelection()
		{
			this.m_ClipPopup = new AnimationWindowClipPopup();
		}

		private void InitializeHierarchy()
		{
			this.m_Hierarchy = new AnimationWindowHierarchy(this.m_State, this.m_OwnerWindow, new Rect(0f, 0f, this.hierarchyWidth, 100f));
		}

		private void InitializeDopeSheet()
		{
			this.m_DopeSheet = new DopeSheetEditor(this.m_OwnerWindow);
			this.m_DopeSheet.SetTickMarkerRanges();
			this.m_DopeSheet.hSlider = true;
			this.m_DopeSheet.shownArea = new Rect(1f, 1f, 1f, 1f);
			this.m_DopeSheet.rect = new Rect(0f, 0f, this.contentWidth, 100f);
			this.m_DopeSheet.hTicks.SetTickModulosForFrameRate(this.m_State.frameRate);
		}

		private void InitializeEvents()
		{
			this.m_Events = new AnimationEventTimeLine(this.m_OwnerWindow);
		}

		private void InitializeCurveEditor()
		{
			this.m_CurveEditor = new CurveEditor(new Rect(0f, 0f, this.contentWidth, 100f), new CurveWrapper[0], false);
			CurveEditorSettings curveEditorSettings = new CurveEditorSettings();
			curveEditorSettings.hTickStyle.distMin = 30;
			curveEditorSettings.hTickStyle.distFull = 80;
			curveEditorSettings.hTickStyle.distLabel = 0;
			if (EditorGUIUtility.isProSkin)
			{
				curveEditorSettings.vTickStyle.color = new Color(1f, 1f, 1f, curveEditorSettings.vTickStyle.color.a);
				curveEditorSettings.vTickStyle.labelColor = new Color(1f, 1f, 1f, curveEditorSettings.vTickStyle.labelColor.a);
			}
			curveEditorSettings.vTickStyle.distMin = 15;
			curveEditorSettings.vTickStyle.distFull = 40;
			curveEditorSettings.vTickStyle.distLabel = 30;
			curveEditorSettings.vTickStyle.stubs = true;
			curveEditorSettings.hRangeMin = 0f;
			curveEditorSettings.hRangeLocked = false;
			curveEditorSettings.vRangeLocked = false;
			curveEditorSettings.hSlider = true;
			curveEditorSettings.vSlider = true;
			this.m_CurveEditor.shownArea = new Rect(1f, 1f, 1f, 1f);
			this.m_CurveEditor.settings = curveEditorSettings;
			this.m_CurveEditor.m_PlayHead = this.m_State;
		}

		private void InitializeHorizontalSplitter()
		{
			this.m_HorizontalSplitter = new SplitterState(new float[]
			{
				300f,
				900f
			}, new int[]
			{
				300,
				300
			}, null);
			this.m_HorizontalSplitter.realSizes[0] = 300;
			this.m_HorizontalSplitter.realSizes[1] = (int)this.m_Position.width - 300;
			this.ClampSplitterSize();
		}

		private void InitializeNonserializedValues()
		{
			AnimationWindowState expr_06 = this.m_State;
			expr_06.onFrameRateChange = (Action<float>)Delegate.Combine(expr_06.onFrameRateChange, new Action<float>(delegate(float newFrameRate)
			{
				this.m_CurveEditor.invSnap = newFrameRate;
				this.m_CurveEditor.hTicks.SetTickModulosForFrameRate(newFrameRate);
			}));
		}
	}
}
