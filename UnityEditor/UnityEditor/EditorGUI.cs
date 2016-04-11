using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor
{
	public sealed class EditorGUI
	{
		public class DisabledGroupScope : GUI.Scope
		{
			public DisabledGroupScope(bool disabled)
			{
				EditorGUI.BeginDisabledGroup(disabled);
			}

			protected override void CloseScope()
			{
				EditorGUI.EndDisabledGroup();
			}
		}

		internal class RecycledTextEditor : TextEditor
		{
			internal static bool s_ActuallyEditing;

			internal static bool s_AllowContextCutOrPaste = true;

			internal bool IsEditingControl(int id)
			{
				return GUIUtility.keyboardControl == id && this.controlID == id && EditorGUI.RecycledTextEditor.s_ActuallyEditing && GUIView.current.hasFocus;
			}

			public virtual void BeginEditing(int id, string newText, Rect position, GUIStyle style, bool multiline, bool passwordField)
			{
				if (this.IsEditingControl(id))
				{
					return;
				}
				if (EditorGUI.activeEditor != null)
				{
					EditorGUI.activeEditor.EndEditing();
				}
				EditorGUI.activeEditor = this;
				this.controlID = id;
				EditorGUI.s_OriginalText = newText;
				base.text = newText;
				this.multiline = multiline;
				this.style = style;
				base.position = position;
				this.isPasswordField = passwordField;
				EditorGUI.RecycledTextEditor.s_ActuallyEditing = true;
				this.scrollOffset = Vector2.zero;
				UnityEditor.Undo.IncrementCurrentGroup();
			}

			public virtual void EndEditing()
			{
				if (EditorGUI.activeEditor == this)
				{
					EditorGUI.activeEditor = null;
				}
				this.controlID = 0;
				EditorGUI.RecycledTextEditor.s_ActuallyEditing = false;
				EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste = true;
				UnityEditor.Undo.IncrementCurrentGroup();
			}
		}

		internal sealed class DelayedTextEditor : EditorGUI.RecycledTextEditor
		{
			private int controlThatHadFocus;

			private int messageControl;

			internal string controlThatHadFocusValue = string.Empty;

			private GUIView viewThatHadFocus;

			private int controlThatLostFocus;

			private bool m_IgnoreBeginGUI;

			public void BeginGUI()
			{
				if (this.m_IgnoreBeginGUI)
				{
					return;
				}
				if (GUIUtility.keyboardControl == this.controlID)
				{
					this.controlThatHadFocus = GUIUtility.keyboardControl;
					this.controlThatHadFocusValue = base.text;
					this.viewThatHadFocus = GUIView.current;
				}
				else
				{
					this.controlThatHadFocus = 0;
				}
			}

			public void EndGUI(EventType type)
			{
				int num = 0;
				if (this.controlThatLostFocus != 0)
				{
					this.messageControl = this.controlThatLostFocus;
					this.controlThatLostFocus = 0;
				}
				if (this.controlThatHadFocus != 0 && this.controlThatHadFocus != GUIUtility.keyboardControl)
				{
					num = this.controlThatHadFocus;
					this.controlThatHadFocus = 0;
				}
				if (num != 0)
				{
					this.messageControl = num;
					this.m_IgnoreBeginGUI = true;
					this.viewThatHadFocus.SendEvent(EditorGUIUtility.CommandEvent("DelayedControlShouldCommit"));
					this.m_IgnoreBeginGUI = false;
					this.messageControl = 0;
				}
			}

			public override void EndEditing()
			{
				base.EndEditing();
				this.messageControl = 0;
			}

			public string OnGUI(int id, string value, out bool changed)
			{
				Event current = Event.current;
				if (current.type == EventType.ExecuteCommand && current.commandName == "DelayedControlShouldCommit" && id == this.messageControl)
				{
					changed = (value != this.controlThatHadFocusValue);
					current.Use();
					return this.controlThatHadFocusValue;
				}
				changed = false;
				return value;
			}
		}

		internal sealed class PopupMenuEvent
		{
			public string commandName;

			public GUIView receiver;

			public PopupMenuEvent(string cmd, GUIView v)
			{
				this.commandName = cmd;
				this.receiver = v;
			}

			public void SendEvent()
			{
				if (this.receiver)
				{
					this.receiver.SendEvent(EditorGUIUtility.CommandEvent(this.commandName));
				}
				else
				{
					Debug.LogError("BUG: We don't have a receiver set up, please report");
				}
			}
		}

		internal sealed class PopupCallbackInfo
		{
			internal const string kPopupMenuChangedMessage = "PopupMenuChanged";

			public static EditorGUI.PopupCallbackInfo instance;

			private int m_ControlID;

			private int m_SelectedIndex;

			private GUIView m_SourceView;

			public PopupCallbackInfo(int controlID)
			{
				this.m_ControlID = controlID;
				this.m_SourceView = GUIView.current;
			}

			public static int GetSelectedValueForControl(int controlID, int selected)
			{
				Event current = Event.current;
				if (current.type == EventType.ExecuteCommand && current.commandName == "PopupMenuChanged")
				{
					if (EditorGUI.PopupCallbackInfo.instance == null)
					{
						Debug.LogError("Popup menu has no instance");
						return selected;
					}
					if (EditorGUI.PopupCallbackInfo.instance.m_ControlID == controlID)
					{
						selected = EditorGUI.PopupCallbackInfo.instance.m_SelectedIndex;
						EditorGUI.PopupCallbackInfo.instance = null;
						GUI.changed = true;
						current.Use();
					}
				}
				return selected;
			}

			internal void SetEnumValueDelegate(object userData, string[] options, int selected)
			{
				this.m_SelectedIndex = selected;
				if (this.m_SourceView)
				{
					this.m_SourceView.SendEvent(EditorGUIUtility.CommandEvent("PopupMenuChanged"));
				}
			}
		}

		public class PropertyScope : GUI.Scope
		{
			public GUIContent content
			{
				get;
				protected set;
			}

			public PropertyScope(Rect totalPosition, GUIContent label, SerializedProperty property)
			{
				this.content = EditorGUI.BeginProperty(totalPosition, label, property);
			}

			protected override void CloseScope()
			{
				EditorGUI.EndProperty();
			}
		}

		private class ColorBrightnessFieldStateObject
		{
			public float m_Hue;

			public float m_Saturation;

			public float m_Brightness;
		}

		internal sealed class GUIContents
		{
			private class IconName : Attribute
			{
				private string m_Name;

				public virtual string name
				{
					get
					{
						return this.m_Name;
					}
				}

				public IconName(string name)
				{
					this.m_Name = name;
				}
			}

			[EditorGUI.GUIContents.IconName("_Popup")]
			internal static GUIContent titleSettingsIcon
			{
				get;
				private set;
			}

			[EditorGUI.GUIContents.IconName("_Help")]
			internal static GUIContent helpIcon
			{
				get;
				private set;
			}

			static GUIContents()
			{
				PropertyInfo[] properties = typeof(EditorGUI.GUIContents).GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
				PropertyInfo[] array = properties;
				for (int i = 0; i < array.Length; i++)
				{
					PropertyInfo propertyInfo = array[i];
					EditorGUI.GUIContents.IconName[] array2 = (EditorGUI.GUIContents.IconName[])propertyInfo.GetCustomAttributes(typeof(EditorGUI.GUIContents.IconName), false);
					if (array2.Length > 0)
					{
						string name = array2[0].name;
						GUIContent value = EditorGUIUtility.IconContent(name);
						propertyInfo.SetValue(null, value, null);
					}
				}
			}
		}

		internal struct KnobContext
		{
			private const int kPixelRange = 250;

			private readonly Rect position;

			private readonly Vector2 knobSize;

			private readonly float currentValue;

			private readonly float start;

			private readonly float end;

			private readonly string unit;

			private readonly Color activeColor;

			private readonly Color backgroundColor;

			private readonly bool showValue;

			private readonly int id;

			private static Material knobMaterial;

			public KnobContext(Rect position, Vector2 knobSize, float currentValue, float start, float end, string unit, Color backgroundColor, Color activeColor, bool showValue, int id)
			{
				this.position = position;
				this.knobSize = knobSize;
				this.currentValue = currentValue;
				this.start = start;
				this.end = end;
				this.unit = unit;
				this.activeColor = activeColor;
				this.backgroundColor = backgroundColor;
				this.showValue = showValue;
				this.id = id;
			}

			public float Handle()
			{
				if (this.KnobState().isEditing && this.CurrentEventType() != EventType.Repaint)
				{
					return this.DoKeyboardInput();
				}
				switch (this.CurrentEventType())
				{
				case EventType.MouseDown:
					return this.OnMouseDown();
				case EventType.MouseUp:
					return this.OnMouseUp();
				case EventType.MouseDrag:
					return this.OnMouseDrag();
				case EventType.Repaint:
					return this.OnRepaint();
				}
				return this.currentValue;
			}

			private EventType CurrentEventType()
			{
				return this.CurrentEvent().type;
			}

			private bool IsEmptyKnob()
			{
				return this.start == this.end;
			}

			private Event CurrentEvent()
			{
				return Event.current;
			}

			private float Clamp(float value)
			{
				return Mathf.Clamp(value, this.MinValue(), this.MaxValue());
			}

			private float ClampedCurrentValue()
			{
				return this.Clamp(this.currentValue);
			}

			private float MaxValue()
			{
				return Mathf.Max(this.start, this.end);
			}

			private float MinValue()
			{
				return Mathf.Min(this.start, this.end);
			}

			private float GetCurrentValuePercent()
			{
				return (this.ClampedCurrentValue() - this.MinValue()) / (this.MaxValue() - this.MinValue());
			}

			private float MousePosition()
			{
				return this.CurrentEvent().mousePosition.y - this.position.y;
			}

			private bool WasDoubleClick()
			{
				return this.CurrentEventType() == EventType.MouseDown && this.CurrentEvent().clickCount == 2;
			}

			private float ValuesPerPixel()
			{
				return 250f / (this.MaxValue() - this.MinValue());
			}

			private KnobState KnobState()
			{
				return (KnobState)GUIUtility.GetStateObject(typeof(KnobState), this.id);
			}

			private void StartDraggingWithValue(float dragStartValue)
			{
				KnobState knobState = this.KnobState();
				knobState.dragStartPos = this.MousePosition();
				knobState.dragStartValue = dragStartValue;
				knobState.isDragging = true;
			}

			private float OnMouseDown()
			{
				if (!this.position.Contains(this.CurrentEvent().mousePosition) || this.KnobState().isEditing || this.IsEmptyKnob())
				{
					return this.currentValue;
				}
				GUIUtility.hotControl = this.id;
				if (this.WasDoubleClick())
				{
					this.KnobState().isEditing = true;
				}
				else
				{
					this.StartDraggingWithValue(this.ClampedCurrentValue());
				}
				this.CurrentEvent().Use();
				return this.currentValue;
			}

			private float OnMouseDrag()
			{
				if (GUIUtility.hotControl != this.id)
				{
					return this.currentValue;
				}
				KnobState knobState = this.KnobState();
				if (!knobState.isDragging)
				{
					return this.currentValue;
				}
				GUI.changed = true;
				this.CurrentEvent().Use();
				float num = knobState.dragStartPos - this.MousePosition();
				float value = knobState.dragStartValue + num / this.ValuesPerPixel();
				return this.Clamp(value);
			}

			private float OnMouseUp()
			{
				if (GUIUtility.hotControl == this.id)
				{
					this.CurrentEvent().Use();
					GUIUtility.hotControl = 0;
					this.KnobState().isDragging = false;
				}
				return this.currentValue;
			}

			private void PrintValue()
			{
				Rect rect = new Rect(this.position.x + this.knobSize.x / 2f - 8f, this.position.y + this.knobSize.y / 2f - 8f, this.position.width, 20f);
				string str = this.currentValue.ToString("0.##");
				GUI.Label(rect, str + " " + this.unit);
			}

			private float DoKeyboardInput()
			{
				GUI.SetNextControlName("KnobInput");
				GUI.FocusControl("KnobInput");
				EditorGUI.BeginChangeCheck();
				Rect rect = new Rect(this.position.x + this.knobSize.x / 2f - 6f, this.position.y + this.knobSize.y / 2f - 7f, this.position.width, 20f);
				GUIStyle none = GUIStyle.none;
				none.normal.textColor = new Color(0.703f, 0.703f, 0.703f, 1f);
				none.fontStyle = FontStyle.Normal;
				string text = EditorGUI.DelayedTextField(rect, this.currentValue.ToString("0.##"), none);
				if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(text))
				{
					this.KnobState().isEditing = false;
					float num;
					if (float.TryParse(text, out num) && num != this.currentValue)
					{
						return this.Clamp(num);
					}
				}
				return this.currentValue;
			}

			private static void CreateKnobMaterial()
			{
				if (!EditorGUI.KnobContext.knobMaterial)
				{
					Shader shader = Resources.GetBuiltinResource(typeof(Shader), "Internal-GUITextureClip.shader") as Shader;
					EditorGUI.KnobContext.knobMaterial = new Material(shader);
					EditorGUI.KnobContext.knobMaterial.hideFlags = HideFlags.HideAndDontSave;
					EditorGUI.KnobContext.knobMaterial.mainTexture = EditorGUIUtility.FindTexture("KnobCShape");
					EditorGUI.KnobContext.knobMaterial.name = "Knob Material";
					if (EditorGUI.KnobContext.knobMaterial.mainTexture == null)
					{
						Debug.Log("Did not find 'KnobCShape'");
					}
				}
			}

			private Vector3 GetUVForPoint(Vector3 point)
			{
				Vector3 result = new Vector3((point.x - this.position.x) / this.knobSize.x, (point.y - this.position.y - this.knobSize.y) / -this.knobSize.y);
				return result;
			}

			private void VertexPointWithUV(Vector3 point)
			{
				GL.TexCoord(this.GetUVForPoint(point));
				GL.Vertex(point);
			}

			private void DrawValueArc(float angle)
			{
				EditorGUI.KnobContext.CreateKnobMaterial();
				Vector3 point = new Vector3(this.position.x + this.knobSize.x / 2f, this.position.y + this.knobSize.y / 2f, 0f);
				Vector3 point2 = new Vector3(this.position.x + this.knobSize.x, this.position.y + this.knobSize.y / 2f, 0f);
				Vector3 vector = new Vector3(this.position.x + this.knobSize.x, this.position.y + this.knobSize.y, 0f);
				Vector3 vector2 = new Vector3(this.position.x, this.position.y + this.knobSize.y, 0f);
				Vector3 vector3 = new Vector3(this.position.x, this.position.y, 0f);
				Vector3 point3 = new Vector3(this.position.x + this.knobSize.x, this.position.y, 0f);
				EditorGUI.KnobContext.knobMaterial.SetPass(0);
				GL.Begin(7);
				GL.Color(this.backgroundColor);
				this.VertexPointWithUV(vector3);
				this.VertexPointWithUV(point3);
				this.VertexPointWithUV(vector);
				this.VertexPointWithUV(vector2);
				GL.End();
				GL.Begin(4);
				GL.Color(this.activeColor * ((!GUI.enabled) ? 0.5f : 1f));
				if (angle > 0f)
				{
					this.VertexPointWithUV(point);
					this.VertexPointWithUV(point2);
					this.VertexPointWithUV(vector);
					Vector3 point4 = vector;
					if (angle > 1.57079637f)
					{
						this.VertexPointWithUV(point);
						this.VertexPointWithUV(vector);
						this.VertexPointWithUV(vector2);
						point4 = vector2;
						if (angle > 3.14159274f)
						{
							this.VertexPointWithUV(point);
							this.VertexPointWithUV(vector2);
							this.VertexPointWithUV(vector3);
							point4 = vector3;
						}
					}
					if (angle == 4.712389f)
					{
						this.VertexPointWithUV(point);
						this.VertexPointWithUV(vector3);
						this.VertexPointWithUV(point3);
						this.VertexPointWithUV(point);
						this.VertexPointWithUV(point3);
						this.VertexPointWithUV(point2);
					}
					else
					{
						float num = angle + 0.7853982f;
						Vector3 point5;
						if (angle < 1.57079637f)
						{
							point5 = vector + new Vector3(this.knobSize.y / 2f * Mathf.Tan(1.57079637f - num) - this.knobSize.x / 2f, 0f, 0f);
						}
						else if (angle < 3.14159274f)
						{
							point5 = vector2 + new Vector3(0f, this.knobSize.x / 2f * Mathf.Tan(3.14159274f - num) - this.knobSize.y / 2f, 0f);
						}
						else
						{
							point5 = vector3 + new Vector3(-(this.knobSize.y / 2f * Mathf.Tan(4.712389f - num)) + this.knobSize.x / 2f, 0f, 0f);
						}
						this.VertexPointWithUV(point);
						this.VertexPointWithUV(point4);
						this.VertexPointWithUV(point5);
					}
				}
				GL.End();
			}

			private float OnRepaint()
			{
				this.DrawValueArc(this.GetCurrentValuePercent() * 3.14159274f * 1.5f);
				if (this.KnobState().isEditing)
				{
					return this.DoKeyboardInput();
				}
				if (this.showValue)
				{
					this.PrintValue();
				}
				return this.currentValue;
			}
		}

		internal enum ObjectFieldVisualType
		{
			IconAndText,
			LargePreview,
			MiniPreivew
		}

		internal class VUMeter
		{
			public struct SmoothingData
			{
				public float lastValue;

				public float peakValue;

				public float peakValueTime;
			}

			private const float VU_SPLIT = 0.9f;

			private static Texture2D s_VerticalVUTexture;

			private static Texture2D s_HorizontalVUTexture;

			public static Texture2D verticalVUTexture
			{
				get
				{
					if (EditorGUI.VUMeter.s_VerticalVUTexture == null)
					{
						EditorGUI.VUMeter.s_VerticalVUTexture = EditorGUIUtility.LoadIcon("VUMeterTextureVertical");
					}
					return EditorGUI.VUMeter.s_VerticalVUTexture;
				}
			}

			public static Texture2D horizontalVUTexture
			{
				get
				{
					if (EditorGUI.VUMeter.s_HorizontalVUTexture == null)
					{
						EditorGUI.VUMeter.s_HorizontalVUTexture = EditorGUIUtility.LoadIcon("VUMeterTextureHorizontal");
					}
					return EditorGUI.VUMeter.s_HorizontalVUTexture;
				}
			}

			public static void HorizontalMeter(Rect position, float value, float peak, Texture2D foregroundTexture, Color peakColor)
			{
				if (Event.current.type != EventType.Repaint)
				{
					return;
				}
				Color color = GUI.color;
				EditorStyles.progressBarBack.Draw(position, false, false, false, false);
				GUI.color = new Color(1f, 1f, 1f, (!GUI.enabled) ? 0.5f : 1f);
				float num = position.width * value - 2f;
				if (num < 2f)
				{
					num = 2f;
				}
				Rect position2 = new Rect(position.x + 1f, position.y + 1f, num, position.height - 2f);
				Rect texCoords = new Rect(0f, 0f, value, 1f);
				GUI.DrawTextureWithTexCoords(position2, foregroundTexture, texCoords);
				GUI.color = peakColor;
				float num2 = position.width * peak - 2f;
				if (num2 < 2f)
				{
					num2 = 2f;
				}
				position2 = new Rect(position.x + num2, position.y + 1f, 1f, position.height - 2f);
				GUI.DrawTexture(position2, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
				GUI.color = color;
			}

			public static void VerticalMeter(Rect position, float value, float peak, Texture2D foregroundTexture, Color peakColor)
			{
				if (Event.current.type != EventType.Repaint)
				{
					return;
				}
				Color color = GUI.color;
				EditorStyles.progressBarBack.Draw(position, false, false, false, false);
				GUI.color = new Color(1f, 1f, 1f, (!GUI.enabled) ? 0.5f : 1f);
				float num = (position.height - 2f) * value;
				if (num < 2f)
				{
					num = 2f;
				}
				Rect position2 = new Rect(position.x + 1f, position.y + position.height - 1f - num, position.width - 2f, num);
				Rect texCoords = new Rect(0f, 0f, 1f, value);
				GUI.DrawTextureWithTexCoords(position2, foregroundTexture, texCoords);
				GUI.color = peakColor;
				float num2 = (position.height - 2f) * peak;
				if (num2 < 2f)
				{
					num2 = 2f;
				}
				position2 = new Rect(position.x + 1f, position.y + position.height - 1f - num2, position.width - 2f, 1f);
				GUI.DrawTexture(position2, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
				GUI.color = color;
			}

			public static void HorizontalMeter(Rect position, float value, ref EditorGUI.VUMeter.SmoothingData data, Texture2D foregroundTexture, Color peakColor)
			{
				if (Event.current.type != EventType.Repaint)
				{
					return;
				}
				float value2;
				float peak;
				EditorGUI.VUMeter.SmoothVUMeterData(ref value, ref data, out value2, out peak);
				EditorGUI.VUMeter.HorizontalMeter(position, value2, peak, foregroundTexture, peakColor);
			}

			public static void VerticalMeter(Rect position, float value, ref EditorGUI.VUMeter.SmoothingData data, Texture2D foregroundTexture, Color peakColor)
			{
				if (Event.current.type != EventType.Repaint)
				{
					return;
				}
				float value2;
				float peak;
				EditorGUI.VUMeter.SmoothVUMeterData(ref value, ref data, out value2, out peak);
				EditorGUI.VUMeter.VerticalMeter(position, value2, peak, foregroundTexture, peakColor);
			}

			private static void SmoothVUMeterData(ref float value, ref EditorGUI.VUMeter.SmoothingData data, out float renderValue, out float renderPeak)
			{
				if (value <= data.lastValue)
				{
					value = Mathf.Lerp(data.lastValue, value, Time.smoothDeltaTime * 7f);
				}
				else
				{
					value = Mathf.Lerp(value, data.lastValue, Time.smoothDeltaTime * 2f);
					data.peakValue = value;
					data.peakValueTime = Time.realtimeSinceStartup;
				}
				if (value > 1.11111116f)
				{
					value = 1.11111116f;
				}
				if (data.peakValue > 1.11111116f)
				{
					data.peakValue = 1.11111116f;
				}
				renderValue = value * 0.9f;
				renderPeak = data.peakValue * 0.9f;
				data.lastValue = value;
			}
		}

		internal delegate UnityEngine.Object ObjectFieldValidator(UnityEngine.Object[] references, Type objType, SerializedProperty property);

		private const double kFoldoutExpandTimeout = 0.7;

		private const float kDragSensitivity = 0.03f;

		internal const float kMiniLabelW = 13f;

		internal const float kLabelW = 80f;

		internal const float kSpacing = 5f;

		internal const float kSpacingSubLabel = 2f;

		internal const float kSliderMinW = 60f;

		internal const float kSliderMaxW = 100f;

		internal const float kSingleLineHeight = 16f;

		internal const float kStructHeaderLineHeight = 16f;

		internal const float kObjectFieldThumbnailHeight = 64f;

		internal const float kObjectFieldMiniThumbnailHeight = 18f;

		internal const float kObjectFieldMiniThumbnailWidth = 32f;

		private const float kIndentPerLevel = 15f;

		internal const int kControlVerticalSpacing = 2;

		internal const int kInspTitlebarIconWidth = 16;

		internal const float kNearFarLabelsWidth = 35f;

		private const int kInspTitlebarToggleWidth = 16;

		private const int kInspTitlebarSpacing = 2;

		private const string kEmptyDropDownElement = "--empty--";

		private static EditorGUI.RecycledTextEditor activeEditor;

		internal static EditorGUI.DelayedTextEditor s_DelayedTextEditor = new EditorGUI.DelayedTextEditor();

		internal static EditorGUI.RecycledTextEditor s_RecycledEditor = new EditorGUI.RecycledTextEditor();

		internal static string s_OriginalText = string.Empty;

		internal static string s_RecycledCurrentEditingString;

		internal static double s_RecycledCurrentEditingFloat;

		internal static long s_RecycledCurrentEditingInt;

		private static bool bKeyEventActive = false;

		internal static bool s_DragToPosition = true;

		internal static bool s_Dragged = false;

		internal static bool s_PostPoneMove = false;

		internal static bool s_SelectAllOnMouseUp = true;

		private static double s_FoldoutDestTime;

		private static int s_DragUpdatedOverID = 0;

		private static bool s_WasBoldDefaultFont;

		private static int s_FoldoutHash = "Foldout".GetHashCode();

		private static int s_TagFieldHash = "s_TagFieldHash".GetHashCode();

		private static int s_PPtrHash = "s_PPtrHash".GetHashCode();

		private static int s_ObjectFieldHash = "s_ObjectFieldHash".GetHashCode();

		private static int s_ToggleHash = "s_ToggleHash".GetHashCode();

		private static int s_ColorHash = "s_ColorHash".GetHashCode();

		private static int s_CurveHash = "s_CurveHash".GetHashCode();

		private static int s_LayerMaskField = "s_LayerMaskField".GetHashCode();

		private static int s_MaskField = "s_MaskField".GetHashCode();

		private static int s_GenericField = "s_GenericField".GetHashCode();

		private static int s_PopupHash = "EditorPopup".GetHashCode();

		private static int s_KeyEventFieldHash = "KeyEventField".GetHashCode();

		private static int s_TextFieldHash = "EditorTextField".GetHashCode();

		private static int s_SearchFieldHash = "EditorSearchField".GetHashCode();

		private static int s_TextAreaHash = "EditorTextField".GetHashCode();

		private static int s_PasswordFieldHash = "PasswordField".GetHashCode();

		private static int s_FloatFieldHash = "EditorTextField".GetHashCode();

		private static int s_DelayedTextFieldHash = "DelayedEditorTextField".GetHashCode();

		private static int s_ArraySizeFieldHash = "ArraySizeField".GetHashCode();

		private static int s_SliderHash = "EditorSlider".GetHashCode();

		private static int s_SliderKnobHash = "EditorSliderKnob".GetHashCode();

		private static int s_MinMaxSliderHash = "EditorMinMaxSlider".GetHashCode();

		private static int s_TitlebarHash = "GenericTitlebar".GetHashCode();

		private static int s_ProgressBarHash = "s_ProgressBarHash".GetHashCode();

		private static int s_SelectableLabelHash = "s_SelectableLabel".GetHashCode();

		private static int s_SortingLayerFieldHash = "s_SortingLayerFieldHash".GetHashCode();

		private static int s_TextFieldDropDownHash = "s_TextFieldDropDown".GetHashCode();

		private static int s_DragCandidateState = 0;

		private static float kDragDeadzone = 16f;

		private static Vector2 s_DragStartPos;

		private static double s_DragStartValue = 0.0;

		private static long s_DragStartIntValue = 0L;

		private static double s_DragSensitivity = 0.0;

		internal static string kFloatFieldFormatString = "g7";

		internal static string kDoubleFieldFormatString = "g15";

		internal static string kIntFieldFormatString = "#######0";

		internal static int ms_IndentLevel = 0;

		internal static string s_UnitString = string.Empty;

		private static float[] s_Vector2Floats = new float[2];

		private static GUIContent[] s_XYLabels = new GUIContent[]
		{
			EditorGUIUtility.TextContent("X"),
			EditorGUIUtility.TextContent("Y")
		};

		private static float[] s_Vector3Floats = new float[3];

		private static GUIContent[] s_XYZLabels = new GUIContent[]
		{
			EditorGUIUtility.TextContent("X"),
			EditorGUIUtility.TextContent("Y"),
			EditorGUIUtility.TextContent("Z")
		};

		private static float[] s_Vector4Floats = new float[4];

		private static GUIContent[] s_XYZWLabels = new GUIContent[]
		{
			EditorGUIUtility.TextContent("X"),
			EditorGUIUtility.TextContent("Y"),
			EditorGUIUtility.TextContent("Z"),
			EditorGUIUtility.TextContent("W")
		};

		private static GUIContent[] s_WHLabels = new GUIContent[]
		{
			EditorGUIUtility.TextContent("W"),
			EditorGUIUtility.TextContent("H")
		};

		internal static readonly GUIContent s_ClipingPlanesLabel = EditorGUIUtility.TextContent("Clipping Planes");

		internal static readonly GUIContent[] s_NearAndFarLabels = new GUIContent[]
		{
			EditorGUIUtility.TextContent("Near"),
			EditorGUIUtility.TextContent("Far")
		};

		private static int s_ColorPickID;

		private static int s_CurveID;

		internal static Color kCurveColor = Color.green;

		internal static Color kCurveBGColor = new Color(0.337f, 0.337f, 0.337f, 1f);

		private static GUIContent s_PropertyFieldTempContent = new GUIContent();

		private static GUIContent s_IconDropDown;

		private static Material s_IconTextureInactive;

		internal static GUIContent s_PrefixLabel = new GUIContent(null);

		internal static Rect s_PrefixTotalRect;

		internal static Rect s_PrefixRect;

		internal static GUIStyle s_PrefixStyle;

		private static bool s_ShowMixedValue;

		private static GUIContent s_MixedValueContent = EditorGUIUtility.TextContent("—|Mixed Values");

		private static Color s_MixedValueContentColor = new Color(1f, 1f, 1f, 0.5f);

		private static Color s_MixedValueContentColorTemp = Color.white;

		private static Stack<PropertyGUIData> s_PropertyStack = new Stack<PropertyGUIData>();

		private static Stack<bool> s_EnabledStack = new Stack<bool>();

		private static Stack<bool> s_ChangedStack = new Stack<bool>();

		internal static readonly string s_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";

		internal static readonly string s_AllowedCharactersForInt = "0123456789-*/+%^()";

		private static SerializedProperty s_PendingPropertyKeyboardHandling = null;

		private static SerializedProperty s_PendingPropertyDelete = null;

		internal static bool s_CollectingToolTips;

		private static readonly int s_GradientHash = "s_GradientHash".GetHashCode();

		private static int s_GradientID;

		private static readonly GUIContent s_HDRWarning = new GUIContent(string.Empty, EditorGUIUtility.warningIcon, LocalizationDatabase.GetLocalizedString("For HDR colors the normalized LDR hex color value is shown"));

		private static int s_ButtonMouseDownHash = "ButtonMouseDown".GetHashCode();

		private static int s_MouseDeltaReaderHash = "MouseDeltaReader".GetHashCode();

		private static Vector2 s_MouseDeltaReaderLastPos;

		public static bool showMixedValue
		{
			get
			{
				return EditorGUI.s_ShowMixedValue;
			}
			set
			{
				EditorGUI.s_ShowMixedValue = value;
			}
		}

		internal static GUIContent mixedValueContent
		{
			get
			{
				return EditorGUI.s_MixedValueContent;
			}
		}

		public static bool actionKey
		{
			get
			{
				if (Event.current == null)
				{
					return false;
				}
				if (Application.platform == RuntimePlatform.OSXEditor)
				{
					return Event.current.command;
				}
				return Event.current.control;
			}
		}

		public static int indentLevel
		{
			get
			{
				return EditorGUI.ms_IndentLevel;
			}
			set
			{
				EditorGUI.ms_IndentLevel = value;
			}
		}

		internal static float indent
		{
			get
			{
				return (float)EditorGUI.indentLevel * 15f;
			}
		}

		internal static Material alphaMaterial
		{
			get
			{
				return EditorGUIUtility.LoadRequired("Previews/PreviewAlphaMaterial.mat") as Material;
			}
		}

		internal static Material transparentMaterial
		{
			get
			{
				return EditorGUIUtility.LoadRequired("Previews/PreviewTransparentMaterial.mat") as Material;
			}
		}

		internal static Texture2D transparentCheckerTexture
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
				{
					return EditorGUIUtility.LoadRequired("Previews/Textures/textureCheckerDark.png") as Texture2D;
				}
				return EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;
			}
		}

		internal static Material lightmapRGBMMaterial
		{
			get
			{
				return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedLightmapRGBMMaterial.mat") as Material;
			}
		}

		internal static Material lightmapDoubleLDRMaterial
		{
			get
			{
				return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedLightmapDoubleLDRMaterial.mat") as Material;
			}
		}

		internal static Material normalmapMaterial
		{
			get
			{
				return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedNormalsMaterial.mat") as Material;
			}
		}

		internal static bool isCollectingTooltips
		{
			get
			{
				return EditorGUI.s_CollectingToolTips;
			}
			set
			{
				EditorGUI.s_CollectingToolTips = value;
			}
		}

		[ExcludeFromDocs]
		public static void LabelField(Rect position, string label)
		{
			GUIStyle label2 = EditorStyles.label;
			EditorGUI.LabelField(position, label, label2);
		}

		public static void LabelField(Rect position, string label, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.LabelField(position, GUIContent.none, EditorGUIUtility.TempContent(label), style);
		}

		[ExcludeFromDocs]
		public static void LabelField(Rect position, GUIContent label)
		{
			GUIStyle label2 = EditorStyles.label;
			EditorGUI.LabelField(position, label, label2);
		}

		public static void LabelField(Rect position, GUIContent label, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.LabelField(position, GUIContent.none, label, style);
		}

		[ExcludeFromDocs]
		public static void LabelField(Rect position, string label, string label2)
		{
			GUIStyle label3 = EditorStyles.label;
			EditorGUI.LabelField(position, label, label2, label3);
		}

		public static void LabelField(Rect position, string label, string label2, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.LabelField(position, new GUIContent(label), EditorGUIUtility.TempContent(label2), style);
		}

		[ExcludeFromDocs]
		public static void LabelField(Rect position, GUIContent label, GUIContent label2)
		{
			GUIStyle label3 = EditorStyles.label;
			EditorGUI.LabelField(position, label, label2, label3);
		}

		public static void LabelField(Rect position, GUIContent label, GUIContent label2, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.LabelFieldInternal(position, label, label2, style);
		}

		[ExcludeFromDocs]
		public static bool ToggleLeft(Rect position, string label, bool value)
		{
			GUIStyle label2 = EditorStyles.label;
			return EditorGUI.ToggleLeft(position, label, value, label2);
		}

		public static bool ToggleLeft(Rect position, string label, bool value, [DefaultValue("EditorStyles.label")] GUIStyle labelStyle)
		{
			return EditorGUI.ToggleLeft(position, EditorGUIUtility.TempContent(label), value, labelStyle);
		}

		[ExcludeFromDocs]
		public static bool ToggleLeft(Rect position, GUIContent label, bool value)
		{
			GUIStyle label2 = EditorStyles.label;
			return EditorGUI.ToggleLeft(position, label, value, label2);
		}

		public static bool ToggleLeft(Rect position, GUIContent label, bool value, [DefaultValue("EditorStyles.label")] GUIStyle labelStyle)
		{
			return EditorGUI.ToggleLeftInternal(position, label, value, labelStyle);
		}

		[ExcludeFromDocs]
		public static string TextField(Rect position, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.TextField(position, text, textField);
		}

		public static string TextField(Rect position, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.TextFieldInternal(position, text, style);
		}

		[ExcludeFromDocs]
		public static string TextField(Rect position, string label, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.TextField(position, label, text, textField);
		}

		public static string TextField(Rect position, string label, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.TextField(position, EditorGUIUtility.TempContent(label), text, style);
		}

		[ExcludeFromDocs]
		public static string TextField(Rect position, GUIContent label, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.TextField(position, label, text, textField);
		}

		public static string TextField(Rect position, GUIContent label, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.TextFieldInternal(position, label, text, style);
		}

		[ExcludeFromDocs]
		public static string DelayedTextField(Rect position, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.DelayedTextField(position, text, textField);
		}

		public static string DelayedTextField(Rect position, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.DelayedTextField(position, GUIContent.none, text, style);
		}

		[ExcludeFromDocs]
		public static string DelayedTextField(Rect position, string label, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.DelayedTextField(position, label, text, textField);
		}

		public static string DelayedTextField(Rect position, string label, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.DelayedTextField(position, EditorGUIUtility.TempContent(label), text, style);
		}

		[ExcludeFromDocs]
		public static string DelayedTextField(Rect position, GUIContent label, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.DelayedTextField(position, label, text, textField);
		}

		public static string DelayedTextField(Rect position, GUIContent label, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DelayedTextFieldInternal(position, controlID, label, text, null, style);
		}

		[ExcludeFromDocs]
		public static void DelayedTextField(Rect position, SerializedProperty property)
		{
			GUIContent label = null;
			EditorGUI.DelayedTextField(position, property, label);
		}

		public static void DelayedTextField(Rect position, SerializedProperty property, [DefaultValue("null")] GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldHash, FocusType.Keyboard, position);
			EditorGUI.DelayedTextFieldInternal(position, controlID, property, null, label);
		}

		[ExcludeFromDocs]
		public static string TextArea(Rect position, string text)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.TextArea(position, text, textField);
		}

		public static string TextArea(Rect position, string text, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.TextAreaInternal(position, text, style);
		}

		[ExcludeFromDocs]
		public static void SelectableLabel(Rect position, string text)
		{
			GUIStyle label = EditorStyles.label;
			EditorGUI.SelectableLabel(position, text, label);
		}

		public static void SelectableLabel(Rect position, string text, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.SelectableLabelInternal(position, text, style);
		}

		[ExcludeFromDocs]
		public static string PasswordField(Rect position, string password)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.PasswordField(position, password, textField);
		}

		public static string PasswordField(Rect position, string password, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.PasswordFieldInternal(position, password, style);
		}

		[ExcludeFromDocs]
		public static string PasswordField(Rect position, string label, string password)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.PasswordField(position, label, password, textField);
		}

		public static string PasswordField(Rect position, string label, string password, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.PasswordField(position, EditorGUIUtility.TempContent(label), password, style);
		}

		[ExcludeFromDocs]
		public static string PasswordField(Rect position, GUIContent label, string password)
		{
			GUIStyle textField = EditorStyles.textField;
			return EditorGUI.PasswordField(position, label, password, textField);
		}

		public static string PasswordField(Rect position, GUIContent label, string password, [DefaultValue("EditorStyles.textField")] GUIStyle style)
		{
			return EditorGUI.PasswordFieldInternal(position, label, password, style);
		}

		[ExcludeFromDocs]
		public static float FloatField(Rect position, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.FloatField(position, value, numberField);
		}

		public static float FloatField(Rect position, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.FloatFieldInternal(position, value, style);
		}

		[ExcludeFromDocs]
		public static float FloatField(Rect position, string label, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.FloatField(position, label, value, numberField);
		}

		public static float FloatField(Rect position, string label, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.FloatField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static float FloatField(Rect position, GUIContent label, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.FloatField(position, label, value, numberField);
		}

		public static float FloatField(Rect position, GUIContent label, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.FloatFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static float DelayedFloatField(Rect position, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedFloatField(position, value, numberField);
		}

		public static float DelayedFloatField(Rect position, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedFloatField(position, GUIContent.none, value, style);
		}

		[ExcludeFromDocs]
		public static float DelayedFloatField(Rect position, string label, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedFloatField(position, label, value, numberField);
		}

		public static float DelayedFloatField(Rect position, string label, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedFloatField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static float DelayedFloatField(Rect position, GUIContent label, float value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedFloatField(position, label, value, numberField);
		}

		public static float DelayedFloatField(Rect position, GUIContent label, float value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedFloatFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static void DelayedFloatField(Rect position, SerializedProperty property)
		{
			GUIContent label = null;
			EditorGUI.DelayedFloatField(position, property, label);
		}

		public static void DelayedFloatField(Rect position, SerializedProperty property, [DefaultValue("null")] GUIContent label)
		{
			EditorGUI.DelayedFloatFieldInternal(position, property, label);
		}

		[ExcludeFromDocs]
		public static double DoubleField(Rect position, double value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DoubleField(position, value, numberField);
		}

		public static double DoubleField(Rect position, double value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DoubleFieldInternal(position, value, style);
		}

		[ExcludeFromDocs]
		public static double DoubleField(Rect position, string label, double value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DoubleField(position, label, value, numberField);
		}

		public static double DoubleField(Rect position, string label, double value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DoubleField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static double DoubleField(Rect position, GUIContent label, double value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DoubleField(position, label, value, numberField);
		}

		public static double DoubleField(Rect position, GUIContent label, double value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DoubleFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static int IntField(Rect position, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.IntField(position, value, numberField);
		}

		public static int IntField(Rect position, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.IntFieldInternal(position, value, style);
		}

		[ExcludeFromDocs]
		public static int IntField(Rect position, string label, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.IntField(position, label, value, numberField);
		}

		public static int IntField(Rect position, string label, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.IntField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static int IntField(Rect position, GUIContent label, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.IntField(position, label, value, numberField);
		}

		public static int IntField(Rect position, GUIContent label, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.IntFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static int DelayedIntField(Rect position, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedIntField(position, value, numberField);
		}

		public static int DelayedIntField(Rect position, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedIntField(position, GUIContent.none, value, style);
		}

		[ExcludeFromDocs]
		public static int DelayedIntField(Rect position, string label, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedIntField(position, label, value, numberField);
		}

		public static int DelayedIntField(Rect position, string label, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedIntField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static int DelayedIntField(Rect position, GUIContent label, int value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.DelayedIntField(position, label, value, numberField);
		}

		public static int DelayedIntField(Rect position, GUIContent label, int value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.DelayedIntFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static void DelayedIntField(Rect position, SerializedProperty property)
		{
			GUIContent label = null;
			EditorGUI.DelayedIntField(position, property, label);
		}

		public static void DelayedIntField(Rect position, SerializedProperty property, [DefaultValue("null")] GUIContent label)
		{
			EditorGUI.DelayedIntFieldInternal(position, property, label);
		}

		[ExcludeFromDocs]
		public static long LongField(Rect position, long value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.LongField(position, value, numberField);
		}

		public static long LongField(Rect position, long value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.LongFieldInternal(position, value, style);
		}

		[ExcludeFromDocs]
		public static long LongField(Rect position, string label, long value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.LongField(position, label, value, numberField);
		}

		public static long LongField(Rect position, string label, long value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.LongField(position, EditorGUIUtility.TempContent(label), value, style);
		}

		[ExcludeFromDocs]
		public static long LongField(Rect position, GUIContent label, long value)
		{
			GUIStyle numberField = EditorStyles.numberField;
			return EditorGUI.LongField(position, label, value, numberField);
		}

		public static long LongField(Rect position, GUIContent label, long value, [DefaultValue("EditorStyles.numberField")] GUIStyle style)
		{
			return EditorGUI.LongFieldInternal(position, label, value, style);
		}

		[ExcludeFromDocs]
		public static int Popup(Rect position, int selectedIndex, string[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.Popup(position, selectedIndex, displayedOptions, popup);
		}

		public static int Popup(Rect position, int selectedIndex, string[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.DoPopup(EditorGUI.IndentedRect(position), GUIUtility.GetControlID(EditorGUI.s_PopupHash, EditorGUIUtility.native, position), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style);
		}

		[ExcludeFromDocs]
		public static int Popup(Rect position, int selectedIndex, GUIContent[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.Popup(position, selectedIndex, displayedOptions, popup);
		}

		public static int Popup(Rect position, int selectedIndex, GUIContent[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.DoPopup(EditorGUI.IndentedRect(position), GUIUtility.GetControlID(EditorGUI.s_PopupHash, EditorGUIUtility.native, position), selectedIndex, displayedOptions, style);
		}

		[ExcludeFromDocs]
		public static int Popup(Rect position, string label, int selectedIndex, string[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.Popup(position, label, selectedIndex, displayedOptions, popup);
		}

		public static int Popup(Rect position, string label, int selectedIndex, string[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.PopupInternal(position, EditorGUIUtility.TempContent(label), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style);
		}

		[ExcludeFromDocs]
		public static int Popup(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.Popup(position, label, selectedIndex, displayedOptions, popup);
		}

		public static int Popup(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.PopupInternal(position, label, selectedIndex, displayedOptions, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumPopup(Rect position, Enum selected)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumPopup(position, selected, popup);
		}

		public static Enum EnumPopup(Rect position, Enum selected, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumPopup(position, GUIContent.none, selected, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumPopup(Rect position, string label, Enum selected)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumPopup(position, label, selected, popup);
		}

		public static Enum EnumPopup(Rect position, string label, Enum selected, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumPopup(position, EditorGUIUtility.TempContent(label), selected, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumPopup(Rect position, GUIContent label, Enum selected)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumPopup(position, label, selected, popup);
		}

		public static Enum EnumPopup(Rect position, GUIContent label, Enum selected, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumPopupInternal(position, label, selected, style);
		}

		[ExcludeFromDocs]
		internal static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected, out int changedFlags, out bool changedToValue)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumMaskPopup(position, label, selected, out changedFlags, out changedToValue, popup);
		}

		internal static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected, out int changedFlags, out bool changedToValue, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumMaskPopupInternal(position, label, selected, out changedFlags, out changedToValue, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumMaskPopup(position, label, selected, popup);
		}

		public static Enum EnumMaskPopup(Rect position, GUIContent label, Enum selected, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			int num;
			bool flag;
			return EditorGUI.EnumMaskPopupInternal(position, label, selected, out num, out flag, style);
		}

		[ExcludeFromDocs]
		public static int IntPopup(Rect position, int selectedValue, string[] displayedOptions, int[] optionValues)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.IntPopup(position, selectedValue, displayedOptions, optionValues, popup);
		}

		public static int IntPopup(Rect position, int selectedValue, string[] displayedOptions, int[] optionValues, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.IntPopup(position, GUIContent.none, selectedValue, EditorGUIUtility.TempContent(displayedOptions), optionValues, style);
		}

		[ExcludeFromDocs]
		public static int IntPopup(Rect position, int selectedValue, GUIContent[] displayedOptions, int[] optionValues)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.IntPopup(position, selectedValue, displayedOptions, optionValues, popup);
		}

		public static int IntPopup(Rect position, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.IntPopup(position, GUIContent.none, selectedValue, displayedOptions, optionValues, style);
		}

		[ExcludeFromDocs]
		public static int IntPopup(Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.IntPopup(position, label, selectedValue, displayedOptions, optionValues, popup);
		}

		public static int IntPopup(Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.IntPopupInternal(position, label, selectedValue, displayedOptions, optionValues, style);
		}

		[ExcludeFromDocs]
		public static void IntPopup(Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues)
		{
			GUIContent label = null;
			EditorGUI.IntPopup(position, property, displayedOptions, optionValues, label);
		}

		public static void IntPopup(Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, [DefaultValue("null")] GUIContent label)
		{
			EditorGUI.IntPopupInternal(position, property, displayedOptions, optionValues, label);
		}

		[ExcludeFromDocs]
		public static int IntPopup(Rect position, string label, int selectedValue, string[] displayedOptions, int[] optionValues)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.IntPopup(position, label, selectedValue, displayedOptions, optionValues, popup);
		}

		public static int IntPopup(Rect position, string label, int selectedValue, string[] displayedOptions, int[] optionValues, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.IntPopupInternal(position, EditorGUIUtility.TempContent(label), selectedValue, EditorGUIUtility.TempContent(displayedOptions), optionValues, style);
		}

		[ExcludeFromDocs]
		public static string TagField(Rect position, string tag)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.TagField(position, tag, popup);
		}

		public static string TagField(Rect position, string tag, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.TagFieldInternal(position, EditorGUIUtility.TempContent(string.Empty), tag, style);
		}

		[ExcludeFromDocs]
		public static string TagField(Rect position, string label, string tag)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.TagField(position, label, tag, popup);
		}

		public static string TagField(Rect position, string label, string tag, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.TagFieldInternal(position, EditorGUIUtility.TempContent(label), tag, style);
		}

		[ExcludeFromDocs]
		public static string TagField(Rect position, GUIContent label, string tag)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.TagField(position, label, tag, popup);
		}

		public static string TagField(Rect position, GUIContent label, string tag, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.TagFieldInternal(position, label, tag, style);
		}

		[ExcludeFromDocs]
		public static int LayerField(Rect position, int layer)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.LayerField(position, layer, popup);
		}

		public static int LayerField(Rect position, int layer, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.LayerFieldInternal(position, GUIContent.none, layer, style);
		}

		[ExcludeFromDocs]
		public static int LayerField(Rect position, string label, int layer)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.LayerField(position, label, layer, popup);
		}

		public static int LayerField(Rect position, string label, int layer, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.LayerFieldInternal(position, EditorGUIUtility.TempContent(label), layer, style);
		}

		[ExcludeFromDocs]
		public static int LayerField(Rect position, GUIContent label, int layer)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.LayerField(position, label, layer, popup);
		}

		public static int LayerField(Rect position, GUIContent label, int layer, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.LayerFieldInternal(position, label, layer, style);
		}

		[ExcludeFromDocs]
		public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.MaskField(position, label, mask, displayedOptions, popup);
		}

		public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.MaskFieldInternal(position, label, mask, displayedOptions, style);
		}

		[ExcludeFromDocs]
		public static int MaskField(Rect position, string label, int mask, string[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.MaskField(position, label, mask, displayedOptions, popup);
		}

		public static int MaskField(Rect position, string label, int mask, string[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.MaskFieldInternal(position, GUIContent.Temp(label), mask, displayedOptions, style);
		}

		[ExcludeFromDocs]
		public static int MaskField(Rect position, int mask, string[] displayedOptions)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.MaskField(position, mask, displayedOptions, popup);
		}

		public static int MaskField(Rect position, int mask, string[] displayedOptions, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.MaskFieldInternal(position, mask, displayedOptions, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumMaskField(Rect position, GUIContent label, Enum enumValue)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumMaskField(position, label, enumValue, popup);
		}

		public static Enum EnumMaskField(Rect position, GUIContent label, Enum enumValue, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumMaskFieldInternal(position, label, enumValue, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumMaskField(Rect position, string label, Enum enumValue)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumMaskField(position, label, enumValue, popup);
		}

		public static Enum EnumMaskField(Rect position, string label, Enum enumValue, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumMaskFieldInternal(position, EditorGUIUtility.TempContent(label), enumValue, style);
		}

		[ExcludeFromDocs]
		public static Enum EnumMaskField(Rect position, Enum enumValue)
		{
			GUIStyle popup = EditorStyles.popup;
			return EditorGUI.EnumMaskField(position, enumValue, popup);
		}

		public static Enum EnumMaskField(Rect position, Enum enumValue, [DefaultValue("EditorStyles.popup")] GUIStyle style)
		{
			return EditorGUI.EnumMaskFieldInternal(position, enumValue, style);
		}

		[ExcludeFromDocs]
		public static bool Foldout(Rect position, bool foldout, string content)
		{
			GUIStyle foldout2 = EditorStyles.foldout;
			return EditorGUI.Foldout(position, foldout, content, foldout2);
		}

		public static bool Foldout(Rect position, bool foldout, string content, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
		{
			return EditorGUI.FoldoutInternal(position, foldout, EditorGUIUtility.TempContent(content), false, style);
		}

		[ExcludeFromDocs]
		public static bool Foldout(Rect position, bool foldout, string content, bool toggleOnLabelClick)
		{
			GUIStyle foldout2 = EditorStyles.foldout;
			return EditorGUI.Foldout(position, foldout, content, toggleOnLabelClick, foldout2);
		}

		public static bool Foldout(Rect position, bool foldout, string content, bool toggleOnLabelClick, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
		{
			return EditorGUI.FoldoutInternal(position, foldout, EditorGUIUtility.TempContent(content), toggleOnLabelClick, style);
		}

		[ExcludeFromDocs]
		public static bool Foldout(Rect position, bool foldout, GUIContent content)
		{
			GUIStyle foldout2 = EditorStyles.foldout;
			return EditorGUI.Foldout(position, foldout, content, foldout2);
		}

		public static bool Foldout(Rect position, bool foldout, GUIContent content, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
		{
			return EditorGUI.FoldoutInternal(position, foldout, content, false, style);
		}

		[ExcludeFromDocs]
		public static bool Foldout(Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick)
		{
			GUIStyle foldout2 = EditorStyles.foldout;
			return EditorGUI.Foldout(position, foldout, content, toggleOnLabelClick, foldout2);
		}

		public static bool Foldout(Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick, [DefaultValue("EditorStyles.foldout")] GUIStyle style)
		{
			return EditorGUI.FoldoutInternal(position, foldout, content, toggleOnLabelClick, style);
		}

		[ExcludeFromDocs]
		public static void HandlePrefixLabel(Rect totalPosition, Rect labelPosition, GUIContent label, int id)
		{
			GUIStyle label2 = EditorStyles.label;
			EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id, label2);
		}

		[ExcludeFromDocs]
		public static void HandlePrefixLabel(Rect totalPosition, Rect labelPosition, GUIContent label)
		{
			GUIStyle label2 = EditorStyles.label;
			int id = 0;
			EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id, label2);
		}

		public static void HandlePrefixLabel(Rect totalPosition, Rect labelPosition, GUIContent label, [DefaultValue("0")] int id, [DefaultValue("EditorStyles.label")] GUIStyle style)
		{
			EditorGUI.HandlePrefixLabelInternal(totalPosition, labelPosition, label, id, style);
		}

		[ExcludeFromDocs]
		public static void DrawTextureAlpha(Rect position, Texture image, ScaleMode scaleMode)
		{
			float imageAspect = 0f;
			EditorGUI.DrawTextureAlpha(position, image, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawTextureAlpha(Rect position, Texture image)
		{
			float imageAspect = 0f;
			ScaleMode scaleMode = ScaleMode.StretchToFill;
			EditorGUI.DrawTextureAlpha(position, image, scaleMode, imageAspect);
		}

		public static void DrawTextureAlpha(Rect position, Texture image, [DefaultValue("ScaleMode.StretchToFill")] ScaleMode scaleMode, [DefaultValue("0")] float imageAspect)
		{
			EditorGUI.DrawTextureAlphaInternal(position, image, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawTextureTransparent(Rect position, Texture image, ScaleMode scaleMode)
		{
			float imageAspect = 0f;
			EditorGUI.DrawTextureTransparent(position, image, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawTextureTransparent(Rect position, Texture image)
		{
			float imageAspect = 0f;
			ScaleMode scaleMode = ScaleMode.StretchToFill;
			EditorGUI.DrawTextureTransparent(position, image, scaleMode, imageAspect);
		}

		public static void DrawTextureTransparent(Rect position, Texture image, [DefaultValue("ScaleMode.StretchToFill")] ScaleMode scaleMode, [DefaultValue("0")] float imageAspect)
		{
			EditorGUI.DrawTextureTransparentInternal(position, image, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawPreviewTexture(Rect position, Texture image, Material mat, ScaleMode scaleMode)
		{
			float imageAspect = 0f;
			EditorGUI.DrawPreviewTexture(position, image, mat, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawPreviewTexture(Rect position, Texture image, Material mat)
		{
			float imageAspect = 0f;
			ScaleMode scaleMode = ScaleMode.StretchToFill;
			EditorGUI.DrawPreviewTexture(position, image, mat, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static void DrawPreviewTexture(Rect position, Texture image)
		{
			float imageAspect = 0f;
			ScaleMode scaleMode = ScaleMode.StretchToFill;
			Material mat = null;
			EditorGUI.DrawPreviewTexture(position, image, mat, scaleMode, imageAspect);
		}

		public static void DrawPreviewTexture(Rect position, Texture image, [DefaultValue("null")] Material mat, [DefaultValue("ScaleMode.StretchToFill")] ScaleMode scaleMode, [DefaultValue("0")] float imageAspect)
		{
			EditorGUI.DrawPreviewTextureInternal(position, image, mat, scaleMode, imageAspect);
		}

		[ExcludeFromDocs]
		public static float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			bool includeChildren = true;
			return EditorGUI.GetPropertyHeight(property, label, includeChildren);
		}

		[ExcludeFromDocs]
		public static float GetPropertyHeight(SerializedProperty property)
		{
			bool includeChildren = true;
			GUIContent label = null;
			return EditorGUI.GetPropertyHeight(property, label, includeChildren);
		}

		public static float GetPropertyHeight(SerializedProperty property, [DefaultValue("null")] GUIContent label, [DefaultValue("true")] bool includeChildren)
		{
			return EditorGUI.GetPropertyHeightInternal(property, label, includeChildren);
		}

		[ExcludeFromDocs]
		public static bool PropertyField(Rect position, SerializedProperty property)
		{
			bool includeChildren = false;
			return EditorGUI.PropertyField(position, property, includeChildren);
		}

		public static bool PropertyField(Rect position, SerializedProperty property, [DefaultValue("false")] bool includeChildren)
		{
			return EditorGUI.PropertyFieldInternal(position, property, null, includeChildren);
		}

		[ExcludeFromDocs]
		public static bool PropertyField(Rect position, SerializedProperty property, GUIContent label)
		{
			bool includeChildren = false;
			return EditorGUI.PropertyField(position, property, label, includeChildren);
		}

		public static bool PropertyField(Rect position, SerializedProperty property, GUIContent label, [DefaultValue("false")] bool includeChildren)
		{
			return EditorGUI.PropertyFieldInternal(position, property, label, includeChildren);
		}

		internal static void BeginHandleMixedValueContentColor()
		{
			EditorGUI.s_MixedValueContentColorTemp = GUI.contentColor;
			GUI.contentColor = ((!EditorGUI.showMixedValue) ? GUI.contentColor : (GUI.contentColor * EditorGUI.s_MixedValueContentColor));
		}

		internal static void EndHandleMixedValueContentColor()
		{
			GUI.contentColor = EditorGUI.s_MixedValueContentColorTemp;
		}

		internal static bool IsEditingTextField()
		{
			return EditorGUI.RecycledTextEditor.s_ActuallyEditing;
		}

		internal static void EndEditingActiveTextField()
		{
			if (EditorGUI.activeEditor != null)
			{
				EditorGUI.activeEditor.EndEditing();
			}
		}

		public static void FocusTextInControl(string name)
		{
			GUI.FocusControl(name);
			EditorGUIUtility.editingTextField = true;
		}

		internal static void ClearStacks()
		{
			EditorGUI.s_EnabledStack.Clear();
			EditorGUI.s_ChangedStack.Clear();
			EditorGUI.s_PropertyStack.Clear();
			ScriptAttributeUtility.s_DrawerStack.Clear();
		}

		public static void BeginDisabledGroup(bool disabled)
		{
			EditorGUI.s_EnabledStack.Push(GUI.enabled);
			GUI.enabled &= !disabled;
		}

		public static void EndDisabledGroup()
		{
			GUI.enabled = EditorGUI.s_EnabledStack.Pop();
		}

		public static void BeginChangeCheck()
		{
			EditorGUI.s_ChangedStack.Push(GUI.changed);
			GUI.changed = false;
		}

		public static bool EndChangeCheck()
		{
			bool changed = GUI.changed;
			GUI.changed |= EditorGUI.s_ChangedStack.Pop();
			return changed;
		}

		private static void ShowTextEditorPopupMenu()
		{
			GenericMenu genericMenu = new GenericMenu();
			if (EditorGUI.s_RecycledEditor.hasSelection && !EditorGUI.s_RecycledEditor.isPasswordField)
			{
				if (EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste)
				{
					genericMenu.AddItem(EditorGUIUtility.TextContent("Cut"), false, new GenericMenu.MenuFunction(new EditorGUI.PopupMenuEvent("Cut", GUIView.current).SendEvent));
				}
				genericMenu.AddItem(EditorGUIUtility.TextContent("Copy"), false, new GenericMenu.MenuFunction(new EditorGUI.PopupMenuEvent("Copy", GUIView.current).SendEvent));
			}
			else
			{
				if (EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste)
				{
					genericMenu.AddDisabledItem(EditorGUIUtility.TextContent("Cut"));
				}
				genericMenu.AddDisabledItem(EditorGUIUtility.TextContent("Copy"));
			}
			if (EditorGUI.s_RecycledEditor.CanPaste() && EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste)
			{
				genericMenu.AddItem(EditorGUIUtility.TextContent("Paste"), false, new GenericMenu.MenuFunction(new EditorGUI.PopupMenuEvent("Paste", GUIView.current).SendEvent));
			}
			genericMenu.ShowAsContext();
		}

		internal static void BeginCollectTooltips()
		{
			EditorGUI.isCollectingTooltips = true;
		}

		internal static void EndCollectTooltips()
		{
			EditorGUI.isCollectingTooltips = false;
		}

		public static void DropShadowLabel(Rect position, string text)
		{
			EditorGUI.DoDropShadowLabel(position, EditorGUIUtility.TempContent(text), "PreOverlayLabel", 0.6f);
		}

		public static void DropShadowLabel(Rect position, GUIContent content)
		{
			EditorGUI.DoDropShadowLabel(position, content, "PreOverlayLabel", 0.6f);
		}

		public static void DropShadowLabel(Rect position, string text, GUIStyle style)
		{
			EditorGUI.DoDropShadowLabel(position, EditorGUIUtility.TempContent(text), style, 0.6f);
		}

		public static void DropShadowLabel(Rect position, GUIContent content, GUIStyle style)
		{
			EditorGUI.DoDropShadowLabel(position, content, style, 0.6f);
		}

		internal static void DoDropShadowLabel(Rect position, GUIContent content, GUIStyle style, float shadowOpa)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			EditorGUI.DrawLabelShadow(position, content, style, shadowOpa);
			style.Draw(position, content, false, false, false, false);
		}

		internal static void DrawLabelShadow(Rect position, GUIContent content, GUIStyle style, float shadowOpa)
		{
			Color color = GUI.color;
			Color contentColor = GUI.contentColor;
			Color backgroundColor = GUI.backgroundColor;
			GUI.contentColor = new Color(0f, 0f, 0f, 0f);
			style.Draw(position, content, false, false, false, false);
			position.y += 1f;
			GUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
			GUI.contentColor = contentColor;
			EditorGUI.Draw4(position, content, 1f, GUI.color.a * shadowOpa, style);
			EditorGUI.Draw4(position, content, 2f, GUI.color.a * shadowOpa * 0.42f, style);
			GUI.color = color;
			GUI.backgroundColor = backgroundColor;
		}

		private static void Draw4(Rect position, GUIContent content, float offset, float alpha, GUIStyle style)
		{
			GUI.color = new Color(0f, 0f, 0f, alpha);
			position.y -= offset;
			style.Draw(position, content, false, false, false, false);
			position.y += offset * 2f;
			style.Draw(position, content, false, false, false, false);
			position.y -= offset;
			position.x -= offset;
			style.Draw(position, content, false, false, false, false);
			position.x += offset * 2f;
			style.Draw(position, content, false, false, false, false);
		}

		internal static string DoTextField(EditorGUI.RecycledTextEditor editor, int id, Rect position, string text, GUIStyle style, string allowedletters, out bool changed, bool reset, bool multiline, bool passwordField)
		{
			Event current = Event.current;
			string result = text;
			if (text == null)
			{
				text = string.Empty;
			}
			if (EditorGUI.showMixedValue)
			{
				text = string.Empty;
			}
			if (EditorGUI.HasKeyboardFocus(id) && Event.current.type != EventType.Layout)
			{
				if (editor.IsEditingControl(id))
				{
					editor.position = position;
					editor.style = style;
					editor.controlID = id;
					editor.multiline = multiline;
					editor.isPasswordField = passwordField;
					editor.DetectFocusChange();
				}
				else if (EditorGUIUtility.editingTextField)
				{
					editor.BeginEditing(id, text, position, style, multiline, passwordField);
					if (GUI.skin.settings.cursorColor.a > 0f)
					{
						editor.SelectAll();
					}
				}
			}
			if (editor.controlID == id && GUIUtility.keyboardControl != id)
			{
				editor.controlID = 0;
			}
			bool flag = false;
			string text2 = editor.text;
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.MouseDown:
				if (position.Contains(current.mousePosition) && current.button == 0)
				{
					if (editor.IsEditingControl(id))
					{
						if (Event.current.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
						{
							editor.MoveCursorToPosition(Event.current.mousePosition);
							editor.SelectCurrentWord();
							editor.MouseDragSelectsWholeWords(true);
							editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
							EditorGUI.s_DragToPosition = false;
						}
						else if (Event.current.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
						{
							editor.MoveCursorToPosition(Event.current.mousePosition);
							editor.SelectCurrentParagraph();
							editor.MouseDragSelectsWholeWords(true);
							editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
							EditorGUI.s_DragToPosition = false;
						}
						else
						{
							editor.MoveCursorToPosition(Event.current.mousePosition);
							EditorGUI.s_SelectAllOnMouseUp = false;
						}
					}
					else
					{
						GUIUtility.keyboardControl = id;
						editor.BeginEditing(id, text, position, style, multiline, passwordField);
						editor.MoveCursorToPosition(Event.current.mousePosition);
						if (GUI.skin.settings.cursorColor.a > 0f)
						{
							EditorGUI.s_SelectAllOnMouseUp = true;
						}
					}
					GUIUtility.hotControl = id;
					current.Use();
				}
				goto IL_9EF;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					if (EditorGUI.s_Dragged && EditorGUI.s_DragToPosition)
					{
						editor.MoveSelectionToAltCursor();
						flag = true;
					}
					else if (EditorGUI.s_PostPoneMove)
					{
						editor.MoveCursorToPosition(Event.current.mousePosition);
					}
					else if (EditorGUI.s_SelectAllOnMouseUp)
					{
						if (GUI.skin.settings.cursorColor.a > 0f)
						{
							editor.SelectAll();
						}
						EditorGUI.s_SelectAllOnMouseUp = false;
					}
					editor.MouseDragSelectsWholeWords(false);
					EditorGUI.s_DragToPosition = true;
					EditorGUI.s_Dragged = false;
					EditorGUI.s_PostPoneMove = false;
					if (current.button == 0)
					{
						GUIUtility.hotControl = 0;
						current.Use();
					}
				}
				goto IL_9EF;
			case EventType.MouseMove:
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_116:
				switch (typeForControl)
				{
				case EventType.ValidateCommand:
					if (GUIUtility.keyboardControl == id)
					{
						string commandName = current.commandName;
						switch (commandName)
						{
						case "Cut":
						case "Copy":
							if (editor.hasSelection)
							{
								current.Use();
							}
							break;
						case "Paste":
							if (editor.CanPaste())
							{
								current.Use();
							}
							break;
						case "SelectAll":
							current.Use();
							break;
						case "UndoRedoPerformed":
							editor.text = text;
							current.Use();
							break;
						case "Delete":
							current.Use();
							break;
						}
					}
					goto IL_9EF;
				case EventType.ExecuteCommand:
					if (GUIUtility.keyboardControl == id)
					{
						string commandName = current.commandName;
						switch (commandName)
						{
						case "OnLostFocus":
							if (EditorGUI.activeEditor != null)
							{
								EditorGUI.activeEditor.EndEditing();
							}
							current.Use();
							break;
						case "Cut":
							editor.BeginEditing(id, text, position, style, multiline, passwordField);
							editor.Cut();
							flag = true;
							break;
						case "Copy":
							editor.Copy();
							current.Use();
							break;
						case "Paste":
							editor.BeginEditing(id, text, position, style, multiline, passwordField);
							editor.Paste();
							flag = true;
							break;
						case "SelectAll":
							editor.SelectAll();
							current.Use();
							break;
						case "Delete":
							editor.BeginEditing(id, text, position, style, multiline, passwordField);
							if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.OSXDashboardPlayer || Application.platform == RuntimePlatform.OSXEditor || (Application.platform == RuntimePlatform.WebGLPlayer && SystemInfo.operatingSystem.StartsWith("Mac")))
							{
								editor.Delete();
							}
							else
							{
								editor.Cut();
							}
							flag = true;
							current.Use();
							break;
						}
					}
					goto IL_9EF;
				case EventType.DragExited:
					goto IL_9EF;
				case EventType.ContextClick:
					if (position.Contains(current.mousePosition))
					{
						if (!editor.IsEditingControl(id))
						{
							GUIUtility.keyboardControl = id;
							editor.BeginEditing(id, text, position, style, multiline, passwordField);
							editor.MoveCursorToPosition(Event.current.mousePosition);
						}
						EditorGUI.ShowTextEditorPopupMenu();
						Event.current.Use();
					}
					goto IL_9EF;
				default:
					goto IL_9EF;
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					if (!current.shift && editor.hasSelection && EditorGUI.s_DragToPosition)
					{
						editor.MoveAltCursorToPosition(Event.current.mousePosition);
					}
					else
					{
						if (current.shift)
						{
							editor.MoveCursorToPosition(Event.current.mousePosition);
						}
						else
						{
							editor.SelectToPosition(Event.current.mousePosition);
						}
						EditorGUI.s_DragToPosition = false;
						EditorGUI.s_SelectAllOnMouseUp = !editor.hasSelection;
					}
					EditorGUI.s_Dragged = true;
					current.Use();
				}
				goto IL_9EF;
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == id)
				{
					char character = current.character;
					if (editor.IsEditingControl(id) && editor.HandleKeyEvent(current))
					{
						current.Use();
						flag = true;
					}
					else if (current.keyCode == KeyCode.Escape)
					{
						if (editor.IsEditingControl(id))
						{
							if (style == EditorStyles.toolbarSearchField || style == EditorStyles.searchField)
							{
								EditorGUI.s_OriginalText = string.Empty;
							}
							editor.text = EditorGUI.s_OriginalText;
							editor.EndEditing();
							flag = true;
						}
					}
					else if (character == '\n' || character == '\u0003')
					{
						if (!editor.IsEditingControl(id))
						{
							editor.BeginEditing(id, text, position, style, multiline, passwordField);
							editor.SelectAll();
						}
						else
						{
							if (multiline && !current.alt && !current.shift && !current.control)
							{
								editor.Insert(character);
								flag = true;
								goto IL_9EF;
							}
							editor.EndEditing();
						}
						current.Use();
					}
					else if (character == '\t' || current.keyCode == KeyCode.Tab)
					{
						if (multiline && editor.IsEditingControl(id))
						{
							bool flag2 = allowedletters == null || allowedletters.IndexOf(character) != -1;
							bool flag3 = !current.alt && !current.shift && !current.control && character == '\t';
							if (flag3 && flag2)
							{
								editor.Insert(character);
								flag = true;
							}
						}
					}
					else if (character != '\u0019' && character != '\u001b')
					{
						if (editor.IsEditingControl(id))
						{
							bool flag4 = (allowedletters == null || allowedletters.IndexOf(character) != -1) && character != '\0';
							if (flag4)
							{
								editor.Insert(character);
								flag = true;
							}
							else
							{
								if (Input.compositionString != string.Empty)
								{
									editor.ReplaceSelection(string.Empty);
									flag = true;
								}
								current.Use();
							}
						}
					}
				}
				goto IL_9EF;
			case EventType.Repaint:
			{
				string text3;
				if (editor.IsEditingControl(id))
				{
					text3 = ((!passwordField) ? editor.text : string.Empty.PadRight(editor.text.Length, '*'));
				}
				else if (EditorGUI.showMixedValue)
				{
					text3 = EditorGUI.s_MixedValueContent.text;
				}
				else
				{
					text3 = ((!passwordField) ? text : string.Empty.PadRight(text.Length, '*'));
				}
				if (!string.IsNullOrEmpty(EditorGUI.s_UnitString) && !passwordField)
				{
					text3 = text3 + " " + EditorGUI.s_UnitString;
				}
				if (GUIUtility.hotControl == 0)
				{
					EditorGUIUtility.AddCursorRect(position, MouseCursor.Text);
				}
				if (!editor.IsEditingControl(id))
				{
					EditorGUI.BeginHandleMixedValueContentColor();
					style.Draw(position, EditorGUIUtility.TempContent(text3), id, false);
					EditorGUI.EndHandleMixedValueContentColor();
				}
				else
				{
					editor.DrawCursor(text3);
				}
				goto IL_9EF;
			}
			}
			goto IL_116;
			IL_9EF:
			if (GUIUtility.keyboardControl == id)
			{
				GUIUtility.textFieldInput = true;
			}
			editor.UpdateScrollOffsetIfNeeded();
			changed = false;
			if (flag)
			{
				changed = (text2 != editor.text);
				current.Use();
			}
			if (changed)
			{
				GUI.changed = true;
				return editor.text;
			}
			EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste = true;
			return result;
		}

		internal static Event KeyEventField(Rect position, Event evt)
		{
			return EditorGUI.DoKeyEventField(position, evt, GUI.skin.textField);
		}

		internal static Event DoKeyEventField(Rect position, Event _event, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_KeyEventFieldHash, FocusType.Native, position);
			Event current = Event.current;
			switch (current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
				if (position.Contains(current.mousePosition))
				{
					GUIUtility.hotControl = controlID;
					current.Use();
					if (EditorGUI.bKeyEventActive)
					{
						EditorGUI.bKeyEventActive = false;
					}
					else
					{
						EditorGUI.bKeyEventActive = true;
					}
				}
				return _event;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = controlID;
					current.Use();
				}
				return _event;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
				{
					current.Use();
				}
				break;
			case EventType.KeyDown:
				if (GUIUtility.hotControl == controlID && EditorGUI.bKeyEventActive)
				{
					if (current.character == '\0' && ((current.alt && (current.keyCode == KeyCode.AltGr || current.keyCode == KeyCode.LeftAlt || current.keyCode == KeyCode.RightAlt)) || (current.control && (current.keyCode == KeyCode.LeftControl || current.keyCode == KeyCode.RightControl)) || (current.command && (current.keyCode == KeyCode.LeftCommand || current.keyCode == KeyCode.RightCommand || current.keyCode == KeyCode.LeftWindows || current.keyCode == KeyCode.RightWindows)) || (current.shift && (current.keyCode == KeyCode.LeftShift || current.keyCode == KeyCode.RightShift || current.keyCode == KeyCode.None))))
					{
						return _event;
					}
					EditorGUI.bKeyEventActive = false;
					GUI.changed = true;
					GUIUtility.hotControl = 0;
					Event result = new Event(current);
					current.Use();
					return result;
				}
				break;
			case EventType.Repaint:
				if (EditorGUI.bKeyEventActive)
				{
					GUIContent content = EditorGUIUtility.TempContent("[Please press a key]");
					style.Draw(position, content, controlID);
				}
				else
				{
					string t = InternalEditorUtility.TextifyEvent(_event);
					style.Draw(position, EditorGUIUtility.TempContent(t), controlID);
				}
				break;
			}
			return _event;
		}

		internal static Rect GetInspectorTitleBarObjectFoldoutRenderRect(Rect rect)
		{
			return new Rect(rect.x + 3f, rect.y + 3f, 16f, 16f);
		}

		internal static bool DoObjectFoldout(bool foldout, Rect interactionRect, Rect renderRect, UnityEngine.Object[] targetObjs, int id)
		{
			bool enabled = GUI.enabled;
			GUI.enabled = true;
			Event current = Event.current;
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.MouseDown:
				if (interactionRect.Contains(current.mousePosition))
				{
					if (current.button == 1 && targetObjs[0] != null)
					{
						EditorUtility.DisplayObjectContextMenu(new Rect(current.mousePosition.x, current.mousePosition.y, 0f, 0f), targetObjs, 0);
						current.Use();
					}
					else if (current.button == 0 && (Application.platform != RuntimePlatform.OSXEditor || !current.control))
					{
						GUIUtility.hotControl = id;
						GUIUtility.keyboardControl = id;
						DragAndDropDelay dragAndDropDelay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
						dragAndDropDelay.mouseDownPosition = current.mousePosition;
						current.Use();
					}
				}
				goto IL_364;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					GUIUtility.hotControl = 0;
					current.Use();
					if (interactionRect.Contains(current.mousePosition))
					{
						GUI.changed = true;
						foldout = !foldout;
					}
				}
				goto IL_364;
			case EventType.MouseMove:
			case EventType.KeyUp:
			case EventType.ScrollWheel:
			case EventType.Layout:
				IL_4F:
				if (typeForControl != EventType.ContextClick)
				{
					goto IL_364;
				}
				if (interactionRect.Contains(current.mousePosition) && targetObjs[0] != null)
				{
					EditorUtility.DisplayObjectContextMenu(new Rect(current.mousePosition.x, current.mousePosition.y, 0f, 0f), targetObjs, 0);
					current.Use();
				}
				goto IL_364;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					DragAndDropDelay dragAndDropDelay2 = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
					if (dragAndDropDelay2.CanStartDrag())
					{
						GUIUtility.hotControl = 0;
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.objectReferences = targetObjs;
						if (targetObjs.Length > 1)
						{
							DragAndDrop.StartDrag("<Multiple>");
						}
						else
						{
							DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(targetObjs[0]));
						}
					}
					current.Use();
				}
				goto IL_364;
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == id)
				{
					if (current.keyCode == KeyCode.LeftArrow)
					{
						foldout = false;
						current.Use();
					}
					if (current.keyCode == KeyCode.RightArrow)
					{
						foldout = true;
						current.Use();
					}
				}
				goto IL_364;
			case EventType.Repaint:
			{
				bool flag = GUIUtility.hotControl == id;
				EditorStyles.foldout.Draw(renderRect, flag, flag, foldout, false);
				goto IL_364;
			}
			case EventType.DragUpdated:
				if (EditorGUI.s_DragUpdatedOverID == id)
				{
					if (interactionRect.Contains(current.mousePosition))
					{
						if ((double)Time.realtimeSinceStartup > EditorGUI.s_FoldoutDestTime)
						{
							foldout = true;
							HandleUtility.Repaint();
						}
					}
					else
					{
						EditorGUI.s_DragUpdatedOverID = 0;
					}
				}
				else if (interactionRect.Contains(current.mousePosition))
				{
					EditorGUI.s_DragUpdatedOverID = id;
					EditorGUI.s_FoldoutDestTime = (double)Time.realtimeSinceStartup + 0.7;
				}
				if (interactionRect.Contains(current.mousePosition))
				{
					DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(targetObjs, false);
					Event.current.Use();
				}
				goto IL_364;
			case EventType.DragPerform:
				if (interactionRect.Contains(current.mousePosition))
				{
					DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(targetObjs, true);
					DragAndDrop.AcceptDrag();
					Event.current.Use();
				}
				goto IL_364;
			}
			goto IL_4F;
			IL_364:
			GUI.enabled = enabled;
			return foldout;
		}

		internal static void LabelFieldInternal(Rect position, GUIContent label, GUIContent label2, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Passive, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			if (Event.current.type == EventType.Repaint)
			{
				style.Draw(position, label2, controlID);
			}
		}

		public static bool Toggle(Rect position, bool value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ToggleHash, EditorGUIUtility.native, position);
			return EditorGUIInternal.DoToggleForward(EditorGUI.IndentedRect(position), controlID, value, GUIContent.none, EditorStyles.toggle);
		}

		public static bool Toggle(Rect position, string label, bool value)
		{
			return EditorGUI.Toggle(position, EditorGUIUtility.TempContent(label), value);
		}

		public static bool Toggle(Rect position, bool value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ToggleHash, EditorGUIUtility.native, position);
			return EditorGUIInternal.DoToggleForward(position, controlID, value, GUIContent.none, style);
		}

		public static bool Toggle(Rect position, string label, bool value, GUIStyle style)
		{
			return EditorGUI.Toggle(position, EditorGUIUtility.TempContent(label), value, style);
		}

		public static bool Toggle(Rect position, GUIContent label, bool value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ToggleHash, EditorGUIUtility.native, position);
			return EditorGUIInternal.DoToggleForward(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, GUIContent.none, EditorStyles.toggle);
		}

		public static bool Toggle(Rect position, GUIContent label, bool value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ToggleHash, EditorGUIUtility.native, position);
			return EditorGUIInternal.DoToggleForward(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, GUIContent.none, style);
		}

		internal static bool ToggleLeftInternal(Rect position, GUIContent label, bool value, GUIStyle labelStyle)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ToggleHash, EditorGUIUtility.native, position);
			Rect position2 = EditorGUI.IndentedRect(position);
			Rect labelPosition = EditorGUI.IndentedRect(position);
			labelPosition.xMin += 13f;
			EditorGUI.HandlePrefixLabel(position, labelPosition, label, controlID, labelStyle);
			return EditorGUIInternal.DoToggleForward(position2, controlID, value, GUIContent.none, EditorStyles.toggle);
		}

		internal static bool DoToggle(Rect position, int id, bool value, GUIContent content, GUIStyle style)
		{
			return EditorGUIInternal.DoToggleForward(position, id, value, content, style);
		}

		internal static string TextFieldInternal(Rect position, string text, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldHash, FocusType.Keyboard, position);
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, EditorGUI.IndentedRect(position), text, style, null, out flag, false, false, false);
			return text;
		}

		internal static string TextFieldInternal(Rect position, GUIContent label, string text, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldHash, FocusType.Keyboard, position);
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, EditorGUI.PrefixLabel(position, controlID, label), text, style, null, out flag, false, false, false);
			return text;
		}

		internal static string ToolbarSearchField(int id, Rect position, string text, bool showWithPopupArrow)
		{
			Rect position2 = position;
			position2.width -= 14f;
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, position2, text, (!showWithPopupArrow) ? EditorStyles.toolbarSearchField : EditorStyles.toolbarSearchFieldPopup, null, out flag, false, false, false);
			Rect position3 = position;
			position3.x += position.width - 14f;
			position3.width = 14f;
			if (GUI.Button(position3, GUIContent.none, (!(text != string.Empty)) ? EditorStyles.toolbarSearchFieldCancelButtonEmpty : EditorStyles.toolbarSearchFieldCancelButton) && text != string.Empty)
			{
				text = (EditorGUI.s_RecycledEditor.text = string.Empty);
				GUIUtility.keyboardControl = 0;
			}
			return text;
		}

		internal static string ToolbarSearchField(Rect position, string[] searchModes, ref int searchMode, string text)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SearchFieldHash, FocusType.Keyboard, position);
			return EditorGUI.ToolbarSearchField(controlID, position, searchModes, ref searchMode, text);
		}

		internal static string ToolbarSearchField(int id, Rect position, string[] searchModes, ref int searchMode, string text)
		{
			bool flag = searchModes != null;
			if (flag)
			{
				searchMode = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(id, searchMode);
				Rect rect = position;
				rect.width = 20f;
				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
				{
					EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(id);
					EditorUtility.DisplayCustomMenu(position, EditorGUIUtility.TempContent(searchModes), searchMode, new EditorUtility.SelectMenuItemFunction(EditorGUI.PopupCallbackInfo.instance.SetEnumValueDelegate), null);
					if (EditorGUI.s_RecycledEditor.IsEditingControl(id))
					{
						Event.current.Use();
					}
				}
			}
			text = EditorGUI.ToolbarSearchField(id, position, text, flag);
			if (flag && text == string.Empty && !EditorGUI.s_RecycledEditor.IsEditingControl(id) && Event.current.type == EventType.Repaint)
			{
				position.width -= 14f;
				EditorGUI.BeginDisabledGroup(true);
				EditorStyles.toolbarSearchFieldPopup.Draw(position, EditorGUIUtility.TempContent(searchModes[searchMode]), id, false);
				EditorGUI.EndDisabledGroup();
			}
			return text;
		}

		internal static string SearchField(Rect position, string text)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SearchFieldHash, FocusType.Keyboard, position);
			Rect position2 = position;
			position2.width -= 15f;
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, position2, text, EditorStyles.searchField, null, out flag, false, false, false);
			Rect position3 = position;
			position3.x += position.width - 15f;
			position3.width = 15f;
			if (GUI.Button(position3, GUIContent.none, (!(text != string.Empty)) ? EditorStyles.searchFieldCancelButtonEmpty : EditorStyles.searchFieldCancelButton) && text != string.Empty)
			{
				text = (EditorGUI.s_RecycledEditor.text = string.Empty);
				GUIUtility.keyboardControl = 0;
			}
			return text;
		}

		internal static string ScrollableTextAreaInternal(Rect position, string text, ref Vector2 scrollPosition, GUIStyle style)
		{
			if (Event.current.type == EventType.Layout)
			{
				return text;
			}
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextAreaHash, FocusType.Keyboard, position);
			float height = style.CalcHeight(GUIContent.Temp(text), position.width);
			Rect rect = new Rect(0f, 0f, position.width, height);
			Vector2 contentOffset = style.contentOffset;
			if (position.height < rect.height)
			{
				Rect position2 = position;
				position2.width = GUI.skin.verticalScrollbar.fixedWidth;
				position2.height -= 2f;
				position2.y += 1f;
				position2.x = position.x + position.width - position2.width;
				position.width -= position2.width;
				height = style.CalcHeight(GUIContent.Temp(text), position.width);
				rect = new Rect(0f, 0f, position.width, height);
				if (position.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
				{
					float value = scrollPosition.y + Event.current.delta.y * 10f;
					scrollPosition.y = Mathf.Clamp(value, 0f, rect.height);
					Event.current.Use();
				}
				scrollPosition.y = GUI.VerticalScrollbar(position2, scrollPosition.y, position.height, 0f, rect.height);
				if (!EditorGUI.s_RecycledEditor.IsEditingControl(controlID))
				{
					style.contentOffset -= scrollPosition;
					style.Internal_clipOffset = scrollPosition;
				}
				else
				{
					EditorGUI.s_RecycledEditor.scrollOffset.y = scrollPosition.y;
				}
			}
			EventType type = Event.current.type;
			bool flag;
			string result = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, EditorGUI.IndentedRect(position), text, style, null, out flag, false, true, false);
			if (type != Event.current.type)
			{
				scrollPosition = EditorGUI.s_RecycledEditor.scrollOffset;
			}
			style.contentOffset = contentOffset;
			style.Internal_clipOffset = Vector2.zero;
			return result;
		}

		internal static string TextAreaInternal(Rect position, string text, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextAreaHash, FocusType.Keyboard, position);
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, EditorGUI.IndentedRect(position), text, style, null, out flag, false, true, false);
			return text;
		}

		internal static void SelectableLabelInternal(Rect position, string text, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SelectableLabelHash, FocusType.Keyboard, position);
			Event current = Event.current;
			if (GUIUtility.keyboardControl == controlID && current.GetTypeForControl(controlID) == EventType.KeyDown)
			{
				KeyCode keyCode = current.keyCode;
				switch (keyCode)
				{
				case KeyCode.UpArrow:
				case KeyCode.DownArrow:
				case KeyCode.RightArrow:
				case KeyCode.LeftArrow:
				case KeyCode.Home:
				case KeyCode.End:
				case KeyCode.PageUp:
				case KeyCode.PageDown:
					goto IL_A0;
				case KeyCode.Insert:
					IL_64:
					if (keyCode != KeyCode.Space)
					{
						if (current.character != '\t')
						{
							current.Use();
						}
						goto IL_A0;
					}
					GUIUtility.hotControl = 0;
					GUIUtility.keyboardControl = 0;
					goto IL_A0;
				}
				goto IL_64;
			}
			IL_A0:
			if (current.type == EventType.ExecuteCommand && (current.commandName == "Paste" || current.commandName == "Cut") && GUIUtility.keyboardControl == controlID)
			{
				current.Use();
			}
			Color cursorColor = GUI.skin.settings.cursorColor;
			GUI.skin.settings.cursorColor = new Color(0f, 0f, 0f, 0f);
			EditorGUI.RecycledTextEditor.s_AllowContextCutOrPaste = false;
			bool flag;
			text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, EditorGUI.IndentedRect(position), text, style, string.Empty, out flag, false, true, false);
			GUI.skin.settings.cursorColor = cursorColor;
		}

		public static string DoPasswordField(int id, Rect position, string password, GUIStyle style)
		{
			bool flag;
			return EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, position, password, style, null, out flag, false, false, true);
		}

		public static string DoPasswordField(int id, Rect position, GUIContent label, string password, GUIStyle style)
		{
			bool flag;
			return EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, EditorGUI.PrefixLabel(position, id, label), password, style, null, out flag, false, false, true);
		}

		internal static string PasswordFieldInternal(Rect position, string password, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_PasswordFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoPasswordField(controlID, EditorGUI.IndentedRect(position), password, style);
		}

		internal static string PasswordFieldInternal(Rect position, GUIContent label, string password, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_PasswordFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoPasswordField(controlID, position, label, password, style);
		}

		internal static float FloatFieldInternal(Rect position, float value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, EditorGUI.IndentedRect(position), new Rect(0f, 0f, 0f, 0f), controlID, value, EditorGUI.kFloatFieldFormatString, style, false);
		}

		internal static float FloatFieldInternal(Rect position, GUIContent label, float value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = position2.x;
			return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, position2, position, controlID, value, EditorGUI.kFloatFieldFormatString, style, true);
		}

		internal static double DoubleFieldInternal(Rect position, double value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoDoubleField(EditorGUI.s_RecycledEditor, EditorGUI.IndentedRect(position), new Rect(0f, 0f, 0f, 0f), controlID, value, EditorGUI.kDoubleFieldFormatString, style, false);
		}

		internal static double DoubleFieldInternal(Rect position, GUIContent label, double value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = position2.x;
			return EditorGUI.DoDoubleField(EditorGUI.s_RecycledEditor, position2, position, controlID, value, EditorGUI.kDoubleFieldFormatString, style, true);
		}

		private static double CalculateFloatDragSensitivity(double value)
		{
			if (double.IsInfinity(value) || double.IsNaN(value))
			{
				return 0.0;
			}
			return Math.Max(1.0, Math.Pow(Math.Abs(value), 0.5)) * 0.029999999329447746;
		}

		private static long CalculateIntDragSensitivity(long value)
		{
			return (long)Math.Max(1.0, Math.Pow(Math.Abs((double)value), 0.5) * 0.029999999329447746);
		}

		private static void DragNumberValue(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, string formatString, GUIStyle style, double dragSensitivity)
		{
			Event current = Event.current;
			switch (current.GetTypeForControl(id))
			{
			case EventType.MouseDown:
				if (dragHotZone.Contains(current.mousePosition) && current.button == 0)
				{
					EditorGUIUtility.editingTextField = false;
					GUIUtility.hotControl = id;
					if (EditorGUI.activeEditor != null)
					{
						EditorGUI.activeEditor.EndEditing();
					}
					current.Use();
					GUIUtility.keyboardControl = id;
					EditorGUI.s_DragCandidateState = 1;
					EditorGUI.s_DragStartValue = doubleVal;
					EditorGUI.s_DragStartIntValue = longVal;
					EditorGUI.s_DragStartPos = current.mousePosition;
					EditorGUI.s_DragSensitivity = dragSensitivity;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id && EditorGUI.s_DragCandidateState != 0)
				{
					GUIUtility.hotControl = 0;
					EditorGUI.s_DragCandidateState = 0;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(0);
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					int num = EditorGUI.s_DragCandidateState;
					if (num != 1)
					{
						if (num == 2)
						{
							if (isDouble)
							{
								doubleVal += (double)HandleUtility.niceMouseDelta * EditorGUI.s_DragSensitivity;
								doubleVal = MathUtils.RoundBasedOnMinimumDifference(doubleVal, EditorGUI.s_DragSensitivity);
							}
							else
							{
								longVal += (long)Math.Round((double)HandleUtility.niceMouseDelta * EditorGUI.s_DragSensitivity);
							}
							GUI.changed = true;
							current.Use();
						}
					}
					else
					{
						if ((Event.current.mousePosition - EditorGUI.s_DragStartPos).sqrMagnitude > EditorGUI.kDragDeadzone)
						{
							EditorGUI.s_DragCandidateState = 2;
							GUIUtility.keyboardControl = id;
						}
						current.Use();
					}
				}
				break;
			case EventType.KeyDown:
				if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape && EditorGUI.s_DragCandidateState != 0)
				{
					doubleVal = EditorGUI.s_DragStartValue;
					longVal = EditorGUI.s_DragStartIntValue;
					GUI.changed = true;
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
			case EventType.Repaint:
				EditorGUIUtility.AddCursorRect(dragHotZone, MouseCursor.SlideArrow);
				break;
			}
		}

		internal static float DoFloatField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable)
		{
			return EditorGUI.DoFloatField(editor, position, dragHotZone, id, value, formatString, style, draggable, (Event.current.GetTypeForControl(id) != EventType.MouseDown) ? 0f : ((float)EditorGUI.CalculateFloatDragSensitivity(EditorGUI.s_DragStartValue)));
		}

		internal static float DoFloatField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
		{
			long num = 0L;
			double value2 = (double)value;
			EditorGUI.DoNumberField(editor, position, dragHotZone, id, true, ref value2, ref num, formatString, style, draggable, (double)dragSensitivity);
			return MathUtils.ClampToFloat(value2);
		}

		internal static int DoIntField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, int value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
		{
			double num = 0.0;
			long value2 = (long)value;
			EditorGUI.DoNumberField(editor, position, dragHotZone, id, false, ref num, ref value2, formatString, style, draggable, (double)dragSensitivity);
			return MathUtils.ClampToInt(value2);
		}

		internal static double DoDoubleField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, double value, string formatString, GUIStyle style, bool draggable)
		{
			return EditorGUI.DoDoubleField(editor, position, dragHotZone, id, value, formatString, style, draggable, (Event.current.GetTypeForControl(id) != EventType.MouseDown) ? 0.0 : EditorGUI.CalculateFloatDragSensitivity(EditorGUI.s_DragStartValue));
		}

		internal static double DoDoubleField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, double value, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
		{
			long num = 0L;
			EditorGUI.DoNumberField(editor, position, dragHotZone, id, true, ref value, ref num, formatString, style, draggable, dragSensitivity);
			return value;
		}

		internal static long DoLongField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, long value, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
		{
			double num = 0.0;
			EditorGUI.DoNumberField(editor, position, dragHotZone, id, false, ref num, ref value, formatString, style, draggable, dragSensitivity);
			return value;
		}

		private static bool HasKeyboardFocus(int controlID)
		{
			return GUIUtility.keyboardControl == controlID && GUIView.current.hasFocus;
		}

		internal static void DoNumberField(EditorGUI.RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
		{
			string allowedletters = (!isDouble) ? EditorGUI.s_AllowedCharactersForInt : EditorGUI.s_AllowedCharactersForFloat;
			if (draggable)
			{
				EditorGUI.DragNumberValue(editor, position, dragHotZone, id, isDouble, ref doubleVal, ref longVal, formatString, style, dragSensitivity);
			}
			Event current = Event.current;
			string text;
			if (EditorGUI.HasKeyboardFocus(id) || (current.type == EventType.MouseDown && current.button == 0 && position.Contains(current.mousePosition)))
			{
				if (!editor.IsEditingControl(id))
				{
					text = (EditorGUI.s_RecycledCurrentEditingString = ((!isDouble) ? longVal.ToString(formatString) : doubleVal.ToString(formatString)));
				}
				else
				{
					text = EditorGUI.s_RecycledCurrentEditingString;
					if (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed")
					{
						text = ((!isDouble) ? longVal.ToString(formatString) : doubleVal.ToString(formatString));
					}
				}
			}
			else
			{
				text = ((!isDouble) ? longVal.ToString(formatString) : doubleVal.ToString(formatString));
			}
			if (GUIUtility.keyboardControl == id)
			{
				bool flag;
				text = EditorGUI.DoTextField(editor, id, position, text, style, allowedletters, out flag, false, false, false);
				if (flag)
				{
					GUI.changed = true;
					EditorGUI.s_RecycledCurrentEditingString = text;
					if (isDouble)
					{
						string a = text.ToLower();
						if (a == "inf" || a == "infinity")
						{
							doubleVal = double.PositiveInfinity;
						}
						else if (a == "-inf" || a == "-infinity")
						{
							doubleVal = double.NegativeInfinity;
						}
						else
						{
							text = text.Replace(',', '.');
							if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out doubleVal))
							{
								doubleVal = (EditorGUI.s_RecycledCurrentEditingFloat = ExpressionEvaluator.Evaluate<double>(text));
								return;
							}
							if (double.IsNaN(doubleVal))
							{
								doubleVal = 0.0;
							}
							EditorGUI.s_RecycledCurrentEditingFloat = doubleVal;
						}
					}
					else
					{
						if (!long.TryParse(text, out longVal))
						{
							longVal = (EditorGUI.s_RecycledCurrentEditingInt = ExpressionEvaluator.Evaluate<long>(text));
							return;
						}
						EditorGUI.s_RecycledCurrentEditingInt = longVal;
					}
				}
			}
			else
			{
				bool flag;
				text = EditorGUI.DoTextField(editor, id, position, text, style, allowedletters, out flag, false, false, false);
			}
		}

		internal static int ArraySizeField(Rect position, GUIContent label, int value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ArraySizeFieldHash, FocusType.Keyboard, position);
			EditorGUI.BeginChangeCheck();
			string s = EditorGUI.DelayedTextFieldInternal(position, controlID, label, value.ToString(EditorGUI.kIntFieldFormatString), "0123456789-", style);
			if (EditorGUI.EndChangeCheck())
			{
				try
				{
					value = int.Parse(s, CultureInfo.InvariantCulture.NumberFormat);
				}
				catch (FormatException)
				{
				}
			}
			return value;
		}

		internal static string DelayedTextFieldInternal(Rect position, string value, string allowedLetters, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_DelayedTextFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DelayedTextFieldInternal(position, controlID, GUIContent.none, value, allowedLetters, style);
		}

		internal static string DelayedTextFieldInternal(Rect position, int id, GUIContent label, string value, string allowedLetters, GUIStyle style)
		{
			string text;
			if (EditorGUI.HasKeyboardFocus(id))
			{
				if (!EditorGUI.s_DelayedTextEditor.IsEditingControl(id))
				{
					text = (EditorGUI.s_RecycledCurrentEditingString = value);
				}
				else
				{
					text = EditorGUI.s_RecycledCurrentEditingString;
				}
				Event current = Event.current;
				if (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed")
				{
					text = value;
				}
			}
			else
			{
				text = value;
			}
			bool changed = GUI.changed;
			bool flag;
			text = EditorGUI.s_DelayedTextEditor.OnGUI(id, text, out flag);
			GUI.changed = false;
			if (!flag)
			{
				text = EditorGUI.DoTextField(EditorGUI.s_DelayedTextEditor, id, EditorGUI.PrefixLabel(position, id, label), text, style, allowedLetters, out flag, false, false, false);
				GUI.changed = false;
				if (GUIUtility.keyboardControl == id)
				{
					if (!EditorGUI.s_DelayedTextEditor.IsEditingControl(id))
					{
						if (value != text)
						{
							GUI.changed = true;
							value = text;
						}
					}
					else
					{
						EditorGUI.s_RecycledCurrentEditingString = text;
					}
				}
			}
			else
			{
				GUI.changed = true;
				value = text;
			}
			GUI.changed |= changed;
			return value;
		}

		internal static void DelayedTextFieldInternal(Rect position, int id, SerializedProperty property, string allowedLetters, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			string stringValue = EditorGUI.DelayedTextFieldInternal(position, id, label, property.stringValue, allowedLetters, EditorStyles.textField);
			if (EditorGUI.EndChangeCheck())
			{
				property.stringValue = stringValue;
			}
			EditorGUI.EndProperty();
		}

		internal static float DelayedFloatFieldInternal(Rect position, GUIContent label, float value, GUIStyle style)
		{
			float num = value;
			float num2 = num;
			EditorGUI.BeginChangeCheck();
			int controlID = GUIUtility.GetControlID(EditorGUI.s_DelayedTextFieldHash, FocusType.Keyboard, position);
			string s = EditorGUI.DelayedTextFieldInternal(position, controlID, label, num.ToString(), EditorGUI.s_AllowedCharactersForFloat, style);
			if (EditorGUI.EndChangeCheck() && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out num2) && num2 != num)
			{
				value = num2;
				GUI.changed = true;
			}
			return num2;
		}

		internal static void DelayedFloatFieldInternal(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			float floatValue = EditorGUI.DelayedFloatFieldInternal(position, label, property.floatValue, EditorStyles.numberField);
			if (EditorGUI.EndChangeCheck())
			{
				property.floatValue = floatValue;
			}
			EditorGUI.EndProperty();
		}

		internal static int DelayedIntFieldInternal(Rect position, GUIContent label, int value, GUIStyle style)
		{
			int num = value;
			int num2 = num;
			EditorGUI.BeginChangeCheck();
			int controlID = GUIUtility.GetControlID(EditorGUI.s_DelayedTextFieldHash, FocusType.Keyboard, position);
			string s = EditorGUI.DelayedTextFieldInternal(position, controlID, label, num.ToString(), EditorGUI.s_AllowedCharactersForInt, style);
			if (EditorGUI.EndChangeCheck() && int.TryParse(s, out num2) && num2 != num)
			{
				value = num2;
				GUI.changed = true;
			}
			return num2;
		}

		internal static void DelayedIntFieldInternal(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			int intValue = EditorGUI.DelayedIntFieldInternal(position, label, property.intValue, EditorStyles.numberField);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
			EditorGUI.EndProperty();
		}

		internal static int IntFieldInternal(Rect position, int value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoIntField(EditorGUI.s_RecycledEditor, EditorGUI.IndentedRect(position), new Rect(0f, 0f, 0f, 0f), controlID, value, EditorGUI.kIntFieldFormatString, style, false, (float)EditorGUI.CalculateIntDragSensitivity((long)value));
		}

		internal static int IntFieldInternal(Rect position, GUIContent label, int value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = position2.x;
			return EditorGUI.DoIntField(EditorGUI.s_RecycledEditor, position2, position, controlID, value, EditorGUI.kIntFieldFormatString, style, true, (float)EditorGUI.CalculateIntDragSensitivity((long)value));
		}

		internal static long LongFieldInternal(Rect position, long value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			return EditorGUI.DoLongField(EditorGUI.s_RecycledEditor, EditorGUI.IndentedRect(position), new Rect(0f, 0f, 0f, 0f), controlID, value, EditorGUI.kIntFieldFormatString, style, false, (double)EditorGUI.CalculateIntDragSensitivity(value));
		}

		internal static long LongFieldInternal(Rect position, GUIContent label, long value, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = position2.x;
			return EditorGUI.DoLongField(EditorGUI.s_RecycledEditor, position2, position, controlID, value, EditorGUI.kIntFieldFormatString, style, true, (double)EditorGUI.CalculateIntDragSensitivity(value));
		}

		public static float Slider(Rect position, float value, float leftValue, float rightValue)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SliderHash, EditorGUIUtility.native, position);
			return EditorGUI.DoSlider(EditorGUI.IndentedRect(position), EditorGUIUtility.DragZoneRect(position), controlID, value, leftValue, rightValue, EditorGUI.kFloatFieldFormatString);
		}

		public static float Slider(Rect position, string label, float value, float leftValue, float rightValue)
		{
			return EditorGUI.Slider(position, EditorGUIUtility.TempContent(label), value, leftValue, rightValue);
		}

		public static float Slider(Rect position, GUIContent label, float value, float leftValue, float rightValue)
		{
			return EditorGUI.PowerSlider(position, label, value, leftValue, rightValue, 1f);
		}

		internal static float PowerSlider(Rect position, string label, float sliderValue, float leftValue, float rightValue, float power)
		{
			return EditorGUI.PowerSlider(position, EditorGUIUtility.TempContent(label), sliderValue, leftValue, rightValue, power);
		}

		internal static float PowerSlider(Rect position, GUIContent label, float sliderValue, float leftValue, float rightValue, float power)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SliderHash, EditorGUIUtility.native, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			Rect dragZonePosition = (!EditorGUI.LabelHasContent(label)) ? default(Rect) : EditorGUIUtility.DragZoneRect(position);
			return EditorGUI.DoSlider(position2, dragZonePosition, controlID, sliderValue, leftValue, rightValue, EditorGUI.kFloatFieldFormatString, power);
		}

		private static float PowPreserveSign(float f, float p)
		{
			float num = Mathf.Pow(Mathf.Abs(f), p);
			return (f >= 0f) ? num : (-num);
		}

		private static void DoPropertyContextMenu(SerializedProperty property)
		{
			GenericMenu genericMenu = new GenericMenu();
			SerializedProperty serializedProperty = property.serializedObject.FindProperty(property.propertyPath);
			ScriptAttributeUtility.GetHandler(property).AddMenuItems(property, genericMenu);
			if (property.hasMultipleDifferentValues && !property.hasVisibleChildren)
			{
				TargetChoiceHandler.AddSetToValueOfTargetMenuItems(genericMenu, serializedProperty, new TargetChoiceHandler.TargetChoiceMenuFunction(TargetChoiceHandler.SetToValueOfTarget));
			}
			if (property.serializedObject.targetObjects.Length == 1 && property.isInstantiatedPrefab)
			{
				genericMenu.AddItem(EditorGUIUtility.TextContent("Revert Value to Prefab"), false, new GenericMenu.MenuFunction2(TargetChoiceHandler.SetPrefabOverride), serializedProperty);
			}
			if (property.propertyPath.LastIndexOf(']') == property.propertyPath.Length - 1)
			{
				if (genericMenu.GetItemCount() > 0)
				{
					genericMenu.AddSeparator(string.Empty);
				}
				genericMenu.AddItem(EditorGUIUtility.TextContent("Duplicate Array Element"), false, new GenericMenu.MenuFunction2(TargetChoiceHandler.DuplicateArrayElement), serializedProperty);
				genericMenu.AddItem(EditorGUIUtility.TextContent("Delete Array Element"), false, new GenericMenu.MenuFunction2(TargetChoiceHandler.DeleteArrayElement), serializedProperty);
			}
			if (Event.current.shift)
			{
				if (genericMenu.GetItemCount() > 0)
				{
					genericMenu.AddSeparator(string.Empty);
				}
				genericMenu.AddItem(EditorGUIUtility.TextContent("Print Property Path"), false, delegate(object e)
				{
					Debug.Log(((SerializedProperty)e).propertyPath);
				}, serializedProperty);
			}
			Event.current.Use();
			if (genericMenu.GetItemCount() == 0)
			{
				return;
			}
			genericMenu.ShowAsContext();
		}

		public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue)
		{
			EditorGUI.Slider(position, property, leftValue, rightValue, property.displayName);
		}

		public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue, string label)
		{
			EditorGUI.Slider(position, property, leftValue, rightValue, EditorGUIUtility.TempContent(label));
		}

		public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			float floatValue = EditorGUI.Slider(position, label, property.floatValue, leftValue, rightValue);
			if (EditorGUI.EndChangeCheck())
			{
				property.floatValue = floatValue;
			}
			EditorGUI.EndProperty();
		}

		public static int IntSlider(Rect position, int value, int leftValue, int rightValue)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SliderHash, EditorGUIUtility.native, position);
			return Mathf.RoundToInt(EditorGUI.DoSlider(EditorGUI.IndentedRect(position), EditorGUIUtility.DragZoneRect(position), controlID, (float)value, (float)leftValue, (float)rightValue, EditorGUI.kIntFieldFormatString));
		}

		public static int IntSlider(Rect position, string label, int value, int leftValue, int rightValue)
		{
			return EditorGUI.IntSlider(position, EditorGUIUtility.TempContent(label), value, leftValue, rightValue);
		}

		public static int IntSlider(Rect position, GUIContent label, int value, int leftValue, int rightValue)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SliderHash, EditorGUIUtility.native, position);
			return Mathf.RoundToInt(EditorGUI.DoSlider(EditorGUI.PrefixLabel(position, controlID, label), EditorGUIUtility.DragZoneRect(position), controlID, (float)value, (float)leftValue, (float)rightValue, EditorGUI.kIntFieldFormatString));
		}

		public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue)
		{
			EditorGUI.IntSlider(position, property, leftValue, rightValue, property.displayName);
		}

		public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue, string label)
		{
			EditorGUI.IntSlider(position, property, leftValue, rightValue, EditorGUIUtility.TempContent(label));
		}

		public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			int intValue = EditorGUI.IntSlider(position, label, property.intValue, leftValue, rightValue);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
			EditorGUI.EndProperty();
		}

		internal static void DoTwoLabels(Rect rect, GUIContent leftLabel, GUIContent rightLabel, GUIStyle labelStyle)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			TextAnchor alignment = labelStyle.alignment;
			labelStyle.alignment = TextAnchor.UpperLeft;
			GUI.Label(rect, leftLabel, labelStyle);
			labelStyle.alignment = TextAnchor.UpperRight;
			GUI.Label(rect, rightLabel, labelStyle);
			labelStyle.alignment = alignment;
		}

		private static float DoSlider(Rect position, Rect dragZonePosition, int id, float value, float left, float right, string formatString)
		{
			return EditorGUI.DoSlider(position, dragZonePosition, id, value, left, right, formatString, 1f);
		}

		private static float DoSlider(Rect position, Rect dragZonePosition, int id, float value, float left, float right, string formatString, float power)
		{
			float num = position.width;
			if (num >= 65f + EditorGUIUtility.fieldWidth)
			{
				float num2 = num - 5f - EditorGUIUtility.fieldWidth;
				EditorGUI.BeginChangeCheck();
				int controlID = GUIUtility.GetControlID(EditorGUI.s_SliderKnobHash, FocusType.Native, position);
				if (GUIUtility.keyboardControl == id && !EditorGUI.s_RecycledEditor.IsEditingControl(id))
				{
					GUIUtility.keyboardControl = controlID;
				}
				float start = left;
				float end = right;
				if (power != 1f)
				{
					start = EditorGUI.PowPreserveSign(left, 1f / power);
					end = EditorGUI.PowPreserveSign(right, 1f / power);
					value = EditorGUI.PowPreserveSign(value, 1f / power);
				}
				Rect position2 = new Rect(position.x, position.y, num2, position.height);
				value = GUI.Slider(position2, value, 0f, start, end, GUI.skin.horizontalSlider, (!EditorGUI.showMixedValue) ? GUI.skin.horizontalSliderThumb : "SliderMixed", true, controlID);
				if (power != 1f)
				{
					value = EditorGUI.PowPreserveSign(value, power);
				}
				if (EditorGUIUtility.sliderLabels.HasLabels())
				{
					Color color = GUI.color;
					GUI.color *= new Color(1f, 1f, 1f, 0.5f);
					Rect rect = new Rect(position2.x, position2.y + 10f, position2.width, position2.height);
					EditorGUI.DoTwoLabels(rect, EditorGUIUtility.sliderLabels.leftLabel, EditorGUIUtility.sliderLabels.rightLabel, EditorStyles.miniLabel);
					GUI.color = color;
					EditorGUIUtility.sliderLabels.SetLabels(null, null);
				}
				if (GUIUtility.keyboardControl == controlID || GUIUtility.hotControl == controlID)
				{
					GUIUtility.keyboardControl = id;
				}
				if (GUIUtility.keyboardControl == id && Event.current.type == EventType.KeyDown && !EditorGUI.s_RecycledEditor.IsEditingControl(id) && (Event.current.keyCode == KeyCode.LeftArrow || Event.current.keyCode == KeyCode.RightArrow))
				{
					float num3 = MathUtils.GetClosestPowerOfTen(Mathf.Abs((right - left) * 0.01f));
					if (formatString == EditorGUI.kIntFieldFormatString && num3 < 1f)
					{
						num3 = 1f;
					}
					if (Event.current.shift)
					{
						num3 *= 10f;
					}
					if (Event.current.keyCode == KeyCode.LeftArrow)
					{
						value -= num3 * 0.5001f;
					}
					else
					{
						value += num3 * 0.5001f;
					}
					value = MathUtils.RoundToMultipleOf(value, num3);
					GUI.changed = true;
					Event.current.Use();
				}
				if (EditorGUI.EndChangeCheck())
				{
					float f = (right - left) / (num2 - (float)GUI.skin.horizontalSlider.padding.horizontal - GUI.skin.horizontalSliderThumb.fixedWidth);
					value = MathUtils.RoundBasedOnMinimumDifference(value, Mathf.Abs(f));
					if (EditorGUI.s_RecycledEditor.IsEditingControl(id))
					{
						EditorGUI.s_RecycledEditor.EndEditing();
					}
				}
				value = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, new Rect(position.x + num2 + 5f, position.y, EditorGUIUtility.fieldWidth, position.height), dragZonePosition, id, value, formatString, EditorStyles.numberField, true);
			}
			else
			{
				num = Mathf.Min(EditorGUIUtility.fieldWidth, num);
				position.x = position.xMax - num;
				position.width = num;
				value = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, position, dragZonePosition, id, value, formatString, EditorStyles.numberField, true);
			}
			value = Mathf.Clamp(value, Mathf.Min(left, right), Mathf.Max(left, right));
			return value;
		}

		public static void MinMaxSlider(GUIContent label, Rect position, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MinMaxSliderHash, FocusType.Native);
			EditorGUI.DoMinMaxSlider(EditorGUI.PrefixLabel(position, controlID, label), controlID, ref minValue, ref maxValue, minLimit, maxLimit);
		}

		public static void MinMaxSlider(Rect position, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
		{
			EditorGUI.DoMinMaxSlider(EditorGUI.IndentedRect(position), GUIUtility.GetControlID(EditorGUI.s_MinMaxSliderHash, FocusType.Native), ref minValue, ref maxValue, minLimit, maxLimit);
		}

		private static void DoMinMaxSlider(Rect position, int id, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
		{
			float num = maxValue - minValue;
			EditorGUI.BeginChangeCheck();
			EditorGUIExt.DoMinMaxSlider(position, id, ref minValue, ref num, minLimit, maxLimit, minLimit, maxLimit, GUI.skin.horizontalSlider, EditorStyles.minMaxHorizontalSliderThumb, true);
			if (EditorGUI.EndChangeCheck())
			{
				maxValue = minValue + num;
			}
		}

		private static int PopupInternal(Rect position, string label, int selectedIndex, string[] displayedOptions, GUIStyle style)
		{
			return EditorGUI.PopupInternal(position, EditorGUIUtility.TempContent(label), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style);
		}

		private static int PopupInternal(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_PopupHash, EditorGUIUtility.native, position);
			return EditorGUI.DoPopup(EditorGUI.PrefixLabel(position, controlID, label), controlID, selectedIndex, displayedOptions, style);
		}

		private static void Popup(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();
			int enumValueIndex = EditorGUI.Popup(position, label, (!property.hasMultipleDifferentValues) ? property.enumValueIndex : -1, EditorGUIUtility.TempContent(property.enumDisplayNames));
			if (EditorGUI.EndChangeCheck())
			{
				property.enumValueIndex = enumValueIndex;
			}
		}

		internal static void Popup(Rect position, SerializedProperty property, GUIContent[] displayedOptions, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			int intValue = EditorGUI.Popup(position, label, (!property.hasMultipleDifferentValues) ? property.intValue : -1, displayedOptions);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
			EditorGUI.EndProperty();
		}

		private static Enum EnumPopupInternal(Rect position, GUIContent label, Enum selected, GUIStyle style)
		{
			Type type = selected.GetType();
			if (!type.IsEnum)
			{
				throw new Exception("parameter _enum must be of type System.Enum");
			}
			Enum[] array = Enum.GetValues(type).Cast<Enum>().ToArray<Enum>();
			string[] names = Enum.GetNames(type);
			int num = Array.IndexOf<Enum>(array, selected);
			num = EditorGUI.Popup(position, label, num, EditorGUIUtility.TempContent((from x in names
			select ObjectNames.NicifyVariableName(x)).ToArray<string>()), style);
			if (num < 0 || num >= names.Length)
			{
				return selected;
			}
			return array[num];
		}

		private static Enum EnumMaskPopupInternal(Rect position, GUIContent label, Enum selected, out int changedFlags, out bool changedToValue, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			return EditorGUI.EnumMaskField(position2, selected, style, out changedFlags, out changedToValue);
		}

		private static int IntPopupInternal(Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style)
		{
			int num;
			if (optionValues != null)
			{
				num = 0;
				while (num < optionValues.Length && selectedValue != optionValues[num])
				{
					num++;
				}
			}
			else
			{
				num = selectedValue;
			}
			num = EditorGUI.PopupInternal(position, label, num, displayedOptions, style);
			if (optionValues == null)
			{
				return num;
			}
			if (num < 0 || num >= optionValues.Length)
			{
				return selectedValue;
			}
			return optionValues[num];
		}

		internal static void IntPopupInternal(Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.BeginChangeCheck();
			int intValue = EditorGUI.IntPopupInternal(position, label, property.intValue, displayedOptions, optionValues, EditorStyles.popup);
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
			EditorGUI.EndProperty();
		}

		internal static void SortingLayerField(Rect position, GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_SortingLayerFieldHash, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label, labelStyle);
			Event current = Event.current;
			int selectedValueForControl = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, -1);
			if (selectedValueForControl != -1)
			{
				int[] sortingLayerUniqueIDs = InternalEditorUtility.sortingLayerUniqueIDs;
				if (selectedValueForControl >= sortingLayerUniqueIDs.Length)
				{
					((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "SortingLayers";
					Selection.activeObject = EditorApplication.tagManager;
				}
				else
				{
					layerID.intValue = sortingLayerUniqueIDs[selectedValueForControl];
				}
			}
			if ((current.type == EventType.MouseDown && position.Contains(current.mousePosition)) || current.MainActionKeyForControl(controlID))
			{
				int[] sortingLayerUniqueIDs2 = InternalEditorUtility.sortingLayerUniqueIDs;
				string[] sortingLayerNames = InternalEditorUtility.sortingLayerNames;
				int i;
				for (i = 0; i < sortingLayerUniqueIDs2.Length; i++)
				{
					if (sortingLayerUniqueIDs2[i] == layerID.intValue)
					{
						break;
					}
				}
				ArrayUtility.Add<string>(ref sortingLayerNames, string.Empty);
				ArrayUtility.Add<string>(ref sortingLayerNames, "Add Sorting Layer...");
				EditorGUI.DoPopup(position, controlID, i, EditorGUIUtility.TempContent(sortingLayerNames), style);
			}
			else if (Event.current.type == EventType.Repaint)
			{
				GUIContent content;
				if (layerID.hasMultipleDifferentValues)
				{
					content = EditorGUI.mixedValueContent;
				}
				else
				{
					content = EditorGUIUtility.TempContent(InternalEditorUtility.GetSortingLayerNameFromUniqueID(layerID.intValue));
				}
				EditorGUI.showMixedValue = layerID.hasMultipleDifferentValues;
				EditorGUI.BeginHandleMixedValueContentColor();
				style.Draw(position, content, controlID, false);
				EditorGUI.EndHandleMixedValueContentColor();
				EditorGUI.showMixedValue = false;
			}
		}

		internal static int DoPopup(Rect position, int controlID, int selected, GUIContent[] popupValues, GUIStyle style)
		{
			selected = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, selected);
			GUIContent gUIContent = null;
			if (gUIContent == null)
			{
				if (EditorGUI.showMixedValue)
				{
					gUIContent = EditorGUI.s_MixedValueContent;
				}
				else if (selected < 0 || selected >= popupValues.Length)
				{
					gUIContent = GUIContent.none;
				}
				else
				{
					gUIContent = popupValues[selected];
				}
			}
			Event current = Event.current;
			EventType type = current.type;
			switch (type)
			{
			case EventType.KeyDown:
				if (current.MainActionKeyForControl(controlID))
				{
					if (Application.platform == RuntimePlatform.OSXEditor)
					{
						position.y = position.y - (float)(selected * 16) - 19f;
					}
					EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(controlID);
					EditorUtility.DisplayCustomMenu(position, popupValues, (!EditorGUI.showMixedValue) ? selected : -1, new EditorUtility.SelectMenuItemFunction(EditorGUI.PopupCallbackInfo.instance.SetEnumValueDelegate), null);
					current.Use();
				}
				return selected;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_6A:
				if (type != EventType.MouseDown)
				{
					return selected;
				}
				if (current.button == 0 && position.Contains(current.mousePosition))
				{
					if (Application.platform == RuntimePlatform.OSXEditor)
					{
						position.y = position.y - (float)(selected * 16) - 19f;
					}
					EditorGUI.PopupCallbackInfo.instance = new EditorGUI.PopupCallbackInfo(controlID);
					EditorUtility.DisplayCustomMenu(position, popupValues, (!EditorGUI.showMixedValue) ? selected : -1, new EditorUtility.SelectMenuItemFunction(EditorGUI.PopupCallbackInfo.instance.SetEnumValueDelegate), null);
					GUIUtility.keyboardControl = controlID;
					current.Use();
				}
				return selected;
			case EventType.Repaint:
			{
				Font font = style.font;
				if (font && EditorGUIUtility.GetBoldDefaultFont() && font == EditorStyles.miniFont)
				{
					style.font = EditorStyles.miniBoldFont;
				}
				EditorGUI.BeginHandleMixedValueContentColor();
				style.Draw(position, gUIContent, controlID, false);
				EditorGUI.EndHandleMixedValueContentColor();
				style.font = font;
				return selected;
			}
			}
			goto IL_6A;
		}

		internal static string TagFieldInternal(Rect position, string tag, GUIStyle style)
		{
			position = EditorGUI.IndentedRect(position);
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TagFieldHash, EditorGUIUtility.native, position);
			Event current = Event.current;
			int selectedValueForControl = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, -1);
			if (selectedValueForControl != -1)
			{
				string[] tags = InternalEditorUtility.tags;
				if (selectedValueForControl >= tags.Length)
				{
					((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "Tags";
					Selection.activeObject = EditorApplication.tagManager;
				}
				else
				{
					tag = tags[selectedValueForControl];
				}
			}
			if ((current.type == EventType.MouseDown && position.Contains(current.mousePosition)) || current.MainActionKeyForControl(controlID))
			{
				string[] tags2 = InternalEditorUtility.tags;
				int i;
				for (i = 0; i < tags2.Length; i++)
				{
					if (tags2[i] == tag)
					{
						break;
					}
				}
				ArrayUtility.Add<string>(ref tags2, string.Empty);
				ArrayUtility.Add<string>(ref tags2, "Add Tag...");
				EditorGUI.DoPopup(position, controlID, i, EditorGUIUtility.TempContent(tags2), style);
				return tag;
			}
			if (Event.current.type == EventType.Repaint)
			{
				EditorGUI.BeginHandleMixedValueContentColor();
				style.Draw(position, (!EditorGUI.showMixedValue) ? EditorGUIUtility.TempContent(tag) : EditorGUI.s_MixedValueContent, controlID, false);
				EditorGUI.EndHandleMixedValueContentColor();
			}
			return tag;
		}

		internal static string TagFieldInternal(Rect position, GUIContent label, string tag, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TagFieldHash, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			Event current = Event.current;
			int selectedValueForControl = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, -1);
			if (selectedValueForControl != -1)
			{
				string[] tags = InternalEditorUtility.tags;
				if (selectedValueForControl >= tags.Length)
				{
					((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "Tags";
					Selection.activeObject = EditorApplication.tagManager;
				}
				else
				{
					tag = tags[selectedValueForControl];
				}
			}
			if ((current.type == EventType.MouseDown && position.Contains(current.mousePosition)) || current.MainActionKeyForControl(controlID))
			{
				string[] tags2 = InternalEditorUtility.tags;
				int i;
				for (i = 0; i < tags2.Length; i++)
				{
					if (tags2[i] == tag)
					{
						break;
					}
				}
				ArrayUtility.Add<string>(ref tags2, string.Empty);
				ArrayUtility.Add<string>(ref tags2, "Add Tag...");
				EditorGUI.DoPopup(position, controlID, i, EditorGUIUtility.TempContent(tags2), style);
				return tag;
			}
			if (Event.current.type == EventType.Repaint)
			{
				style.Draw(position, EditorGUIUtility.TempContent(tag), controlID, false);
			}
			return tag;
		}

		internal static int LayerFieldInternal(Rect position, GUIContent label, int layer, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TagFieldHash, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			Event current = Event.current;
			bool changed = GUI.changed;
			int selectedValueForControl = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(controlID, -1);
			if (selectedValueForControl != -1)
			{
				if (selectedValueForControl >= InternalEditorUtility.layers.Length)
				{
					((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "Layers";
					Selection.activeObject = EditorApplication.tagManager;
					GUI.changed = changed;
				}
				else
				{
					int num = 0;
					for (int i = 0; i < 32; i++)
					{
						if (InternalEditorUtility.GetLayerName(i).Length != 0)
						{
							if (num == selectedValueForControl)
							{
								layer = i;
								break;
							}
							num++;
						}
					}
				}
			}
			if ((current.type == EventType.MouseDown && position.Contains(current.mousePosition)) || current.MainActionKeyForControl(controlID))
			{
				int num2 = 0;
				for (int j = 0; j < 32; j++)
				{
					if (InternalEditorUtility.GetLayerName(j).Length != 0)
					{
						if (j == layer)
						{
							break;
						}
						num2++;
					}
				}
				string[] layers = InternalEditorUtility.layers;
				ArrayUtility.Add<string>(ref layers, string.Empty);
				ArrayUtility.Add<string>(ref layers, "Add Layer...");
				EditorGUI.DoPopup(position, controlID, num2, EditorGUIUtility.TempContent(layers), style);
				Event.current.Use();
				return layer;
			}
			if (current.type == EventType.Repaint)
			{
				style.Draw(position, EditorGUIUtility.TempContent(InternalEditorUtility.GetLayerName(layer)), controlID, false);
			}
			return layer;
		}

		internal static int MaskFieldInternal(Rect position, GUIContent label, int mask, string[] displayedOptions, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			return MaskFieldGUI.DoMaskField(position, controlID, mask, displayedOptions, style);
		}

		internal static int MaskFieldInternal(Rect position, int mask, string[] displayedOptions, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position);
			return MaskFieldGUI.DoMaskField(EditorGUI.IndentedRect(position), controlID, mask, displayedOptions, style);
		}

		internal static Enum EnumMaskFieldInternal(Rect position, GUIContent label, Enum enumValue, GUIStyle style)
		{
			Type type = enumValue.GetType();
			if (!type.IsEnum)
			{
				throw new Exception("parameter _enum must be of type System.Enum");
			}
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = position2.x;
			string[] flagNames = (from x in Enum.GetNames(enumValue.GetType())
			select ObjectNames.NicifyVariableName(x)).ToArray<string>();
			int value = MaskFieldGUI.DoMaskField(position2, controlID, Convert.ToInt32(enumValue), flagNames, style);
			return EditorGUI.EnumFlagsToInt(type, value);
		}

		internal static Enum EnumMaskFieldInternal(Rect position, Enum enumValue, GUIStyle style)
		{
			Type type = enumValue.GetType();
			if (!type.IsEnum)
			{
				throw new Exception("parameter _enum must be of type System.Enum");
			}
			string[] flagNames = (from x in Enum.GetNames(enumValue.GetType())
			select ObjectNames.NicifyVariableName(x)).ToArray<string>();
			int value = MaskFieldGUI.DoMaskField(EditorGUI.IndentedRect(position), GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position), Convert.ToInt32(enumValue), flagNames, style);
			return EditorGUI.EnumFlagsToInt(type, value);
		}

		internal static Enum EnumMaskField(Rect position, Enum enumValue, GUIStyle style, out int changedFlags, out bool changedToValue)
		{
			Type type = enumValue.GetType();
			if (!type.IsEnum)
			{
				throw new Exception("parameter _enum must be of type System.Enum");
			}
			string[] flagNames = (from x in Enum.GetNames(enumValue.GetType())
			select ObjectNames.NicifyVariableName(x)).ToArray<string>();
			int value = MaskFieldGUI.DoMaskField(EditorGUI.IndentedRect(position), GUIUtility.GetControlID(EditorGUI.s_MaskField, EditorGUIUtility.native, position), Convert.ToInt32(enumValue), flagNames, style, out changedFlags, out changedToValue);
			return EditorGUI.EnumFlagsToInt(type, value);
		}

		public static void ObjectField(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.ObjectField(position, property, null, label, EditorStyles.objectField);
		}

		public static void ObjectField(Rect position, SerializedProperty property, Type objType, GUIContent label)
		{
			EditorGUI.ObjectField(position, property, objType, label, EditorStyles.objectField);
		}

		internal static void ObjectField(Rect position, SerializedProperty property, Type objType, GUIContent label, GUIStyle style)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.ObjectFieldInternal(position, property, objType, label, style);
			EditorGUI.EndProperty();
		}

		private static void ObjectFieldInternal(Rect position, SerializedProperty property, Type objType, GUIContent label, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_PPtrHash, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			bool allowSceneObjects = false;
			if (property != null)
			{
				UnityEngine.Object targetObject = property.serializedObject.targetObject;
				if (targetObject != null && !EditorUtility.IsPersistent(targetObject))
				{
					allowSceneObjects = true;
				}
			}
			EditorGUI.DoObjectField(position, position, controlID, null, null, property, null, allowSceneObjects, style);
		}

		public static UnityEngine.Object ObjectField(Rect position, UnityEngine.Object obj, Type objType, bool allowSceneObjects)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ObjectFieldHash, EditorGUIUtility.native, position);
			return EditorGUI.DoObjectField(EditorGUI.IndentedRect(position), EditorGUI.IndentedRect(position), controlID, obj, objType, null, null, allowSceneObjects);
		}

		[Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
		public static UnityEngine.Object ObjectField(Rect position, UnityEngine.Object obj, Type objType)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ObjectFieldHash, EditorGUIUtility.native, position);
			return EditorGUI.DoObjectField(position, position, controlID, obj, objType, null, null, true);
		}

		public static UnityEngine.Object ObjectField(Rect position, string label, UnityEngine.Object obj, Type objType, bool allowSceneObjects)
		{
			return EditorGUI.ObjectField(position, EditorGUIUtility.TempContent(label), obj, objType, allowSceneObjects);
		}

		[Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
		public static UnityEngine.Object ObjectField(Rect position, string label, UnityEngine.Object obj, Type objType)
		{
			return EditorGUI.ObjectField(position, EditorGUIUtility.TempContent(label), obj, objType, true);
		}

		public static UnityEngine.Object ObjectField(Rect position, GUIContent label, UnityEngine.Object obj, Type objType, bool allowSceneObjects)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ObjectFieldHash, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			if (EditorGUIUtility.HasObjectThumbnail(objType) && position.height > 16f)
			{
				float num = Mathf.Min(position.width, position.height);
				position.height = num;
				position.xMin = position.xMax - num;
			}
			return EditorGUI.DoObjectField(position, position, controlID, obj, objType, null, null, allowSceneObjects);
		}

		internal static void GetRectsForMiniThumbnailField(Rect position, out Rect thumbRect, out Rect labelRect)
		{
			thumbRect = EditorGUI.IndentedRect(position);
			thumbRect.y -= 1f;
			thumbRect.height = 18f;
			thumbRect.width = 32f;
			float num = thumbRect.x + 30f;
			labelRect = new Rect(num, position.y, thumbRect.x + EditorGUIUtility.labelWidth - num, position.height);
		}

		internal static UnityEngine.Object MiniThumbnailObjectField(Rect position, GUIContent label, UnityEngine.Object obj, Type objType, EditorGUI.ObjectFieldValidator validator)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ObjectFieldHash, EditorGUIUtility.native, position);
			Rect rect;
			Rect labelPosition;
			EditorGUI.GetRectsForMiniThumbnailField(position, out rect, out labelPosition);
			EditorGUI.HandlePrefixLabel(position, labelPosition, label, controlID, EditorStyles.label);
			return EditorGUI.DoObjectField(rect, rect, controlID, obj, objType, null, validator, false);
		}

		[Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
		public static UnityEngine.Object ObjectField(Rect position, GUIContent label, UnityEngine.Object obj, Type objType)
		{
			return EditorGUI.ObjectField(position, label, obj, objType, true);
		}

		internal static UnityEngine.Object ValidateObjectFieldAssignment(UnityEngine.Object[] references, Type objType, SerializedProperty property)
		{
			if (references.Length > 0)
			{
				bool flag = DragAndDrop.objectReferences.Length > 0;
				bool flag2 = references[0] != null && references[0].GetType() == typeof(Texture2D);
				if (objType == typeof(Sprite) && flag2 && flag)
				{
					return SpriteUtility.TextureToSprite(references[0] as Texture2D);
				}
				if (property != null)
				{
					if (references[0] != null && property.ValidateObjectReferenceValue(references[0]))
					{
						return references[0];
					}
					if ((property.type == "PPtr<Sprite>" || property.type == "PPtr<$Sprite>" || property.type == "vector") && flag2 && flag)
					{
						return SpriteUtility.TextureToSprite(references[0] as Texture2D);
					}
				}
				else
				{
					if (references[0] != null && references[0].GetType() == typeof(GameObject) && typeof(Component).IsAssignableFrom(objType))
					{
						GameObject gameObject = (GameObject)references[0];
						references = gameObject.GetComponents(typeof(Component));
					}
					UnityEngine.Object[] array = references;
					for (int i = 0; i < array.Length; i++)
					{
						UnityEngine.Object @object = array[i];
						if (@object != null && objType.IsAssignableFrom(@object.GetType()))
						{
							return @object;
						}
					}
				}
			}
			return null;
		}

		private static UnityEngine.Object HandleTextureToSprite(Texture2D tex)
		{
			UnityEngine.Object[] array = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex));
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].GetType() == typeof(Sprite))
				{
					return array[i];
				}
			}
			return tex;
		}

		public static Rect IndentedRect(Rect source)
		{
			float indent = EditorGUI.indent;
			return new Rect(source.x + indent, source.y, source.width - indent, source.height);
		}

		public static Vector2 Vector2Field(Rect position, string label, Vector2 value)
		{
			return EditorGUI.Vector2Field(position, EditorGUIUtility.TempContent(label), value);
		}

		public static Vector2 Vector2Field(Rect position, GUIContent label, Vector2 value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 2);
			position.height = 16f;
			return EditorGUI.Vector2Field(position, value);
		}

		private static Vector2 Vector2Field(Rect position, Vector2 value)
		{
			EditorGUI.s_Vector2Floats[0] = value.x;
			EditorGUI.s_Vector2Floats[1] = value.y;
			position.height = 16f;
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, EditorGUI.s_XYLabels, EditorGUI.s_Vector2Floats);
			if (EditorGUI.EndChangeCheck())
			{
				value.x = EditorGUI.s_Vector2Floats[0];
				value.y = EditorGUI.s_Vector2Floats[1];
			}
			return value;
		}

		public static Vector3 Vector3Field(Rect position, string label, Vector3 value)
		{
			return EditorGUI.Vector3Field(position, EditorGUIUtility.TempContent(label), value);
		}

		public static Vector3 Vector3Field(Rect position, GUIContent label, Vector3 value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3);
			position.height = 16f;
			return EditorGUI.Vector3Field(position, value);
		}

		private static Vector3 Vector3Field(Rect position, Vector3 value)
		{
			EditorGUI.s_Vector3Floats[0] = value.x;
			EditorGUI.s_Vector3Floats[1] = value.y;
			EditorGUI.s_Vector3Floats[2] = value.z;
			position.height = 16f;
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, EditorGUI.s_XYZLabels, EditorGUI.s_Vector3Floats);
			if (EditorGUI.EndChangeCheck())
			{
				value.x = EditorGUI.s_Vector3Floats[0];
				value.y = EditorGUI.s_Vector3Floats[1];
				value.z = EditorGUI.s_Vector3Floats[2];
			}
			return value;
		}

		private static void Vector2Field(Rect position, SerializedProperty property, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 2);
			position.height = 16f;
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.NextVisible(true);
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYLabels, serializedProperty);
		}

		private static void Vector3Field(Rect position, SerializedProperty property, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3);
			position.height = 16f;
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.NextVisible(true);
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYZLabels, serializedProperty);
		}

		private static void Vector4Field(Rect position, SerializedProperty property, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 4);
			position.height = 16f;
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.NextVisible(true);
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYZWLabels, serializedProperty);
		}

		public static Vector4 Vector4Field(Rect position, string label, Vector4 value)
		{
			EditorGUI.s_Vector4Floats[0] = value.x;
			EditorGUI.s_Vector4Floats[1] = value.y;
			EditorGUI.s_Vector4Floats[2] = value.z;
			EditorGUI.s_Vector4Floats[3] = value.w;
			position.height = 16f;
			GUI.Label(EditorGUI.IndentedRect(position), label, EditorStyles.label);
			position.y += 16f;
			EditorGUI.BeginChangeCheck();
			EditorGUI.indentLevel++;
			EditorGUI.MultiFloatField(position, EditorGUI.s_XYZWLabels, EditorGUI.s_Vector4Floats);
			EditorGUI.indentLevel--;
			if (EditorGUI.EndChangeCheck())
			{
				value.x = EditorGUI.s_Vector4Floats[0];
				value.y = EditorGUI.s_Vector4Floats[1];
				value.z = EditorGUI.s_Vector4Floats[2];
				value.w = EditorGUI.s_Vector4Floats[3];
			}
			return value;
		}

		public static Rect RectField(Rect position, Rect value)
		{
			position.height = 16f;
			EditorGUI.s_Vector2Floats[0] = value.x;
			EditorGUI.s_Vector2Floats[1] = value.y;
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, EditorGUI.s_XYLabels, EditorGUI.s_Vector2Floats);
			if (EditorGUI.EndChangeCheck())
			{
				value.x = EditorGUI.s_Vector2Floats[0];
				value.y = EditorGUI.s_Vector2Floats[1];
			}
			position.y += 16f;
			EditorGUI.s_Vector2Floats[0] = value.width;
			EditorGUI.s_Vector2Floats[1] = value.height;
			EditorGUI.BeginChangeCheck();
			EditorGUI.MultiFloatField(position, EditorGUI.s_WHLabels, EditorGUI.s_Vector2Floats);
			if (EditorGUI.EndChangeCheck())
			{
				value.width = EditorGUI.s_Vector2Floats[0];
				value.height = EditorGUI.s_Vector2Floats[1];
			}
			return value;
		}

		public static Rect RectField(Rect position, string label, Rect value)
		{
			return EditorGUI.RectField(position, EditorGUIUtility.TempContent(label), value);
		}

		public static Rect RectField(Rect position, GUIContent label, Rect value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 2);
			position.height = 16f;
			return EditorGUI.RectField(position, value);
		}

		private static void RectField(Rect position, SerializedProperty property, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 2);
			position.height = 16f;
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.NextVisible(true);
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYLabels, serializedProperty);
			position.y += 16f;
			EditorGUI.MultiPropertyField(position, EditorGUI.s_WHLabels, serializedProperty);
		}

		private static Rect DrawBoundsFieldLabelsAndAdjustPositionForValues(Rect position, bool drawOutside)
		{
			if (drawOutside)
			{
				position.xMin -= 53f;
			}
			GUI.Label(EditorGUI.IndentedRect(position), "Center:", EditorStyles.label);
			position.y += 16f;
			GUI.Label(EditorGUI.IndentedRect(position), "Extents:", EditorStyles.label);
			position.y -= 16f;
			position.xMin += 53f;
			return position;
		}

		public static Bounds BoundsField(Rect position, Bounds value)
		{
			return EditorGUI.BoundsField(position, GUIContent.none, value);
		}

		public static Bounds BoundsField(Rect position, GUIContent label, Bounds value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3);
			if (EditorGUIUtility.wideMode && EditorGUI.LabelHasContent(label))
			{
				position.y += 16f;
			}
			position.height = 16f;
			position = EditorGUI.DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && EditorGUI.LabelHasContent(label));
			value.center = EditorGUI.Vector3Field(position, value.center);
			position.y += 16f;
			value.extents = EditorGUI.Vector3Field(position, value.extents);
			return value;
		}

		private static void BoundsField(Rect position, SerializedProperty property, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, 3);
			if (EditorGUIUtility.wideMode && EditorGUI.LabelHasContent(label))
			{
				position.y += 16f;
			}
			position.height = 16f;
			position = EditorGUI.DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && EditorGUI.LabelHasContent(label));
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.NextVisible(true);
			serializedProperty.NextVisible(true);
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYZLabels, serializedProperty);
			serializedProperty.NextVisible(true);
			position.y += 16f;
			EditorGUI.MultiPropertyField(position, EditorGUI.s_XYZLabels, serializedProperty);
		}

		public static void MultiFloatField(Rect position, GUIContent label, GUIContent[] subLabels, float[] values)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, subLabels.Length);
			position.height = 16f;
			EditorGUI.MultiFloatField(position, subLabels, values);
		}

		public static void MultiFloatField(Rect position, GUIContent[] subLabels, float[] values)
		{
			EditorGUI.MultiFloatField(position, subLabels, values, 13f);
		}

		internal static void MultiFloatField(Rect position, GUIContent[] subLabels, float[] values, float labelWidth)
		{
			int num = values.Length;
			float num2 = (position.width - (float)(num - 1) * 2f) / (float)num;
			Rect position2 = new Rect(position);
			position2.width = num2;
			float labelWidth2 = EditorGUIUtility.labelWidth;
			int indentLevel = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.indentLevel = 0;
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = EditorGUI.FloatField(position2, subLabels[i], values[i]);
				position2.x += num2 + 2f;
			}
			EditorGUIUtility.labelWidth = labelWidth2;
			EditorGUI.indentLevel = indentLevel;
		}

		public static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, GUIContent label)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			position = EditorGUI.MultiFieldPrefixLabel(position, controlID, label, subLabels.Length);
			position.height = 16f;
			EditorGUI.MultiPropertyField(position, subLabels, valuesIterator);
		}

		public static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator)
		{
			EditorGUI.MultiPropertyField(position, subLabels, valuesIterator, 13f, null);
		}

		internal static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, float labelWidth, bool[] disabledMask)
		{
			int num = subLabels.Length;
			float num2 = (position.width - (float)(num - 1) * 2f) / (float)num;
			Rect position2 = new Rect(position);
			position2.width = num2;
			float labelWidth2 = EditorGUIUtility.labelWidth;
			int indentLevel = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.indentLevel = 0;
			for (int i = 0; i < subLabels.Length; i++)
			{
				if (disabledMask != null)
				{
					EditorGUI.BeginDisabledGroup(disabledMask[i]);
				}
				EditorGUI.PropertyField(position2, valuesIterator, subLabels[i]);
				if (disabledMask != null)
				{
					EditorGUI.EndDisabledGroup();
				}
				position2.x += num2 + 2f;
				valuesIterator.NextVisible(false);
			}
			EditorGUIUtility.labelWidth = labelWidth2;
			EditorGUI.indentLevel = indentLevel;
		}

		internal static void PropertiesField(Rect position, GUIContent label, SerializedProperty[] properties, GUIContent[] propertyLabels, float propertyLabelsWidth)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			Rect position2 = EditorGUI.PrefixLabel(position, controlID, label);
			position2.height = 16f;
			float labelWidth = EditorGUIUtility.labelWidth;
			int indentLevel = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = propertyLabelsWidth;
			EditorGUI.indentLevel = 0;
			for (int i = 0; i < properties.Length; i++)
			{
				EditorGUI.PropertyField(position2, properties[i], propertyLabels[i]);
				position2.y += 16f;
			}
			EditorGUI.indentLevel = indentLevel;
			EditorGUIUtility.labelWidth = labelWidth;
		}

		internal static int CycleButton(Rect position, int selected, GUIContent[] options, GUIStyle style)
		{
			if (selected >= options.Length || selected < 0)
			{
				selected = 0;
				GUI.changed = true;
			}
			if (GUI.Button(position, options[selected], style))
			{
				selected++;
				GUI.changed = true;
				if (selected >= options.Length)
				{
					selected = 0;
				}
			}
			return selected;
		}

		public static Color ColorField(Rect position, Color value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ColorHash, EditorGUIUtility.native, position);
			return EditorGUI.DoColorField(EditorGUI.IndentedRect(position), controlID, value, true, true, false, null);
		}

		internal static Color ColorField(Rect position, Color value, bool showEyedropper, bool showAlpha)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ColorHash, EditorGUIUtility.native, position);
			return EditorGUI.DoColorField(position, controlID, value, showEyedropper, showAlpha, false, null);
		}

		public static Color ColorField(Rect position, string label, Color value)
		{
			return EditorGUI.ColorField(position, EditorGUIUtility.TempContent(label), value);
		}

		public static Color ColorField(Rect position, GUIContent label, Color value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ColorHash, EditorGUIUtility.native, position);
			return EditorGUI.DoColorField(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, true, true, false, null);
		}

		internal static Color ColorField(Rect position, GUIContent label, Color value, bool showEyedropper, bool showAlpha)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ColorHash, EditorGUIUtility.native, position);
			return EditorGUI.DoColorField(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, showEyedropper, showAlpha, false, null);
		}

		public static Color ColorField(Rect position, GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ColorHash, EditorGUIUtility.native, position);
			return EditorGUI.DoColorField(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, showEyedropper, showAlpha, hdr, hdrConfig);
		}

		private static Color DoColorField(Rect position, int id, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
		{
			Event current = Event.current;
			GUIStyle colorField = EditorStyles.colorField;
			Color result = value;
			value = ((!EditorGUI.showMixedValue) ? value : Color.white);
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.KeyDown:
				if (current.MainActionKeyForControl(id))
				{
					Event.current.Use();
					EditorGUI.showMixedValue = false;
					ColorPicker.Show(GUIView.current, value, showAlpha, hdr, hdrConfig);
					GUIUtility.ExitGUI();
				}
				return result;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_47:
				if (typeForControl == EventType.ValidateCommand)
				{
					string commandName = current.commandName;
					if (commandName != null)
					{
						if (EditorGUI.<>f__switch$map2 == null)
						{
							EditorGUI.<>f__switch$map2 = new Dictionary<string, int>(3)
							{
								{
									"UndoRedoPerformed",
									0
								},
								{
									"Copy",
									1
								},
								{
									"Paste",
									1
								}
							};
						}
						int button;
						if (EditorGUI.<>f__switch$map2.TryGetValue(commandName, out button))
						{
							if (button != 0)
							{
								if (button == 1)
								{
									current.Use();
								}
							}
							else if (GUIUtility.keyboardControl == id && ColorPicker.visible)
							{
								ColorPicker.color = value;
							}
						}
					}
					return result;
				}
				if (typeForControl == EventType.ExecuteCommand)
				{
					if (GUIUtility.keyboardControl == id)
					{
						string commandName = current.commandName;
						switch (commandName)
						{
						case "EyeDropperUpdate":
							HandleUtility.Repaint();
							break;
						case "EyeDropperClicked":
						{
							GUI.changed = true;
							HandleUtility.Repaint();
							Color lastPickedColor = EyeDropper.GetLastPickedColor();
							lastPickedColor.a = value.a;
							EditorGUI.s_ColorPickID = 0;
							return lastPickedColor;
						}
						case "EyeDropperCancelled":
							HandleUtility.Repaint();
							EditorGUI.s_ColorPickID = 0;
							break;
						case "ColorPickerChanged":
							GUI.changed = true;
							HandleUtility.Repaint();
							return ColorPicker.color;
						case "Copy":
							ColorClipboard.SetColor(value);
							current.Use();
							break;
						case "Paste":
						{
							Color color;
							if (ColorClipboard.TryGetColor(hdr, out color))
							{
								if (!showAlpha)
								{
									color.a = result.a;
								}
								result = color;
								GUI.changed = true;
								current.Use();
							}
							break;
						}
						}
					}
					return result;
				}
				if (typeForControl != EventType.MouseDown)
				{
					return result;
				}
				if (showEyedropper)
				{
					position.width -= 20f;
				}
				if (position.Contains(current.mousePosition))
				{
					int button = current.button;
					if (button != 0)
					{
						if (button == 1)
						{
							GUIUtility.keyboardControl = id;
							string[] options2 = new string[]
							{
								"Copy",
								"Paste"
							};
							bool[] enabled = new bool[]
							{
								true,
								ColorClipboard.HasColor()
							};
							EditorUtility.DisplayCustomMenu(position, options2, enabled, null, delegate(object data, string[] options, int selected)
							{
								if (selected == 0)
								{
									Event e = EditorGUIUtility.CommandEvent("Copy");
									GUIView.current.SendEvent(e);
								}
								else if (selected == 1)
								{
									Event e2 = EditorGUIUtility.CommandEvent("Paste");
									GUIView.current.SendEvent(e2);
								}
							}, null);
							return result;
						}
					}
					else
					{
						GUIUtility.keyboardControl = id;
						EditorGUI.showMixedValue = false;
						ColorPicker.Show(GUIView.current, value, showAlpha, hdr, hdrConfig);
						GUIUtility.ExitGUI();
					}
				}
				if (showEyedropper)
				{
					position.width += 20f;
					if (position.Contains(current.mousePosition))
					{
						GUIUtility.keyboardControl = id;
						EyeDropper.Start(GUIView.current);
						EditorGUI.s_ColorPickID = id;
						GUIUtility.ExitGUI();
					}
				}
				return result;
			case EventType.Repaint:
			{
				Rect position2;
				if (showEyedropper)
				{
					position2 = colorField.padding.Remove(position);
				}
				else
				{
					position2 = position;
				}
				if (showEyedropper && EditorGUI.s_ColorPickID == id)
				{
					Color pickedColor = EyeDropper.GetPickedColor();
					pickedColor.a = value.a;
					EditorGUIUtility.DrawColorSwatch(position2, pickedColor, showAlpha, hdr);
				}
				else
				{
					EditorGUIUtility.DrawColorSwatch(position2, value, showAlpha, hdr);
				}
				if (showEyedropper)
				{
					colorField.Draw(position, GUIContent.none, id);
				}
				else
				{
					EditorStyles.colorPickerBox.Draw(position, GUIContent.none, id);
				}
				return result;
			}
			}
			goto IL_47;
		}

		internal static Color ColorSelector(Rect activatorRect, Rect renderRect, int id, Color value)
		{
			Event current = Event.current;
			Color result = value;
			value = ((!EditorGUI.showMixedValue) ? value : Color.white);
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.KeyDown:
				if (current.MainActionKeyForControl(id))
				{
					current.Use();
					EditorGUI.showMixedValue = false;
					ColorPicker.Show(GUIView.current, value, false, false, null);
					GUIUtility.ExitGUI();
				}
				return result;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_3F:
				if (typeForControl == EventType.ValidateCommand)
				{
					if (current.commandName == "UndoRedoPerformed" && GUIUtility.keyboardControl == id && ColorPicker.visible)
					{
						ColorPicker.color = value;
					}
					return result;
				}
				if (typeForControl == EventType.ExecuteCommand)
				{
					if (GUIUtility.keyboardControl == id)
					{
						string commandName = current.commandName;
						if (commandName != null)
						{
							if (EditorGUI.<>f__switch$map4 == null)
							{
								EditorGUI.<>f__switch$map4 = new Dictionary<string, int>(1)
								{
									{
										"ColorPickerChanged",
										0
									}
								};
							}
							int num;
							if (EditorGUI.<>f__switch$map4.TryGetValue(commandName, out num))
							{
								if (num == 0)
								{
									current.Use();
									GUI.changed = true;
									HandleUtility.Repaint();
									return ColorPicker.color;
								}
							}
						}
					}
					return result;
				}
				if (typeForControl != EventType.MouseDown)
				{
					return result;
				}
				if (activatorRect.Contains(current.mousePosition))
				{
					current.Use();
					GUIUtility.keyboardControl = id;
					EditorGUI.showMixedValue = false;
					ColorPicker.Show(GUIView.current, value, false, false, null);
					GUIUtility.ExitGUI();
				}
				return result;
			case EventType.Repaint:
				if (renderRect.height > 0f && renderRect.width > 0f)
				{
					EditorGUI.DrawRect(renderRect, value);
				}
				return result;
			}
			goto IL_3F;
		}

		public static AnimationCurve CurveField(Rect position, AnimationCurve value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
			return EditorGUI.DoCurveField(EditorGUI.IndentedRect(position), controlID, value, EditorGUI.kCurveColor, default(Rect), null);
		}

		public static AnimationCurve CurveField(Rect position, string label, AnimationCurve value)
		{
			return EditorGUI.CurveField(position, EditorGUIUtility.TempContent(label), value);
		}

		public static AnimationCurve CurveField(Rect position, GUIContent label, AnimationCurve value)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
			return EditorGUI.DoCurveField(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, EditorGUI.kCurveColor, default(Rect), null);
		}

		public static AnimationCurve CurveField(Rect position, AnimationCurve value, Color color, Rect ranges)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
			return EditorGUI.DoCurveField(EditorGUI.IndentedRect(position), controlID, value, color, ranges, null);
		}

		public static AnimationCurve CurveField(Rect position, string label, AnimationCurve value, Color color, Rect ranges)
		{
			return EditorGUI.CurveField(position, EditorGUIUtility.TempContent(label), value, color, ranges);
		}

		public static AnimationCurve CurveField(Rect position, GUIContent label, AnimationCurve value, Color color, Rect ranges)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
			return EditorGUI.DoCurveField(EditorGUI.PrefixLabel(position, controlID, label), controlID, value, color, ranges, null);
		}

		public static void CurveField(Rect position, SerializedProperty value, Color color, Rect ranges)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
			EditorGUI.DoCurveField(position, controlID, null, color, ranges, value);
		}

		private static void SetCurveEditorWindowCurve(AnimationCurve value, SerializedProperty property, Color color)
		{
			if (property != null)
			{
				CurveEditorWindow.curve = ((!property.hasMultipleDifferentValues) ? property.animationCurveValue : new AnimationCurve());
			}
			else
			{
				CurveEditorWindow.curve = value;
			}
			CurveEditorWindow.color = color;
		}

		private static AnimationCurve DoCurveField(Rect position, int id, AnimationCurve value, Color color, Rect ranges, SerializedProperty property)
		{
			Event current = Event.current;
			position.width = Mathf.Max(position.width, 2f);
			position.height = Mathf.Max(position.height, 2f);
			if (GUIUtility.keyboardControl == id && Event.current.type != EventType.Layout)
			{
				if (EditorGUI.s_CurveID != id)
				{
					EditorGUI.s_CurveID = id;
					if (CurveEditorWindow.visible)
					{
						EditorGUI.SetCurveEditorWindowCurve(value, property, color);
						EditorGUI.ShowCurvePopup(GUIView.current, ranges);
					}
				}
				else if (CurveEditorWindow.visible && Event.current.type == EventType.Repaint)
				{
					EditorGUI.SetCurveEditorWindowCurve(value, property, color);
					CurveEditorWindow.instance.Repaint();
				}
			}
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.KeyDown:
				if (current.MainActionKeyForControl(id))
				{
					EditorGUI.s_CurveID = id;
					EditorGUI.SetCurveEditorWindowCurve(value, property, color);
					EditorGUI.ShowCurvePopup(GUIView.current, ranges);
					current.Use();
					GUIUtility.ExitGUI();
				}
				return value;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_D3:
				if (typeForControl == EventType.MouseDown)
				{
					if (position.Contains(current.mousePosition))
					{
						EditorGUI.s_CurveID = id;
						GUIUtility.keyboardControl = id;
						EditorGUI.SetCurveEditorWindowCurve(value, property, color);
						EditorGUI.ShowCurvePopup(GUIView.current, ranges);
						current.Use();
						GUIUtility.ExitGUI();
					}
					return value;
				}
				if (typeForControl != EventType.ExecuteCommand)
				{
					return value;
				}
				if (EditorGUI.s_CurveID == id)
				{
					string commandName = current.commandName;
					if (commandName != null)
					{
						if (EditorGUI.<>f__switch$map5 == null)
						{
							EditorGUI.<>f__switch$map5 = new Dictionary<string, int>(1)
							{
								{
									"CurveChanged",
									0
								}
							};
						}
						int num;
						if (EditorGUI.<>f__switch$map5.TryGetValue(commandName, out num))
						{
							if (num == 0)
							{
								GUI.changed = true;
								AnimationCurvePreviewCache.ClearCache();
								HandleUtility.Repaint();
								if (property != null)
								{
									property.animationCurveValue = CurveEditorWindow.curve;
									if (property.hasMultipleDifferentValues)
									{
										Debug.LogError("AnimationCurve SerializedProperty hasMultipleDifferentValues is true after writing.");
									}
								}
								return CurveEditorWindow.curve;
							}
						}
					}
				}
				return value;
			case EventType.Repaint:
			{
				Rect position2 = position;
				position2.y += 1f;
				position2.height -= 1f;
				if (ranges != default(Rect))
				{
					EditorGUIUtility.DrawCurveSwatch(position2, value, property, color, EditorGUI.kCurveBGColor, ranges);
				}
				else
				{
					EditorGUIUtility.DrawCurveSwatch(position2, value, property, color, EditorGUI.kCurveBGColor);
				}
				EditorStyles.colorPickerBox.Draw(position2, GUIContent.none, id, false);
				return value;
			}
			}
			goto IL_D3;
		}

		private static void ShowCurvePopup(GUIView viewToUpdate, Rect ranges)
		{
			CurveEditorSettings curveEditorSettings = new CurveEditorSettings();
			if (ranges.width > 0f && ranges.height > 0f && ranges.width != float.PositiveInfinity && ranges.height != float.PositiveInfinity)
			{
				curveEditorSettings.hRangeMin = ranges.xMin;
				curveEditorSettings.hRangeMax = ranges.xMax;
				curveEditorSettings.vRangeMin = ranges.yMin;
				curveEditorSettings.vRangeMax = ranges.yMax;
			}
			CurveEditorWindow.instance.Show(GUIView.current, curveEditorSettings);
		}

		private static bool ValidTargetForIconSelection(UnityEngine.Object[] targets)
		{
			return (targets[0] as MonoScript || targets[0] as GameObject) && targets.Length == 1;
		}

		internal static void ObjectIconDropDown(Rect position, UnityEngine.Object[] targets, bool showLabelIcons, Texture2D nullIcon, SerializedProperty iconProperty)
		{
			if (EditorGUI.s_IconTextureInactive == null)
			{
				EditorGUI.s_IconTextureInactive = (Material)EditorGUIUtility.LoadRequired("Inspectors/InactiveGUI.mat");
			}
			if (Event.current.type == EventType.Repaint)
			{
				Texture2D texture2D = null;
				if (!iconProperty.hasMultipleDifferentValues)
				{
					texture2D = AssetPreview.GetMiniThumbnail(targets[0]);
				}
				if (texture2D == null)
				{
					texture2D = nullIcon;
				}
				Vector2 vector = new Vector2(position.width, position.height);
				if (texture2D)
				{
					vector.x = Mathf.Min((float)texture2D.width, vector.x);
					vector.y = Mathf.Min((float)texture2D.height, vector.y);
				}
				Rect rect = new Rect(position.x + position.width / 2f - vector.x / 2f, position.y + position.height / 2f - vector.y / 2f, vector.x, vector.y);
				GameObject gameObject = targets[0] as GameObject;
				bool flag = gameObject && !EditorUtility.IsPersistent(targets[0]) && (!gameObject.activeSelf || !gameObject.activeInHierarchy);
				if (flag)
				{
					Graphics.DrawTexture(rect, texture2D, new Rect(0f, 0f, 1f, 1f), 0, 0, 0, 0, new Color(0.5f, 0.5f, 0.5f, 1f), EditorGUI.s_IconTextureInactive);
				}
				else
				{
					GUI.DrawTexture(rect, texture2D);
				}
				if (EditorGUI.ValidTargetForIconSelection(targets))
				{
					if (EditorGUI.s_IconDropDown == null)
					{
						EditorGUI.s_IconDropDown = EditorGUIUtility.IconContent("Icon Dropdown");
					}
					GUIStyle.none.Draw(new Rect(Mathf.Max(position.x + 2f, rect.x - 6f), rect.yMax - rect.height * 0.2f, 13f, 8f), EditorGUI.s_IconDropDown, false, false, false, false);
				}
			}
			if (EditorGUI.ButtonMouseDown(position, GUIContent.none, FocusType.Passive, GUIStyle.none) && EditorGUI.ValidTargetForIconSelection(targets) && IconSelector.ShowAtPosition(targets[0], position, showLabelIcons))
			{
				GUIUtility.ExitGUI();
			}
		}

		public static void InspectorTitlebar(Rect position, UnityEngine.Object[] targetObjs)
		{
			GUIStyle none = GUIStyle.none;
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
			EditorGUI.DoInspectorTitlebar(position, controlID, true, targetObjs, none);
		}

		public static bool InspectorTitlebar(Rect position, bool foldout, UnityEngine.Object targetObj, bool expandable)
		{
			return EditorGUI.InspectorTitlebar(position, foldout, new UnityEngine.Object[]
			{
				targetObj
			}, expandable);
		}

		public static bool InspectorTitlebar(Rect position, bool foldout, UnityEngine.Object[] targetObjs, bool expandable)
		{
			GUIStyle inspectorTitlebar = EditorStyles.inspectorTitlebar;
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
			EditorGUI.DoInspectorTitlebar(position, controlID, foldout, targetObjs, inspectorTitlebar);
			if (expandable)
			{
				Rect inspectorTitleBarObjectFoldoutRenderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(position);
				return EditorGUI.DoObjectFoldout(foldout, position, inspectorTitleBarObjectFoldoutRenderRect, targetObjs, controlID);
			}
			return foldout;
		}

		internal static void DoInspectorTitlebar(Rect position, int id, bool foldout, UnityEngine.Object[] targetObjs, GUIStyle baseStyle)
		{
			GUIStyle inspectorTitlebarText = EditorStyles.inspectorTitlebarText;
			Rect rect = new Rect(position.x + (float)baseStyle.padding.left, position.y + (float)baseStyle.padding.top, 16f, 16f);
			Rect rect2 = new Rect(position.xMax - (float)baseStyle.padding.right - 2f - 16f, rect.y, 16f, 16f);
			Rect position2 = new Rect(rect.xMax + 2f + 2f + 16f, rect.y, 100f, rect.height);
			position2.xMax = rect2.xMin - 2f;
			int num = -1;
			for (int i = 0; i < targetObjs.Length; i++)
			{
				UnityEngine.Object target = targetObjs[i];
				int objectEnabled = EditorUtility.GetObjectEnabled(target);
				if (num == -1)
				{
					num = objectEnabled;
				}
				else if (num != objectEnabled)
				{
					num = -2;
				}
			}
			if (num != -1)
			{
				bool flag = AnimationMode.IsPropertyAnimated(targetObjs[0], "m_Enabled");
				bool flag2 = num != 0;
				EditorGUI.showMixedValue = (num == -2);
				Rect position3 = rect;
				position3.x = rect.xMax + 2f;
				EditorGUI.BeginChangeCheck();
				Color color = GUI.color;
				if (flag)
				{
					GUI.color = AnimationMode.animatedPropertyColor;
				}
				int controlID = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
				flag2 = EditorGUIInternal.DoToggleForward(position3, controlID, flag2, GUIContent.none, EditorStyles.toggle);
				if (flag)
				{
					GUI.color = color;
				}
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObjects(targetObjs, ((!flag2) ? "Disable" : "Enable") + " Component" + ((targetObjs.Length <= 1) ? string.Empty : "s"));
					for (int j = 0; j < targetObjs.Length; j++)
					{
						UnityEngine.Object target2 = targetObjs[j];
						EditorUtility.SetObjectEnabled(target2, flag2);
					}
				}
				EditorGUI.showMixedValue = false;
			}
			Rect position4 = rect2;
			position4.x -= 18f;
			if (EditorGUI.HelpIconButton(position4, targetObjs[0]))
			{
				position2.xMax = position4.xMin - 2f;
			}
			Event current = Event.current;
			Texture2D i2 = null;
			if (current.type == EventType.Repaint)
			{
				i2 = AssetPreview.GetMiniThumbnail(targetObjs[0]);
			}
			if (EditorGUI.ButtonMouseDown(rect, EditorGUIUtility.TempContent(i2), FocusType.Passive, GUIStyle.none) && targetObjs[0] as MonoScript != null && IconSelector.ShowAtPosition(targetObjs[0], rect, true))
			{
				GUIUtility.ExitGUI();
			}
			EventType type = current.type;
			if (type != EventType.MouseDown)
			{
				if (type == EventType.Repaint)
				{
					baseStyle.Draw(position, GUIContent.none, id, foldout);
					position = baseStyle.padding.Remove(position);
					inspectorTitlebarText.Draw(position2, EditorGUIUtility.TempContent(ObjectNames.GetInspectorTitle(targetObjs[0])), id, foldout);
					inspectorTitlebarText.Draw(rect2, EditorGUI.GUIContents.titleSettingsIcon, id, foldout);
				}
			}
			else if (rect2.Contains(current.mousePosition))
			{
				EditorUtility.DisplayObjectContextMenu(rect2, targetObjs, 0);
				current.Use();
			}
		}

		internal static bool ToggleTitlebar(Rect position, GUIContent label, bool foldout, ref bool toggleValue)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
			GUIStyle inspectorTitlebar = EditorStyles.inspectorTitlebar;
			GUIStyle inspectorTitlebarText = EditorStyles.inspectorTitlebarText;
			Rect position2 = new Rect(position.x + (float)inspectorTitlebar.padding.left, position.y + (float)inspectorTitlebar.padding.top, 16f, 16f);
			Rect position3 = new Rect(position2.xMax + 2f, position2.y, 200f, 16f);
			int controlID2 = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
			toggleValue = EditorGUIInternal.DoToggleForward(position2, controlID2, toggleValue, GUIContent.none, EditorStyles.toggle);
			if (Event.current.type == EventType.Repaint)
			{
				inspectorTitlebar.Draw(position, GUIContent.none, controlID, foldout);
				position = inspectorTitlebar.padding.Remove(position);
				inspectorTitlebarText.Draw(position3, label, controlID, foldout);
			}
			return EditorGUIInternal.DoToggleForward(EditorGUI.IndentedRect(position), controlID, foldout, GUIContent.none, GUIStyle.none);
		}

		internal static bool FoldoutTitlebar(Rect position, GUIContent label, bool foldout)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TitlebarHash, EditorGUIUtility.native, position);
			if (Event.current.type == EventType.Repaint)
			{
				GUIStyle inspectorTitlebar = EditorStyles.inspectorTitlebar;
				GUIStyle inspectorTitlebarText = EditorStyles.inspectorTitlebarText;
				Rect position2 = new Rect(position.x + (float)inspectorTitlebar.padding.left + 2f + 16f, position.y + (float)inspectorTitlebar.padding.top, 200f, 16f);
				inspectorTitlebar.Draw(position, GUIContent.none, controlID, foldout);
				position = inspectorTitlebar.padding.Remove(position);
				inspectorTitlebarText.Draw(position2, EditorGUIUtility.TempContent(label.text), controlID, foldout);
			}
			return EditorGUIInternal.DoToggleForward(EditorGUI.IndentedRect(position), controlID, foldout, GUIContent.none, GUIStyle.none);
		}

		internal static bool HelpIconButton(Rect position, UnityEngine.Object obj)
		{
			bool flag = Unsupported.IsDeveloperBuild();
			bool defaultToMonoBehaviour = !flag || obj.GetType().Assembly.ToString().StartsWith("Assembly-");
			bool flag2 = Help.HasHelpForObject(obj, defaultToMonoBehaviour);
			if (flag2 || flag)
			{
				Color color = GUI.color;
				GUIContent gUIContent = new GUIContent(EditorGUI.GUIContents.helpIcon);
				string niceHelpNameForObject = Help.GetNiceHelpNameForObject(obj, defaultToMonoBehaviour);
				if (flag && !flag2)
				{
					GUI.color = Color.yellow;
					bool flag3 = obj is MonoBehaviour;
					string arg = ((!flag3) ? "sealed partial class-" : "script-") + niceHelpNameForObject;
					gUIContent.tooltip = string.Format("Could not find Reference page for {0} ({1}).\nDocs for this object is missing or all docs are missing.\nThis warning only shows up in development builds.", niceHelpNameForObject, arg);
				}
				else
				{
					gUIContent.tooltip = string.Format("Open Reference for {0}.", niceHelpNameForObject);
				}
				GUIStyle inspectorTitlebarText = EditorStyles.inspectorTitlebarText;
				if (GUI.Button(position, gUIContent, inspectorTitlebarText))
				{
					Help.ShowHelpForObject(obj);
				}
				GUI.color = color;
				return true;
			}
			return false;
		}

		internal static bool FoldoutInternal(Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick, GUIStyle style)
		{
			Rect rect = position;
			if (EditorGUIUtility.hierarchyMode)
			{
				int num = EditorStyles.foldout.padding.left - EditorStyles.label.padding.left;
				position.xMin -= (float)num;
			}
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FoldoutHash, EditorGUIUtility.native, position);
			EventType eventType = Event.current.type;
			if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown || Event.current.rawType == EventType.MouseDrag || Event.current.rawType == EventType.MouseUp))
			{
				eventType = Event.current.rawType;
			}
			EventType eventType2 = eventType;
			switch (eventType2)
			{
			case EventType.MouseDown:
				if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
				{
					int num2 = controlID;
					GUIUtility.hotControl = num2;
					GUIUtility.keyboardControl = num2;
					Event.current.Use();
				}
				return foldout;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
					Event.current.Use();
					Rect rect2 = position;
					if (!toggleOnLabelClick)
					{
						rect2.width = (float)style.padding.left;
						rect2.x += EditorGUI.indent;
					}
					if (rect2.Contains(Event.current.mousePosition))
					{
						GUI.changed = true;
						return !foldout;
					}
				}
				return foldout;
			case EventType.MouseMove:
			case EventType.KeyUp:
			case EventType.ScrollWheel:
			case EventType.Layout:
				IL_D8:
				if (eventType2 != EventType.DragExited)
				{
					return foldout;
				}
				if (EditorGUI.s_DragUpdatedOverID == controlID)
				{
					EditorGUI.s_DragUpdatedOverID = 0;
					Event.current.Use();
				}
				return foldout;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
				{
					Event.current.Use();
				}
				return foldout;
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == controlID)
				{
					KeyCode keyCode = Event.current.keyCode;
					if ((keyCode == KeyCode.LeftArrow && foldout) || (keyCode == KeyCode.RightArrow && !foldout))
					{
						foldout = !foldout;
						GUI.changed = true;
						Event.current.Use();
					}
				}
				return foldout;
			case EventType.Repaint:
			{
				EditorStyles.foldoutSelected.Draw(position, GUIContent.none, controlID, EditorGUI.s_DragUpdatedOverID == controlID);
				Rect position2 = new Rect(position.x + EditorGUI.indent, position.y, EditorGUIUtility.labelWidth - EditorGUI.indent, position.height);
				if (EditorGUI.showMixedValue && !foldout)
				{
					style.Draw(position2, content, controlID, foldout);
					EditorGUI.BeginHandleMixedValueContentColor();
					Rect position3 = rect;
					position3.xMin += EditorGUIUtility.labelWidth;
					EditorStyles.label.Draw(position3, EditorGUI.s_MixedValueContent, controlID, false);
					EditorGUI.EndHandleMixedValueContentColor();
				}
				else
				{
					style.Draw(position2, content, controlID, foldout);
				}
				return foldout;
			}
			case EventType.DragUpdated:
				if (EditorGUI.s_DragUpdatedOverID == controlID)
				{
					if (position.Contains(Event.current.mousePosition))
					{
						if ((double)Time.realtimeSinceStartup > EditorGUI.s_FoldoutDestTime)
						{
							foldout = true;
							Event.current.Use();
						}
					}
					else
					{
						EditorGUI.s_DragUpdatedOverID = 0;
					}
				}
				else if (position.Contains(Event.current.mousePosition))
				{
					EditorGUI.s_DragUpdatedOverID = controlID;
					EditorGUI.s_FoldoutDestTime = (double)Time.realtimeSinceStartup + 0.7;
					Event.current.Use();
				}
				return foldout;
			}
			goto IL_D8;
		}

		public static void ProgressBar(Rect position, float value, string text)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ProgressBarHash, EditorGUIUtility.native, position);
			Event current = Event.current;
			EventType typeForControl = current.GetTypeForControl(controlID);
			if (typeForControl == EventType.Repaint)
			{
				EditorStyles.progressBarBack.Draw(position, false, false, false, false);
				Rect position2 = new Rect(position);
				value = Mathf.Clamp01(value);
				position2.width *= value;
				EditorStyles.progressBarBar.Draw(position2, false, false, false, false);
				EditorStyles.progressBarText.Draw(position, text, false, false, false, false);
			}
		}

		public static void HelpBox(Rect position, string message, MessageType type)
		{
			GUI.Label(position, EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), EditorStyles.helpBox);
		}

		internal static bool LabelHasContent(GUIContent label)
		{
			return label == null || label.text != string.Empty || label.image != null;
		}

		private static void DrawTextDebugHelpers(Rect labelPosition)
		{
			Color color = GUI.color;
			GUI.color = Color.white;
			GUI.DrawTexture(new Rect(labelPosition.x, labelPosition.y, labelPosition.width, 4f), EditorGUIUtility.whiteTexture);
			GUI.color = Color.cyan;
			GUI.DrawTexture(new Rect(labelPosition.x, labelPosition.yMax - 4f, labelPosition.width, 4f), EditorGUIUtility.whiteTexture);
			GUI.color = color;
		}

		internal static void HandlePrefixLabelInternal(Rect totalPosition, Rect labelPosition, GUIContent label, int id, GUIStyle style)
		{
			if (id == 0 && label != null)
			{
				EditorGUI.s_PrefixLabel.text = label.text;
				EditorGUI.s_PrefixLabel.image = label.image;
				EditorGUI.s_PrefixLabel.tooltip = label.tooltip;
				EditorGUI.s_PrefixTotalRect = totalPosition;
				EditorGUI.s_PrefixRect = labelPosition;
				EditorGUI.s_PrefixStyle = style;
				return;
			}
			if (Highlighter.searchMode == HighlightSearchMode.PrefixLabel || Highlighter.searchMode == HighlightSearchMode.Auto)
			{
				Highlighter.Handle(totalPosition, label.text);
			}
			EventType type = Event.current.type;
			if (type != EventType.MouseDown)
			{
				if (type == EventType.Repaint)
				{
					labelPosition.width += 1f;
					style.DrawPrefixLabel(labelPosition, label, id);
				}
			}
			else if (Event.current.button == 0 && labelPosition.Contains(Event.current.mousePosition))
			{
				if (EditorGUIUtility.CanHaveKeyboardFocus(id))
				{
					GUIUtility.keyboardControl = id;
				}
				EditorGUIUtility.editingTextField = false;
				HandleUtility.Repaint();
			}
			EditorGUI.s_PrefixLabel.text = null;
		}

		public static Rect PrefixLabel(Rect totalPosition, GUIContent label)
		{
			return EditorGUI.PrefixLabel(totalPosition, 0, label, EditorStyles.label);
		}

		public static Rect PrefixLabel(Rect totalPosition, GUIContent label, GUIStyle style)
		{
			return EditorGUI.PrefixLabel(totalPosition, 0, label, style);
		}

		public static Rect PrefixLabel(Rect totalPosition, int id, GUIContent label)
		{
			return EditorGUI.PrefixLabel(totalPosition, id, label, EditorStyles.label);
		}

		public static Rect PrefixLabel(Rect totalPosition, int id, GUIContent label, GUIStyle style)
		{
			if (!EditorGUI.LabelHasContent(label))
			{
				return EditorGUI.IndentedRect(totalPosition);
			}
			Rect labelPosition = new Rect(totalPosition.x + EditorGUI.indent, totalPosition.y, EditorGUIUtility.labelWidth - EditorGUI.indent, 16f);
			Rect result = new Rect(totalPosition.x + EditorGUIUtility.labelWidth, totalPosition.y, totalPosition.width - EditorGUIUtility.labelWidth, totalPosition.height);
			EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id, style);
			return result;
		}

		internal static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns)
		{
			if (!EditorGUI.LabelHasContent(label))
			{
				return EditorGUI.IndentedRect(totalPosition);
			}
			if (EditorGUIUtility.wideMode)
			{
				Rect labelPosition = new Rect(totalPosition.x + EditorGUI.indent, totalPosition.y, EditorGUIUtility.labelWidth - EditorGUI.indent, 16f);
				Rect result = totalPosition;
				result.xMin += EditorGUIUtility.labelWidth;
				if (columns > 1)
				{
					labelPosition.width -= 1f;
					result.xMin -= 1f;
				}
				if (columns == 2)
				{
					float num = (result.width - 4f) / 3f;
					result.xMax -= num + 2f;
				}
				EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id);
				return result;
			}
			Rect labelPosition2 = new Rect(totalPosition.x + EditorGUI.indent, totalPosition.y, totalPosition.width - EditorGUI.indent, 16f);
			Rect result2 = totalPosition;
			result2.xMin += EditorGUI.indent + 15f;
			result2.yMin += 16f;
			EditorGUI.HandlePrefixLabel(totalPosition, labelPosition2, label, id);
			return result2;
		}

		public static GUIContent BeginProperty(Rect totalPosition, GUIContent label, SerializedProperty property)
		{
			Highlighter.HighlightIdentifier(totalPosition, property.propertyPath);
			if (EditorGUI.s_PendingPropertyKeyboardHandling != null)
			{
				EditorGUI.DoPropertyFieldKeyboardHandling(EditorGUI.s_PendingPropertyKeyboardHandling);
			}
			EditorGUI.s_PendingPropertyKeyboardHandling = property;
			if (property == null)
			{
				string message = ((label != null) ? (label.text + ": ") : string.Empty) + "SerializedProperty is null";
				EditorGUI.HelpBox(totalPosition, "null", MessageType.Error);
				throw new NullReferenceException(message);
			}
			EditorGUI.s_PropertyFieldTempContent.text = LocalizationDatabase.GetLocalizedString((label != null) ? label.text : property.displayName);
			EditorGUI.s_PropertyFieldTempContent.tooltip = ((!EditorGUI.isCollectingTooltips) ? null : ((label != null) ? label.tooltip : property.tooltip));
			string tooltip = ScriptAttributeUtility.GetHandler(property).tooltip;
			if (tooltip != null)
			{
				EditorGUI.s_PropertyFieldTempContent.tooltip = tooltip;
			}
			EditorGUI.s_PropertyFieldTempContent.image = ((label != null) ? label.image : null);
			if (Event.current.alt && property.serializedObject.inspectorMode != InspectorMode.Normal)
			{
				GUIContent arg_134_0 = EditorGUI.s_PropertyFieldTempContent;
				string propertyPath = property.propertyPath;
				EditorGUI.s_PropertyFieldTempContent.text = propertyPath;
				arg_134_0.tooltip = propertyPath;
			}
			bool boldDefaultFont = EditorGUIUtility.GetBoldDefaultFont();
			if (property.serializedObject.targetObjects.Length == 1 && property.isInstantiatedPrefab)
			{
				EditorGUIUtility.SetBoldDefaultFont(property.prefabOverride);
			}
			EditorGUI.s_PropertyStack.Push(new PropertyGUIData(property, totalPosition, boldDefaultFont, GUI.enabled, GUI.color));
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			if (property.isAnimated)
			{
				Color animatedPropertyColor = AnimationMode.animatedPropertyColor;
				animatedPropertyColor.a *= GUI.color.a;
				GUI.color = animatedPropertyColor;
			}
			GUI.enabled &= property.editable;
			return EditorGUI.s_PropertyFieldTempContent;
		}

		public static void EndProperty()
		{
			EditorGUI.showMixedValue = false;
			PropertyGUIData propertyGUIData = EditorGUI.s_PropertyStack.Pop();
			if (Event.current.type == EventType.ContextClick && propertyGUIData.totalPosition.Contains(Event.current.mousePosition))
			{
				EditorGUI.DoPropertyContextMenu(propertyGUIData.property);
			}
			EditorGUIUtility.SetBoldDefaultFont(propertyGUIData.wasBoldDefaultFont);
			GUI.enabled = propertyGUIData.wasEnabled;
			GUI.color = propertyGUIData.color;
			if (EditorGUI.s_PendingPropertyKeyboardHandling != null)
			{
				EditorGUI.DoPropertyFieldKeyboardHandling(EditorGUI.s_PendingPropertyKeyboardHandling);
			}
			if (EditorGUI.s_PendingPropertyDelete != null && EditorGUI.s_PropertyStack.Count == 0)
			{
				if (EditorGUI.s_PendingPropertyDelete.propertyPath == propertyGUIData.property.propertyPath)
				{
					propertyGUIData.property.DeleteCommand();
				}
				else
				{
					EditorGUI.s_PendingPropertyDelete.DeleteCommand();
				}
				EditorGUI.s_PendingPropertyDelete = null;
			}
		}

		private static void DoPropertyFieldKeyboardHandling(SerializedProperty property)
		{
			if (Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.ValidateCommand)
			{
				if (GUIUtility.keyboardControl == EditorGUIUtility.s_LastControlID && (Event.current.commandName == "Delete" || Event.current.commandName == "SoftDelete"))
				{
					if (Event.current.type == EventType.ExecuteCommand)
					{
						EditorGUI.s_PendingPropertyDelete = property.Copy();
					}
					Event.current.Use();
				}
				if (GUIUtility.keyboardControl == EditorGUIUtility.s_LastControlID && Event.current.commandName == "Duplicate")
				{
					if (Event.current.type == EventType.ExecuteCommand)
					{
						property.DuplicateCommand();
					}
					Event.current.Use();
				}
			}
			EditorGUI.s_PendingPropertyKeyboardHandling = null;
		}

		internal static void LayerMaskField(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.LayerMaskField(position, property, label, EditorStyles.layerMaskField);
		}

		internal static void LayerMaskField(Rect position, SerializedProperty property, GUIContent label, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_LayerMaskField, EditorGUIUtility.native, position);
			position = EditorGUI.PrefixLabel(position, controlID, label);
			Event current = Event.current;
			if (current.type == EventType.Repaint)
			{
				if (EditorGUI.showMixedValue)
				{
					EditorGUI.BeginHandleMixedValueContentColor();
					style.Draw(position, EditorGUI.s_MixedValueContent, controlID, false);
					EditorGUI.EndHandleMixedValueContentColor();
				}
				else
				{
					style.Draw(position, EditorGUIUtility.TempContent(property.layerMaskStringValue), controlID, false);
				}
			}
			else if ((current.type == EventType.MouseDown && position.Contains(current.mousePosition)) || current.MainActionKeyForControl(controlID))
			{
				SerializedProperty userData = property.serializedObject.FindProperty(property.propertyPath);
				EditorUtility.DisplayCustomMenu(position, property.GetLayerMaskNames(), (!property.hasMultipleDifferentValues) ? property.GetLayerMaskSelectedIndex() : new int[0], new EditorUtility.SelectMenuItemFunction(EditorGUI.SetLayerMaskValueDelegate), userData);
				Event.current.Use();
			}
		}

		internal static void SetLayerMaskValueDelegate(object userData, string[] options, int selected)
		{
			SerializedProperty serializedProperty = (SerializedProperty)userData;
			serializedProperty.ToggleLayerMaskAtIndex(selected);
			serializedProperty.serializedObject.ApplyModifiedProperties();
		}

		internal static void ShowRepaints()
		{
			if (Unsupported.IsDeveloperBuild())
			{
				Color backgroundColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
				GUI.Label(new Rect(0f, 0f, 10f, 10f), string.Empty, GUI.skin.button);
				GUI.backgroundColor = backgroundColor;
			}
		}

		internal static void DrawTextureAlphaInternal(Rect position, Texture image, ScaleMode scaleMode, float imageAspect)
		{
			EditorGUI.DrawPreviewTextureInternal(position, image, EditorGUI.alphaMaterial, scaleMode, imageAspect);
		}

		internal static void DrawTextureTransparentInternal(Rect position, Texture image, ScaleMode scaleMode, float imageAspect)
		{
			if (imageAspect == 0f && image == null)
			{
				Debug.LogError("Please specify an image or a imageAspect");
				return;
			}
			if (imageAspect == 0f)
			{
				imageAspect = (float)image.width / (float)image.height;
			}
			EditorGUI.DrawTransparencyCheckerTexture(position, scaleMode, imageAspect);
			if (image != null)
			{
				EditorGUI.DrawPreviewTexture(position, image, EditorGUI.transparentMaterial, scaleMode, imageAspect);
			}
		}

		internal static void DrawTransparencyCheckerTexture(Rect position, ScaleMode scaleMode, float imageAspect)
		{
			Rect position2 = default(Rect);
			Rect rect = default(Rect);
			GUI.CalculateScaledTextureRects(position, scaleMode, imageAspect, ref position2, ref rect);
			GUI.DrawTextureWithTexCoords(position2, EditorGUI.transparentCheckerTexture, new Rect(position2.width * -0.5f / (float)EditorGUI.transparentCheckerTexture.width, position2.height * -0.5f / (float)EditorGUI.transparentCheckerTexture.height, position2.width / (float)EditorGUI.transparentCheckerTexture.width, position2.height / (float)EditorGUI.transparentCheckerTexture.height), false);
		}

		internal static void DrawPreviewTextureInternal(Rect position, Texture image, Material mat, ScaleMode scaleMode, float imageAspect)
		{
			if (Event.current.type == EventType.Repaint)
			{
				if (imageAspect == 0f)
				{
					imageAspect = (float)image.width / (float)image.height;
				}
				if (mat == null)
				{
					mat = EditorGUI.GetMaterialForSpecialTexture(image);
				}
				GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear && !TextureUtil.GetLinearSampled(image));
				if (mat == null)
				{
					GUI.DrawTexture(position, image, scaleMode, false, imageAspect);
					GL.sRGBWrite = false;
					return;
				}
				Rect screenRect = default(Rect);
				Rect sourceRect = default(Rect);
				GUI.CalculateScaledTextureRects(position, scaleMode, imageAspect, ref screenRect, ref sourceRect);
				Texture2D texture2D = image as Texture2D;
				if (texture2D != null && TextureUtil.GetUsageMode(image) == TextureUsageMode.AlwaysPadded)
				{
					sourceRect.width *= (float)texture2D.width / (float)TextureUtil.GetGLWidth(texture2D);
					sourceRect.height *= (float)texture2D.height / (float)TextureUtil.GetGLHeight(texture2D);
				}
				Graphics.DrawTexture(screenRect, image, sourceRect, 0, 0, 0, 0, GUI.color, mat);
				GL.sRGBWrite = false;
			}
		}

		internal static Material GetMaterialForSpecialTexture(Texture t)
		{
			if (!t)
			{
				return null;
			}
			TextureUsageMode usageMode = TextureUtil.GetUsageMode(t);
			if (usageMode == TextureUsageMode.LightmapRGBM || usageMode == TextureUsageMode.RGBMEncoded)
			{
				return EditorGUI.lightmapRGBMMaterial;
			}
			if (usageMode == TextureUsageMode.LightmapDoubleLDR)
			{
				return EditorGUI.lightmapDoubleLDRMaterial;
			}
			if (usageMode == TextureUsageMode.NormalmapPlain || usageMode == TextureUsageMode.NormalmapDXT5nm)
			{
				return EditorGUI.normalmapMaterial;
			}
			if (TextureUtil.IsAlphaOnlyTextureFormat(TextureUtil.GetTextureFormat(t)))
			{
				return EditorGUI.alphaMaterial;
			}
			return null;
		}

		private static void SetExpandedRecurse(SerializedProperty property, bool expanded)
		{
			SerializedProperty serializedProperty = property.Copy();
			serializedProperty.isExpanded = expanded;
			int depth = serializedProperty.depth;
			while (serializedProperty.NextVisible(true) && serializedProperty.depth > depth)
			{
				if (serializedProperty.hasVisibleChildren)
				{
					serializedProperty.isExpanded = expanded;
				}
			}
		}

		internal static float GetSinglePropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property == null)
			{
				return 16f;
			}
			SerializedPropertyType propertyType = property.propertyType;
			if (propertyType == SerializedPropertyType.Vector3 || propertyType == SerializedPropertyType.Vector2)
			{
				return ((EditorGUI.LabelHasContent(label) && !EditorGUIUtility.wideMode) ? 16f : 0f) + 16f;
			}
			if (propertyType == SerializedPropertyType.Rect)
			{
				return ((EditorGUI.LabelHasContent(label) && !EditorGUIUtility.wideMode) ? 16f : 0f) + 32f;
			}
			if (propertyType == SerializedPropertyType.Bounds)
			{
				return (EditorGUI.LabelHasContent(label) ? 16f : 0f) + 32f;
			}
			return 16f;
		}

		internal static float GetPropertyHeightInternal(SerializedProperty property, GUIContent label, bool includeChildren)
		{
			return ScriptAttributeUtility.GetHandler(property).GetHeight(property, label, includeChildren);
		}

		internal static bool HasVisibleChildFields(SerializedProperty property)
		{
			switch (property.propertyType)
			{
			case SerializedPropertyType.Vector2:
			case SerializedPropertyType.Vector3:
			case SerializedPropertyType.Rect:
			case SerializedPropertyType.Bounds:
				return false;
			}
			return property.hasVisibleChildren;
		}

		internal static bool PropertyFieldInternal(Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
		{
			return ScriptAttributeUtility.GetHandler(property).OnGUI(position, property, label, includeChildren);
		}

		internal static bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);
			SerializedPropertyType propertyType = property.propertyType;
			bool flag = false;
			if (!EditorGUI.HasVisibleChildFields(property))
			{
				switch (propertyType)
				{
				case SerializedPropertyType.Integer:
				{
					EditorGUI.BeginChangeCheck();
					long longValue = EditorGUI.LongField(position, label, property.longValue);
					if (EditorGUI.EndChangeCheck())
					{
						property.longValue = longValue;
					}
					break;
				}
				case SerializedPropertyType.Boolean:
				{
					EditorGUI.BeginChangeCheck();
					bool boolValue = EditorGUI.Toggle(position, label, property.boolValue);
					if (EditorGUI.EndChangeCheck())
					{
						property.boolValue = boolValue;
					}
					break;
				}
				case SerializedPropertyType.Float:
				{
					EditorGUI.BeginChangeCheck();
					bool flag2 = property.type == "float";
					double doubleValue = (!flag2) ? EditorGUI.DoubleField(position, label, property.doubleValue) : ((double)EditorGUI.FloatField(position, label, property.floatValue));
					if (EditorGUI.EndChangeCheck())
					{
						property.doubleValue = doubleValue;
					}
					break;
				}
				case SerializedPropertyType.String:
				{
					EditorGUI.BeginChangeCheck();
					string stringValue = EditorGUI.TextField(position, label, property.stringValue);
					if (EditorGUI.EndChangeCheck())
					{
						property.stringValue = stringValue;
					}
					break;
				}
				case SerializedPropertyType.Color:
				{
					EditorGUI.BeginChangeCheck();
					Color colorValue = EditorGUI.ColorField(position, label, property.colorValue);
					if (EditorGUI.EndChangeCheck())
					{
						property.colorValue = colorValue;
					}
					break;
				}
				case SerializedPropertyType.ObjectReference:
					EditorGUI.ObjectFieldInternal(position, property, null, label, EditorStyles.objectField);
					break;
				case SerializedPropertyType.LayerMask:
					EditorGUI.LayerMaskField(position, property, label);
					break;
				case SerializedPropertyType.Enum:
					EditorGUI.Popup(position, property, label);
					break;
				case SerializedPropertyType.Vector2:
					EditorGUI.Vector2Field(position, property, label);
					break;
				case SerializedPropertyType.Vector3:
					EditorGUI.Vector3Field(position, property, label);
					break;
				case SerializedPropertyType.Vector4:
					EditorGUI.Vector4Field(position, property, label);
					break;
				case SerializedPropertyType.Rect:
					EditorGUI.RectField(position, property, label);
					break;
				case SerializedPropertyType.ArraySize:
				{
					EditorGUI.BeginChangeCheck();
					int intValue = EditorGUI.ArraySizeField(position, label, property.intValue, EditorStyles.numberField);
					if (EditorGUI.EndChangeCheck())
					{
						property.intValue = intValue;
					}
					break;
				}
				case SerializedPropertyType.Character:
				{
					char[] value = new char[]
					{
						(char)property.intValue
					};
					bool changed = GUI.changed;
					GUI.changed = false;
					string text = EditorGUI.TextField(position, label, new string(value));
					if (GUI.changed)
					{
						if (text.Length == 1)
						{
							property.intValue = (int)text[0];
						}
						else
						{
							GUI.changed = false;
						}
					}
					GUI.changed |= changed;
					break;
				}
				case SerializedPropertyType.AnimationCurve:
				{
					int controlID = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
					EditorGUI.DoCurveField(EditorGUI.PrefixLabel(position, controlID, label), controlID, null, EditorGUI.kCurveColor, default(Rect), property);
					break;
				}
				case SerializedPropertyType.Bounds:
					EditorGUI.BoundsField(position, property, label);
					break;
				case SerializedPropertyType.Gradient:
				{
					int controlID2 = GUIUtility.GetControlID(EditorGUI.s_CurveHash, EditorGUIUtility.native, position);
					EditorGUI.DoGradientField(EditorGUI.PrefixLabel(position, controlID2, label), controlID2, null, property);
					break;
				}
				default:
				{
					int controlID3 = GUIUtility.GetControlID(EditorGUI.s_GenericField, FocusType.Keyboard, position);
					EditorGUI.PrefixLabel(position, controlID3, label);
					break;
				}
				}
			}
			else
			{
				Event @event = new Event(Event.current);
				flag = property.isExpanded;
				EditorGUI.BeginDisabledGroup(!property.editable);
				GUIStyle style = (DragAndDrop.activeControlID != -10) ? EditorStyles.foldout : EditorStyles.foldoutPreDrop;
				bool flag3 = EditorGUI.Foldout(position, flag, EditorGUI.s_PropertyFieldTempContent, true, style);
				EditorGUI.EndDisabledGroup();
				if (flag3 != flag)
				{
					if (Event.current.alt)
					{
						EditorGUI.SetExpandedRecurse(property, flag3);
					}
					else
					{
						property.isExpanded = flag3;
					}
				}
				flag = flag3;
				int s_LastControlID = EditorGUIUtility.s_LastControlID;
				EventType type = @event.type;
				if (type != EventType.DragUpdated && type != EventType.DragPerform)
				{
					if (type == EventType.DragExited)
					{
						if (GUI.enabled)
						{
							HandleUtility.Repaint();
						}
					}
				}
				else if (position.Contains(@event.mousePosition) && GUI.enabled)
				{
					UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
					UnityEngine.Object[] array = new UnityEngine.Object[1];
					bool flag4 = false;
					UnityEngine.Object[] array2 = objectReferences;
					for (int i = 0; i < array2.Length; i++)
					{
						UnityEngine.Object @object = array2[i];
						array[0] = @object;
						UnityEngine.Object object2 = EditorGUI.ValidateObjectFieldAssignment(array, null, property);
						if (object2 != null)
						{
							DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
							if (@event.type == EventType.DragPerform)
							{
								property.AppendFoldoutPPtrValue(object2);
								flag4 = true;
								DragAndDrop.activeControlID = 0;
							}
							else
							{
								DragAndDrop.activeControlID = s_LastControlID;
							}
						}
					}
					if (flag4)
					{
						GUI.changed = true;
						DragAndDrop.AcceptDrag();
					}
				}
			}
			EditorGUI.EndProperty();
			return flag;
		}

		internal static void DrawLegend(Rect position, Color color, string label, bool enabled)
		{
			position = new Rect(position.x + 2f, position.y + 2f, position.width - 2f, position.height - 2f);
			Color backgroundColor = GUI.backgroundColor;
			if (enabled)
			{
				GUI.backgroundColor = color;
			}
			else
			{
				GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
			}
			GUI.Label(position, label, "ProfilerPaneSubLabel");
			GUI.backgroundColor = backgroundColor;
		}

		internal static string TextFieldDropDown(Rect position, string text, string[] dropDownElement)
		{
			return EditorGUI.TextFieldDropDown(position, GUIContent.none, text, dropDownElement);
		}

		internal static string TextFieldDropDown(Rect position, GUIContent label, string text, string[] dropDownElement)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldDropDownHash, FocusType.Keyboard, position);
			return EditorGUI.DoTextFieldDropDown(EditorGUI.PrefixLabel(position, controlID, label), controlID, text, dropDownElement, false);
		}

		internal static string DelayedTextFieldDropDown(Rect position, string text, string[] dropDownElement)
		{
			return EditorGUI.DelayedTextFieldDropDown(position, GUIContent.none, text, dropDownElement);
		}

		internal static string DelayedTextFieldDropDown(Rect position, GUIContent label, string text, string[] dropDownElement)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldDropDownHash, FocusType.Keyboard, position);
			return EditorGUI.DoTextFieldDropDown(EditorGUI.PrefixLabel(position, controlID, label), controlID, text, dropDownElement, true);
		}

		private static Enum EnumFlagsToInt(Type type, int value)
		{
			return Enum.Parse(type, value.ToString()) as Enum;
		}

		internal static Color ColorBrightnessField(Rect r, GUIContent label, Color value, float minBrightness, float maxBrightness)
		{
			return EditorGUI.ColorBrightnessFieldInternal(r, label, value, minBrightness, maxBrightness, EditorStyles.numberField);
		}

		internal static Color ColorBrightnessFieldInternal(Rect position, GUIContent label, Color value, float minBrightness, float maxBrightness, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, position);
			Rect rect = EditorGUI.PrefixLabel(position, controlID, label);
			position.xMax = rect.x;
			return EditorGUI.DoColorBrightnessField(rect, position, value, minBrightness, maxBrightness, style);
		}

		internal static Color DoColorBrightnessField(Rect rect, Rect dragRect, Color col, float minBrightness, float maxBrightness, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(18975602, FocusType.Keyboard);
			Event current = Event.current;
			switch (current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
				if (current.button == 0 && dragRect.Contains(Event.current.mousePosition) && GUIUtility.hotControl == 0)
				{
					EditorGUI.ColorBrightnessFieldStateObject colorBrightnessFieldStateObject = GUIUtility.GetStateObject(typeof(EditorGUI.ColorBrightnessFieldStateObject), controlID) as EditorGUI.ColorBrightnessFieldStateObject;
					if (colorBrightnessFieldStateObject != null)
					{
						Color.RGBToHSV(col, out colorBrightnessFieldStateObject.m_Hue, out colorBrightnessFieldStateObject.m_Saturation, out colorBrightnessFieldStateObject.m_Brightness);
					}
					GUIUtility.keyboardControl = 0;
					GUIUtility.hotControl = controlID;
					GUI.changed = true;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
					EditorGUIUtility.SetWantsMouseJumping(0);
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
				{
					EditorGUI.ColorBrightnessFieldStateObject colorBrightnessFieldStateObject2 = GUIUtility.QueryStateObject(typeof(EditorGUI.ColorBrightnessFieldStateObject), controlID) as EditorGUI.ColorBrightnessFieldStateObject;
					float maxColorComponent = col.maxColorComponent;
					float num = Mathf.Clamp01(Mathf.Max(1f, Mathf.Pow(Mathf.Abs(maxColorComponent), 0.5f)) * 0.004f);
					float num2 = HandleUtility.niceMouseDelta * num;
					float num3 = Mathf.Clamp(colorBrightnessFieldStateObject2.m_Brightness + num2, minBrightness, maxBrightness);
					colorBrightnessFieldStateObject2.m_Brightness = (float)Math.Round((double)num3, 3);
					col = Color.HSVToRGB(colorBrightnessFieldStateObject2.m_Hue, colorBrightnessFieldStateObject2.m_Saturation, colorBrightnessFieldStateObject2.m_Brightness, maxBrightness > 1f);
					GUIUtility.keyboardControl = 0;
					GUI.changed = true;
					current.Use();
				}
				break;
			case EventType.Repaint:
				if (GUIUtility.hotControl == 0)
				{
					EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SlideArrow);
				}
				break;
			}
			EditorGUI.BeginChangeCheck();
			float value = EditorGUI.DelayedFloatField(rect, col.maxColorComponent, style);
			if (EditorGUI.EndChangeCheck())
			{
				float h;
				float s;
				float num4;
				Color.RGBToHSV(col, out h, out s, out num4);
				float v = Mathf.Clamp(value, minBrightness, maxBrightness);
				col = Color.HSVToRGB(h, s, v, maxBrightness > 1f);
			}
			return col;
		}

		internal static Gradient GradientField(Rect position, Gradient gradient)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_GradientHash, EditorGUIUtility.native, position);
			return EditorGUI.DoGradientField(position, controlID, gradient, null);
		}

		internal static Gradient GradientField(string label, Rect position, Gradient gradient)
		{
			return EditorGUI.GradientField(EditorGUIUtility.TempContent(label), position, gradient);
		}

		internal static Gradient GradientField(GUIContent label, Rect position, Gradient gradient)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_GradientHash, EditorGUIUtility.native, position);
			return EditorGUI.DoGradientField(EditorGUI.PrefixLabel(position, controlID, label), controlID, gradient, null);
		}

		internal static Gradient GradientField(Rect position, SerializedProperty gradient)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_GradientHash, EditorGUIUtility.native, position);
			return EditorGUI.DoGradientField(position, controlID, null, gradient);
		}

		internal static Gradient GradientField(string label, Rect position, SerializedProperty property)
		{
			return EditorGUI.GradientField(EditorGUIUtility.TempContent(label), position, property);
		}

		internal static Gradient GradientField(GUIContent label, Rect position, SerializedProperty property)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_GradientHash, EditorGUIUtility.native, position);
			return EditorGUI.DoGradientField(EditorGUI.PrefixLabel(position, controlID, label), controlID, null, property);
		}

		internal static Gradient DoGradientField(Rect position, int id, Gradient value, SerializedProperty property)
		{
			Event current = Event.current;
			EventType typeForControl = current.GetTypeForControl(id);
			switch (typeForControl)
			{
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == id && (current.keyCode == KeyCode.Space || current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter))
				{
					Event.current.Use();
					Gradient newGradient = (property == null) ? value : property.gradientValue;
					GradientPicker.Show(newGradient);
					GUIUtility.ExitGUI();
				}
				return value;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_28:
				if (typeForControl != EventType.ValidateCommand)
				{
					if (typeForControl != EventType.ExecuteCommand)
					{
						if (typeForControl != EventType.MouseDown)
						{
							return value;
						}
						if (position.Contains(current.mousePosition))
						{
							if (current.button == 0)
							{
								EditorGUI.s_GradientID = id;
								GUIUtility.keyboardControl = id;
								Gradient newGradient2 = (property == null) ? value : property.gradientValue;
								GradientPicker.Show(newGradient2);
								GUIUtility.ExitGUI();
							}
							else if (current.button == 1 && property != null)
							{
								GradientContextMenu.Show(property);
							}
						}
						return value;
					}
					else
					{
						if (EditorGUI.s_GradientID == id && current.commandName == "GradientPickerChanged")
						{
							GUI.changed = true;
							GradientPreviewCache.ClearCache();
							HandleUtility.Repaint();
							if (property != null)
							{
								property.gradientValue = GradientPicker.gradient;
							}
							return GradientPicker.gradient;
						}
						return value;
					}
				}
				else
				{
					if (EditorGUI.s_GradientID == id && current.commandName == "UndoRedoPerformed")
					{
						if (property != null)
						{
							GradientPicker.SetCurrentGradient(property.gradientValue);
						}
						GradientPreviewCache.ClearCache();
						return value;
					}
					return value;
				}
				break;
			case EventType.Repaint:
			{
				Rect position2 = new Rect(position.x + 1f, position.y + 1f, position.width - 2f, position.height - 2f);
				if (property != null)
				{
					GradientEditor.DrawGradientSwatch(position2, property, Color.white);
				}
				else
				{
					GradientEditor.DrawGradientSwatch(position2, value, Color.white);
				}
				EditorStyles.colorPickerBox.Draw(position, GUIContent.none, id);
				return value;
			}
			}
			goto IL_28;
		}

		internal static Color HexColorTextField(Rect rect, GUIContent label, Color color, bool showAlpha)
		{
			return EditorGUI.HexColorTextField(rect, label, color, showAlpha, EditorStyles.textField);
		}

		internal static Color HexColorTextField(Rect rect, GUIContent label, Color color, bool showAlpha, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_FloatFieldHash, FocusType.Keyboard, rect);
			Rect rect2 = EditorGUI.PrefixLabel(rect, controlID, label);
			return EditorGUI.DoHexColorTextField(rect2, color, showAlpha, style);
		}

		internal static Color DoHexColorTextField(Rect rect, Color color, bool showAlpha, GUIStyle style)
		{
			bool flag = false;
			if (color.maxColorComponent > 1f)
			{
				color = color.RGBMultiplied(1f / color.maxColorComponent);
				flag = true;
			}
			Rect position = new Rect(rect.x, rect.y, 14f, rect.height);
			rect.xMin += 14f;
			GUI.Label(position, GUIContent.Temp("#"));
			string text = (!showAlpha) ? ColorUtility.ToHtmlStringRGB(color) : ColorUtility.ToHtmlStringRGBA(color);
			EditorGUI.BeginChangeCheck();
			int controlID = GUIUtility.GetControlID(EditorGUI.s_TextFieldHash, FocusType.Keyboard, rect);
			bool flag2;
			string str = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, controlID, rect, text, style, "0123456789ABCDEFabcdef", out flag2, false, false, false);
			if (EditorGUI.EndChangeCheck())
			{
				EditorGUI.s_RecycledEditor.text = EditorGUI.s_RecycledEditor.text.ToUpper();
				Color color2;
				if (ColorUtility.TryParseHtmlString("#" + str, out color2))
				{
					color = new Color(color2.r, color2.g, color2.b, (!showAlpha) ? color.a : color2.a);
				}
			}
			if (flag)
			{
				EditorGUIUtility.SetIconSize(new Vector2(16f, 16f));
				GUI.Label(new Rect(position.x - 20f, rect.y, 20f, 20f), EditorGUI.s_HDRWarning);
				EditorGUIUtility.SetIconSize(Vector2.zero);
			}
			return color;
		}

		internal static bool ButtonMouseDown(Rect position, GUIContent content, FocusType focusType, GUIStyle style)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_ButtonMouseDownHash, focusType, position);
			return EditorGUI.ButtonMouseDown(controlID, position, content, style);
		}

		internal static bool ButtonMouseDown(int id, Rect position, GUIContent content, GUIStyle style)
		{
			Event current = Event.current;
			EventType type = current.type;
			switch (type)
			{
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == id && current.character == ' ')
				{
					Event.current.Use();
					return true;
				}
				return false;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
				IL_25:
				if (type != EventType.MouseDown)
				{
					return false;
				}
				if (position.Contains(current.mousePosition) && current.button == 0)
				{
					Event.current.Use();
					return true;
				}
				return false;
			case EventType.Repaint:
				if (EditorGUI.showMixedValue)
				{
					EditorGUI.BeginHandleMixedValueContentColor();
					style.Draw(position, EditorGUI.s_MixedValueContent, id, false);
					EditorGUI.EndHandleMixedValueContentColor();
				}
				else
				{
					style.Draw(position, content, id, false);
				}
				return false;
			}
			goto IL_25;
		}

		internal static bool IconButton(int id, Rect position, GUIContent content, GUIStyle style)
		{
			GUIUtility.CheckOnGUI();
			switch (Event.current.GetTypeForControl(id))
			{
			case EventType.MouseDown:
				if (position.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = id;
					Event.current.Use();
					return true;
				}
				return false;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					GUIUtility.hotControl = 0;
					Event.current.Use();
					return position.Contains(Event.current.mousePosition);
				}
				return false;
			case EventType.MouseDrag:
				if (position.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl = id;
					Event.current.Use();
					return true;
				}
				break;
			case EventType.Repaint:
				style.Draw(position, content, id);
				break;
			}
			return false;
		}

		internal static Vector2 MouseDeltaReader(Rect position, bool activated)
		{
			int controlID = GUIUtility.GetControlID(EditorGUI.s_MouseDeltaReaderHash, FocusType.Passive, position);
			Event current = Event.current;
			switch (current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
				if (activated && GUIUtility.hotControl == 0 && position.Contains(current.mousePosition) && current.button == 0)
				{
					GUIUtility.hotControl = controlID;
					GUIUtility.keyboardControl = 0;
					EditorGUI.s_MouseDeltaReaderLastPos = GUIClip.Unclip(current.mousePosition);
					current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID && current.button == 0)
				{
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
				{
					Vector2 a = GUIClip.Unclip(current.mousePosition);
					Vector2 result = a - EditorGUI.s_MouseDeltaReaderLastPos;
					EditorGUI.s_MouseDeltaReaderLastPos = a;
					current.Use();
					return result;
				}
				break;
			}
			return Vector2.zero;
		}

		internal static bool ButtonWithDropdownList(string buttonName, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
		{
			GUIContent content = EditorGUIUtility.TempContent(buttonName);
			return EditorGUI.ButtonWithDropdownList(content, buttonNames, callback, options);
		}

		internal static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
		{
			Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.dropDownList, options);
			Rect rect2 = rect;
			rect2.xMin = rect2.xMax - 20f;
			if (Event.current.type == EventType.MouseDown && rect2.Contains(Event.current.mousePosition))
			{
				GenericMenu genericMenu = new GenericMenu();
				for (int num = 0; num != buttonNames.Length; num++)
				{
					genericMenu.AddItem(new GUIContent(buttonNames[num]), false, callback, num);
				}
				genericMenu.DropDown(rect);
				Event.current.Use();
				return false;
			}
			return GUI.Button(rect, content, EditorStyles.dropDownList);
		}

		internal static void GameViewSizePopup(Rect buttonRect, GameViewSizeGroupType groupType, int selectedIndex, Action<int, object> itemClickedCallback, GUIStyle guiStyle)
		{
			GameViewSizeGroup group = ScriptableSingleton<GameViewSizes>.instance.GetGroup(groupType);
			string t;
			if (selectedIndex >= 0 && selectedIndex < group.GetTotalCount())
			{
				t = group.GetGameViewSize(selectedIndex).displayText;
			}
			else
			{
				t = string.Empty;
			}
			if (EditorGUI.ButtonMouseDown(buttonRect, GUIContent.Temp(t), FocusType.Passive, guiStyle))
			{
				IFlexibleMenuItemProvider itemProvider = new GameViewSizesMenuItemProvider(groupType);
				FlexibleMenu windowContent = new FlexibleMenu(itemProvider, selectedIndex, new GameViewSizesMenuModifyItemUI(), itemClickedCallback);
				PopupWindow.Show(buttonRect, windowContent);
			}
		}

		public static void DrawRect(Rect rect, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			Color color2 = GUI.color;
			GUI.color *= color;
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
			GUI.color = color2;
		}

		internal static float Knob(Rect position, Vector2 knobSize, float currentValue, float start, float end, string unit, Color backgroundColor, Color activeColor, bool showValue, int id)
		{
			EditorGUI.KnobContext knobContext = new EditorGUI.KnobContext(position, knobSize, currentValue, start, end, unit, backgroundColor, activeColor, showValue, id);
			return knobContext.Handle();
		}

		internal static float OffsetKnob(Rect position, float currentValue, float start, float end, float median, string unit, Color backgroundColor, Color activeColor, GUIStyle knob, int id)
		{
			return 0f;
		}

		internal static UnityEngine.Object DoObjectField(Rect position, Rect dropRect, int id, UnityEngine.Object obj, Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidator validator, bool allowSceneObjects)
		{
			return EditorGUI.DoObjectField(position, dropRect, id, obj, objType, property, validator, allowSceneObjects, EditorStyles.objectField);
		}

		internal static UnityEngine.Object DoObjectField(Rect position, Rect dropRect, int id, UnityEngine.Object obj, Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
		{
			if (validator == null)
			{
				validator = new EditorGUI.ObjectFieldValidator(EditorGUI.ValidateObjectFieldAssignment);
			}
			Event current = Event.current;
			EventType eventType = current.type;
			if (!GUI.enabled && GUIClip.enabled && Event.current.rawType == EventType.MouseDown)
			{
				eventType = Event.current.rawType;
			}
			bool flag = EditorGUIUtility.HasObjectThumbnail(objType);
			EditorGUI.ObjectFieldVisualType objectFieldVisualType = EditorGUI.ObjectFieldVisualType.IconAndText;
			if (flag && position.height <= 18f && position.width <= 32f)
			{
				objectFieldVisualType = EditorGUI.ObjectFieldVisualType.MiniPreivew;
			}
			else if (flag && position.height > 16f)
			{
				objectFieldVisualType = EditorGUI.ObjectFieldVisualType.LargePreview;
			}
			Vector2 iconSize = EditorGUIUtility.GetIconSize();
			if (objectFieldVisualType == EditorGUI.ObjectFieldVisualType.IconAndText)
			{
				EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));
			}
			else if (objectFieldVisualType == EditorGUI.ObjectFieldVisualType.LargePreview)
			{
				EditorGUIUtility.SetIconSize(new Vector2(64f, 64f));
			}
			EventType eventType2 = eventType;
			switch (eventType2)
			{
			case EventType.KeyDown:
				if (GUIUtility.keyboardControl == id)
				{
					if (current.keyCode == KeyCode.Backspace || current.keyCode == KeyCode.Delete)
					{
						if (property != null)
						{
							property.objectReferenceValue = null;
						}
						else
						{
							obj = null;
						}
						GUI.changed = true;
						current.Use();
					}
					if (current.MainActionKeyForControl(id))
					{
						ObjectSelector.get.Show(obj, objType, property, allowSceneObjects);
						ObjectSelector.get.objectSelectorID = id;
						current.Use();
						GUIUtility.ExitGUI();
					}
				}
				goto IL_60B;
			case EventType.KeyUp:
			case EventType.ScrollWheel:
			case EventType.Layout:
			case EventType.Ignore:
			case EventType.Used:
			case EventType.ValidateCommand:
				IL_11F:
				if (eventType2 != EventType.MouseDown)
				{
					goto IL_60B;
				}
				if (Event.current.button != 0)
				{
					goto IL_60B;
				}
				if (position.Contains(Event.current.mousePosition))
				{
					Rect rect;
					switch (objectFieldVisualType)
					{
					case EditorGUI.ObjectFieldVisualType.IconAndText:
						rect = new Rect(position.xMax - 15f, position.y, 15f, position.height);
						break;
					case EditorGUI.ObjectFieldVisualType.LargePreview:
						rect = new Rect(position.xMax - 36f, position.yMax - 14f, 36f, 14f);
						break;
					case EditorGUI.ObjectFieldVisualType.MiniPreivew:
						rect = new Rect(position.xMax - 15f, position.y, 15f, position.height);
						break;
					default:
						throw new ArgumentOutOfRangeException();
					}
					EditorGUIUtility.editingTextField = false;
					if (rect.Contains(Event.current.mousePosition))
					{
						if (GUI.enabled)
						{
							GUIUtility.keyboardControl = id;
							ObjectSelector.get.Show(obj, objType, property, allowSceneObjects);
							ObjectSelector.get.objectSelectorID = id;
							current.Use();
							GUIUtility.ExitGUI();
						}
					}
					else
					{
						UnityEngine.Object @object = (property == null) ? obj : property.objectReferenceValue;
						Component component = @object as Component;
						if (component)
						{
							@object = component.gameObject;
						}
						if (EditorGUI.showMixedValue)
						{
							@object = null;
						}
						if (Event.current.clickCount == 1)
						{
							GUIUtility.keyboardControl = id;
							if (@object)
							{
								bool flag2 = current.shift || current.control;
								if (!flag2)
								{
									EditorGUIUtility.PingObject(@object);
								}
								if (flag2 && @object is Texture)
								{
									PopupWindowWithoutFocus.Show(new RectOffset(6, 3, 0, 3).Add(position), new ObjectPreviewPopup(@object), new PopupLocationHelper.PopupLocation[]
									{
										PopupLocationHelper.PopupLocation.Left,
										PopupLocationHelper.PopupLocation.Below,
										PopupLocationHelper.PopupLocation.Right
									});
								}
							}
							current.Use();
						}
						else if (Event.current.clickCount == 2)
						{
							if (@object)
							{
								AssetDatabase.OpenAsset(@object);
								GUIUtility.ExitGUI();
							}
							current.Use();
						}
					}
				}
				goto IL_60B;
			case EventType.Repaint:
			{
				GUIContent content;
				if (EditorGUI.showMixedValue)
				{
					content = EditorGUI.s_MixedValueContent;
				}
				else if (property != null)
				{
					content = EditorGUIUtility.TempContent(property.objectReferenceStringValue, AssetPreview.GetMiniThumbnail(property.objectReferenceValue));
					obj = property.objectReferenceValue;
					if (obj != null)
					{
						UnityEngine.Object[] references = new UnityEngine.Object[]
						{
							obj
						};
						if (validator(references, objType, property) == null)
						{
							content = EditorGUIUtility.TempContent("Type mismatch");
						}
					}
				}
				else
				{
					content = EditorGUIUtility.ObjectContent(obj, objType);
				}
				switch (objectFieldVisualType)
				{
				case EditorGUI.ObjectFieldVisualType.IconAndText:
					EditorGUI.BeginHandleMixedValueContentColor();
					style.Draw(position, content, id, DragAndDrop.activeControlID == id);
					EditorGUI.EndHandleMixedValueContentColor();
					break;
				case EditorGUI.ObjectFieldVisualType.LargePreview:
					EditorGUI.DrawObjectFieldLargeThumb(position, id, obj, content);
					break;
				case EditorGUI.ObjectFieldVisualType.MiniPreivew:
					EditorGUI.DrawObjectFieldMiniThumb(position, id, obj, content);
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
				goto IL_60B;
			}
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
				{
					UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
					UnityEngine.Object object2 = validator(objectReferences, objType, property);
					if (object2 != null && !allowSceneObjects && !EditorUtility.IsPersistent(object2))
					{
						object2 = null;
					}
					if (object2 != null)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						if (eventType == EventType.DragPerform)
						{
							if (property != null)
							{
								property.objectReferenceValue = object2;
							}
							else
							{
								obj = object2;
							}
							GUI.changed = true;
							DragAndDrop.AcceptDrag();
							DragAndDrop.activeControlID = 0;
						}
						else
						{
							DragAndDrop.activeControlID = id;
						}
						Event.current.Use();
					}
				}
				goto IL_60B;
			case EventType.ExecuteCommand:
			{
				string commandName = current.commandName;
				if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id)
				{
					UnityEngine.Object[] references2 = new UnityEngine.Object[]
					{
						ObjectSelector.GetCurrentObject()
					};
					UnityEngine.Object object3 = validator(references2, objType, property);
					if (property != null)
					{
						property.objectReferenceValue = object3;
					}
					GUI.changed = true;
					current.Use();
					return object3;
				}
				goto IL_60B;
			}
			case EventType.DragExited:
				if (GUI.enabled)
				{
					HandleUtility.Repaint();
				}
				goto IL_60B;
			}
			goto IL_11F;
			IL_60B:
			EditorGUIUtility.SetIconSize(iconSize);
			return obj;
		}

		private static void DrawObjectFieldLargeThumb(Rect position, int id, UnityEngine.Object obj, GUIContent content)
		{
			GUIStyle objectFieldThumb = EditorStyles.objectFieldThumb;
			objectFieldThumb.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
			if (obj != null && !EditorGUI.showMixedValue)
			{
				bool flag = obj is Cubemap;
				bool flag2 = obj is Sprite;
				Rect position2 = objectFieldThumb.padding.Remove(position);
				if (flag || flag2)
				{
					Texture2D assetPreview = AssetPreview.GetAssetPreview(obj);
					if (assetPreview != null)
					{
						if (flag2 || assetPreview.alphaIsTransparency)
						{
							EditorGUI.DrawTextureTransparent(position2, assetPreview);
						}
						else
						{
							EditorGUI.DrawPreviewTexture(position2, assetPreview);
						}
					}
					else
					{
						position2.x += (position2.width - (float)content.image.width) / 2f;
						position2.y += (position2.height - (float)content.image.width) / 2f;
						GUIStyle.none.Draw(position2, content.image, false, false, false, false);
						HandleUtility.Repaint();
					}
				}
				else
				{
					Texture2D texture2D = content.image as Texture2D;
					if (texture2D != null && texture2D.alphaIsTransparency)
					{
						EditorGUI.DrawTextureTransparent(position2, texture2D);
					}
					else
					{
						EditorGUI.DrawPreviewTexture(position2, content.image);
					}
				}
			}
			else
			{
				GUIStyle gUIStyle = objectFieldThumb.name + "Overlay";
				EditorGUI.BeginHandleMixedValueContentColor();
				gUIStyle.Draw(position, content, id);
				EditorGUI.EndHandleMixedValueContentColor();
			}
			GUIStyle gUIStyle2 = objectFieldThumb.name + "Overlay2";
			gUIStyle2.Draw(position, EditorGUIUtility.TempContent("Select"), id);
		}

		private static void DrawObjectFieldMiniThumb(Rect position, int id, UnityEngine.Object obj, GUIContent content)
		{
			GUIStyle objectFieldMiniThumb = EditorStyles.objectFieldMiniThumb;
			position.width = 32f;
			EditorGUI.BeginHandleMixedValueContentColor();
			bool isHover = obj != null;
			bool on = DragAndDrop.activeControlID == id;
			bool hasKeyboardFocus = GUIUtility.keyboardControl == id;
			objectFieldMiniThumb.Draw(position, isHover, false, on, hasKeyboardFocus);
			EditorGUI.EndHandleMixedValueContentColor();
			if (obj != null && !EditorGUI.showMixedValue)
			{
				Rect position2 = new Rect(position.x + 1f, position.y + 1f, position.height - 2f, position.height - 2f);
				Texture2D texture2D = content.image as Texture2D;
				if (texture2D != null && texture2D.alphaIsTransparency)
				{
					EditorGUI.DrawTextureTransparent(position2, texture2D);
				}
				else
				{
					EditorGUI.DrawPreviewTexture(position2, content.image);
				}
				if (position2.Contains(Event.current.mousePosition))
				{
					GUI.Label(position2, GUIContent.Temp(string.Empty, "Ctrl + Click to show preview"));
				}
			}
		}

		internal static UnityEngine.Object DoDropField(Rect position, int id, Type objType, EditorGUI.ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
		{
			if (validator == null)
			{
				validator = new EditorGUI.ObjectFieldValidator(EditorGUI.ValidateObjectFieldAssignment);
			}
			Event current = Event.current;
			EventType eventType = current.type;
			if (!GUI.enabled && GUIClip.enabled && Event.current.rawType == EventType.MouseDown)
			{
				eventType = Event.current.rawType;
			}
			EventType eventType2 = eventType;
			switch (eventType2)
			{
			case EventType.Repaint:
				style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
				goto IL_144;
			case EventType.Layout:
				IL_6B:
				if (eventType2 != EventType.DragExited)
				{
					goto IL_144;
				}
				if (GUI.enabled)
				{
					HandleUtility.Repaint();
				}
				goto IL_144;
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if (position.Contains(Event.current.mousePosition) && GUI.enabled)
				{
					UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
					UnityEngine.Object @object = validator(objectReferences, objType, null);
					if (@object != null && !allowSceneObjects && !EditorUtility.IsPersistent(@object))
					{
						@object = null;
					}
					if (@object != null)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						if (eventType == EventType.DragPerform)
						{
							GUI.changed = true;
							DragAndDrop.AcceptDrag();
							DragAndDrop.activeControlID = 0;
							Event.current.Use();
							return @object;
						}
						DragAndDrop.activeControlID = id;
						Event.current.Use();
					}
				}
				goto IL_144;
			}
			goto IL_6B;
			IL_144:
			return null;
		}

		internal static void TargetChoiceField(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.TargetChoiceField(position, property, label, new TargetChoiceHandler.TargetChoiceMenuFunction(TargetChoiceHandler.SetToValueOfTarget));
		}

		internal static void TargetChoiceField(Rect position, SerializedProperty property, GUIContent label, TargetChoiceHandler.TargetChoiceMenuFunction func)
		{
			EditorGUI.BeginProperty(position, label, property);
			position = EditorGUI.PrefixLabel(position, 0, label);
			EditorGUI.BeginHandleMixedValueContentColor();
			if (GUI.Button(position, EditorGUI.mixedValueContent, EditorStyles.popup))
			{
				GenericMenu genericMenu = new GenericMenu();
				TargetChoiceHandler.AddSetToValueOfTargetMenuItems(genericMenu, property, func);
				genericMenu.DropDown(position);
			}
			EditorGUI.EndHandleMixedValueContentColor();
			EditorGUI.EndProperty();
		}

		internal static string DoTextFieldDropDown(Rect rect, int id, string text, string[] dropDownElements, bool delayed)
		{
			Rect position = new Rect(rect.x, rect.y, rect.width - EditorStyles.textFieldDropDown.fixedWidth, rect.height);
			Rect rect2 = new Rect(position.xMax, position.y, EditorStyles.textFieldDropDown.fixedWidth, rect.height);
			if (delayed)
			{
				text = EditorGUI.DelayedTextField(position, text, EditorStyles.textFieldDropDownText);
			}
			else
			{
				bool flag;
				text = EditorGUI.DoTextField(EditorGUI.s_RecycledEditor, id, position, text, EditorStyles.textFieldDropDownText, null, out flag, false, false, false);
			}
			EditorGUI.BeginChangeCheck();
			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			Rect arg_C2_0 = rect2;
			string arg_C2_1 = string.Empty;
			int arg_C2_2 = -1;
			string[] arg_C2_3;
			if (dropDownElements.Length > 0)
			{
				arg_C2_3 = dropDownElements;
			}
			else
			{
				(arg_C2_3 = new string[1])[0] = "--empty--";
			}
			int num = EditorGUI.Popup(arg_C2_0, arg_C2_1, arg_C2_2, arg_C2_3, EditorStyles.textFieldDropDown);
			if (EditorGUI.EndChangeCheck() && dropDownElements.Length > 0)
			{
				text = dropDownElements[num];
			}
			EditorGUI.indentLevel = indentLevel;
			return text;
		}
	}
}
