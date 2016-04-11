using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
	[CustomEditor(typeof(RenderSettings))]
	internal class LightingEditor : Editor
	{
		internal class Styles
		{
			public static readonly GUIContent environmentHeader = EditorGUIUtility.TextContent("Environment Lighting|Settings for the scene's surroundings, that can cast light into the scene.");

			public static readonly GUIContent sunLabel = EditorGUIUtility.TextContent("Sun|The light used by the procedural skybox. If none, the brightest directional light is used.");

			public static readonly GUIContent skyboxLabel = EditorGUIUtility.TextContent("Skybox|A skybox is rendered behind everything else in the scene in order to give the impression of scenery that is far away such as the sky or mountains. If 'Skybox' is set as the Ambient Source, light from this is cast into the scene.");

			public static readonly GUIContent ambientIntensity = EditorGUIUtility.TextContent("Ambient Intensity|How much the light from the Ambient Source affects the scene.");

			public static readonly GUIContent reflectionIntensity = EditorGUIUtility.TextContent("Reflection Intensity|How much the skybox / custom cubemap reflection affects the scene.");

			public static readonly GUIContent reflectionBounces = EditorGUIUtility.TextContent("Reflection Bounces|How many times reflection reflects another reflection, for ex., if you set 1 bounce, a reflection will not reflect another reflection, and will show black.");

			public static readonly GUIContent skyboxWarning = EditorGUIUtility.TextContent("Shader of this material does not support skybox rendering.");

			public static readonly GUIContent createLight = EditorGUIUtility.TextContent("Create Light");

			public static readonly GUIContent ambientModeLabel = EditorGUIUtility.TextContent("Ambient Source|The source of the ambient light that shines into the scene.");

			public static readonly GUIContent ambientUp = EditorGUIUtility.TextContent("Sky Color|Ambient lighting coming from above.");

			public static readonly GUIContent ambientMid = EditorGUIUtility.TextContent("Equator Color|Ambient lighting coming from the sides.");

			public static readonly GUIContent ambientDown = EditorGUIUtility.TextContent("Ground Color|Ambient lighting coming from below.");

			public static readonly GUIContent ambient = EditorGUIUtility.TextContent("Ambient Color|The color used for the ambient light shining into the scene.");

			public static readonly GUIContent reflectionModeLabel = EditorGUIUtility.TextContent("Reflection Source|Default reflection cubemap - custom or generated from current skybox.");

			public static readonly GUIContent customReflection = EditorGUIUtility.TextContent("Cubemap|Custom reflection cubemap.");

			public static readonly GUIContent skyLightColor = EditorGUIUtility.TextContent("Sky Light Color");

			public static readonly GUIContent skyboxTint = EditorGUIUtility.TextContent("Skybox Tint");

			public static readonly GUIContent SkyLightBaked = EditorGUIUtility.TextContent("Ambient GI|Which of the two Global Illumination modes (Precomputed Realtime or Baked) that should handle the ambient light. Only needed if both GI modes are enabled.");

			public static readonly GUIContent ReflectionCompression = EditorGUIUtility.TextContent("Compression|If Auto is selected Reflection Probes would be compressed unless doing so would result in ugly artefacts, e.g. PVRTC compression is \"warp around\" compression, so it is impossible to have seamless cubemap.");

			public static readonly GUIContent defaultReflectionResolution = EditorGUIUtility.TextContent("Resolution|Cubemap resolution for default reflection.");

			public static int[] defaultReflectionSizesValues = new int[]
			{
				128,
				256,
				512,
				1024
			};

			public static GUIContent[] defaultReflectionSizes = (from n in LightingEditor.Styles.defaultReflectionSizesValues
			select new GUIContent(n.ToString())).ToArray<GUIContent>();
		}

		private const string kShowLightingEditorKey = "ShowLightingEditor";

		private static readonly GUIContent[] kFullAmbientModes = new GUIContent[]
		{
			EditorGUIUtility.TextContent("Skybox"),
			EditorGUIUtility.TextContent("Gradient"),
			EditorGUIUtility.TextContent("Color")
		};

		private static readonly int[] kFullAmbientModeValues = new int[]
		{
			0,
			1,
			3
		};

		protected SerializedProperty m_Sun;

		protected SerializedProperty m_AmbientMode;

		protected SerializedProperty m_AmbientSkyColor;

		protected SerializedProperty m_AmbientEquatorColor;

		protected SerializedProperty m_AmbientGroundColor;

		protected SerializedProperty m_AmbientIntensity;

		protected SerializedProperty m_ReflectionIntensity;

		protected SerializedProperty m_ReflectionBounces;

		protected SerializedProperty m_SkyboxMaterial;

		protected SerializedProperty m_DefaultReflectionMode;

		protected SerializedProperty m_DefaultReflectionResolution;

		protected SerializedProperty m_CustomReflection;

		protected SerializedProperty m_ReflectionCompression;

		protected SerializedObject m_lightmapSettings;

		protected SerializedProperty m_EnvironmentLightingMode;

		private bool m_ShowEditor;

		private AnimBool m_ShowAmbientBakeMode = new AnimBool();

		private EditorWindow m_ParentWindow;

		public EditorWindow parentWindow
		{
			get
			{
				return this.m_ParentWindow;
			}
			set
			{
				if (this.m_ParentWindow != null)
				{
					this.m_ShowAmbientBakeMode.valueChanged.RemoveListener(new UnityAction(this.m_ParentWindow.Repaint));
				}
				this.m_ParentWindow = value;
				this.m_ShowAmbientBakeMode.valueChanged.AddListener(new UnityAction(this.m_ParentWindow.Repaint));
			}
		}

		public virtual void OnEnable()
		{
			this.m_Sun = base.serializedObject.FindProperty("m_Sun");
			this.m_AmbientMode = base.serializedObject.FindProperty("m_AmbientMode");
			this.m_AmbientSkyColor = base.serializedObject.FindProperty("m_AmbientSkyColor");
			this.m_AmbientEquatorColor = base.serializedObject.FindProperty("m_AmbientEquatorColor");
			this.m_AmbientGroundColor = base.serializedObject.FindProperty("m_AmbientGroundColor");
			this.m_AmbientIntensity = base.serializedObject.FindProperty("m_AmbientIntensity");
			this.m_ReflectionIntensity = base.serializedObject.FindProperty("m_ReflectionIntensity");
			this.m_ReflectionBounces = base.serializedObject.FindProperty("m_ReflectionBounces");
			this.m_SkyboxMaterial = base.serializedObject.FindProperty("m_SkyboxMaterial");
			this.m_DefaultReflectionMode = base.serializedObject.FindProperty("m_DefaultReflectionMode");
			this.m_DefaultReflectionResolution = base.serializedObject.FindProperty("m_DefaultReflectionResolution");
			this.m_CustomReflection = base.serializedObject.FindProperty("m_CustomReflection");
			this.m_lightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
			this.m_EnvironmentLightingMode = this.m_lightmapSettings.FindProperty("m_GISettings.m_EnvironmentLightingMode");
			this.m_ReflectionCompression = this.m_lightmapSettings.FindProperty("m_LightmapEditorSettings.m_ReflectionCompression");
			this.m_ShowEditor = SessionState.GetBool("ShowLightingEditor", true);
			this.m_ShowAmbientBakeMode.target = LightingEditor.ShowAmbientField();
		}

		public virtual void OnDisable()
		{
			SessionState.SetBool("ShowLightingEditor", this.m_ShowEditor);
			this.m_ShowAmbientBakeMode.valueChanged.RemoveAllListeners();
			this.m_ParentWindow = null;
		}

		public override void OnInspectorGUI()
		{
			base.serializedObject.Update();
			this.m_lightmapSettings.Update();
			EditorGUILayout.Space();
			this.m_ShowEditor = EditorGUILayout.FoldoutTitlebar(this.m_ShowEditor, LightingEditor.Styles.environmentHeader);
			if (!this.m_ShowEditor)
			{
				return;
			}
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(this.m_SkyboxMaterial, LightingEditor.Styles.skyboxLabel, new GUILayoutOption[0]);
			Material material = this.m_SkyboxMaterial.objectReferenceValue as Material;
			if (material && !EditorMaterialUtility.IsBackgroundMaterial(material))
			{
				EditorGUILayout.HelpBox(LightingEditor.Styles.skyboxWarning.text, MessageType.Warning);
			}
			EditorGUILayout.PropertyField(this.m_Sun, LightingEditor.Styles.sunLabel, new GUILayoutOption[0]);
			EditorGUILayout.Space();
			EditorGUILayout.IntPopup(this.m_AmbientMode, LightingEditor.kFullAmbientModes, LightingEditor.kFullAmbientModeValues, LightingEditor.Styles.ambientModeLabel, new GUILayoutOption[0]);
			EditorGUI.indentLevel++;
			switch (this.m_AmbientMode.intValue)
			{
			case 0:
				if (material == null)
				{
					EditorGUILayout.PropertyField(this.m_AmbientSkyColor, LightingEditor.Styles.ambient, new GUILayoutOption[0]);
				}
				break;
			case 1:
				EditorGUILayout.PropertyField(this.m_AmbientSkyColor, LightingEditor.Styles.ambientUp, new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(this.m_AmbientEquatorColor, LightingEditor.Styles.ambientMid, new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(this.m_AmbientGroundColor, LightingEditor.Styles.ambientDown, new GUILayoutOption[0]);
				break;
			case 3:
				EditorGUILayout.PropertyField(this.m_AmbientSkyColor, LightingEditor.Styles.ambient, new GUILayoutOption[0]);
				break;
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Slider(this.m_AmbientIntensity, 0f, 8f, LightingEditor.Styles.ambientIntensity, new GUILayoutOption[0]);
			this.m_ShowAmbientBakeMode.target = LightingEditor.ShowAmbientField();
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowAmbientBakeMode.faded))
			{
				bool flag = Lightmapping.realtimeLightmapsEnabled && Lightmapping.bakedLightmapsEnabled;
				EditorGUI.BeginDisabledGroup(!flag);
				if (flag)
				{
					EditorGUILayout.PropertyField(this.m_EnvironmentLightingMode, LightingEditor.Styles.SkyLightBaked, new GUILayoutOption[0]);
				}
				else
				{
					int num = (!Lightmapping.bakedLightmapsEnabled) ? 0 : 1;
					EditorGUILayout.LabelField(LightingEditor.Styles.SkyLightBaked, GUIContent.Temp(this.m_EnvironmentLightingMode.enumNames[num]), EditorStyles.popup, new GUILayoutOption[0]);
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(this.m_DefaultReflectionMode, LightingEditor.Styles.reflectionModeLabel, new GUILayoutOption[0]);
			EditorGUI.indentLevel++;
			DefaultReflectionMode intValue = (DefaultReflectionMode)this.m_DefaultReflectionMode.intValue;
			if (intValue != DefaultReflectionMode.FromSkybox)
			{
				if (intValue == DefaultReflectionMode.Custom)
				{
					EditorGUILayout.PropertyField(this.m_CustomReflection, LightingEditor.Styles.customReflection, new GUILayoutOption[0]);
				}
			}
			else
			{
				EditorGUILayout.IntPopup(this.m_DefaultReflectionResolution, LightingEditor.Styles.defaultReflectionSizes, LightingEditor.Styles.defaultReflectionSizesValues, LightingEditor.Styles.defaultReflectionResolution, new GUILayoutOption[]
				{
					GUILayout.MinWidth(40f)
				});
			}
			EditorGUILayout.PropertyField(this.m_ReflectionCompression, LightingEditor.Styles.ReflectionCompression, new GUILayoutOption[0]);
			EditorGUI.indentLevel--;
			EditorGUILayout.Slider(this.m_ReflectionIntensity, 0f, 1f, LightingEditor.Styles.reflectionIntensity, new GUILayoutOption[0]);
			EditorGUILayout.IntSlider(this.m_ReflectionBounces, 1, 5, LightingEditor.Styles.reflectionBounces, new GUILayoutOption[0]);
			EditorGUI.indentLevel--;
			base.serializedObject.ApplyModifiedProperties();
			this.m_lightmapSettings.ApplyModifiedProperties();
		}

		private static bool ShowAmbientField()
		{
			return Lightmapping.realtimeLightmapsEnabled || Lightmapping.bakedLightmapsEnabled;
		}
	}
}
