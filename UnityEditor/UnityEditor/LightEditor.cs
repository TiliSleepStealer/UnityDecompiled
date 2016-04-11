using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(Light))]
	internal class LightEditor : Editor
	{
		private class Styles
		{
			public readonly GUIContent LightBounceIntensity = EditorGUIUtility.TextContent("Bounce Intensity|Indirect light intensity multiplier.");

			public readonly GUIContent ShadowType = EditorGUIUtility.TextContent("Shadow Type|Shadow cast options");

			public readonly GUIContent BakedShadowRadius = EditorGUIUtility.TextContent("Baked Shadow Radius");

			public readonly GUIContent BakedShadowAngle = EditorGUIUtility.TextContent("Baked Shadow Angle");

			public readonly GUIContent LightmappingModeLabel = EditorGUIUtility.TextContent("Baking");

			public readonly GUIContent[] LightmappingModes = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Realtime"),
				EditorGUIUtility.TextContent("Baked"),
				EditorGUIUtility.TextContent("Mixed")
			};

			public readonly int[] LightmappingModeValues = new int[]
			{
				4,
				2,
				1
			};
		}

		private SerializedProperty m_Type;

		private SerializedProperty m_Range;

		private SerializedProperty m_SpotAngle;

		private SerializedProperty m_CookieSize;

		private SerializedProperty m_Color;

		private SerializedProperty m_Intensity;

		private SerializedProperty m_BounceIntensity;

		private SerializedProperty m_Cookie;

		private SerializedProperty m_ShadowsType;

		private SerializedProperty m_ShadowsStrength;

		private SerializedProperty m_ShadowsResolution;

		private SerializedProperty m_ShadowsBias;

		private SerializedProperty m_ShadowsNormalBias;

		private SerializedProperty m_ShadowsNearPlane;

		private SerializedProperty m_Halo;

		private SerializedProperty m_Flare;

		private SerializedProperty m_RenderMode;

		private SerializedProperty m_CullingMask;

		private SerializedProperty m_Lightmapping;

		private SerializedProperty m_AreaSizeX;

		private SerializedProperty m_AreaSizeY;

		private SerializedProperty m_BakedShadowRadius;

		private SerializedProperty m_BakedShadowAngle;

		private AnimBool m_ShowSpotOptions = new AnimBool();

		private AnimBool m_ShowPointOptions = new AnimBool();

		private AnimBool m_ShowDirOptions = new AnimBool();

		private AnimBool m_ShowAreaOptions = new AnimBool();

		private AnimBool m_ShowRuntimeOptions = new AnimBool();

		private AnimBool m_ShowShadowOptions = new AnimBool();

		private AnimBool m_ShowIndirectWarning = new AnimBool();

		private AnimBool m_ShowBakingWarning = new AnimBool();

		private AnimBool m_BakedShadowAngleOptions = new AnimBool();

		private AnimBool m_BakedShadowRadiusOptions = new AnimBool();

		private static LightEditor.Styles s_Styles;

		internal static Color kGizmoLight = new Color(0.996078432f, 0.992156863f, 0.533333361f, 0.5019608f);

		internal static Color kGizmoDisabledLight = new Color(0.5294118f, 0.454901963f, 0.196078435f, 0.5019608f);

		private bool typeIsSame
		{
			get
			{
				return !this.m_Type.hasMultipleDifferentValues;
			}
		}

		private bool shadowTypeIsSame
		{
			get
			{
				return !this.m_ShadowsType.hasMultipleDifferentValues;
			}
		}

		private Light light
		{
			get
			{
				return this.target as Light;
			}
		}

		private bool isBakedOrMixed
		{
			get
			{
				return this.m_Lightmapping.intValue != 4;
			}
		}

		private bool spotOptionsValue
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Spot;
			}
		}

		private bool pointOptionsValue
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Point;
			}
		}

		private bool dirOptionsValue
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Directional;
			}
		}

		private bool areaOptionsValue
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Area;
			}
		}

		private bool runtimeOptionsValue
		{
			get
			{
				return this.typeIsSame && this.light.type != LightType.Area && this.m_Lightmapping.intValue != 2;
			}
		}

		private bool bakedShadowRadius
		{
			get
			{
				return this.typeIsSame && (this.light.type == LightType.Point || this.light.type == LightType.Spot) && this.isBakedOrMixed;
			}
		}

		private bool bakedShadowAngle
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Directional && this.isBakedOrMixed;
			}
		}

		private bool shadowOptionsValue
		{
			get
			{
				return this.shadowTypeIsSame && this.light.shadows != LightShadows.None;
			}
		}

		private bool areaWarningValue
		{
			get
			{
				return this.typeIsSame && this.light.type == LightType.Area;
			}
		}

		private bool bounceWarningValue
		{
			get
			{
				return this.typeIsSame && (this.light.type == LightType.Point || this.light.type == LightType.Spot) && this.m_Lightmapping.intValue == 4 && this.m_BounceIntensity.floatValue > 0f;
			}
		}

		private bool bakingWarningValue
		{
			get
			{
				return !Lightmapping.bakedLightmapsEnabled && this.isBakedOrMixed;
			}
		}

		private void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
		{
			if (initialize)
			{
				animBool.value = targetValue;
				animBool.valueChanged.AddListener(new UnityAction(base.Repaint));
			}
			else
			{
				animBool.target = targetValue;
			}
		}

		private void UpdateShowOptions(bool initialize)
		{
			this.SetOptions(this.m_ShowSpotOptions, initialize, this.spotOptionsValue);
			this.SetOptions(this.m_ShowPointOptions, initialize, this.pointOptionsValue);
			this.SetOptions(this.m_ShowDirOptions, initialize, this.dirOptionsValue);
			this.SetOptions(this.m_ShowAreaOptions, initialize, this.areaOptionsValue);
			this.SetOptions(this.m_ShowShadowOptions, initialize, this.shadowOptionsValue);
			this.SetOptions(this.m_ShowIndirectWarning, initialize, this.bounceWarningValue);
			this.SetOptions(this.m_ShowBakingWarning, initialize, this.bakingWarningValue);
			this.SetOptions(this.m_ShowRuntimeOptions, initialize, this.runtimeOptionsValue);
			this.SetOptions(this.m_BakedShadowAngleOptions, initialize, this.bakedShadowAngle);
			this.SetOptions(this.m_BakedShadowRadiusOptions, initialize, this.bakedShadowRadius);
		}

		private void OnEnable()
		{
			this.m_Type = base.serializedObject.FindProperty("m_Type");
			this.m_Range = base.serializedObject.FindProperty("m_Range");
			this.m_SpotAngle = base.serializedObject.FindProperty("m_SpotAngle");
			this.m_CookieSize = base.serializedObject.FindProperty("m_CookieSize");
			this.m_Color = base.serializedObject.FindProperty("m_Color");
			this.m_Intensity = base.serializedObject.FindProperty("m_Intensity");
			this.m_BounceIntensity = base.serializedObject.FindProperty("m_BounceIntensity");
			this.m_Cookie = base.serializedObject.FindProperty("m_Cookie");
			this.m_ShadowsType = base.serializedObject.FindProperty("m_Shadows.m_Type");
			this.m_ShadowsStrength = base.serializedObject.FindProperty("m_Shadows.m_Strength");
			this.m_ShadowsResolution = base.serializedObject.FindProperty("m_Shadows.m_Resolution");
			this.m_ShadowsBias = base.serializedObject.FindProperty("m_Shadows.m_Bias");
			this.m_ShadowsNormalBias = base.serializedObject.FindProperty("m_Shadows.m_NormalBias");
			this.m_ShadowsNearPlane = base.serializedObject.FindProperty("m_Shadows.m_NearPlane");
			this.m_Halo = base.serializedObject.FindProperty("m_DrawHalo");
			this.m_Flare = base.serializedObject.FindProperty("m_Flare");
			this.m_RenderMode = base.serializedObject.FindProperty("m_RenderMode");
			this.m_CullingMask = base.serializedObject.FindProperty("m_CullingMask");
			this.m_Lightmapping = base.serializedObject.FindProperty("m_Lightmapping");
			this.m_AreaSizeX = base.serializedObject.FindProperty("m_AreaSize.x");
			this.m_AreaSizeY = base.serializedObject.FindProperty("m_AreaSize.y");
			this.m_BakedShadowRadius = base.serializedObject.FindProperty("m_ShadowRadius");
			this.m_BakedShadowAngle = base.serializedObject.FindProperty("m_ShadowAngle");
			this.UpdateShowOptions(true);
		}

		public override void OnInspectorGUI()
		{
			if (LightEditor.s_Styles == null)
			{
				LightEditor.s_Styles = new LightEditor.Styles();
			}
			base.serializedObject.Update();
			this.UpdateShowOptions(false);
			EditorGUILayout.PropertyField(this.m_Type, new GUILayoutOption[0]);
			if (EditorGUILayout.BeginFadeGroup(1f - this.m_ShowAreaOptions.faded))
			{
				EditorGUILayout.IntPopup(this.m_Lightmapping, LightEditor.s_Styles.LightmappingModes, LightEditor.s_Styles.LightmappingModeValues, LightEditor.s_Styles.LightmappingModeLabel, new GUILayoutOption[0]);
				if (EditorGUILayout.BeginFadeGroup(this.m_ShowBakingWarning.faded))
				{
					GUIContent gUIContent = EditorGUIUtility.TextContent("Enable Baked GI from Lighting window to use Baked or Mixed.");
					EditorGUILayout.HelpBox(gUIContent.text, MessageType.Warning, false);
				}
				EditorGUILayout.EndFadeGroup();
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.Space();
			bool flag = this.m_ShowDirOptions.isAnimating && this.m_ShowAreaOptions.isAnimating && (this.m_ShowDirOptions.target || this.m_ShowAreaOptions.target);
			float value = (!flag) ? (1f - Mathf.Max(this.m_ShowDirOptions.faded, this.m_ShowAreaOptions.faded)) : 0f;
			if (EditorGUILayout.BeginFadeGroup(value))
			{
				EditorGUILayout.PropertyField(this.m_Range, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowSpotOptions.faded))
			{
				EditorGUILayout.Slider(this.m_SpotAngle, 1f, 179f, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowAreaOptions.faded))
			{
				EditorGUILayout.PropertyField(this.m_AreaSizeX, EditorGUIUtility.TextContent("Width"), new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(this.m_AreaSizeY, EditorGUIUtility.TextContent("Height"), new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.PropertyField(this.m_Color, new GUILayoutOption[0]);
			EditorGUILayout.Slider(this.m_Intensity, 0f, 8f, new GUILayoutOption[0]);
			EditorGUILayout.Slider(this.m_BounceIntensity, 0f, 8f, LightEditor.s_Styles.LightBounceIntensity, new GUILayoutOption[0]);
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowIndirectWarning.faded))
			{
				GUIContent gUIContent2 = EditorGUIUtility.TextContent("Currently realtime indirect bounce light shadowing for spot and point lights is not supported.");
				EditorGUILayout.HelpBox(gUIContent2.text, MessageType.Warning, false);
			}
			EditorGUILayout.EndFadeGroup();
			this.ShadowsGUI();
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowRuntimeOptions.faded))
			{
				EditorGUILayout.PropertyField(this.m_Cookie, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowRuntimeOptions.faded * this.m_ShowDirOptions.faded))
			{
				EditorGUILayout.PropertyField(this.m_CookieSize, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.PropertyField(this.m_Halo, new GUILayoutOption[0]);
			EditorGUILayout.PropertyField(this.m_Flare, new GUILayoutOption[0]);
			EditorGUILayout.PropertyField(this.m_RenderMode, new GUILayoutOption[0]);
			EditorGUILayout.PropertyField(this.m_CullingMask, new GUILayoutOption[0]);
			EditorGUILayout.Space();
			if (SceneView.currentDrawingSceneView != null && !SceneView.currentDrawingSceneView.m_SceneLighting)
			{
				GUIContent gUIContent3 = EditorGUIUtility.TextContent("One of your scene views has lighting disabled, please keep this in mind when editing lighting.");
				EditorGUILayout.HelpBox(gUIContent3.text, MessageType.Warning, false);
			}
			base.serializedObject.ApplyModifiedProperties();
		}

		private void ShadowsGUI()
		{
			float num = 1f - this.m_ShowAreaOptions.faded;
			if (EditorGUILayout.BeginFadeGroup(num))
			{
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(this.m_ShadowsType, LightEditor.s_Styles.ShadowType, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUI.indentLevel++;
			num *= this.m_ShowShadowOptions.faded;
			if (EditorGUILayout.BeginFadeGroup(num * this.m_ShowRuntimeOptions.faded))
			{
				EditorGUILayout.Slider(this.m_ShadowsStrength, 0f, 1f, new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(this.m_ShadowsResolution, new GUILayoutOption[0]);
				EditorGUILayout.Slider(this.m_ShadowsBias, 0f, 2f, new GUILayoutOption[0]);
				EditorGUILayout.Slider(this.m_ShadowsNormalBias, 0f, 3f, new GUILayoutOption[0]);
				EditorGUILayout.Slider(this.m_ShadowsNearPlane, 0.1f, 10f, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
			if (EditorGUILayout.BeginFadeGroup(num * this.m_BakedShadowRadiusOptions.faded))
			{
				EditorGUI.BeginDisabledGroup(this.m_ShadowsType.intValue != 2);
				EditorGUILayout.PropertyField(this.m_BakedShadowRadius, LightEditor.s_Styles.BakedShadowRadius, new GUILayoutOption[0]);
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFadeGroup();
			if (EditorGUILayout.BeginFadeGroup(num * this.m_BakedShadowAngleOptions.faded))
			{
				EditorGUI.BeginDisabledGroup(this.m_ShadowsType.intValue != 2);
				EditorGUILayout.Slider(this.m_BakedShadowAngle, 0f, 90f, LightEditor.s_Styles.BakedShadowAngle, new GUILayoutOption[0]);
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}

		private void OnSceneGUI()
		{
			Light light = (Light)this.target;
			Color color = Handles.color;
			if (light.enabled)
			{
				Handles.color = LightEditor.kGizmoLight;
			}
			else
			{
				Handles.color = LightEditor.kGizmoDisabledLight;
			}
			float num = light.range;
			switch (light.type)
			{
			case LightType.Spot:
			{
				Color color2 = Handles.color;
				color2.a = Mathf.Clamp01(color.a * 2f);
				Handles.color = color2;
				Vector2 angleAndRange = new Vector2(light.spotAngle, light.range);
				angleAndRange = Handles.ConeHandle(light.transform.rotation, light.transform.position, angleAndRange, 1f, 1f, true);
				if (GUI.changed)
				{
					Undo.RecordObject(light, "Adjust Spot Light");
					light.spotAngle = angleAndRange.x;
					light.range = Mathf.Max(angleAndRange.y, 0.01f);
				}
				break;
			}
			case LightType.Point:
				num = Handles.RadiusHandle(Quaternion.identity, light.transform.position, num, true);
				if (GUI.changed)
				{
					Undo.RecordObject(light, "Adjust Point Light");
					light.range = num;
				}
				break;
			case LightType.Area:
			{
				EditorGUI.BeginChangeCheck();
				Vector2 areaSize = Handles.DoRectHandles(light.transform.rotation, light.transform.position, light.areaSize);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(light, "Adjust Area Light");
					light.areaSize = areaSize;
				}
				break;
			}
			}
			Handles.color = color;
		}
	}
}
