using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
	internal class RendererEditorBase : Editor
	{
		internal class Probes
		{
			private SerializedProperty m_UseLightProbes;

			private SerializedProperty m_ReflectionProbeUsage;

			private SerializedProperty m_ProbeAnchor;

			private SerializedProperty m_ReceiveShadows;

			private GUIContent m_UseLightProbesStyle = EditorGUIUtility.TextContent("Use Light Probes");

			private GUIContent m_ReflectionProbeUsageStyle = EditorGUIUtility.TextContent("Reflection Probes");

			private GUIContent m_ProbeAnchorStyle = EditorGUIUtility.TextContent("Anchor Override|If set, the Renderer will use this Transform's position to sample light probes and find the matching reflection probe.");

			private GUIContent m_DeferredNote = EditorGUIUtility.TextContent("In Deferred Shading, all objects receive shadows and get per-pixel reflection probes.");

			private string[] m_ReflectionProbeUsageNames = (from x in Enum.GetNames(typeof(ReflectionProbeUsage))
			select ObjectNames.NicifyVariableName(x)).ToArray<string>();

			private GUIContent[] m_ReflectionProbeUsageOptions = (from x in (from x in Enum.GetNames(typeof(ReflectionProbeUsage))
			select ObjectNames.NicifyVariableName(x)).ToArray<string>()
			select new GUIContent(x)).ToArray<GUIContent>();

			private List<ReflectionProbeBlendInfo> m_BlendInfo = new List<ReflectionProbeBlendInfo>();

			internal void Initialize(SerializedObject serializedObject, bool initializeLightProbes)
			{
				if (initializeLightProbes)
				{
					this.m_UseLightProbes = serializedObject.FindProperty("m_UseLightProbes");
				}
				this.m_ReflectionProbeUsage = serializedObject.FindProperty("m_ReflectionProbeUsage");
				this.m_ProbeAnchor = serializedObject.FindProperty("m_ProbeAnchor");
				this.m_ReceiveShadows = serializedObject.FindProperty("m_ReceiveShadows");
			}

			internal void Initialize(SerializedObject serializedObject)
			{
				this.Initialize(serializedObject, true);
			}

			internal void OnGUI(UnityEngine.Object[] targets, Renderer renderer, bool useMiniStyle)
			{
				bool flag = SceneView.IsUsingDeferredRenderingPath();
				bool flag2 = flag && UnityEngine.Rendering.GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled;
				bool disabled = false;
				if (targets != null)
				{
					for (int i = 0; i < targets.Length; i++)
					{
						UnityEngine.Object @object = targets[i];
						if (LightmapEditorSettings.IsLightmappedOrDynamicLightmappedForRendering((Renderer)@object))
						{
							disabled = true;
							break;
						}
					}
				}
				if (this.m_UseLightProbes != null)
				{
					EditorGUI.BeginDisabledGroup(disabled);
					if (!useMiniStyle)
					{
						EditorGUILayout.PropertyField(this.m_UseLightProbes, this.m_UseLightProbesStyle, new GUILayoutOption[0]);
					}
					else
					{
						ModuleUI.GUIToggle(this.m_UseLightProbesStyle, this.m_UseLightProbes);
					}
					EditorGUI.EndDisabledGroup();
				}
				EditorGUI.BeginDisabledGroup(flag);
				if (!useMiniStyle)
				{
					if (flag2)
					{
						EditorGUILayout.EnumPopup(this.m_ReflectionProbeUsageStyle, (this.m_ReflectionProbeUsage.intValue == 0) ? ReflectionProbeUsage.Off : ReflectionProbeUsage.Simple, new GUILayoutOption[0]);
					}
					else
					{
						EditorGUILayout.Popup(this.m_ReflectionProbeUsage, this.m_ReflectionProbeUsageOptions, this.m_ReflectionProbeUsageStyle, new GUILayoutOption[0]);
					}
				}
				else if (flag2)
				{
					ModuleUI.GUIPopup(this.m_ReflectionProbeUsageStyle, 3, this.m_ReflectionProbeUsageNames);
				}
				else
				{
					ModuleUI.GUIPopup(this.m_ReflectionProbeUsageStyle, this.m_ReflectionProbeUsage, this.m_ReflectionProbeUsageNames);
				}
				EditorGUI.EndDisabledGroup();
				bool flag3 = !this.m_ReflectionProbeUsage.hasMultipleDifferentValues && this.m_ReflectionProbeUsage.intValue != 0;
				bool flag4 = this.m_UseLightProbes != null && !this.m_UseLightProbes.hasMultipleDifferentValues && this.m_UseLightProbes.boolValue;
				bool flag5 = flag3 || flag4;
				if (flag5)
				{
					if (!useMiniStyle)
					{
						EditorGUILayout.PropertyField(this.m_ProbeAnchor, this.m_ProbeAnchorStyle, new GUILayoutOption[0]);
					}
					else
					{
						ModuleUI.GUIObject(this.m_ProbeAnchorStyle, this.m_ProbeAnchor);
					}
					if (!flag2)
					{
						renderer.GetClosestReflectionProbes(this.m_BlendInfo);
						RendererEditorBase.Probes.ShowClosestReflectionProbes(this.m_BlendInfo);
					}
				}
				bool flag6 = !this.m_ReceiveShadows.hasMultipleDifferentValues && this.m_ReceiveShadows.boolValue;
				if ((flag && flag6) || (flag2 && flag5))
				{
					EditorGUILayout.HelpBox(this.m_DeferredNote.text, MessageType.Info);
				}
			}

			internal static void ShowClosestReflectionProbes(List<ReflectionProbeBlendInfo> blendInfos)
			{
				float num = 20f;
				float num2 = 60f;
				EditorGUI.BeginDisabledGroup(true);
				for (int i = 0; i < blendInfos.Count; i++)
				{
					Rect rect = GUILayoutUtility.GetRect(0f, 16f);
					rect = EditorGUI.IndentedRect(rect);
					float width = rect.width - num - num2;
					Rect position = rect;
					position.width = num;
					GUI.Label(position, "#" + i, EditorStyles.miniLabel);
					position.x += position.width;
					position.width = width;
					EditorGUI.ObjectField(position, blendInfos[i].probe, typeof(ReflectionProbe), true);
					position.x += position.width;
					position.width = num2;
					GUI.Label(position, "Weight " + blendInfos[i].weight.ToString("f2"), EditorStyles.miniLabel);
				}
				EditorGUI.EndDisabledGroup();
			}

			internal static string[] GetFieldsStringArray()
			{
				return new string[]
				{
					"m_UseLightProbes",
					"m_ReflectionProbeUsage",
					"m_ProbeAnchor"
				};
			}
		}

		private SerializedProperty m_SortingOrder;

		private SerializedProperty m_SortingLayerID;

		private GUIContent m_SortingLayerStyle = EditorGUIUtility.TextContent("Sorting Layer");

		private GUIContent m_SortingOrderStyle = EditorGUIUtility.TextContent("Order in Layer");

		protected RendererEditorBase.Probes m_Probes;

		public virtual void OnEnable()
		{
			this.m_SortingOrder = base.serializedObject.FindProperty("m_SortingOrder");
			this.m_SortingLayerID = base.serializedObject.FindProperty("m_SortingLayerID");
		}

		protected void RenderSortingLayerFields()
		{
			EditorGUILayout.Space();
			EditorGUILayout.SortingLayerField(this.m_SortingLayerStyle, this.m_SortingLayerID, EditorStyles.popup, EditorStyles.label);
			EditorGUILayout.PropertyField(this.m_SortingOrder, this.m_SortingOrderStyle, new GUILayoutOption[0]);
		}

		protected void InitializeProbeFields()
		{
			this.m_Probes = new RendererEditorBase.Probes();
			this.m_Probes.Initialize(base.serializedObject);
		}

		protected void RenderProbeFields()
		{
			this.m_Probes.OnGUI(base.targets, (Renderer)this.target, false);
		}
	}
}
