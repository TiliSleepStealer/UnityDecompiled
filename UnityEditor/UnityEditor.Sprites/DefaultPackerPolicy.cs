using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Sprites
{
	internal class DefaultPackerPolicy : IPackerPolicy
	{
		protected class Entry
		{
			public Sprite sprite;

			public AtlasSettings settings;

			public string atlasName;

			public SpritePackingMode packingMode;

			public int anisoLevel;
		}

		private const uint kDefaultPaddingPower = 3u;

		protected virtual string TagPrefix
		{
			get
			{
				return "[TIGHT]";
			}
		}

		protected virtual bool AllowTightWhenTagged
		{
			get
			{
				return true;
			}
		}

		protected virtual bool AllowRotationFlipping
		{
			get
			{
				return false;
			}
		}

		public virtual int GetVersion()
		{
			return 1;
		}

		public void OnGroupAtlases(BuildTarget target, PackerJob job, int[] textureImporterInstanceIDs)
		{
			List<DefaultPackerPolicy.Entry> list = new List<DefaultPackerPolicy.Entry>();
			for (int i = 0; i < textureImporterInstanceIDs.Length; i++)
			{
				int instanceID = textureImporterInstanceIDs[i];
				TextureImporter textureImporter = EditorUtility.InstanceIDToObject(instanceID) as TextureImporter;
				TextureFormat format;
				ColorSpace colorSpace;
				int num;
				textureImporter.ReadTextureImportInstructions(target, out format, out colorSpace, out num);
				TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
				textureImporter.ReadTextureSettings(textureImporterSettings);
				Sprite[] array = (from x in AssetDatabase.LoadAllAssetRepresentationsAtPath(textureImporter.assetPath)
				select x as Sprite into x
				where x != null
				select x).ToArray<Sprite>();
				Sprite[] array2 = array;
				for (int j = 0; j < array2.Length; j++)
				{
					Sprite sprite = array2[j];
					DefaultPackerPolicy.Entry entry = new DefaultPackerPolicy.Entry();
					entry.sprite = sprite;
					entry.settings.format = format;
					entry.settings.colorSpace = colorSpace;
					entry.settings.compressionQuality = ((!TextureUtil.IsCompressedTextureFormat(format)) ? 0 : num);
					entry.settings.filterMode = ((!Enum.IsDefined(typeof(FilterMode), textureImporter.filterMode)) ? FilterMode.Bilinear : textureImporter.filterMode);
					entry.settings.maxWidth = 2048;
					entry.settings.maxHeight = 2048;
					entry.settings.generateMipMaps = textureImporter.mipmapEnabled;
					entry.settings.enableRotation = this.AllowRotationFlipping;
					entry.settings.allowsAlphaSplitting = textureImporter.GetAllowsAlphaSplitting();
					if (textureImporter.mipmapEnabled)
					{
						entry.settings.paddingPower = 3u;
					}
					else
					{
						entry.settings.paddingPower = (uint)EditorSettings.spritePackerPaddingPower;
					}
					entry.atlasName = this.ParseAtlasName(textureImporter.spritePackingTag);
					entry.packingMode = this.GetPackingMode(textureImporter.spritePackingTag, textureImporterSettings.spriteMeshType);
					entry.anisoLevel = textureImporter.anisoLevel;
					list.Add(entry);
				}
				Resources.UnloadAsset(textureImporter);
			}
			IEnumerable<IGrouping<string, DefaultPackerPolicy.Entry>> enumerable = from e in list
			group e by e.atlasName;
			foreach (IGrouping<string, DefaultPackerPolicy.Entry> current in enumerable)
			{
				int num2 = 0;
				IEnumerable<IGrouping<AtlasSettings, DefaultPackerPolicy.Entry>> enumerable2 = from t in current
				group t by t.settings;
				foreach (IGrouping<AtlasSettings, DefaultPackerPolicy.Entry> current2 in enumerable2)
				{
					string text = current.Key;
					if (enumerable2.Count<IGrouping<AtlasSettings, DefaultPackerPolicy.Entry>>() > 1)
					{
						text += string.Format(" (Group {0})", num2);
					}
					AtlasSettings key = current2.Key;
					key.anisoLevel = 1;
					if (key.generateMipMaps)
					{
						foreach (DefaultPackerPolicy.Entry current3 in current2)
						{
							if (current3.anisoLevel > key.anisoLevel)
							{
								key.anisoLevel = current3.anisoLevel;
							}
						}
					}
					job.AddAtlas(text, key);
					foreach (DefaultPackerPolicy.Entry current4 in current2)
					{
						job.AssignToAtlas(text, current4.sprite, current4.packingMode, SpritePackingRotation.None);
					}
					num2++;
				}
			}
		}

		protected bool IsTagPrefixed(string packingTag)
		{
			packingTag = packingTag.Trim();
			return packingTag.Length >= this.TagPrefix.Length && packingTag.Substring(0, this.TagPrefix.Length) == this.TagPrefix;
		}

		private string ParseAtlasName(string packingTag)
		{
			string text = packingTag.Trim();
			if (this.IsTagPrefixed(text))
			{
				text = text.Substring(this.TagPrefix.Length).Trim();
			}
			return (text.Length != 0) ? text : "(unnamed)";
		}

		private SpritePackingMode GetPackingMode(string packingTag, SpriteMeshType meshType)
		{
			if (meshType == SpriteMeshType.Tight && this.IsTagPrefixed(packingTag) == this.AllowTightWhenTagged)
			{
				return SpritePackingMode.Tight;
			}
			return SpritePackingMode.Rectangle;
		}
	}
}
