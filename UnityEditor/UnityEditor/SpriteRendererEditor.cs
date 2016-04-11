using System;
using UnityEngine;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(SpriteRenderer))]
	internal class SpriteRendererEditor : RendererEditorBase
	{
		private SerializedProperty m_Sprite;

		private SerializedProperty m_Color;

		private SerializedProperty m_Material;

		private SerializedProperty m_FlipX;

		private SerializedProperty m_FlipY;

		private GUIContent m_FlipLabel = EditorGUIUtility.TextContent("Flip");

		private static Texture2D s_WarningIcon;

		private GUIContent m_MaterialStyle = EditorGUIUtility.TextContent("Material");

		public override void OnEnable()
		{
			base.OnEnable();
			this.m_Sprite = base.serializedObject.FindProperty("m_Sprite");
			this.m_Color = base.serializedObject.FindProperty("m_Color");
			this.m_FlipX = base.serializedObject.FindProperty("m_FlipX");
			this.m_FlipY = base.serializedObject.FindProperty("m_FlipY");
			this.m_Material = base.serializedObject.FindProperty("m_Materials.Array");
		}

		public override void OnInspectorGUI()
		{
			base.serializedObject.Update();
			EditorGUILayout.PropertyField(this.m_Sprite, new GUILayoutOption[0]);
			EditorGUILayout.PropertyField(this.m_Color, true, new GUILayoutOption[0]);
			this.FlipToggles();
			if (this.m_Material.arraySize == 0)
			{
				this.m_Material.InsertArrayElementAtIndex(0);
			}
			Rect rect = GUILayoutUtility.GetRect(EditorGUILayout.kLabelFloatMinW, EditorGUILayout.kLabelFloatMaxW, 16f, 16f);
			EditorGUI.showMixedValue = this.m_Material.hasMultipleDifferentValues;
			UnityEngine.Object objectReferenceValue = this.m_Material.GetArrayElementAtIndex(0).objectReferenceValue;
			UnityEngine.Object @object = EditorGUI.ObjectField(rect, this.m_MaterialStyle, objectReferenceValue, typeof(Material), false);
			if (@object != objectReferenceValue)
			{
				this.m_Material.GetArrayElementAtIndex(0).objectReferenceValue = @object;
			}
			EditorGUI.showMixedValue = false;
			base.RenderSortingLayerFields();
			this.CheckForErrors();
			base.serializedObject.ApplyModifiedProperties();
		}

		private void FlipToggles()
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUILayout.kLabelFloatMaxW, 16f, 16f, EditorStyles.numberField);
			int controlID = GUIUtility.GetControlID(8476, FocusType.Keyboard, rect);
			rect = EditorGUI.PrefixLabel(rect, controlID, this.m_FlipLabel);
			rect.width = 30f;
			this.FlipToggle(rect, "X", this.m_FlipX);
			rect.x += 30f;
			this.FlipToggle(rect, "Y", this.m_FlipY);
			GUILayout.EndHorizontal();
		}

		private void FlipToggle(Rect r, string label, SerializedProperty property)
		{
			bool flag = property.boolValue;
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			flag = EditorGUI.ToggleLeft(r, label, flag);
			EditorGUI.indentLevel = indentLevel;
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObjects(base.targets, "Edit Constraints");
				property.boolValue = flag;
			}
			EditorGUI.showMixedValue = false;
		}

		private void CheckForErrors()
		{
			if (this.IsMaterialTextureAtlasConflict())
			{
				SpriteRendererEditor.ShowError("Material has CanUseSpriteAtlas=False tag. Sprite texture has atlasHint set. Rendering artifacts possible.");
			}
			bool flag;
			if (!this.DoesMaterialHaveSpriteTexture(out flag))
			{
				SpriteRendererEditor.ShowError("Material does not have a _MainTex texture property. It is required for SpriteRenderer.");
			}
			else if (flag)
			{
				SpriteRendererEditor.ShowError("Material texture property _MainTex has offset/scale set. It is incompatible with SpriteRenderer.");
			}
		}

		private bool IsMaterialTextureAtlasConflict()
		{
			Material sharedMaterial = (this.target as SpriteRenderer).sharedMaterial;
			if (sharedMaterial == null)
			{
				return false;
			}
			string tag = sharedMaterial.GetTag("CanUseSpriteAtlas", false);
			if (tag.ToLower() == "false")
			{
				Sprite assetObject = this.m_Sprite.objectReferenceValue as Sprite;
				TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(assetObject)) as TextureImporter;
				if (textureImporter.spritePackingTag != null && textureImporter.spritePackingTag.Length > 0)
				{
					return true;
				}
			}
			return false;
		}

		private bool DoesMaterialHaveSpriteTexture(out bool tiled)
		{
			tiled = false;
			Material sharedMaterial = (this.target as SpriteRenderer).sharedMaterial;
			if (sharedMaterial == null)
			{
				return true;
			}
			bool flag = sharedMaterial.HasProperty("_MainTex");
			if (flag)
			{
				Vector2 textureOffset = sharedMaterial.GetTextureOffset("_MainTex");
				Vector2 textureScale = sharedMaterial.GetTextureScale("_MainTex");
				if (textureOffset.x != 0f || textureOffset.y != 0f || textureScale.x != 1f || textureScale.y != 1f)
				{
					tiled = true;
				}
			}
			return sharedMaterial.HasProperty("_MainTex");
		}

		private static void ShowError(string error)
		{
			if (SpriteRendererEditor.s_WarningIcon == null)
			{
				SpriteRendererEditor.s_WarningIcon = EditorGUIUtility.LoadIcon("console.warnicon");
			}
			GUIContent content = new GUIContent(error)
			{
				image = SpriteRendererEditor.s_WarningIcon
			};
			GUILayout.Space(5f);
			GUILayout.BeginVertical(EditorStyles.helpBox, new GUILayoutOption[0]);
			GUILayout.Label(content, EditorStyles.wordWrappedMiniLabel, new GUILayoutOption[0]);
			GUILayout.EndVertical();
		}
	}
}
