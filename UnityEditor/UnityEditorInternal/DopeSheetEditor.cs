using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
	[Serializable]
	internal class DopeSheetEditor : TimeArea, CurveUpdater
	{
		private struct DrawElement
		{
			public Rect position;

			public Color color;

			public Texture2D texture;

			public DrawElement(Rect position, Color color, Texture2D texture)
			{
				this.position = position;
				this.color = color;
				this.texture = texture;
			}
		}

		internal class DopeSheetSelectionRect
		{
			private enum SelectionType
			{
				Normal,
				Additive,
				Subtractive
			}

			private Vector2 m_SelectStartPoint;

			private Vector2 m_SelectMousePoint;

			private bool m_ValidRect;

			private DopeSheetEditor owner;

			public readonly GUIStyle createRect = "U2D.createRect";

			private static int s_RectSelectionID = GUIUtility.GetPermanentControlID();

			public DopeSheetSelectionRect(DopeSheetEditor owner)
			{
				this.owner = owner;
			}

			public void OnGUI(Rect position)
			{
				Event current = Event.current;
				Vector2 mousePosition = current.mousePosition;
				int num = DopeSheetEditor.DopeSheetSelectionRect.s_RectSelectionID;
				switch (current.GetTypeForControl(num))
				{
				case EventType.MouseDown:
					if (current.button == 0 && position.Contains(mousePosition))
					{
						GUIUtility.hotControl = num;
						this.m_SelectStartPoint = mousePosition;
						this.m_ValidRect = false;
						current.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == num && current.button == 0)
					{
						if (this.m_ValidRect)
						{
							if (!EditorGUI.actionKey)
							{
								this.owner.state.ClearSelections();
							}
							float frameRate = this.owner.state.frameRate;
							Rect currentTimeRect = this.GetCurrentTimeRect();
							AnimationKeyTime animationKeyTime = AnimationKeyTime.Time(currentTimeRect.xMin, frameRate);
							AnimationKeyTime animationKeyTime2 = AnimationKeyTime.Time(currentTimeRect.xMax, frameRate);
							GUI.changed = true;
							this.owner.state.ClearHierarchySelection();
							List<AnimationWindowKeyframe> list = new List<AnimationWindowKeyframe>();
							List<AnimationWindowKeyframe> list2 = new List<AnimationWindowKeyframe>();
							foreach (DopeLine current2 in this.owner.state.dopelines)
							{
								if (current2.position.yMin >= currentTimeRect.yMin && current2.position.yMax <= currentTimeRect.yMax)
								{
									foreach (AnimationWindowKeyframe current3 in current2.keys)
									{
										AnimationKeyTime animationKeyTime3 = AnimationKeyTime.Time(current3.time, frameRate);
										if (((!current2.tallMode && animationKeyTime3.frame >= animationKeyTime.frame && animationKeyTime3.frame <= animationKeyTime2.frame) || (current2.tallMode && animationKeyTime3.frame >= animationKeyTime.frame && animationKeyTime3.frame < animationKeyTime2.frame)) && !list2.Contains(current3) && !list.Contains(current3))
										{
											if (!this.owner.state.KeyIsSelected(current3))
											{
												list2.Add(current3);
											}
											else if (this.owner.state.KeyIsSelected(current3))
											{
												list.Add(current3);
											}
										}
									}
								}
							}
							if (list2.Count == 0)
							{
								foreach (AnimationWindowKeyframe current4 in list)
								{
									this.owner.state.UnselectKey(current4);
								}
							}
							foreach (AnimationWindowKeyframe current5 in list2)
							{
								this.owner.state.SelectKey(current5);
							}
							foreach (DopeLine current6 in this.owner.state.dopelines)
							{
								if (this.owner.state.AnyKeyIsSelected(current6))
								{
									this.owner.state.SelectHierarchyItem(current6, true, false);
								}
							}
						}
						else
						{
							this.owner.state.ClearSelections();
						}
						current.Use();
						GUIUtility.hotControl = 0;
					}
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == num)
					{
						this.m_ValidRect = (Mathf.Abs((mousePosition - this.m_SelectStartPoint).x) > 1f);
						if (this.m_ValidRect)
						{
							this.m_SelectMousePoint = new Vector2(mousePosition.x, mousePosition.y);
						}
						current.Use();
					}
					break;
				case EventType.Repaint:
					if (GUIUtility.hotControl == num && this.m_ValidRect)
					{
						EditorStyles.selectionRect.Draw(this.GetCurrentPixelRect(), GUIContent.none, false, false, false, false);
					}
					break;
				}
			}

			public Rect GetCurrentPixelRect()
			{
				float num = 16f;
				Rect result = AnimationWindowUtility.FromToRect(this.m_SelectStartPoint, this.m_SelectMousePoint);
				result.xMin = this.owner.state.TimeToPixel(this.owner.state.PixelToTime(result.xMin, true), true);
				result.xMax = this.owner.state.TimeToPixel(this.owner.state.PixelToTime(result.xMax, true), true);
				result.yMin = Mathf.Floor(result.yMin / num) * num;
				result.yMax = (Mathf.Floor(result.yMax / num) + 1f) * num;
				return result;
			}

			public Rect GetCurrentTimeRect()
			{
				float num = 16f;
				Rect result = AnimationWindowUtility.FromToRect(this.m_SelectStartPoint, this.m_SelectMousePoint);
				result.xMin = this.owner.state.PixelToTime(result.xMin, true);
				result.xMax = this.owner.state.PixelToTime(result.xMax, true);
				result.yMin = Mathf.Floor(result.yMin / num) * num;
				result.yMax = (Mathf.Floor(result.yMax / num) + 1f) * num;
				return result;
			}
		}

		internal class DopeSheetPopup
		{
			private Rect position;

			private static int s_width = 96;

			private static int s_height = 112;

			private Rect backgroundRect;

			public DopeSheetPopup(Rect position)
			{
				this.position = position;
			}

			public void OnGUI(AnimationWindowState state, AnimationWindowKeyframe keyframe)
			{
				if (keyframe.isPPtrCurve)
				{
					return;
				}
				this.backgroundRect = this.position;
				this.backgroundRect.x = state.TimeToPixel(keyframe.time) + this.position.x - (float)(DopeSheetEditor.DopeSheetPopup.s_width / 2);
				this.backgroundRect.y = this.backgroundRect.y + 16f;
				this.backgroundRect.width = (float)DopeSheetEditor.DopeSheetPopup.s_width;
				this.backgroundRect.height = (float)DopeSheetEditor.DopeSheetPopup.s_height;
				Rect rect = this.backgroundRect;
				rect.height = 16f;
				Rect rect2 = this.backgroundRect;
				rect2.y += 16f;
				rect2.height = (float)DopeSheetEditor.DopeSheetPopup.s_width;
				GUI.Box(this.backgroundRect, string.Empty);
				GUI.Box(rect2, AssetPreview.GetAssetPreview((UnityEngine.Object)keyframe.value));
				EditorGUI.BeginChangeCheck();
				UnityEngine.Object value = EditorGUI.ObjectField(rect, (UnityEngine.Object)keyframe.value, keyframe.curve.m_ValueType, false);
				if (EditorGUI.EndChangeCheck())
				{
					keyframe.value = value;
					state.SaveCurve(keyframe.curve);
				}
			}
		}

		private const float k_KeyframeOffset = -5.5f;

		private const float k_PptrKeyframeOffset = -1f;

		public AnimationWindowState state;

		[SerializeField]
		public EditorWindow m_Owner;

		private DopeSheetEditor.DopeSheetSelectionRect m_SelectionRect;

		private Texture m_DefaultDopeKeyIcon;

		private float m_DragStartTime;

		private bool m_MousedownOnKeyframe;

		private bool m_IsDragging;

		private bool m_IsDraggingPlayheadStarted;

		private bool m_IsDraggingPlayhead;

		private bool m_Initialized;

		public Bounds m_Bounds = new Bounds(Vector3.zero, Vector3.zero);

		private List<DopeSheetEditor.DrawElement> selectedKeysDrawBuffer;

		private List<DopeSheetEditor.DrawElement> unselectedKeysDrawBuffer;

		private List<DopeSheetEditor.DrawElement> dragdropKeysDrawBuffer;

		public bool m_SpritePreviewLoading;

		public int m_SpritePreviewCacheSize;

		public float contentHeight
		{
			get
			{
				float num = 0f;
				foreach (DopeLine current in this.state.dopelines)
				{
					num += ((!current.tallMode) ? 16f : 32f);
				}
				num += 40f;
				return num;
			}
		}

		public override Bounds drawingBounds
		{
			get
			{
				return this.m_Bounds;
			}
		}

		internal int assetPreviewManagerID
		{
			get
			{
				return (!(this.m_Owner != null)) ? 0 : this.m_Owner.GetInstanceID();
			}
		}

		public DopeSheetEditor(EditorWindow owner) : base(false)
		{
			this.m_Owner = owner;
		}

		internal void OnDestroy()
		{
			AssetPreview.DeletePreviewTextureManagerByID(this.assetPreviewManagerID);
		}

		public void OnGUI(Rect position, Vector2 scrollPosition)
		{
			this.Init();
			EditorGUI.BeginDisabledGroup(!this.state.clipIsEditable);
			this.HandleDragAndDropToEmptyArea();
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(this.state.animationIsReadOnly);
			GUIClip.Push(position, scrollPosition, Vector2.zero, false);
			Rect position2 = new Rect(0f, 0f, position.width, position.height);
			Rect rect = this.DopelinesGUI(position2, scrollPosition);
			if (GUI.enabled)
			{
				this.HandleKeyboard();
				this.HandleDragging();
				this.HandleSelectionRect(rect);
				this.HandleDelete();
			}
			GUIClip.Pop();
			EditorGUI.EndDisabledGroup();
		}

		public void Init()
		{
			if (!this.m_Initialized)
			{
				if (this.m_DefaultDopeKeyIcon == null)
				{
					this.m_DefaultDopeKeyIcon = EditorGUIUtility.LoadIcon("blendKey");
				}
				base.hSlider = true;
				base.vSlider = false;
				base.hRangeLocked = false;
				base.vRangeLocked = true;
				base.hRangeMin = 0f;
				base.margin = 40f;
				base.scaleWithWindow = true;
				base.ignoreScrollWheelUntilClicked = false;
			}
			this.m_Initialized = true;
		}

		public void RecalculateBounds()
		{
			if (this.state.activeAnimationClip)
			{
				this.m_Bounds.SetMinMax(new Vector3(this.state.activeAnimationClip.startTime, 0f, 0f), new Vector3(this.state.activeAnimationClip.stopTime, 0f, 0f));
			}
		}

		private Rect DopelinesGUI(Rect position, Vector2 scrollPosition)
		{
			Color color = GUI.color;
			Rect position2 = position;
			this.selectedKeysDrawBuffer = new List<DopeSheetEditor.DrawElement>();
			this.unselectedKeysDrawBuffer = new List<DopeSheetEditor.DrawElement>();
			this.dragdropKeysDrawBuffer = new List<DopeSheetEditor.DrawElement>();
			if (Event.current.type == EventType.Repaint)
			{
				this.m_SpritePreviewLoading = false;
			}
			if (Event.current.type == EventType.MouseDown)
			{
				this.m_IsDragging = false;
			}
			this.UpdateSpritePreviewCacheSize();
			foreach (DopeLine current in this.state.dopelines)
			{
				current.position = position2;
				current.position.height = ((!current.tallMode) ? 16f : 32f);
				if ((current.position.yMin + scrollPosition.y >= position.yMin && current.position.yMin + scrollPosition.y <= position.yMax) || (current.position.yMax + scrollPosition.y >= position.yMin && current.position.yMax + scrollPosition.y <= position.yMax))
				{
					Event current2 = Event.current;
					EventType type = current2.type;
					switch (type)
					{
					case EventType.Repaint:
						this.DopeLineRepaint(current);
						goto IL_1B7;
					case EventType.Layout:
						IL_14B:
						if (type == EventType.MouseDown)
						{
							if (current2.button == 0)
							{
								this.HandleMouseDown(current);
							}
							goto IL_1B7;
						}
						if (type != EventType.ContextClick)
						{
							goto IL_1B7;
						}
						if (!this.m_IsDraggingPlayhead)
						{
							this.HandleContextMenu(current);
						}
						goto IL_1B7;
					case EventType.DragUpdated:
					case EventType.DragPerform:
						if (this.state.clipIsEditable)
						{
							this.HandleDragAndDrop(current);
						}
						goto IL_1B7;
					}
					goto IL_14B;
				}
				IL_1B7:
				position2.y += current.position.height;
			}
			if (Event.current.type == EventType.MouseUp)
			{
				this.m_IsDraggingPlayheadStarted = false;
				this.m_IsDraggingPlayhead = false;
			}
			Rect result = new Rect(position.xMin, position.yMin, position.width, position2.yMax - position.yMin);
			this.DrawElements(this.unselectedKeysDrawBuffer);
			this.DrawElements(this.selectedKeysDrawBuffer);
			this.DrawElements(this.dragdropKeysDrawBuffer);
			GUI.color = color;
			return result;
		}

		private void DrawGrid(Rect position)
		{
			base.TimeRuler(position, this.state.frameRate, false, true, 0.2f);
		}

		public void DrawMasterDopelineBackground(Rect position)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			AnimationWindowStyles.eventBackground.Draw(position, false, false, false, false);
		}

		private void UpdateSpritePreviewCacheSize()
		{
			int num = 1;
			foreach (DopeLine current in this.state.dopelines)
			{
				if (current.tallMode && current.isPptrDopeline)
				{
					num += current.keys.Count;
				}
			}
			num += DragAndDrop.objectReferences.Length;
			if (num > this.m_SpritePreviewCacheSize)
			{
				AssetPreview.SetPreviewTextureCacheSize(num, this.assetPreviewManagerID);
				this.m_SpritePreviewCacheSize = num;
			}
		}

		private void DrawElements(List<DopeSheetEditor.DrawElement> elements)
		{
			Color color = GUI.color;
			Color color2 = Color.white;
			GUI.color = color2;
			Texture defaultDopeKeyIcon = this.m_DefaultDopeKeyIcon;
			foreach (DopeSheetEditor.DrawElement current in elements)
			{
				if (current.color != color2)
				{
					color2 = ((!GUI.enabled) ? (current.color * 0.8f) : current.color);
					GUI.color = color2;
				}
				if (current.texture != null)
				{
					GUI.DrawTexture(current.position, current.texture);
				}
				else
				{
					Rect position = new Rect(current.position.center.x - (float)(defaultDopeKeyIcon.width / 2), current.position.center.y - (float)(defaultDopeKeyIcon.height / 2), (float)defaultDopeKeyIcon.width, (float)defaultDopeKeyIcon.height);
					GUI.DrawTexture(position, defaultDopeKeyIcon, ScaleMode.ScaleToFit, true, 1f);
				}
			}
			GUI.color = color;
		}

		private void DopeLineRepaint(DopeLine dopeline)
		{
			Color color = GUI.color;
			AnimationWindowHierarchyNode animationWindowHierarchyNode = (AnimationWindowHierarchyNode)this.state.hierarchyData.FindItem(dopeline.m_HierarchyNodeID);
			bool flag = animationWindowHierarchyNode != null && animationWindowHierarchyNode.depth > 0;
			Color color2 = (!flag) ? Color.gray.AlphaMultiplied(0.16f) : Color.gray.AlphaMultiplied(0.05f);
			if (!dopeline.isMasterDopeline)
			{
				DopeSheetEditor.DrawBox(dopeline.position, color2);
			}
			int? num = null;
			int count = dopeline.keys.Count;
			for (int i = 0; i < count; i++)
			{
				AnimationWindowKeyframe animationWindowKeyframe = dopeline.keys[i];
				if (!(num == animationWindowKeyframe.m_TimeHash))
				{
					num = new int?(animationWindowKeyframe.m_TimeHash);
					Rect rect = this.GetKeyframeRect(dopeline, animationWindowKeyframe);
					color2 = ((!dopeline.isMasterDopeline) ? Color.gray.RGBMultiplied(1.2f) : Color.gray.RGBMultiplied(0.85f));
					Texture2D texture2D = null;
					if (animationWindowKeyframe.isPPtrCurve && dopeline.tallMode)
					{
						texture2D = ((animationWindowKeyframe.value != null) ? AssetPreview.GetAssetPreview(((UnityEngine.Object)animationWindowKeyframe.value).GetInstanceID(), this.assetPreviewManagerID) : null);
					}
					if (texture2D != null)
					{
						rect = this.GetPreviewRectFromKeyFrameRect(rect);
						color2 = Color.white.AlphaMultiplied(0.5f);
					}
					else if (animationWindowKeyframe.value != null && animationWindowKeyframe.isPPtrCurve && dopeline.tallMode)
					{
						this.m_SpritePreviewLoading = true;
					}
					if (Mathf.Approximately(animationWindowKeyframe.time, 0f))
					{
						rect.xMin -= 0.01f;
					}
					if (this.AnyKeyIsSelectedAtTime(dopeline, i))
					{
						color2 = ((!dopeline.tallMode || !dopeline.isPptrDopeline) ? new Color(0.34f, 0.52f, 0.85f, 1f) : Color.white);
						if (dopeline.isMasterDopeline)
						{
							color2 = color2.RGBMultiplied(0.85f);
						}
						this.selectedKeysDrawBuffer.Add(new DopeSheetEditor.DrawElement(rect, color2, texture2D));
					}
					else
					{
						this.unselectedKeysDrawBuffer.Add(new DopeSheetEditor.DrawElement(rect, color2, texture2D));
					}
				}
			}
			if (this.state.clipIsEditable && this.DoDragAndDrop(dopeline, dopeline.position, false))
			{
				float num2 = Mathf.Max(this.state.PixelToTime(Event.current.mousePosition.x), 0f);
				Color color3 = Color.gray.RGBMultiplied(1.2f);
				Texture2D texture2D2 = null;
				UnityEngine.Object[] sortedDragAndDropObjectReferences = this.GetSortedDragAndDropObjectReferences();
				for (int j = 0; j < sortedDragAndDropObjectReferences.Length; j++)
				{
					UnityEngine.Object @object = sortedDragAndDropObjectReferences[j];
					Rect rect2 = this.GetDragAndDropRect(dopeline, this.state.TimeToPixel(num2));
					if (dopeline.isPptrDopeline && dopeline.tallMode)
					{
						texture2D2 = AssetPreview.GetAssetPreview(@object.GetInstanceID(), this.assetPreviewManagerID);
					}
					if (texture2D2 != null)
					{
						rect2 = this.GetPreviewRectFromKeyFrameRect(rect2);
						color3 = Color.white.AlphaMultiplied(0.5f);
					}
					this.dragdropKeysDrawBuffer.Add(new DopeSheetEditor.DrawElement(rect2, color3, texture2D2));
					num2 += 1f / this.state.frameRate;
				}
			}
			GUI.color = color;
		}

		private Rect GetPreviewRectFromKeyFrameRect(Rect keyframeRect)
		{
			keyframeRect.width -= 2f;
			keyframeRect.height -= 2f;
			keyframeRect.xMin += 2f;
			keyframeRect.yMin += 2f;
			return keyframeRect;
		}

		private Rect GetDragAndDropRect(DopeLine dopeline, float screenX)
		{
			Rect keyframeRect = this.GetKeyframeRect(dopeline, null);
			float keyframeOffset = this.GetKeyframeOffset(dopeline, null);
			float time = Mathf.Max(this.state.PixelToTime(screenX - keyframeRect.width * 0.5f, true), 0f);
			keyframeRect.center = new Vector2(this.state.TimeToPixel(time) + keyframeRect.width * 0.5f + keyframeOffset, keyframeRect.center.y);
			return keyframeRect;
		}

		private static void DrawBox(Rect position, Color color)
		{
			Color color2 = GUI.color;
			GUI.color = color;
			DopeLine.dopekeyStyle.Draw(position, GUIContent.none, 0, false);
			GUI.color = color2;
		}

		private GenericMenu GenerateMenu(DopeLine dopeline, bool clickedEmpty)
		{
			GenericMenu genericMenu = new GenericMenu();
			this.state.recording = true;
			this.state.ResampleAnimation();
			string text = "Add Key";
			if (clickedEmpty)
			{
				genericMenu.AddItem(new GUIContent(text), false, new GenericMenu.MenuFunction2(this.AddKeyToDopeline), dopeline);
			}
			else
			{
				genericMenu.AddDisabledItem(new GUIContent(text));
			}
			text = ((this.state.selectedKeys.Count <= 1) ? "Delete Key" : "Delete Keys");
			if (this.state.selectedKeys.Count > 0)
			{
				genericMenu.AddItem(new GUIContent(text), false, new GenericMenu.MenuFunction(this.DeleteSelectedKeys));
			}
			else
			{
				genericMenu.AddDisabledItem(new GUIContent(text));
			}
			if (AnimationWindowUtility.ContainsFloatKeyframes(this.state.selectedKeys))
			{
				genericMenu.AddSeparator(string.Empty);
				List<KeyIdentifier> list = new List<KeyIdentifier>();
				foreach (AnimationWindowKeyframe current in this.state.selectedKeys)
				{
					if (!current.isPPtrCurve)
					{
						int keyframeIndex = current.curve.GetKeyframeIndex(AnimationKeyTime.Time(current.time, this.state.frameRate));
						if (keyframeIndex != -1)
						{
							CurveRenderer curveRenderer = CurveRendererCache.GetCurveRenderer(this.state.activeAnimationClip, current.curve.binding);
							int curveID = CurveUtility.GetCurveID(this.state.activeAnimationClip, current.curve.binding);
							list.Add(new KeyIdentifier(curveRenderer, curveID, keyframeIndex, current.curve.binding));
						}
					}
				}
				CurveMenuManager curveMenuManager = new CurveMenuManager(this);
				curveMenuManager.AddTangentMenuItems(genericMenu, list);
			}
			return genericMenu;
		}

		private void HandleDragging()
		{
			int controlID = GUIUtility.GetControlID("dopesheetdrag".GetHashCode(), FocusType.Passive, default(Rect));
			if ((Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp) && this.m_MousedownOnKeyframe)
			{
				if (Event.current.type == EventType.MouseDrag && !EditorGUI.actionKey && !Event.current.shift && !this.m_IsDragging && this.state.selectedKeys.Count > 0)
				{
					this.m_IsDragging = true;
					this.m_IsDraggingPlayheadStarted = true;
					GUIUtility.hotControl = controlID;
					this.m_DragStartTime = this.state.PixelToTime(Event.current.mousePosition.x);
					Event.current.Use();
				}
				float num = 3.40282347E+38f;
				foreach (AnimationWindowKeyframe current in this.state.selectedKeys)
				{
					num = Mathf.Min(current.time, num);
				}
				float num2 = this.state.SnapToFrame(this.state.PixelToTime(Event.current.mousePosition.x));
				float deltaTime = Mathf.Max(num2 - this.m_DragStartTime, num * -1f);
				if (this.m_IsDragging && !Mathf.Approximately(num2, this.m_DragStartTime))
				{
					this.m_DragStartTime = num2;
					this.state.MoveSelectedKeys(deltaTime, true, false);
					if (this.state.activeKeyframe != null && !this.state.playing)
					{
						this.state.frame = this.state.TimeToFrameFloor(this.state.activeKeyframe.time);
					}
					Event.current.Use();
				}
				if (Event.current.type == EventType.MouseUp)
				{
					if (this.m_IsDragging && GUIUtility.hotControl == controlID)
					{
						this.state.MoveSelectedKeys(deltaTime, true, true);
						Event.current.Use();
						this.m_IsDragging = false;
					}
					this.m_MousedownOnKeyframe = false;
					GUIUtility.hotControl = 0;
				}
			}
			if (this.m_IsDraggingPlayheadStarted && Event.current.type == EventType.MouseDrag && Event.current.button == 1)
			{
				this.m_IsDraggingPlayhead = true;
				Event.current.Use();
			}
		}

		private void HandleKeyboard()
		{
			if (Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand)
			{
				string commandName = Event.current.commandName;
				if (commandName != null)
				{
					if (DopeSheetEditor.<>f__switch$mapD == null)
					{
						DopeSheetEditor.<>f__switch$mapD = new Dictionary<string, int>(2)
						{
							{
								"SelectAll",
								0
							},
							{
								"FrameSelected",
								1
							}
						};
					}
					int num;
					if (DopeSheetEditor.<>f__switch$mapD.TryGetValue(commandName, out num))
					{
						if (num != 0)
						{
							if (num == 1)
							{
								if (Event.current.type == EventType.ExecuteCommand)
								{
									this.FrameSelected();
								}
								Event.current.Use();
							}
						}
						else
						{
							if (Event.current.type == EventType.ExecuteCommand)
							{
								this.HandleSelectAll();
							}
							Event.current.Use();
						}
					}
				}
			}
		}

		private void HandleSelectAll()
		{
			foreach (DopeLine current in this.state.dopelines)
			{
				foreach (AnimationWindowKeyframe current2 in current.keys)
				{
					this.state.SelectKey(current2);
				}
				this.state.SelectHierarchyItem(current, true, false);
			}
		}

		private void HandleDelete()
		{
			EventType type = Event.current.type;
			if (type != EventType.ValidateCommand && type != EventType.ExecuteCommand)
			{
				if (type == EventType.KeyDown)
				{
					if (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Delete)
					{
						this.state.DeleteSelectedKeys();
						Event.current.Use();
					}
				}
			}
			else if (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete")
			{
				if (Event.current.type == EventType.ExecuteCommand)
				{
					this.state.DeleteSelectedKeys();
				}
				Event.current.Use();
			}
		}

		private void HandleSelectionRect(Rect rect)
		{
			if (this.m_SelectionRect == null)
			{
				this.m_SelectionRect = new DopeSheetEditor.DopeSheetSelectionRect(this);
			}
			if (!this.m_MousedownOnKeyframe)
			{
				this.m_SelectionRect.OnGUI(rect);
			}
		}

		private void HandleDragAndDropToEmptyArea()
		{
			Event current = Event.current;
			if (current.type != EventType.DragPerform && current.type != EventType.DragUpdated)
			{
				return;
			}
			if (!DopeSheetEditor.ValidateDragAndDropObjects() || this.state.activeGameObject == null)
			{
				return;
			}
			if (DragAndDrop.objectReferences[0].GetType() != typeof(Sprite) && DragAndDrop.objectReferences[0].GetType() != typeof(Texture2D))
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
				return;
			}
			if (this.DopelineForValueTypeExists(typeof(Sprite)))
			{
				return;
			}
			if (current.type == EventType.DragPerform)
			{
				EditorCurveBinding? spriteBinding = this.CreateNewPptrDopeline(typeof(Sprite));
				if (spriteBinding.HasValue)
				{
					this.DoSpriteDropAfterGeneratingNewDopeline(spriteBinding);
				}
			}
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			current.Use();
		}

		private void DoSpriteDropAfterGeneratingNewDopeline(EditorCurveBinding? spriteBinding)
		{
			if (DragAndDrop.objectReferences.Length == 1)
			{
				Analytics.Event("Sprite Drag and Drop", "Drop single sprite into empty dopesheet", "null", 1);
			}
			else
			{
				Analytics.Event("Sprite Drag and Drop", "Drop multiple sprites into empty dopesheet", "null", 1);
			}
			AnimationWindowCurve animationWindowCurve = new AnimationWindowCurve(this.state.activeAnimationClip, spriteBinding.Value, typeof(Sprite));
			this.state.SaveCurve(animationWindowCurve);
			this.PeformDragAndDrop(animationWindowCurve, 0f);
		}

		private bool DopelineForValueTypeExists(Type valueType)
		{
			string value = AnimationUtility.CalculateTransformPath(this.state.activeGameObject.transform, this.state.activeRootGameObject.transform);
			foreach (DopeLine current in this.state.dopelines)
			{
				if (current.valueType == valueType)
				{
					AnimationWindowHierarchyNode animationWindowHierarchyNode = (AnimationWindowHierarchyNode)this.state.hierarchyData.FindItem(current.m_HierarchyNodeID);
					if (animationWindowHierarchyNode != null && animationWindowHierarchyNode.path.Equals(value))
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
						return true;
					}
				}
			}
			return false;
		}

		private EditorCurveBinding? CreateNewPptrDopeline(Type valueType)
		{
			List<EditorCurveBinding> animatableProperties = AnimationWindowUtility.GetAnimatableProperties(this.state.activeRootGameObject, this.state.activeRootGameObject, valueType);
			if (animatableProperties.Count == 0 && valueType == typeof(Sprite))
			{
				return this.CreateNewSpriteRendererDopeline();
			}
			if (animatableProperties.Count == 1)
			{
				return new EditorCurveBinding?(animatableProperties[0]);
			}
			List<string> list = new List<string>();
			foreach (EditorCurveBinding current in animatableProperties)
			{
				list.Add(current.type.Name);
			}
			Rect position = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1f, 1f);
			EditorUtility.DisplayCustomMenu(position, EditorGUIUtility.TempContent(list.ToArray()), -1, new EditorUtility.SelectMenuItemFunction(this.SelectTypeForCreatingNewPptrDopeline), animatableProperties);
			return null;
		}

		private void SelectTypeForCreatingNewPptrDopeline(object userData, string[] options, int selected)
		{
			EditorCurveBinding[] array = userData as EditorCurveBinding[];
			if (array.Length > selected)
			{
				this.DoSpriteDropAfterGeneratingNewDopeline(new EditorCurveBinding?(array[selected]));
			}
		}

		private EditorCurveBinding? CreateNewSpriteRendererDopeline()
		{
			if (!this.state.activeGameObject.GetComponent<SpriteRenderer>())
			{
				this.state.activeGameObject.AddComponent<SpriteRenderer>();
			}
			List<EditorCurveBinding> animatableProperties = AnimationWindowUtility.GetAnimatableProperties(this.state.activeRootGameObject, this.state.activeRootGameObject, typeof(SpriteRenderer), typeof(Sprite));
			if (animatableProperties.Count == 1)
			{
				return new EditorCurveBinding?(animatableProperties[0]);
			}
			Debug.LogError("Unable to create animatable SpriteRenderer component");
			return null;
		}

		private void HandleDragAndDrop(DopeLine dopeline)
		{
			Event current = Event.current;
			if (current.type != EventType.DragPerform && current.type != EventType.DragUpdated)
			{
				return;
			}
			if (this.DoDragAndDrop(dopeline, dopeline.position, current.type == EventType.DragPerform))
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				current.Use();
			}
			else
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
		}

		private void HandleMouseDown(DopeLine dopeline)
		{
			Event current = Event.current;
			if (!EditorGUI.actionKey && !current.shift)
			{
				foreach (AnimationWindowKeyframe current2 in dopeline.keys)
				{
					if (this.GetKeyframeRect(dopeline, current2).Contains(current.mousePosition) && !this.state.KeyIsSelected(current2))
					{
						this.state.ClearSelections();
						break;
					}
				}
			}
			float num = this.state.PixelToTime(Event.current.mousePosition.x);
			float num2 = num;
			if (Event.current.shift)
			{
				foreach (AnimationWindowKeyframe current3 in dopeline.keys)
				{
					if (this.state.KeyIsSelected(current3))
					{
						if (current3.time < num)
						{
							num = current3.time;
						}
						if (current3.time > num2)
						{
							num2 = current3.time;
						}
					}
				}
			}
			bool flag = false;
			foreach (AnimationWindowKeyframe current4 in dopeline.keys)
			{
				if (this.GetKeyframeRect(dopeline, current4).Contains(current.mousePosition))
				{
					flag = true;
					if (!this.state.KeyIsSelected(current4))
					{
						if (Event.current.shift)
						{
							foreach (AnimationWindowKeyframe current5 in dopeline.keys)
							{
								if (current5 == current4 || (current5.time > num && current5.time < num2))
								{
									this.state.SelectKey(current5);
								}
							}
						}
						else
						{
							this.state.SelectKey(current4);
						}
						if (!dopeline.isMasterDopeline)
						{
							this.state.SelectHierarchyItem(dopeline, EditorGUI.actionKey || current.shift);
						}
					}
					else if (EditorGUI.actionKey)
					{
						this.state.UnselectKey(current4);
						if (!this.state.AnyKeyIsSelected(dopeline))
						{
							this.state.UnSelectHierarchyItem(dopeline);
						}
					}
					this.state.activeKeyframe = current4;
					this.m_MousedownOnKeyframe = true;
					if (!this.state.playing)
					{
						this.state.frame = this.state.TimeToFrameFloor(this.state.activeKeyframe.time);
					}
					current.Use();
				}
			}
			if (dopeline.isMasterDopeline)
			{
				this.state.ClearHierarchySelection();
				List<int> affectedHierarchyIDs = this.state.GetAffectedHierarchyIDs(this.state.selectedKeys);
				foreach (int current6 in affectedHierarchyIDs)
				{
					this.state.SelectHierarchyItem(current6, true, true);
				}
			}
			if (dopeline.position.Contains(Event.current.mousePosition))
			{
				if (current.clickCount == 2 && current.button == 0 && !Event.current.shift && !EditorGUI.actionKey)
				{
					this.HandleDopelineDoubleclick(dopeline);
				}
				if (current.button == 1 && !this.state.playing)
				{
					float time = this.state.PixelToTime(Event.current.mousePosition.x, true);
					AnimationKeyTime animationKeyTime = AnimationKeyTime.Time(time, this.state.frameRate);
					this.state.frame = animationKeyTime.frame;
					if (!flag)
					{
						this.state.ClearSelections();
						this.m_IsDraggingPlayheadStarted = true;
						HandleUtility.Repaint();
						current.Use();
					}
				}
			}
		}

		private void HandleDopelineDoubleclick(DopeLine dopeline)
		{
			this.state.ClearSelections();
			float time = this.state.PixelToTime(Event.current.mousePosition.x, true);
			AnimationKeyTime time2 = AnimationKeyTime.Time(time, this.state.frameRate);
			AnimationWindowCurve[] curves = dopeline.m_Curves;
			for (int i = 0; i < curves.Length; i++)
			{
				AnimationWindowCurve curve = curves[i];
				AnimationWindowKeyframe keyframe = AnimationWindowUtility.AddKeyframeToCurve(this.state, curve, time2);
				this.state.SelectKey(keyframe);
			}
			if (!this.state.playing)
			{
				this.state.frame = time2.frame;
			}
			Event.current.Use();
		}

		private void HandleContextMenu(DopeLine dopeline)
		{
			if (!dopeline.position.Contains(Event.current.mousePosition))
			{
				return;
			}
			bool clickedEmpty = true;
			foreach (AnimationWindowKeyframe current in dopeline.keys)
			{
				if (this.GetKeyframeRect(dopeline, current).Contains(Event.current.mousePosition))
				{
					clickedEmpty = false;
					break;
				}
			}
			this.GenerateMenu(dopeline, clickedEmpty).ShowAsContext();
		}

		private Rect GetKeyframeRect(DopeLine dopeline, AnimationWindowKeyframe keyframe)
		{
			float time = (keyframe == null) ? 0f : keyframe.time;
			float width = 10f;
			if (dopeline.isPptrDopeline && dopeline.tallMode && (keyframe == null || keyframe.value != null))
			{
				width = dopeline.position.height;
			}
			if (dopeline.isPptrDopeline && dopeline.tallMode)
			{
				return new Rect(this.state.TimeToPixel(this.state.SnapToFrame(time)) + this.GetKeyframeOffset(dopeline, keyframe), dopeline.position.yMin, width, dopeline.position.height);
			}
			return new Rect(this.state.TimeToPixel(this.state.SnapToFrame(time)) + this.GetKeyframeOffset(dopeline, keyframe), dopeline.position.yMin, width, dopeline.position.height);
		}

		private float GetKeyframeOffset(DopeLine dopeline, AnimationWindowKeyframe keyframe)
		{
			if (dopeline.isPptrDopeline && dopeline.tallMode && (keyframe == null || keyframe.value != null))
			{
				return -1f;
			}
			return -5.5f;
		}

		public void FrameClip()
		{
			if (!this.state.activeAnimationClip)
			{
				return;
			}
			float max = Mathf.Max(this.state.activeAnimationClip.length, 1f);
			base.SetShownHRangeInsideMargins(0f, max);
		}

		public void FrameSelected()
		{
			float num = 3.40282347E+38f;
			float num2 = -3.40282347E+38f;
			bool flag = this.state.selectedKeys.Count > 0;
			if (flag)
			{
				foreach (AnimationWindowKeyframe current in this.state.selectedKeys)
				{
					num = Mathf.Min(current.time, num);
					num2 = Mathf.Max(current.time, num2);
				}
			}
			bool flag2 = !flag;
			if (!flag)
			{
				bool flag3 = this.state.hierarchyState.selectedIDs.Count > 0;
				if (flag3)
				{
					foreach (AnimationWindowCurve current2 in this.state.activeCurves)
					{
						int count = current2.m_Keyframes.Count;
						if (count > 1)
						{
							num = Mathf.Min(current2.m_Keyframes[0].time, num);
							num2 = Mathf.Max(current2.m_Keyframes[count - 1].time, num2);
							flag2 = false;
						}
					}
				}
			}
			if (flag2)
			{
				this.FrameClip();
			}
			else
			{
				float num3 = this.state.FrameToTime(Mathf.Min(4f, this.state.frameRate));
				num3 = Mathf.Min(num3, Mathf.Max(this.state.activeAnimationClip.length, this.state.FrameToTime(4f)));
				num2 = Mathf.Max(num2, num + num3);
				base.SetShownHRangeInsideMargins(num, num2);
			}
		}

		private bool DoDragAndDrop(DopeLine dopeLine, Rect position, bool perform)
		{
			return this.DoDragAndDrop(dopeLine, position, false, perform);
		}

		private bool DoDragAndDrop(DopeLine dopeLine, bool perform)
		{
			return this.DoDragAndDrop(dopeLine, default(Rect), true, perform);
		}

		private bool DoDragAndDrop(DopeLine dopeLine, Rect position, bool ignoreMousePosition, bool perform)
		{
			if (!ignoreMousePosition && !position.Contains(Event.current.mousePosition))
			{
				return false;
			}
			if (!DopeSheetEditor.ValidateDragAndDropObjects())
			{
				return false;
			}
			Type type = DragAndDrop.objectReferences[0].GetType();
			AnimationWindowCurve animationWindowCurve = null;
			if (dopeLine.valueType == type)
			{
				animationWindowCurve = dopeLine.m_Curves[0];
			}
			else
			{
				AnimationWindowCurve[] curves = dopeLine.m_Curves;
				for (int i = 0; i < curves.Length; i++)
				{
					AnimationWindowCurve animationWindowCurve2 = curves[i];
					if (animationWindowCurve2.isPPtrCurve)
					{
						if (animationWindowCurve2.m_ValueType == type)
						{
							animationWindowCurve = animationWindowCurve2;
						}
						Sprite[] spriteFromDraggedPathsOrObjects = SpriteUtility.GetSpriteFromDraggedPathsOrObjects();
						if (animationWindowCurve2.m_ValueType == typeof(Sprite) && spriteFromDraggedPathsOrObjects != null && spriteFromDraggedPathsOrObjects.Length > 0)
						{
							animationWindowCurve = animationWindowCurve2;
							type = typeof(Sprite);
						}
					}
				}
			}
			bool result = true;
			if (animationWindowCurve != null)
			{
				if (perform)
				{
					if (DragAndDrop.objectReferences.Length == 1)
					{
						Analytics.Event("Sprite Drag and Drop", "Drop single sprite into existing dopeline", "null", 1);
					}
					else
					{
						Analytics.Event("Sprite Drag and Drop", "Drop multiple sprites into existing dopeline", "null", 1);
					}
					Rect dragAndDropRect = this.GetDragAndDropRect(dopeLine, Event.current.mousePosition.x);
					float time = Mathf.Max(this.state.PixelToTime(dragAndDropRect.xMin, true), 0f);
					AnimationWindowCurve curveOfType = this.GetCurveOfType(dopeLine, type);
					this.PeformDragAndDrop(curveOfType, time);
				}
			}
			else
			{
				result = false;
			}
			return result;
		}

		private void PeformDragAndDrop(AnimationWindowCurve targetCurve, float time)
		{
			if (DragAndDrop.objectReferences.Length == 0 || targetCurve == null)
			{
				return;
			}
			this.state.ClearSelections();
			UnityEngine.Object[] sortedDragAndDropObjectReferences = this.GetSortedDragAndDropObjectReferences();
			UnityEngine.Object[] array = sortedDragAndDropObjectReferences;
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object @object = array[i];
				UnityEngine.Object object2 = @object;
				if (object2 is Texture2D)
				{
					object2 = SpriteUtility.TextureToSprite(@object as Texture2D);
				}
				this.CreateNewPPtrKeyframe(time, object2, targetCurve);
				time += 1f / this.state.activeAnimationClip.frameRate;
			}
			this.state.SaveCurve(targetCurve);
			DragAndDrop.AcceptDrag();
		}

		private UnityEngine.Object[] GetSortedDragAndDropObjectReferences()
		{
			UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
			Array.Sort<UnityEngine.Object>(objectReferences, (UnityEngine.Object a, UnityEngine.Object b) => EditorUtility.NaturalCompare(a.name, b.name));
			return objectReferences;
		}

		private void CreateNewPPtrKeyframe(float time, UnityEngine.Object value, AnimationWindowCurve targetCurve)
		{
			AnimationWindowKeyframe animationWindowKeyframe = new AnimationWindowKeyframe(targetCurve, new ObjectReferenceKeyframe
			{
				time = time,
				value = value
			});
			AnimationKeyTime keyTime = AnimationKeyTime.Time(animationWindowKeyframe.time, this.state.frameRate);
			targetCurve.AddKeyframe(animationWindowKeyframe, keyTime);
			this.state.SelectKey(animationWindowKeyframe);
		}

		private static bool ValidateDragAndDropObjects()
		{
			if (DragAndDrop.objectReferences.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
			{
				UnityEngine.Object @object = DragAndDrop.objectReferences[i];
				if (@object == null)
				{
					return false;
				}
				if (i < DragAndDrop.objectReferences.Length - 1)
				{
					UnityEngine.Object object2 = DragAndDrop.objectReferences[i + 1];
					bool flag = (@object is Texture2D || @object is Sprite) && (object2 is Texture2D || object2 is Sprite);
					if (@object.GetType() != object2.GetType() && !flag)
					{
						return false;
					}
				}
			}
			return true;
		}

		private AnimationWindowCurve GetCurveOfType(DopeLine dopeLine, Type type)
		{
			AnimationWindowCurve[] curves = dopeLine.m_Curves;
			for (int i = 0; i < curves.Length; i++)
			{
				AnimationWindowCurve animationWindowCurve = curves[i];
				if (animationWindowCurve.m_ValueType == type)
				{
					return animationWindowCurve;
				}
			}
			return null;
		}

		private bool AnyKeyIsSelectedAtTime(DopeLine dopeLine, int keyIndex)
		{
			int timeHash = dopeLine.keys[keyIndex].m_TimeHash;
			int count = dopeLine.keys.Count;
			for (int i = keyIndex; i < count; i++)
			{
				AnimationWindowKeyframe animationWindowKeyframe = dopeLine.keys[i];
				if (animationWindowKeyframe.m_TimeHash != timeHash)
				{
					return false;
				}
				if (this.state.KeyIsSelected(animationWindowKeyframe))
				{
					return true;
				}
			}
			return false;
		}

		private void AddKeyToDopeline(object obj)
		{
			this.AddKeyToDopeline((DopeLine)obj);
		}

		private void AddKeyToDopeline(DopeLine dopeLine)
		{
			this.state.ClearSelections();
			AnimationWindowCurve[] curves = dopeLine.m_Curves;
			for (int i = 0; i < curves.Length; i++)
			{
				AnimationWindowCurve curve = curves[i];
				AnimationWindowKeyframe keyframe = AnimationWindowUtility.AddKeyframeToCurve(this.state, curve, this.state.time);
				this.state.SelectKey(keyframe);
			}
		}

		private void DeleteSelectedKeys()
		{
			this.state.DeleteSelectedKeys();
		}

		public void UpdateCurves(List<int> curveIds, string undoText)
		{
		}

		public void UpdateCurves(List<ChangedCurve> changedCurves, string undoText)
		{
			Undo.RegisterCompleteObjectUndo(this.state.activeAnimationClip, undoText);
			foreach (ChangedCurve current in changedCurves)
			{
				AnimationUtility.SetEditorCurve(this.state.activeAnimationClip, current.binding, current.curve);
			}
		}
	}
}
