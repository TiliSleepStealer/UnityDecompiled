using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
	internal class EditorGUIExt
	{
		private class Styles
		{
			public GUIStyle selectionRect = "SelectionRect";
		}

		private class MinMaxSliderState
		{
			public float dragStartPos;

			public float dragStartValue;

			public float dragStartSize;

			public float dragStartValuesPerPixel;

			public float dragStartLimit;

			public float dragEndLimit;

			public int whereWeDrag = -1;
		}

		private enum DragSelectionState
		{
			None,
			DragSelecting,
			Dragging
		}

		private static EditorGUIExt.Styles ms_Styles = new EditorGUIExt.Styles();

		private static int repeatButtonHash = "repeatButton".GetHashCode();

		private static float nextScrollStepTime = 0f;

		private static int firstScrollWait = 250;

		private static int scrollWait = 30;

		private static int scrollControlID;

		private static EditorGUIExt.MinMaxSliderState s_MinMaxSliderState;

		private static int kFirstScrollWait = 250;

		private static int kScrollWait = 30;

		private static DateTime s_NextScrollStepTime = DateTime.Now;

		private static Vector2 s_MouseDownPos = Vector2.zero;

		private static EditorGUIExt.DragSelectionState s_MultiSelectDragSelection = EditorGUIExt.DragSelectionState.None;

		private static Vector2 s_StartSelectPos = Vector2.zero;

		private static List<bool> s_SelectionBackup = null;

		private static List<bool> s_LastFrameSelections = null;

		internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();

		private static bool adding = false;

		private static bool[] initSelections;

		private static int initIndex = 0;

		private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
		{
			int controlID = GUIUtility.GetControlID(EditorGUIExt.repeatButtonHash, focusType, position);
			EventType typeForControl = Event.current.GetTypeForControl(controlID);
			if (typeForControl == EventType.MouseDown)
			{
				if (position.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = controlID;
					Event.current.Use();
				}
				return false;
			}
			if (typeForControl != EventType.MouseUp)
			{
				if (typeForControl != EventType.Repaint)
				{
					return false;
				}
				style.Draw(position, content, controlID);
				return controlID == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);
			}
			else
			{
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
					Event.current.Use();
					return position.Contains(Event.current.mousePosition);
				}
				return false;
			}
		}

		private static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
		{
			bool result = false;
			if (EditorGUIExt.DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
			{
				bool flag = EditorGUIExt.scrollControlID != scrollerID;
				EditorGUIExt.scrollControlID = scrollerID;
				if (flag)
				{
					result = true;
					EditorGUIExt.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)EditorGUIExt.firstScrollWait;
				}
				else if (Time.realtimeSinceStartup >= EditorGUIExt.nextScrollStepTime)
				{
					result = true;
					EditorGUIExt.nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * (float)EditorGUIExt.scrollWait;
				}
				if (Event.current.type == EventType.Repaint)
				{
					HandleUtility.Repaint();
				}
			}
			return result;
		}

		public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
		{
			float num;
			if (horiz)
			{
				num = size * 10f / position.width;
			}
			else
			{
				num = size * 10f / position.height;
			}
			Rect position2;
			Rect rect;
			Rect rect2;
			if (horiz)
			{
				position2 = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
				rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
				rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
			}
			else
			{
				position2 = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
				rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
				rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
			}
			float num2 = Mathf.Min(visualStart, value);
			float num3 = Mathf.Max(visualEnd, value + size);
			EditorGUIExt.MinMaxSlider(position2, ref value, ref size, num2, num3, num2, num3, slider, thumb, horiz);
			bool flag = false;
			if (Event.current.type == EventType.MouseUp)
			{
				flag = true;
			}
			if (EditorGUIExt.ScrollerRepeatButton(id, rect, leftButton))
			{
				value -= num * ((visualStart >= visualEnd) ? -1f : 1f);
			}
			if (EditorGUIExt.ScrollerRepeatButton(id, rect2, rightButton))
			{
				value += num * ((visualStart >= visualEnd) ? -1f : 1f);
			}
			if (flag && Event.current.type == EventType.Used)
			{
				EditorGUIExt.scrollControlID = 0;
			}
			if (startLimit < endLimit)
			{
				value = Mathf.Clamp(value, startLimit, endLimit - size);
			}
			else
			{
				value = Mathf.Clamp(value, endLimit, startLimit - size);
			}
		}

		public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
		{
			EditorGUIExt.DoMinMaxSlider(position, GUIUtility.GetControlID(EditorGUIExt.s_MinMaxSliderHash, FocusType.Passive), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
		}

		internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
		{
			Event current = Event.current;
			bool flag = size == 0f;
			float num = Mathf.Min(visualStart, visualEnd);
			float num2 = Mathf.Max(visualStart, visualEnd);
			float num3 = Mathf.Min(startLimit, endLimit);
			float num4 = Mathf.Max(startLimit, endLimit);
			EditorGUIExt.MinMaxSliderState minMaxSliderState = EditorGUIExt.s_MinMaxSliderState;
			if (GUIUtility.hotControl == id && minMaxSliderState != null)
			{
				num = minMaxSliderState.dragStartLimit;
				num3 = minMaxSliderState.dragStartLimit;
				num2 = minMaxSliderState.dragEndLimit;
				num4 = minMaxSliderState.dragEndLimit;
			}
			float num5 = 0f;
			float num6 = Mathf.Clamp(value, num, num2);
			float num7 = Mathf.Clamp(value + size, num, num2) - num6;
			float num8 = (float)((visualStart <= visualEnd) ? 1 : -1);
			if (slider == null || thumb == null)
			{
				return;
			}
			float num10;
			Rect position2;
			Rect rect;
			Rect rect2;
			float num11;
			if (horiz)
			{
				float num9 = (thumb.fixedWidth == 0f) ? ((float)thumb.padding.horizontal) : thumb.fixedWidth;
				num10 = (position.width - (float)slider.padding.horizontal - num9) / (num2 - num);
				position2 = new Rect((num6 - num) * num10 + position.x + (float)slider.padding.left, position.y + (float)slider.padding.top, num7 * num10 + num9, position.height - (float)slider.padding.vertical);
				rect = new Rect(position2.x, position2.y, (float)thumb.padding.left, position2.height);
				rect2 = new Rect(position2.xMax - (float)thumb.padding.right, position2.y, (float)thumb.padding.right, position2.height);
				num11 = current.mousePosition.x - position.x;
			}
			else
			{
				float num12 = (thumb.fixedHeight == 0f) ? ((float)thumb.padding.vertical) : thumb.fixedHeight;
				num10 = (position.height - (float)slider.padding.vertical - num12) / (num2 - num);
				position2 = new Rect(position.x + (float)slider.padding.left, (num6 - num) * num10 + position.y + (float)slider.padding.top, position.width - (float)slider.padding.horizontal, num7 * num10 + num12);
				rect = new Rect(position2.x, position2.y, position2.width, (float)thumb.padding.top);
				rect2 = new Rect(position2.x, position2.yMax - (float)thumb.padding.bottom, position2.width, (float)thumb.padding.bottom);
				num11 = current.mousePosition.y - position.y;
			}
			switch (current.GetTypeForControl(id))
			{
			case EventType.MouseDown:
				if (!position.Contains(current.mousePosition) || num - num2 == 0f)
				{
					return;
				}
				if (minMaxSliderState == null)
				{
					minMaxSliderState = (EditorGUIExt.s_MinMaxSliderState = new EditorGUIExt.MinMaxSliderState());
				}
				minMaxSliderState.dragStartLimit = startLimit;
				minMaxSliderState.dragEndLimit = endLimit;
				if (position2.Contains(current.mousePosition))
				{
					minMaxSliderState.dragStartPos = num11;
					minMaxSliderState.dragStartValue = value;
					minMaxSliderState.dragStartSize = size;
					minMaxSliderState.dragStartValuesPerPixel = num10;
					if (rect.Contains(current.mousePosition))
					{
						minMaxSliderState.whereWeDrag = 1;
					}
					else if (rect2.Contains(current.mousePosition))
					{
						minMaxSliderState.whereWeDrag = 2;
					}
					else
					{
						minMaxSliderState.whereWeDrag = 0;
					}
					GUIUtility.hotControl = id;
					current.Use();
					return;
				}
				if (slider == GUIStyle.none)
				{
					return;
				}
				if (size != 0f && flag)
				{
					if (horiz)
					{
						if (num11 > position2.xMax - position.x)
						{
							value += size * num8 * 0.9f;
						}
						else
						{
							value -= size * num8 * 0.9f;
						}
					}
					else if (num11 > position2.yMax - position.y)
					{
						value += size * num8 * 0.9f;
					}
					else
					{
						value -= size * num8 * 0.9f;
					}
					minMaxSliderState.whereWeDrag = 0;
					GUI.changed = true;
					EditorGUIExt.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)EditorGUIExt.kFirstScrollWait);
					float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
					float num14 = (!horiz) ? position2.y : position2.x;
					minMaxSliderState.whereWeDrag = ((num13 <= num14) ? 3 : 4);
				}
				else
				{
					if (horiz)
					{
						value = (num11 - position2.width * 0.5f) / num10 + num - size * 0.5f;
					}
					else
					{
						value = (num11 - position2.height * 0.5f) / num10 + num - size * 0.5f;
					}
					minMaxSliderState.dragStartPos = num11;
					minMaxSliderState.dragStartValue = value;
					minMaxSliderState.dragStartSize = size;
					minMaxSliderState.dragStartValuesPerPixel = num10;
					minMaxSliderState.whereWeDrag = 0;
					GUI.changed = true;
				}
				GUIUtility.hotControl = id;
				value = Mathf.Clamp(value, num3, num4 - size);
				current.Use();
				return;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					current.Use();
					GUIUtility.hotControl = 0;
				}
				break;
			case EventType.MouseDrag:
			{
				if (GUIUtility.hotControl != id)
				{
					return;
				}
				float num15 = (num11 - minMaxSliderState.dragStartPos) / minMaxSliderState.dragStartValuesPerPixel;
				switch (minMaxSliderState.whereWeDrag)
				{
				case 0:
					value = Mathf.Clamp(minMaxSliderState.dragStartValue + num15, num3, num4 - size);
					break;
				case 1:
					value = minMaxSliderState.dragStartValue + num15;
					size = minMaxSliderState.dragStartSize - num15;
					if (value < num3)
					{
						size -= num3 - value;
						value = num3;
					}
					if (size < num5)
					{
						value -= num5 - size;
						size = num5;
					}
					break;
				case 2:
					size = minMaxSliderState.dragStartSize + num15;
					if (value + size > num4)
					{
						size = num4 - value;
					}
					if (size < num5)
					{
						size = num5;
					}
					break;
				}
				GUI.changed = true;
				current.Use();
				break;
			}
			case EventType.Repaint:
			{
				slider.Draw(position, GUIContent.none, id);
				thumb.Draw(position2, GUIContent.none, id);
				if (GUIUtility.hotControl != id || !position.Contains(current.mousePosition) || num - num2 == 0f)
				{
					return;
				}
				if (position2.Contains(current.mousePosition))
				{
					if (minMaxSliderState != null && (minMaxSliderState.whereWeDrag == 3 || minMaxSliderState.whereWeDrag == 4))
					{
						GUIUtility.hotControl = 0;
					}
					return;
				}
				if (DateTime.Now < EditorGUIExt.s_NextScrollStepTime)
				{
					return;
				}
				float num13 = (!horiz) ? current.mousePosition.y : current.mousePosition.x;
				float num14 = (!horiz) ? position2.y : position2.x;
				int num16 = (num13 <= num14) ? 3 : 4;
				if (num16 != minMaxSliderState.whereWeDrag)
				{
					return;
				}
				if (size != 0f && flag)
				{
					if (horiz)
					{
						if (num11 > position2.xMax - position.x)
						{
							value += size * num8 * 0.9f;
						}
						else
						{
							value -= size * num8 * 0.9f;
						}
					}
					else if (num11 > position2.yMax - position.y)
					{
						value += size * num8 * 0.9f;
					}
					else
					{
						value -= size * num8 * 0.9f;
					}
					minMaxSliderState.whereWeDrag = -1;
					GUI.changed = true;
				}
				value = Mathf.Clamp(value, num3, num4 - size);
				EditorGUIExt.s_NextScrollStepTime = DateTime.Now.AddMilliseconds((double)EditorGUIExt.kScrollWait);
				break;
			}
			}
		}

		public static bool DragSelection(Rect[] positions, ref bool[] selections, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(34553287, FocusType.Keyboard);
			Event current = Event.current;
			int num = -1;
			for (int i = positions.Length - 1; i >= 0; i--)
			{
				if (positions[i].Contains(current.mousePosition))
				{
					num = i;
					break;
				}
			}
			switch (current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
				if (current.button == 0 && num >= 0)
				{
					GUIUtility.keyboardControl = 0;
					bool flag = false;
					if (selections[num])
					{
						int num2 = 0;
						bool[] array = selections;
						for (int j = 0; j < array.Length; j++)
						{
							bool flag2 = array[j];
							if (flag2)
							{
								num2++;
								if (num2 > 1)
								{
									break;
								}
							}
						}
						if (num2 == 1)
						{
							flag = true;
						}
					}
					if (!current.shift && !EditorGUI.actionKey)
					{
						for (int k = 0; k < positions.Length; k++)
						{
							selections[k] = false;
						}
					}
					EditorGUIExt.initIndex = num;
					EditorGUIExt.initSelections = (bool[])selections.Clone();
					EditorGUIExt.adding = true;
					if ((current.shift || EditorGUI.actionKey) && selections[num])
					{
						EditorGUIExt.adding = false;
					}
					selections[num] = (!flag && EditorGUIExt.adding);
					GUIUtility.hotControl = controlID;
					current.Use();
					return true;
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID && current.button == 0)
				{
					if (num < 0)
					{
						Rect rect = new Rect(positions[0].x, positions[0].y - 200f, positions[0].width, 200f);
						if (rect.Contains(current.mousePosition))
						{
							num = 0;
						}
						rect.y = positions[positions.Length - 1].yMax;
						if (rect.Contains(current.mousePosition))
						{
							num = selections.Length - 1;
						}
					}
					if (num < 0)
					{
						return false;
					}
					int num3 = Mathf.Min(EditorGUIExt.initIndex, num);
					int num4 = Mathf.Max(EditorGUIExt.initIndex, num);
					for (int l = 0; l < selections.Length; l++)
					{
						if (l >= num3 && l <= num4)
						{
							selections[l] = EditorGUIExt.adding;
						}
						else
						{
							selections[l] = EditorGUIExt.initSelections[l];
						}
					}
					current.Use();
					return true;
				}
				break;
			case EventType.Repaint:
				for (int m = 0; m < positions.Length; m++)
				{
					style.Draw(positions[m], GUIContent.none, controlID, selections[m]);
				}
				break;
			}
			return false;
		}

		private static bool Any(bool[] selections)
		{
			for (int i = 0; i < selections.Length; i++)
			{
				if (selections[i])
				{
					return true;
				}
			}
			return false;
		}

		public static HighLevelEvent MultiSelection(Rect rect, Rect[] positions, GUIContent content, Rect[] hitPositions, ref bool[] selections, bool[] readOnly, out int clickedIndex, out Vector2 offset, out float startSelect, out float endSelect, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(41623453, FocusType.Keyboard);
			Event current = Event.current;
			offset = Vector2.zero;
			clickedIndex = -1;
			startSelect = (endSelect = 0f);
			if (current.type == EventType.Used)
			{
				return HighLevelEvent.None;
			}
			bool flag = false;
			if (Event.current.type != EventType.Layout)
			{
				if (GUIUtility.keyboardControl == controlID)
				{
					flag = true;
				}
				else
				{
					selections = new bool[selections.Length];
				}
			}
			EventType typeForControl = current.GetTypeForControl(controlID);
			EventType eventType = typeForControl;
			switch (eventType)
			{
			case EventType.MouseDown:
			{
				if (current.button != 0)
				{
					return HighLevelEvent.None;
				}
				GUIUtility.hotControl = controlID;
				GUIUtility.keyboardControl = controlID;
				EditorGUIExt.s_StartSelectPos = current.mousePosition;
				int indexUnderMouse = EditorGUIExt.GetIndexUnderMouse(hitPositions, readOnly);
				if (Event.current.clickCount == 2 && indexUnderMouse >= 0)
				{
					for (int i = 0; i < selections.Length; i++)
					{
						selections[i] = false;
					}
					selections[indexUnderMouse] = true;
					current.Use();
					clickedIndex = indexUnderMouse;
					return HighLevelEvent.DoubleClick;
				}
				if (indexUnderMouse >= 0)
				{
					if (!current.shift && !EditorGUI.actionKey && !selections[indexUnderMouse])
					{
						for (int j = 0; j < hitPositions.Length; j++)
						{
							selections[j] = false;
						}
					}
					if (current.shift || EditorGUI.actionKey)
					{
						selections[indexUnderMouse] = !selections[indexUnderMouse];
					}
					else
					{
						selections[indexUnderMouse] = true;
					}
					EditorGUIExt.s_MouseDownPos = current.mousePosition;
					EditorGUIExt.s_MultiSelectDragSelection = EditorGUIExt.DragSelectionState.None;
					current.Use();
					clickedIndex = indexUnderMouse;
					return HighLevelEvent.SelectionChanged;
				}
				bool flag2;
				if (!current.shift && !EditorGUI.actionKey)
				{
					for (int k = 0; k < hitPositions.Length; k++)
					{
						selections[k] = false;
					}
					flag2 = true;
				}
				else
				{
					flag2 = false;
				}
				EditorGUIExt.s_SelectionBackup = new List<bool>(selections);
				EditorGUIExt.s_LastFrameSelections = new List<bool>(selections);
				EditorGUIExt.s_MultiSelectDragSelection = EditorGUIExt.DragSelectionState.DragSelecting;
				current.Use();
				return (!flag2) ? HighLevelEvent.None : HighLevelEvent.SelectionChanged;
			}
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
					if (EditorGUIExt.s_StartSelectPos != current.mousePosition)
					{
						current.Use();
					}
					if (EditorGUIExt.s_MultiSelectDragSelection != EditorGUIExt.DragSelectionState.None)
					{
						EditorGUIExt.s_MultiSelectDragSelection = EditorGUIExt.DragSelectionState.None;
						EditorGUIExt.s_SelectionBackup = null;
						EditorGUIExt.s_LastFrameSelections = null;
						return HighLevelEvent.EndDrag;
					}
					clickedIndex = EditorGUIExt.GetIndexUnderMouse(hitPositions, readOnly);
					if (current.clickCount == 1)
					{
						return HighLevelEvent.Click;
					}
				}
				return HighLevelEvent.None;
			case EventType.MouseMove:
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_A6:
				switch (eventType)
				{
				case EventType.ValidateCommand:
				case EventType.ExecuteCommand:
					if (flag)
					{
						bool flag3 = current.type == EventType.ExecuteCommand;
						string commandName = current.commandName;
						if (commandName != null)
						{
							if (EditorGUIExt.<>f__switch$mapE == null)
							{
								EditorGUIExt.<>f__switch$mapE = new Dictionary<string, int>(1)
								{
									{
										"Delete",
										0
									}
								};
							}
							int num;
							if (EditorGUIExt.<>f__switch$mapE.TryGetValue(commandName, out num))
							{
								if (num == 0)
								{
									current.Use();
									if (flag3)
									{
										return HighLevelEvent.Delete;
									}
								}
							}
						}
					}
					return HighLevelEvent.None;
				case EventType.DragExited:
					return HighLevelEvent.None;
				case EventType.ContextClick:
				{
					int indexUnderMouse = EditorGUIExt.GetIndexUnderMouse(hitPositions, readOnly);
					if (indexUnderMouse >= 0)
					{
						clickedIndex = indexUnderMouse;
						GUIUtility.keyboardControl = controlID;
						current.Use();
						return HighLevelEvent.ContextClick;
					}
					return HighLevelEvent.None;
				}
				default:
					return HighLevelEvent.None;
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl != controlID)
				{
					return HighLevelEvent.None;
				}
				if (EditorGUIExt.s_MultiSelectDragSelection == EditorGUIExt.DragSelectionState.DragSelecting)
				{
					float num2 = Mathf.Min(EditorGUIExt.s_StartSelectPos.x, current.mousePosition.x);
					float num3 = Mathf.Max(EditorGUIExt.s_StartSelectPos.x, current.mousePosition.x);
					EditorGUIExt.s_SelectionBackup.CopyTo(selections);
					for (int l = 0; l < hitPositions.Length; l++)
					{
						if (!selections[l])
						{
							float num4 = hitPositions[l].x + hitPositions[l].width * 0.5f;
							if (num4 >= num2 && num4 <= num3)
							{
								selections[l] = true;
							}
						}
					}
					current.Use();
					startSelect = num2;
					endSelect = num3;
					bool flag4 = false;
					for (int m = 0; m < selections.Length; m++)
					{
						if (selections[m] != EditorGUIExt.s_LastFrameSelections[m])
						{
							flag4 = true;
							EditorGUIExt.s_LastFrameSelections[m] = selections[m];
						}
					}
					return (!flag4) ? HighLevelEvent.None : HighLevelEvent.SelectionChanged;
				}
				offset = current.mousePosition - EditorGUIExt.s_MouseDownPos;
				current.Use();
				if (EditorGUIExt.s_MultiSelectDragSelection == EditorGUIExt.DragSelectionState.None)
				{
					EditorGUIExt.s_MultiSelectDragSelection = EditorGUIExt.DragSelectionState.Dragging;
					return HighLevelEvent.BeginDrag;
				}
				return HighLevelEvent.Drag;
			case EventType.KeyDown:
				if (flag && (current.keyCode == KeyCode.Backspace || current.keyCode == KeyCode.Delete))
				{
					current.Use();
					return HighLevelEvent.Delete;
				}
				return HighLevelEvent.None;
			case EventType.Repaint:
			{
				if (GUIUtility.hotControl == controlID && EditorGUIExt.s_MultiSelectDragSelection == EditorGUIExt.DragSelectionState.DragSelecting)
				{
					float num5 = Mathf.Min(EditorGUIExt.s_StartSelectPos.x, current.mousePosition.x);
					float num6 = Mathf.Max(EditorGUIExt.s_StartSelectPos.x, current.mousePosition.x);
					Rect position = new Rect(0f, 0f, rect.width, rect.height);
					position.x = num5;
					position.width = num6 - num5;
					if (position.width != 0f)
					{
						GUI.Box(position, string.Empty, EditorGUIExt.ms_Styles.selectionRect);
					}
				}
				Color color = GUI.color;
				for (int n = 0; n < positions.Length; n++)
				{
					if (readOnly != null && readOnly[n])
					{
						GUI.color = color * new Color(0.9f, 0.9f, 0.9f, 0.5f);
					}
					else if (selections[n])
					{
						GUI.color = color * new Color(0.3f, 0.55f, 0.95f, 1f);
					}
					else
					{
						GUI.color = color * new Color(0.9f, 0.9f, 0.9f, 1f);
					}
					style.Draw(positions[n], content, controlID, selections[n]);
				}
				GUI.color = color;
				return HighLevelEvent.None;
			}
			}
			goto IL_A6;
		}

		private static int GetIndexUnderMouse(Rect[] hitPositions, bool[] readOnly)
		{
			Vector2 mousePosition = Event.current.mousePosition;
			for (int i = hitPositions.Length - 1; i >= 0; i--)
			{
				if ((readOnly == null || !readOnly[i]) && hitPositions[i].Contains(mousePosition))
				{
					return i;
				}
			}
			return -1;
		}

		internal static Rect FromToRect(Vector2 start, Vector2 end)
		{
			Rect result = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
			if (result.width < 0f)
			{
				result.x += result.width;
				result.width = -result.width;
			}
			if (result.height < 0f)
			{
				result.y += result.height;
				result.height = -result.height;
			}
			return result;
		}
	}
}
