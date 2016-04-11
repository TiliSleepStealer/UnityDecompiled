using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor
{
	[CanEditMultipleObjects, CustomEditor(typeof(TextureImporter))]
	internal class TextureImporterInspector : AssetImporterInspector
	{
		private enum AdvancedTextureType
		{
			Default,
			NormalMap,
			LightMap
		}

		[Serializable]
		protected class PlatformSetting
		{
			[SerializeField]
			public string name;

			[SerializeField]
			private bool m_Overridden;

			[SerializeField]
			private bool m_OverriddenIsDifferent;

			[SerializeField]
			private int m_MaxTextureSize;

			[SerializeField]
			private bool m_MaxTextureSizeIsDifferent;

			[SerializeField]
			private int m_CompressionQuality;

			[SerializeField]
			private bool m_CompressionQualityIsDifferent;

			[SerializeField]
			private TextureImporterFormat[] m_TextureFormatArray;

			[SerializeField]
			private bool m_TextureFormatIsDifferent;

			[SerializeField]
			public BuildTarget m_Target;

			[SerializeField]
			private TextureImporter[] m_Importers;

			[SerializeField]
			private bool m_HasChanged;

			[SerializeField]
			private TextureImporterInspector m_Inspector;

			public bool overridden
			{
				get
				{
					return this.m_Overridden;
				}
			}

			public bool overriddenIsDifferent
			{
				get
				{
					return this.m_OverriddenIsDifferent;
				}
			}

			public bool allAreOverridden
			{
				get
				{
					return this.isDefault || (this.m_Overridden && !this.m_OverriddenIsDifferent);
				}
			}

			public int maxTextureSize
			{
				get
				{
					return this.m_MaxTextureSize;
				}
			}

			public bool maxTextureSizeIsDifferent
			{
				get
				{
					return this.m_MaxTextureSizeIsDifferent;
				}
			}

			public int compressionQuality
			{
				get
				{
					return this.m_CompressionQuality;
				}
			}

			public bool compressionQualityIsDifferent
			{
				get
				{
					return this.m_CompressionQualityIsDifferent;
				}
			}

			public TextureImporterFormat[] textureFormats
			{
				get
				{
					return this.m_TextureFormatArray;
				}
			}

			public bool textureFormatIsDifferent
			{
				get
				{
					return this.m_TextureFormatIsDifferent;
				}
			}

			public TextureImporter[] importers
			{
				get
				{
					return this.m_Importers;
				}
			}

			public bool isDefault
			{
				get
				{
					return this.name == string.Empty;
				}
			}

			public PlatformSetting(string name, BuildTarget target, TextureImporterInspector inspector)
			{
				this.name = name;
				this.m_Target = target;
				this.m_Inspector = inspector;
				this.m_Overridden = false;
				this.m_Importers = (from x in inspector.targets
				select x as TextureImporter).ToArray<TextureImporter>();
				this.m_TextureFormatArray = new TextureImporterFormat[this.importers.Length];
				for (int i = 0; i < this.importers.Length; i++)
				{
					TextureImporter textureImporter = this.importers[i];
					int maxTextureSize;
					TextureImporterFormat textureFormat;
					int compressionQuality;
					bool flag;
					if (!this.isDefault)
					{
						flag = textureImporter.GetPlatformTextureSettings(name, out maxTextureSize, out textureFormat, out compressionQuality);
					}
					else
					{
						flag = true;
						maxTextureSize = textureImporter.maxTextureSize;
						textureFormat = textureImporter.textureFormat;
						compressionQuality = textureImporter.compressionQuality;
					}
					this.m_TextureFormatArray[i] = textureFormat;
					if (i == 0)
					{
						this.m_Overridden = flag;
						this.m_MaxTextureSize = maxTextureSize;
						this.m_CompressionQuality = compressionQuality;
					}
					else
					{
						if (flag != this.m_Overridden)
						{
							this.m_OverriddenIsDifferent = true;
						}
						if (maxTextureSize != this.m_MaxTextureSize)
						{
							this.m_MaxTextureSizeIsDifferent = true;
						}
						if (compressionQuality != this.m_CompressionQuality)
						{
							this.m_CompressionQualityIsDifferent = true;
						}
						if (textureFormat != this.m_TextureFormatArray[0])
						{
							this.m_TextureFormatIsDifferent = true;
						}
					}
				}
				this.Sync();
			}

			public void SetOverriddenForAll(bool overridden)
			{
				this.m_Overridden = overridden;
				this.m_OverriddenIsDifferent = false;
				this.m_HasChanged = true;
			}

			public void SetMaxTextureSizeForAll(int maxTextureSize)
			{
				this.m_MaxTextureSize = maxTextureSize;
				this.m_MaxTextureSizeIsDifferent = false;
				this.m_HasChanged = true;
			}

			public void SetCompressionQualityForAll(int quality)
			{
				this.m_CompressionQuality = quality;
				this.m_CompressionQualityIsDifferent = false;
				this.m_HasChanged = true;
			}

			public void SetTextureFormatForAll(TextureImporterFormat format)
			{
				for (int i = 0; i < this.m_TextureFormatArray.Length; i++)
				{
					this.m_TextureFormatArray[i] = format;
				}
				this.m_TextureFormatIsDifferent = false;
				this.m_HasChanged = true;
			}

			public bool SupportsFormat(TextureImporterFormat format, TextureImporter importer)
			{
				TextureImporterSettings settings = this.GetSettings(importer);
				BuildTarget target = this.m_Target;
				int[] array;
				switch (target)
				{
				case BuildTarget.BlackBerry:
					array = TextureImporterInspector.kTextureFormatsValueBB10;
					goto IL_F3;
				case BuildTarget.Tizen:
					array = TextureImporterInspector.kTextureFormatsValueTizen;
					goto IL_F3;
				case BuildTarget.PSP2:
				case BuildTarget.PS4:
				case BuildTarget.PSM:
				case BuildTarget.XboxOne:
				case BuildTarget.Nintendo3DS:
					IL_40:
					switch (target)
					{
					case BuildTarget.iOS:
						array = TextureImporterInspector.kTextureFormatsValueiPhone;
						goto IL_F3;
					case BuildTarget.Android:
						array = TextureImporterInspector.kTextureFormatsValueAndroid;
						goto IL_F3;
					case BuildTarget.StandaloneGLESEmu:
						array = ((!settings.normalMap) ? TextureImporterInspector.kTextureFormatsValueGLESEmu : TextureImporterInspector.kNormalFormatsValueWeb);
						goto IL_F3;
					}
					array = ((!settings.normalMap) ? TextureImporterInspector.kTextureFormatsValueWeb : TextureImporterInspector.kNormalFormatsValueWeb);
					goto IL_F3;
				case BuildTarget.SamsungTV:
					array = TextureImporterInspector.kTextureFormatsValueSTV;
					goto IL_F3;
				case BuildTarget.WiiU:
					array = TextureImporterInspector.kTextureFormatsValueWiiU;
					goto IL_F3;
				case BuildTarget.tvOS:
					array = TextureImporterInspector.kTextureFormatsValuetvOS;
					goto IL_F3;
				}
				goto IL_40;
				IL_F3:
				return ((IList)array).Contains((int)format);
			}

			public TextureImporterSettings GetSettings(TextureImporter importer)
			{
				TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
				importer.ReadTextureSettings(textureImporterSettings);
				this.m_Inspector.GetSerializedPropertySettings(textureImporterSettings);
				return textureImporterSettings;
			}

			public virtual void SetChanged()
			{
				this.m_HasChanged = true;
			}

			public virtual bool HasChanged()
			{
				return this.m_HasChanged;
			}

			public void Sync()
			{
				if (!this.isDefault && (!this.m_Overridden || this.m_OverriddenIsDifferent))
				{
					TextureImporterInspector.PlatformSetting platformSetting = this.m_Inspector.m_PlatformSettings[0];
					this.m_MaxTextureSize = platformSetting.m_MaxTextureSize;
					this.m_MaxTextureSizeIsDifferent = platformSetting.m_MaxTextureSizeIsDifferent;
					this.m_TextureFormatArray = (TextureImporterFormat[])platformSetting.m_TextureFormatArray.Clone();
					this.m_TextureFormatIsDifferent = platformSetting.m_TextureFormatIsDifferent;
					this.m_CompressionQuality = platformSetting.m_CompressionQuality;
					this.m_CompressionQualityIsDifferent = platformSetting.m_CompressionQualityIsDifferent;
				}
				TextureImporterType textureType = this.m_Inspector.textureType;
				int i = 0;
				while (i < this.importers.Length)
				{
					TextureImporter textureImporter = this.importers[i];
					TextureImporterSettings settings = this.GetSettings(textureImporter);
					if (textureType == TextureImporterType.Advanced)
					{
						if (!this.isDefault)
						{
							if (!this.SupportsFormat(this.m_TextureFormatArray[i], textureImporter))
							{
								this.m_TextureFormatArray[i] = TextureImporter.FullToSimpleTextureFormat(this.m_TextureFormatArray[i]);
							}
							if (this.m_TextureFormatArray[i] < (TextureImporterFormat)0)
							{
								this.m_TextureFormatArray[i] = TextureImporter.SimpleToFullTextureFormat2(this.m_TextureFormatArray[i], textureType, settings, textureImporter.DoesSourceTextureHaveAlpha(), textureImporter.IsSourceTextureHDR(), this.m_Target);
							}
							goto IL_14A;
						}
					}
					else
					{
						if (this.m_TextureFormatArray[i] >= (TextureImporterFormat)0)
						{
							this.m_TextureFormatArray[i] = TextureImporter.FullToSimpleTextureFormat(this.m_TextureFormatArray[i]);
							goto IL_14A;
						}
						goto IL_14A;
					}
					IL_17B:
					i++;
					continue;
					IL_14A:
					if (settings.normalMap && !TextureImporterInspector.IsGLESMobileTargetPlatform(this.m_Target))
					{
						this.m_TextureFormatArray[i] = TextureImporterInspector.MakeTextureFormatHaveAlpha(this.m_TextureFormatArray[i]);
						goto IL_17B;
					}
					goto IL_17B;
				}
				this.m_TextureFormatIsDifferent = false;
				TextureImporterFormat[] textureFormatArray = this.m_TextureFormatArray;
				for (int j = 0; j < textureFormatArray.Length; j++)
				{
					TextureImporterFormat textureImporterFormat = textureFormatArray[j];
					if (textureImporterFormat != this.m_TextureFormatArray[0])
					{
						this.m_TextureFormatIsDifferent = true;
					}
				}
			}

			private bool GetOverridden(TextureImporter importer)
			{
				if (!this.m_OverriddenIsDifferent)
				{
					return this.m_Overridden;
				}
				int num;
				TextureImporterFormat textureImporterFormat;
				return importer.GetPlatformTextureSettings(this.name, out num, out textureImporterFormat);
			}

			public void Apply()
			{
				for (int i = 0; i < this.importers.Length; i++)
				{
					TextureImporter textureImporter = this.importers[i];
					int compressionQuality = -1;
					bool flag = false;
					int maxTextureSize;
					if (this.isDefault)
					{
						maxTextureSize = textureImporter.maxTextureSize;
					}
					else
					{
						TextureImporterFormat textureImporterFormat;
						flag = textureImporter.GetPlatformTextureSettings(this.name, out maxTextureSize, out textureImporterFormat, out compressionQuality);
					}
					if (!flag)
					{
						maxTextureSize = textureImporter.maxTextureSize;
					}
					if (!this.m_MaxTextureSizeIsDifferent)
					{
						maxTextureSize = this.m_MaxTextureSize;
					}
					if (!this.m_CompressionQualityIsDifferent)
					{
						compressionQuality = this.m_CompressionQuality;
					}
					if (!this.isDefault)
					{
						if (!this.m_OverriddenIsDifferent)
						{
							flag = this.m_Overridden;
						}
						bool allowsAlphaSplitting = textureImporter.GetAllowsAlphaSplitting();
						if (flag)
						{
							textureImporter.SetPlatformTextureSettings(this.name, maxTextureSize, this.m_TextureFormatArray[i], compressionQuality, allowsAlphaSplitting);
						}
						else
						{
							textureImporter.ClearPlatformTextureSettings(this.name);
						}
					}
					else
					{
						textureImporter.maxTextureSize = maxTextureSize;
						textureImporter.textureFormat = this.m_TextureFormatArray[i];
						textureImporter.compressionQuality = compressionQuality;
					}
				}
			}
		}

		private enum CookieMode
		{
			Spot,
			Directional,
			Point
		}

		private class Styles
		{
			public readonly GUIContent textureType = EditorGUIUtility.TextContent("Texture Type|What will this texture be used for?");

			public readonly GUIContent[] textureTypeOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Texture|Texture is a normal image such as a diffuse texture or other."),
				EditorGUIUtility.TextContent("Normal map|Texture is a bump or normal map."),
				EditorGUIUtility.TextContent("Editor GUI and Legacy GUI|Texture is used for a GUI element."),
				EditorGUIUtility.TextContent("Sprite (2D and UI)|Texture is used for a sprite."),
				EditorGUIUtility.TextContent("Cursor|Texture is used for a cursor."),
				EditorGUIUtility.TextContent("Cubemap|Texture is a cubemap."),
				EditorGUIUtility.TextContent("Cookie|Texture is a cookie you put on a light."),
				EditorGUIUtility.TextContent("Lightmap|Texture is a lightmap."),
				EditorGUIUtility.TextContent("Advanced|All settings displayed.")
			};

			public readonly GUIContent filterMode = EditorGUIUtility.TextContent("Filter Mode");

			public readonly GUIContent[] filterModeOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Point (no filter)"),
				EditorGUIUtility.TextContent("Bilinear"),
				EditorGUIUtility.TextContent("Trilinear")
			};

			public readonly GUIContent generateAlphaFromGrayscale = EditorGUIUtility.TextContent("Alpha from Grayscale|Generate texture's alpha channel from grayscale.");

			public readonly GUIContent cookieType = EditorGUIUtility.TextContent("Light Type");

			public readonly GUIContent[] cookieOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Spotlight"),
				EditorGUIUtility.TextContent("Directional"),
				EditorGUIUtility.TextContent("Point")
			};

			public readonly GUIContent generateFromBump = EditorGUIUtility.TextContent("Create from Grayscale|The grayscale of the image is used as a heightmap for generating the normal map.");

			public readonly GUIContent generateBumpmap = EditorGUIUtility.TextContent("Create bump map");

			public readonly GUIContent bumpiness = EditorGUIUtility.TextContent("Bumpiness");

			public readonly GUIContent bumpFiltering = EditorGUIUtility.TextContent("Filtering");

			public readonly GUIContent[] bumpFilteringOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Sharp"),
				EditorGUIUtility.TextContent("Smooth")
			};

			public readonly GUIContent cubemap = EditorGUIUtility.TextContent("Mapping");

			public readonly GUIContent[] cubemapOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("None"),
				EditorGUIUtility.TextContent("Auto"),
				EditorGUIUtility.TextContent("6 Frames Layout (Cubic Environment)|Texture contains 6 images arranged in one of the standard cubemap layouts - cross or sequence (+x,-x, +y, -y, +z, -z). Texture can be in vertical or horizontal orientation."),
				EditorGUIUtility.TextContent("Latitude-Longitude Layout (Cylindrical)|Texture contains an image of a ball unwrapped such that latitude and longitude are mapped to horizontal and vertical dimensions (as on a globe)."),
				EditorGUIUtility.TextContent("Mirrored Ball (Spheremap)|Texture contains an image of a mirrored ball.")
			};

			public readonly int[] cubemapValues = new int[]
			{
				0,
				6,
				5,
				2,
				1
			};

			public readonly GUIContent cubemapConvolution = EditorGUIUtility.TextContent("Convolution Type");

			public readonly GUIContent[] cubemapConvolutionOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("None"),
				EditorGUIUtility.TextContent("Specular (Glossy Reflection)|Convolve cubemap for specular reflections with varying smoothness (Glossy Reflections)."),
				EditorGUIUtility.TextContent("Diffuse (Irradiance)|Convolve cubemap for diffuse-only reflection (Irradiance Cubemap).")
			};

			public readonly int[] cubemapConvolutionValues = new int[]
			{
				0,
				1,
				2
			};

			public readonly GUIContent cubemapConvolutionSimple = EditorGUIUtility.TextContent("Glossy Reflection");

			public readonly GUIContent cubemapConvolutionSteps = EditorGUIUtility.TextContent("Steps|Number of smoothness steps represented in mip maps of the cubemap.");

			public readonly GUIContent cubemapConvolutionExp = EditorGUIUtility.TextContent("Exponent|Defines smoothness curve (x^exponent) for convolution.");

			public readonly GUIContent seamlessCubemap = EditorGUIUtility.TextContent("Fixup Edge Seams|Enable if this texture is used for glossy reflections.");

			public readonly GUIContent maxSize = EditorGUIUtility.TextContent("Max Size|Textures larger than this will be scaled down.");

			public readonly GUIContent textureFormat = EditorGUIUtility.TextContent("Format");

			public readonly GUIContent[] textureFormatOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Compressed"),
				EditorGUIUtility.TextContent("16 bits"),
				EditorGUIUtility.TextContent("Truecolor"),
				EditorGUIUtility.TextContent("Crunched")
			};

			public readonly GUIContent defaultPlatform = EditorGUIUtility.TextContent("Default");

			public readonly GUIContent mipmapFadeOutToggle = EditorGUIUtility.TextContent("Fadeout Mip Maps");

			public readonly GUIContent mipmapFadeOut = EditorGUIUtility.TextContent("Fade Range");

			public readonly GUIContent readWrite = EditorGUIUtility.TextContent("Read/Write Enabled|Enable to be able to access the raw pixel data from code.");

			public readonly GUIContent rgbmEncoding = EditorGUIUtility.TextContent("Encode as RGBM|Encode texture as RGBM (for HDR textures).");

			public readonly GUIContent[] rgbmEncodingOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Auto"),
				EditorGUIUtility.TextContent("On"),
				EditorGUIUtility.TextContent("Off"),
				EditorGUIUtility.TextContent("Encoded")
			};

			public readonly GUIContent generateMipMaps = EditorGUIUtility.TextContent("Generate Mip Maps");

			public readonly GUIContent mipMapsInLinearSpace = EditorGUIUtility.TextContent("In Linear Space|Perform mip map generation in linear space.");

			public readonly GUIContent linearTexture = EditorGUIUtility.TextContent("Bypass sRGB Sampling|Texture will not be converted from gamma space to linear when sampled. Enable for IMGUI textures and non-color textures.");

			public readonly GUIContent borderMipMaps = EditorGUIUtility.TextContent("Border Mip Maps");

			public readonly GUIContent mipMapFilter = EditorGUIUtility.TextContent("Mip Map Filtering");

			public readonly GUIContent[] mipMapFilterOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Box"),
				EditorGUIUtility.TextContent("Kaiser")
			};

			public readonly GUIContent normalmap = EditorGUIUtility.TextContent("Normal Map|Enable if this texture is a normal map baked out of a 3D package.");

			public readonly GUIContent npot = EditorGUIUtility.TextContent("Non Power of 2|How non-power-of-two textures are scaled on import.");

			public readonly GUIContent generateCubemap = EditorGUIUtility.TextContent("Generate Cubemap");

			public readonly GUIContent lightmap = EditorGUIUtility.TextContent("Lightmap|Enable if this is a lightmap (best if stored in EXR format).");

			public readonly GUIContent compressionQuality = EditorGUIUtility.TextContent("Compression Quality");

			public readonly GUIContent compressionQualitySlider = EditorGUIUtility.TextContent("Compression Quality|Use the slider to adjust compression quality from 0 (Fastest) to 100 (Best)");

			public readonly GUIContent[] mobileCompressionQualityOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Fast"),
				EditorGUIUtility.TextContent("Normal"),
				EditorGUIUtility.TextContent("Best")
			};

			public readonly GUIContent spriteMode = EditorGUIUtility.TextContent("Sprite Mode");

			public readonly GUIContent[] spriteModeOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Single"),
				EditorGUIUtility.TextContent("Multiple"),
				EditorGUIUtility.TextContent("Polygon")
			};

			public readonly GUIContent[] spriteModeOptionsAdvanced = new GUIContent[]
			{
				EditorGUIUtility.TextContent("None"),
				EditorGUIUtility.TextContent("Single"),
				EditorGUIUtility.TextContent("Multiple"),
				EditorGUIUtility.TextContent("Polygon")
			};

			public readonly GUIContent[] spriteMeshTypeOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Full Rect"),
				EditorGUIUtility.TextContent("Tight")
			};

			public readonly GUIContent spritePackingTag = EditorGUIUtility.TextContent("Packing Tag|Tag for the Sprite Packing system.");

			public readonly GUIContent spritePixelsPerUnit = EditorGUIUtility.TextContent("Pixels Per Unit|How many pixels in the sprite correspond to one unit in the world.");

			public readonly GUIContent spriteExtrude = EditorGUIUtility.TextContent("Extrude Edges|How much empty area to leave around the sprite in the generated mesh.");

			public readonly GUIContent spriteMeshType = EditorGUIUtility.TextContent("Mesh Type|Type of sprite mesh to generate.");

			public readonly GUIContent spriteAlignment = EditorGUIUtility.TextContent("Pivot|Sprite pivot point in its localspace. May be used for syncing animation frames of different sizes.");

			public readonly GUIContent[] spriteAlignmentOptions = new GUIContent[]
			{
				EditorGUIUtility.TextContent("Center"),
				EditorGUIUtility.TextContent("Top Left"),
				EditorGUIUtility.TextContent("Top"),
				EditorGUIUtility.TextContent("Top Right"),
				EditorGUIUtility.TextContent("Left"),
				EditorGUIUtility.TextContent("Right"),
				EditorGUIUtility.TextContent("Bottom Left"),
				EditorGUIUtility.TextContent("Bottom"),
				EditorGUIUtility.TextContent("Bottom Right"),
				EditorGUIUtility.TextContent("Custom")
			};

			public readonly GUIContent alphaIsTransparency = EditorGUIUtility.TextContent("Alpha Is Transparency");

			public readonly GUIContent etc1Compression = EditorGUIUtility.TextContent("Compress using ETC1 (split alpha channel)|This texture will be placed in an atlas that will be compressed using ETC1 compression, provided that the Texture Compression for Android build settings is set to 'ETC (default)'.");
		}

		private SerializedProperty m_TextureType;

		[SerializeField]
		protected List<TextureImporterInspector.PlatformSetting> m_PlatformSettings;

		private static int[] s_TextureFormatsValueAll;

		private static readonly int[] kTextureFormatsValueWeb = new int[]
		{
			10,
			12,
			28,
			29,
			7,
			3,
			1,
			2,
			5
		};

		private static readonly int[] kTextureFormatsValueWiiU = new int[]
		{
			10,
			12,
			7,
			1,
			4,
			13
		};

		private static readonly int[] kTextureFormatsValueiPhone = new int[]
		{
			30,
			31,
			32,
			33,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValuetvOS = new int[]
		{
			30,
			31,
			32,
			33,
			48,
			49,
			50,
			51,
			52,
			53,
			54,
			55,
			56,
			57,
			58,
			59,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValueAndroid = new int[]
		{
			10,
			12,
			28,
			29,
			34,
			45,
			46,
			47,
			30,
			31,
			32,
			33,
			35,
			36,
			48,
			49,
			50,
			51,
			52,
			53,
			54,
			55,
			56,
			57,
			58,
			59,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValueBB10 = new int[]
		{
			34,
			30,
			31,
			32,
			33,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValueTizen = new int[]
		{
			34,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValueSTV = new int[]
		{
			34,
			7,
			3,
			1,
			13,
			4
		};

		private static readonly int[] kTextureFormatsValueGLESEmu = new int[]
		{
			34,
			30,
			31,
			32,
			33,
			45,
			46,
			47,
			48,
			49,
			50,
			51,
			52,
			53,
			54,
			55,
			56,
			57,
			58,
			59,
			7,
			3,
			1,
			13,
			5
		};

		private static int[] s_NormalFormatsValueAll;

		private static readonly int[] kNormalFormatsValueWeb = new int[]
		{
			12,
			29,
			2,
			5
		};

		private static readonly TextureImporterFormat[] kFormatsWithCompressionSettings = new TextureImporterFormat[]
		{
			TextureImporterFormat.DXT1Crunched,
			TextureImporterFormat.DXT5Crunched,
			TextureImporterFormat.PVRTC_RGB2,
			TextureImporterFormat.PVRTC_RGB4,
			TextureImporterFormat.PVRTC_RGBA2,
			TextureImporterFormat.PVRTC_RGBA4,
			TextureImporterFormat.ATC_RGB4,
			TextureImporterFormat.ATC_RGBA8,
			TextureImporterFormat.ETC_RGB4,
			TextureImporterFormat.ETC2_RGB4,
			TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
			TextureImporterFormat.ETC2_RGBA8,
			TextureImporterFormat.ASTC_RGB_4x4,
			TextureImporterFormat.ASTC_RGB_5x5,
			TextureImporterFormat.ASTC_RGB_6x6,
			TextureImporterFormat.ASTC_RGB_8x8,
			TextureImporterFormat.ASTC_RGB_10x10,
			TextureImporterFormat.ASTC_RGB_12x12,
			TextureImporterFormat.ASTC_RGBA_4x4,
			TextureImporterFormat.ASTC_RGBA_5x5,
			TextureImporterFormat.ASTC_RGBA_6x6,
			TextureImporterFormat.ASTC_RGBA_8x8,
			TextureImporterFormat.ASTC_RGBA_10x10,
			TextureImporterFormat.ASTC_RGBA_12x12
		};

		private static string[] s_TextureFormatStringsAll;

		private static string[] s_TextureFormatStringsWiiU;

		private static string[] s_TextureFormatStringsGLESEmu;

		private static string[] s_TextureFormatStringsiPhone;

		private static string[] s_TextureFormatStringstvOS;

		private static string[] s_TextureFormatStringsAndroid;

		private static string[] s_TextureFormatStringsBB10;

		private static string[] s_TextureFormatStringsTizen;

		private static string[] s_TextureFormatStringsSTV;

		private static string[] s_TextureFormatStringsWeb;

		private static string[] s_NormalFormatStringsAll;

		private static string[] s_NormalFormatStringsWeb;

		private readonly AnimBool m_ShowBumpGenerationSettings = new AnimBool();

		private readonly AnimBool m_ShowCookieCubeMapSettings = new AnimBool();

		private readonly AnimBool m_ShowConvolutionCubeMapSettings = new AnimBool();

		private readonly AnimBool m_ShowGenericSpriteSettings = new AnimBool();

		private readonly AnimBool m_ShowManualAtlasGenerationSettings = new AnimBool();

		private readonly GUIContent m_EmptyContent = new GUIContent(" ");

		private readonly int[] m_TextureTypeValues = new int[]
		{
			0,
			1,
			2,
			8,
			7,
			3,
			4,
			6,
			5
		};

		private readonly int[] m_TextureFormatValues = new int[]
		{
			0,
			1,
			2,
			4
		};

		private readonly int[] m_FilterModeOptions = (int[])Enum.GetValues(typeof(FilterMode));

		private string m_ImportWarning;

		private static TextureImporterInspector.Styles s_Styles;

		private SerializedProperty m_GrayscaleToAlpha;

		private SerializedProperty m_ConvertToNormalMap;

		private SerializedProperty m_NormalMap;

		private SerializedProperty m_HeightScale;

		private SerializedProperty m_NormalMapFilter;

		private SerializedProperty m_GenerateCubemap;

		private SerializedProperty m_CubemapConvolution;

		private SerializedProperty m_CubemapConvolutionSteps;

		private SerializedProperty m_CubemapConvolutionExponent;

		private SerializedProperty m_SeamlessCubemap;

		private SerializedProperty m_BorderMipMap;

		private SerializedProperty m_NPOTScale;

		private SerializedProperty m_IsReadable;

		private SerializedProperty m_LinearTexture;

		private SerializedProperty m_RGBM;

		private SerializedProperty m_EnableMipMap;

		private SerializedProperty m_GenerateMipsInLinearSpace;

		private SerializedProperty m_MipMapMode;

		private SerializedProperty m_FadeOut;

		private SerializedProperty m_MipMapFadeDistanceStart;

		private SerializedProperty m_MipMapFadeDistanceEnd;

		private SerializedProperty m_Lightmap;

		private SerializedProperty m_Aniso;

		private SerializedProperty m_FilterMode;

		private SerializedProperty m_WrapMode;

		private SerializedProperty m_SpriteMode;

		private SerializedProperty m_SpritePackingTag;

		private SerializedProperty m_SpritePixelsToUnits;

		private SerializedProperty m_SpriteExtrude;

		private SerializedProperty m_SpriteMeshType;

		private SerializedProperty m_Alignment;

		private SerializedProperty m_SpritePivot;

		private SerializedProperty m_AlphaIsTransparency;

		private static readonly string[] kMaxTextureSizeStrings = new string[]
		{
			"32",
			"64",
			"128",
			"256",
			"512",
			"1024",
			"2048",
			"4096",
			"8192"
		};

		private static readonly int[] kMaxTextureSizeValues = new int[]
		{
			32,
			64,
			128,
			256,
			512,
			1024,
			2048,
			4096,
			8192
		};

		private TextureImporterType textureType
		{
			get
			{
				if (this.textureTypeHasMultipleDifferentValues)
				{
					return (TextureImporterType)(-1);
				}
				if (this.m_TextureType.intValue < 0)
				{
					return (this.target as TextureImporter).textureType;
				}
				return (TextureImporterType)this.m_TextureType.intValue;
			}
		}

		private bool textureTypeHasMultipleDifferentValues
		{
			get
			{
				if (this.m_TextureType.hasMultipleDifferentValues)
				{
					return true;
				}
				if (this.m_TextureType.intValue >= 0)
				{
					return false;
				}
				TextureImporterType textureType = (this.target as TextureImporter).textureType;
				UnityEngine.Object[] targets = base.targets;
				for (int i = 0; i < targets.Length; i++)
				{
					UnityEngine.Object @object = targets[i];
					if ((@object as TextureImporter).textureType != textureType)
					{
						return true;
					}
				}
				return false;
			}
		}

		internal override bool showImportedObject
		{
			get
			{
				return false;
			}
		}

		private static int[] TextureFormatsValueAll
		{
			get
			{
				if (TextureImporterInspector.s_TextureFormatsValueAll != null)
				{
					return TextureImporterInspector.s_TextureFormatsValueAll;
				}
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				BuildPlayerWindow.BuildPlatform[] buildPlayerValidPlatforms = TextureImporterInspector.GetBuildPlayerValidPlatforms();
				BuildPlayerWindow.BuildPlatform[] array = buildPlayerValidPlatforms;
				for (int i = 0; i < array.Length; i++)
				{
					BuildPlayerWindow.BuildPlatform buildPlatform = array[i];
					BuildTarget defaultTarget = buildPlatform.DefaultTarget;
					switch (defaultTarget)
					{
					case BuildTarget.iOS:
						flag2 = true;
						goto IL_DC;
					case BuildTarget.PS3:
					case BuildTarget.XBOX360:
					case (BuildTarget)12:
						IL_60:
						switch (defaultTarget)
						{
						case BuildTarget.SamsungTV:
							flag = true;
							goto IL_DC;
						case BuildTarget.Nintendo3DS:
						case BuildTarget.WiiU:
							IL_7A:
							if (defaultTarget == BuildTarget.BlackBerry)
							{
								flag2 = true;
								flag = true;
								goto IL_DC;
							}
							if (defaultTarget != BuildTarget.Tizen)
							{
								goto IL_DC;
							}
							flag = true;
							goto IL_DC;
						case BuildTarget.tvOS:
							flag2 = true;
							flag5 = true;
							goto IL_DC;
						}
						goto IL_7A;
					case BuildTarget.Android:
						flag2 = true;
						flag = true;
						flag3 = true;
						flag4 = true;
						flag5 = true;
						goto IL_DC;
					case BuildTarget.StandaloneGLESEmu:
						flag2 = true;
						flag = true;
						flag4 = true;
						flag5 = true;
						goto IL_DC;
					}
					goto IL_60;
					IL_DC:;
				}
				List<int> list = new List<int>();
				list.AddRange(new int[]
				{
					-1,
					10,
					12
				});
				if (flag)
				{
					list.Add(34);
				}
				if (flag2)
				{
					list.AddRange(new int[]
					{
						30,
						31,
						32,
						33
					});
				}
				if (flag3)
				{
					list.AddRange(new int[]
					{
						35,
						36
					});
				}
				if (flag4)
				{
					list.AddRange(new int[]
					{
						45,
						46,
						47
					});
				}
				if (flag5)
				{
					list.AddRange(new int[]
					{
						48,
						49,
						50,
						51,
						52,
						53,
						54,
						55,
						56,
						57,
						58,
						59
					});
				}
				list.AddRange(new int[]
				{
					-2,
					7,
					2,
					13,
					-3,
					3,
					1,
					5,
					4,
					-5,
					28,
					29
				});
				TextureImporterInspector.s_TextureFormatsValueAll = list.ToArray();
				return TextureImporterInspector.s_TextureFormatsValueAll;
			}
		}

		private static int[] NormalFormatsValueAll
		{
			get
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				BuildPlayerWindow.BuildPlatform[] buildPlayerValidPlatforms = TextureImporterInspector.GetBuildPlayerValidPlatforms();
				BuildPlayerWindow.BuildPlatform[] array = buildPlayerValidPlatforms;
				for (int i = 0; i < array.Length; i++)
				{
					BuildPlayerWindow.BuildPlatform buildPlatform = array[i];
					BuildTarget defaultTarget = buildPlatform.DefaultTarget;
					switch (defaultTarget)
					{
					case BuildTarget.iOS:
						flag2 = true;
						flag = true;
						goto IL_B8;
					case BuildTarget.PS3:
					case BuildTarget.XBOX360:
					case (BuildTarget)12:
						IL_50:
						if (defaultTarget == BuildTarget.BlackBerry)
						{
							flag2 = true;
							flag = true;
							goto IL_B8;
						}
						if (defaultTarget == BuildTarget.Tizen)
						{
							flag = true;
							goto IL_B8;
						}
						if (defaultTarget != BuildTarget.tvOS)
						{
							goto IL_B8;
						}
						flag2 = true;
						flag = true;
						flag5 = true;
						goto IL_B8;
					case BuildTarget.Android:
						flag2 = true;
						flag3 = true;
						flag = true;
						flag4 = true;
						flag5 = true;
						goto IL_B8;
					case BuildTarget.StandaloneGLESEmu:
						flag2 = true;
						flag = true;
						flag4 = true;
						flag5 = true;
						goto IL_B8;
					}
					goto IL_50;
					IL_B8:;
				}
				List<int> list = new List<int>();
				list.AddRange(new int[]
				{
					-1,
					12
				});
				if (flag2)
				{
					list.AddRange(new int[]
					{
						30,
						31,
						32,
						33
					});
				}
				if (flag3)
				{
					list.AddRange(new int[]
					{
						35,
						36
					});
				}
				if (flag)
				{
					list.AddRange(new int[]
					{
						34
					});
				}
				if (flag4)
				{
					list.AddRange(new int[]
					{
						45,
						46,
						47
					});
				}
				if (flag5)
				{
					list.AddRange(new int[]
					{
						48,
						49,
						50,
						51,
						52,
						53,
						54,
						55,
						56,
						57,
						58,
						59
					});
				}
				list.AddRange(new int[]
				{
					-2,
					2,
					13,
					-3,
					4,
					-5,
					29
				});
				TextureImporterInspector.s_NormalFormatsValueAll = list.ToArray();
				return TextureImporterInspector.s_NormalFormatsValueAll;
			}
		}

		public new void OnDisable()
		{
			base.OnDisable();
		}

		internal static bool IsGLESMobileTargetPlatform(BuildTarget target)
		{
			return target == BuildTarget.iOS || target == BuildTarget.Android || target == BuildTarget.BlackBerry || target == BuildTarget.Tizen || target == BuildTarget.SamsungTV || target == BuildTarget.tvOS;
		}

		private void UpdateImportWarning()
		{
			TextureImporter textureImporter = this.target as TextureImporter;
			this.m_ImportWarning = ((!textureImporter) ? null : textureImporter.GetImportWarnings());
		}

		private void ToggleFromInt(SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			int intValue = (!EditorGUILayout.Toggle(label, property.intValue > 0, new GUILayoutOption[0])) ? 0 : 1;
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
		}

		private void EnumPopup(SerializedProperty property, Type type, GUIContent label)
		{
			EditorGUILayout.IntPopup(property, EditorGUIUtility.TempContent(Enum.GetNames(type)), Enum.GetValues(type) as int[], label, new GUILayoutOption[0]);
		}

		private void CacheSerializedProperties()
		{
			this.m_TextureType = base.serializedObject.FindProperty("m_TextureType");
			this.m_GrayscaleToAlpha = base.serializedObject.FindProperty("m_GrayScaleToAlpha");
			this.m_ConvertToNormalMap = base.serializedObject.FindProperty("m_ConvertToNormalMap");
			this.m_NormalMap = base.serializedObject.FindProperty("m_ExternalNormalMap");
			this.m_HeightScale = base.serializedObject.FindProperty("m_HeightScale");
			this.m_NormalMapFilter = base.serializedObject.FindProperty("m_NormalMapFilter");
			this.m_GenerateCubemap = base.serializedObject.FindProperty("m_GenerateCubemap");
			this.m_SeamlessCubemap = base.serializedObject.FindProperty("m_SeamlessCubemap");
			this.m_BorderMipMap = base.serializedObject.FindProperty("m_BorderMipMap");
			this.m_NPOTScale = base.serializedObject.FindProperty("m_NPOTScale");
			this.m_IsReadable = base.serializedObject.FindProperty("m_IsReadable");
			this.m_LinearTexture = base.serializedObject.FindProperty("m_LinearTexture");
			this.m_RGBM = base.serializedObject.FindProperty("m_RGBM");
			this.m_EnableMipMap = base.serializedObject.FindProperty("m_EnableMipMap");
			this.m_MipMapMode = base.serializedObject.FindProperty("m_MipMapMode");
			this.m_GenerateMipsInLinearSpace = base.serializedObject.FindProperty("correctGamma");
			this.m_FadeOut = base.serializedObject.FindProperty("m_FadeOut");
			this.m_MipMapFadeDistanceStart = base.serializedObject.FindProperty("m_MipMapFadeDistanceStart");
			this.m_MipMapFadeDistanceEnd = base.serializedObject.FindProperty("m_MipMapFadeDistanceEnd");
			this.m_Lightmap = base.serializedObject.FindProperty("m_Lightmap");
			this.m_Aniso = base.serializedObject.FindProperty("m_TextureSettings.m_Aniso");
			this.m_FilterMode = base.serializedObject.FindProperty("m_TextureSettings.m_FilterMode");
			this.m_WrapMode = base.serializedObject.FindProperty("m_TextureSettings.m_WrapMode");
			this.m_CubemapConvolution = base.serializedObject.FindProperty("m_CubemapConvolution");
			this.m_CubemapConvolutionSteps = base.serializedObject.FindProperty("m_CubemapConvolutionSteps");
			this.m_CubemapConvolutionExponent = base.serializedObject.FindProperty("m_CubemapConvolutionExponent");
			this.m_SpriteMode = base.serializedObject.FindProperty("m_SpriteMode");
			this.m_SpritePackingTag = base.serializedObject.FindProperty("m_SpritePackingTag");
			this.m_SpritePixelsToUnits = base.serializedObject.FindProperty("m_SpritePixelsToUnits");
			this.m_SpriteExtrude = base.serializedObject.FindProperty("m_SpriteExtrude");
			this.m_SpriteMeshType = base.serializedObject.FindProperty("m_SpriteMeshType");
			this.m_Alignment = base.serializedObject.FindProperty("m_Alignment");
			this.m_SpritePivot = base.serializedObject.FindProperty("m_SpritePivot");
			this.m_AlphaIsTransparency = base.serializedObject.FindProperty("m_AlphaIsTransparency");
		}

		public virtual void OnEnable()
		{
			this.CacheSerializedProperties();
			this.m_ShowBumpGenerationSettings.valueChanged.AddListener(new UnityAction(base.Repaint));
			this.m_ShowCookieCubeMapSettings.valueChanged.AddListener(new UnityAction(base.Repaint));
			this.m_ShowCookieCubeMapSettings.value = (this.textureType == TextureImporterType.Cookie && this.m_GenerateCubemap.intValue != 0);
			this.m_ShowConvolutionCubeMapSettings.value = (this.m_CubemapConvolution.intValue == 1 && this.m_GenerateCubemap.intValue != 0);
			this.m_ShowGenericSpriteSettings.valueChanged.AddListener(new UnityAction(base.Repaint));
			this.m_ShowManualAtlasGenerationSettings.valueChanged.AddListener(new UnityAction(base.Repaint));
			this.m_ShowGenericSpriteSettings.value = (this.m_SpriteMode.intValue != 0);
			this.m_ShowManualAtlasGenerationSettings.value = (this.m_SpriteMode.intValue == 2);
		}

		private void SetSerializedPropertySettings(TextureImporterSettings settings)
		{
			this.m_GrayscaleToAlpha.intValue = ((!settings.grayscaleToAlpha) ? 0 : 1);
			this.m_ConvertToNormalMap.intValue = ((!settings.convertToNormalMap) ? 0 : 1);
			this.m_NormalMap.intValue = ((!settings.normalMap) ? 0 : 1);
			this.m_HeightScale.floatValue = settings.heightmapScale;
			this.m_NormalMapFilter.intValue = (int)settings.normalMapFilter;
			this.m_GenerateCubemap.intValue = (int)settings.generateCubemap;
			this.m_CubemapConvolution.intValue = (int)settings.cubemapConvolution;
			this.m_CubemapConvolutionSteps.intValue = settings.cubemapConvolutionSteps;
			this.m_CubemapConvolutionExponent.floatValue = settings.cubemapConvolutionExponent;
			this.m_SeamlessCubemap.intValue = ((!settings.seamlessCubemap) ? 0 : 1);
			this.m_BorderMipMap.intValue = ((!settings.borderMipmap) ? 0 : 1);
			this.m_NPOTScale.intValue = (int)settings.npotScale;
			this.m_IsReadable.intValue = ((!settings.readable) ? 0 : 1);
			this.m_EnableMipMap.intValue = ((!settings.mipmapEnabled) ? 0 : 1);
			this.m_LinearTexture.intValue = ((!settings.linearTexture) ? 0 : 1);
			this.m_RGBM.intValue = (int)settings.rgbm;
			this.m_MipMapMode.intValue = (int)settings.mipmapFilter;
			this.m_GenerateMipsInLinearSpace.intValue = ((!settings.generateMipsInLinearSpace) ? 0 : 1);
			this.m_FadeOut.intValue = ((!settings.fadeOut) ? 0 : 1);
			this.m_MipMapFadeDistanceStart.intValue = settings.mipmapFadeDistanceStart;
			this.m_MipMapFadeDistanceEnd.intValue = settings.mipmapFadeDistanceEnd;
			this.m_Lightmap.intValue = ((!settings.lightmap) ? 0 : 1);
			this.m_SpriteMode.intValue = settings.spriteMode;
			this.m_SpritePixelsToUnits.floatValue = settings.spritePixelsPerUnit;
			this.m_SpriteExtrude.intValue = (int)settings.spriteExtrude;
			this.m_SpriteMeshType.intValue = (int)settings.spriteMeshType;
			this.m_Alignment.intValue = settings.spriteAlignment;
			this.m_WrapMode.intValue = (int)settings.wrapMode;
			this.m_FilterMode.intValue = (int)settings.filterMode;
			this.m_Aniso.intValue = settings.aniso;
			this.m_AlphaIsTransparency.intValue = ((!settings.alphaIsTransparency) ? 0 : 1);
		}

		private TextureImporterSettings GetSerializedPropertySettings()
		{
			return this.GetSerializedPropertySettings(new TextureImporterSettings());
		}

		private TextureImporterSettings GetSerializedPropertySettings(TextureImporterSettings settings)
		{
			if (!this.m_GrayscaleToAlpha.hasMultipleDifferentValues)
			{
				settings.grayscaleToAlpha = (this.m_GrayscaleToAlpha.intValue > 0);
			}
			if (!this.m_ConvertToNormalMap.hasMultipleDifferentValues)
			{
				settings.convertToNormalMap = (this.m_ConvertToNormalMap.intValue > 0);
			}
			if (!this.m_NormalMap.hasMultipleDifferentValues)
			{
				settings.normalMap = (this.m_NormalMap.intValue > 0);
			}
			if (!this.m_HeightScale.hasMultipleDifferentValues)
			{
				settings.heightmapScale = this.m_HeightScale.floatValue;
			}
			if (!this.m_NormalMapFilter.hasMultipleDifferentValues)
			{
				settings.normalMapFilter = (TextureImporterNormalFilter)this.m_NormalMapFilter.intValue;
			}
			if (!this.m_GenerateCubemap.hasMultipleDifferentValues)
			{
				settings.generateCubemap = (TextureImporterGenerateCubemap)this.m_GenerateCubemap.intValue;
			}
			if (!this.m_CubemapConvolution.hasMultipleDifferentValues)
			{
				settings.cubemapConvolution = (TextureImporterCubemapConvolution)this.m_CubemapConvolution.intValue;
			}
			if (!this.m_CubemapConvolutionSteps.hasMultipleDifferentValues)
			{
				settings.cubemapConvolutionSteps = this.m_CubemapConvolutionSteps.intValue;
			}
			if (!this.m_CubemapConvolutionExponent.hasMultipleDifferentValues)
			{
				settings.cubemapConvolutionExponent = this.m_CubemapConvolutionExponent.floatValue;
			}
			if (!this.m_SeamlessCubemap.hasMultipleDifferentValues)
			{
				settings.seamlessCubemap = (this.m_SeamlessCubemap.intValue > 0);
			}
			if (!this.m_BorderMipMap.hasMultipleDifferentValues)
			{
				settings.borderMipmap = (this.m_BorderMipMap.intValue > 0);
			}
			if (!this.m_NPOTScale.hasMultipleDifferentValues)
			{
				settings.npotScale = (TextureImporterNPOTScale)this.m_NPOTScale.intValue;
			}
			if (!this.m_IsReadable.hasMultipleDifferentValues)
			{
				settings.readable = (this.m_IsReadable.intValue > 0);
			}
			if (!this.m_LinearTexture.hasMultipleDifferentValues)
			{
				settings.linearTexture = (this.m_LinearTexture.intValue > 0);
			}
			if (!this.m_RGBM.hasMultipleDifferentValues)
			{
				settings.rgbm = (TextureImporterRGBMMode)this.m_RGBM.intValue;
			}
			if (!this.m_EnableMipMap.hasMultipleDifferentValues)
			{
				settings.mipmapEnabled = (this.m_EnableMipMap.intValue > 0);
			}
			if (!this.m_GenerateMipsInLinearSpace.hasMultipleDifferentValues)
			{
				settings.generateMipsInLinearSpace = (this.m_GenerateMipsInLinearSpace.intValue > 0);
			}
			if (!this.m_MipMapMode.hasMultipleDifferentValues)
			{
				settings.mipmapFilter = (TextureImporterMipFilter)this.m_MipMapMode.intValue;
			}
			if (!this.m_FadeOut.hasMultipleDifferentValues)
			{
				settings.fadeOut = (this.m_FadeOut.intValue > 0);
			}
			if (!this.m_MipMapFadeDistanceStart.hasMultipleDifferentValues)
			{
				settings.mipmapFadeDistanceStart = this.m_MipMapFadeDistanceStart.intValue;
			}
			if (!this.m_MipMapFadeDistanceEnd.hasMultipleDifferentValues)
			{
				settings.mipmapFadeDistanceEnd = this.m_MipMapFadeDistanceEnd.intValue;
			}
			if (!this.m_Lightmap.hasMultipleDifferentValues)
			{
				settings.lightmap = (this.m_Lightmap.intValue > 0);
			}
			if (!this.m_SpriteMode.hasMultipleDifferentValues)
			{
				settings.spriteMode = this.m_SpriteMode.intValue;
			}
			if (!this.m_SpritePixelsToUnits.hasMultipleDifferentValues)
			{
				settings.spritePixelsPerUnit = this.m_SpritePixelsToUnits.floatValue;
			}
			if (!this.m_SpriteExtrude.hasMultipleDifferentValues)
			{
				settings.spriteExtrude = (uint)this.m_SpriteExtrude.intValue;
			}
			if (!this.m_SpriteMeshType.hasMultipleDifferentValues)
			{
				settings.spriteMeshType = (SpriteMeshType)this.m_SpriteMeshType.intValue;
			}
			if (!this.m_Alignment.hasMultipleDifferentValues)
			{
				settings.spriteAlignment = this.m_Alignment.intValue;
			}
			if (!this.m_SpritePivot.hasMultipleDifferentValues)
			{
				settings.spritePivot = this.m_SpritePivot.vector2Value;
			}
			if (!this.m_WrapMode.hasMultipleDifferentValues)
			{
				settings.wrapMode = (TextureWrapMode)this.m_WrapMode.intValue;
			}
			if (!this.m_FilterMode.hasMultipleDifferentValues)
			{
				settings.filterMode = (FilterMode)this.m_FilterMode.intValue;
			}
			if (!this.m_Aniso.hasMultipleDifferentValues)
			{
				settings.aniso = this.m_Aniso.intValue;
			}
			if (!this.m_AlphaIsTransparency.hasMultipleDifferentValues)
			{
				settings.alphaIsTransparency = (this.m_AlphaIsTransparency.intValue > 0);
			}
			return settings;
		}

		public override void OnInspectorGUI()
		{
			if (TextureImporterInspector.s_Styles == null)
			{
				TextureImporterInspector.s_Styles = new TextureImporterInspector.Styles();
			}
			bool enabled = GUI.enabled;
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = this.textureTypeHasMultipleDifferentValues;
			int intValue = EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.textureType, (int)this.textureType, TextureImporterInspector.s_Styles.textureTypeOptions, this.m_TextureTypeValues, new GUILayoutOption[0]);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				this.m_TextureType.intValue = intValue;
				TextureImporterSettings serializedPropertySettings = this.GetSerializedPropertySettings();
				serializedPropertySettings.ApplyTextureType(this.textureType, true);
				this.SetSerializedPropertySettings(serializedPropertySettings);
				this.SyncPlatformSettings();
				this.ApplySettingsToTexture();
			}
			if (!this.textureTypeHasMultipleDifferentValues)
			{
				switch (this.textureType)
				{
				case TextureImporterType.Image:
					this.ImageGUI();
					break;
				case TextureImporterType.Bump:
					this.BumpGUI();
					break;
				case TextureImporterType.Cubemap:
					this.CubemapGUI();
					break;
				case TextureImporterType.Cookie:
					this.CookieGUI();
					break;
				case TextureImporterType.Advanced:
					this.AdvancedGUI();
					break;
				case TextureImporterType.Sprite:
					this.SpriteGUI();
					break;
				}
			}
			EditorGUILayout.Space();
			this.PreviewableGUI();
			this.SizeAndFormatGUI();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			base.ApplyRevertGUI();
			GUILayout.EndHorizontal();
			this.UpdateImportWarning();
			if (this.m_ImportWarning != null)
			{
				EditorGUILayout.HelpBox(this.m_ImportWarning, MessageType.Warning);
			}
			GUI.enabled = enabled;
		}

		private void PreviewableGUI()
		{
			EditorGUI.BeginChangeCheck();
			if (this.textureType != TextureImporterType.GUI && this.textureType != TextureImporterType.Sprite && this.textureType != TextureImporterType.Cubemap && this.textureType != TextureImporterType.Cookie && this.textureType != TextureImporterType.Lightmap)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = this.m_WrapMode.hasMultipleDifferentValues;
				TextureWrapMode textureWrapMode = (TextureWrapMode)this.m_WrapMode.intValue;
				if (textureWrapMode == (TextureWrapMode)(-1))
				{
					textureWrapMode = TextureWrapMode.Repeat;
				}
				textureWrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(EditorGUIUtility.TempContent("Wrap Mode"), textureWrapMode, new GUILayoutOption[0]);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					this.m_WrapMode.intValue = (int)textureWrapMode;
				}
				if (this.m_NPOTScale.intValue == 0 && textureWrapMode == TextureWrapMode.Repeat && !ShaderUtil.hardwareSupportsFullNPOT)
				{
					bool flag = false;
					UnityEngine.Object[] targets = base.targets;
					for (int i = 0; i < targets.Length; i++)
					{
						UnityEngine.Object @object = targets[i];
						int value = -1;
						int value2 = -1;
						TextureImporter textureImporter = (TextureImporter)@object;
						textureImporter.GetWidthAndHeight(ref value, ref value2);
						if (!Mathf.IsPowerOfTwo(value) || !Mathf.IsPowerOfTwo(value2))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						GUIContent gUIContent = EditorGUIUtility.TextContent("Graphics device doesn't support Repeat wrap mode on NPOT textures. Falling back to Clamp.");
						EditorGUILayout.HelpBox(gUIContent.text, MessageType.Warning, true);
					}
				}
			}
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = this.m_FilterMode.hasMultipleDifferentValues;
			FilterMode filterMode = (FilterMode)this.m_FilterMode.intValue;
			if (filterMode == (FilterMode)(-1))
			{
				if (this.m_FadeOut.intValue > 0 || this.m_ConvertToNormalMap.intValue > 0 || this.m_NormalMap.intValue > 0)
				{
					filterMode = FilterMode.Trilinear;
				}
				else
				{
					filterMode = FilterMode.Bilinear;
				}
			}
			filterMode = (FilterMode)EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.filterMode, (int)filterMode, TextureImporterInspector.s_Styles.filterModeOptions, this.m_FilterModeOptions, new GUILayoutOption[0]);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				this.m_FilterMode.intValue = (int)filterMode;
			}
			if (filterMode != FilterMode.Point && (this.m_EnableMipMap.intValue > 0 || this.textureType == TextureImporterType.Advanced) && this.textureType != TextureImporterType.Sprite && this.textureType != TextureImporterType.Cubemap)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = this.m_Aniso.hasMultipleDifferentValues;
				int num = this.m_Aniso.intValue;
				if (num == -1)
				{
					num = 1;
				}
				num = EditorGUILayout.IntSlider("Aniso Level", num, 0, 16, new GUILayoutOption[0]);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					this.m_Aniso.intValue = num;
				}
				TextureInspector.DoAnisoGlobalSettingNote(num);
			}
			if (EditorGUI.EndChangeCheck())
			{
				this.ApplySettingsToTexture();
			}
		}

		private void ApplySettingsToTexture()
		{
			UnityEngine.Object[] targets = base.targets;
			for (int i = 0; i < targets.Length; i++)
			{
				AssetImporter assetImporter = (AssetImporter)targets[i];
				Texture tex = AssetDatabase.LoadMainAssetAtPath(assetImporter.assetPath) as Texture;
				if (this.m_Aniso.intValue != -1)
				{
					TextureUtil.SetAnisoLevelNoDirty(tex, this.m_Aniso.intValue);
				}
				if (this.m_FilterMode.intValue != -1)
				{
					TextureUtil.SetFilterModeNoDirty(tex, (FilterMode)this.m_FilterMode.intValue);
				}
				if (this.m_WrapMode.intValue != -1)
				{
					TextureUtil.SetWrapModeNoDirty(tex, (TextureWrapMode)this.m_WrapMode.intValue);
				}
			}
			SceneView.RepaintAll();
		}

		private static bool CountImportersWithAlpha(UnityEngine.Object[] importers, out int count)
		{
			bool result;
			try
			{
				count = 0;
				for (int i = 0; i < importers.Length; i++)
				{
					UnityEngine.Object @object = importers[i];
					if ((@object as TextureImporter).DoesSourceTextureHaveAlpha())
					{
						count++;
					}
				}
				result = true;
			}
			catch
			{
				count = importers.Length;
				result = false;
			}
			return result;
		}

		private void DoAlphaIsTransparencyGUI()
		{
			int num;
			bool flag = TextureImporterInspector.CountImportersWithAlpha(base.targets, out num);
			bool flag2 = this.m_GrayscaleToAlpha.boolValue || num == base.targets.Length;
			if (flag2)
			{
				EditorGUI.BeginDisabledGroup(!flag);
				this.ToggleFromInt(this.m_AlphaIsTransparency, TextureImporterInspector.s_Styles.alphaIsTransparency);
				EditorGUI.EndDisabledGroup();
			}
		}

		private void ImageGUI()
		{
			TextureImporter x = this.target as TextureImporter;
			if (x == null)
			{
				return;
			}
			this.ToggleFromInt(this.m_GrayscaleToAlpha, TextureImporterInspector.s_Styles.generateAlphaFromGrayscale);
			this.DoAlphaIsTransparencyGUI();
		}

		private void BumpGUI()
		{
			this.ToggleFromInt(this.m_ConvertToNormalMap, TextureImporterInspector.s_Styles.generateFromBump);
			this.m_ShowBumpGenerationSettings.target = (this.m_ConvertToNormalMap.intValue > 0);
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowBumpGenerationSettings.faded))
			{
				EditorGUILayout.Slider(this.m_HeightScale, 0f, 0.3f, TextureImporterInspector.s_Styles.bumpiness, new GUILayoutOption[0]);
				EditorGUILayout.Popup(this.m_NormalMapFilter, TextureImporterInspector.s_Styles.bumpFilteringOptions, TextureImporterInspector.s_Styles.bumpFiltering, new GUILayoutOption[0]);
			}
			EditorGUILayout.EndFadeGroup();
		}

		private void SpriteGUI()
		{
			this.SpriteGUI(true);
		}

		private void SpriteGUI(bool showMipMapControls)
		{
			EditorGUI.BeginChangeCheck();
			if (this.textureType == TextureImporterType.Advanced)
			{
				EditorGUILayout.IntPopup(this.m_SpriteMode, TextureImporterInspector.s_Styles.spriteModeOptionsAdvanced, new int[]
				{
					0,
					1,
					2,
					3
				}, TextureImporterInspector.s_Styles.spriteMode, new GUILayoutOption[0]);
			}
			else
			{
				EditorGUILayout.IntPopup(this.m_SpriteMode, TextureImporterInspector.s_Styles.spriteModeOptions, new int[]
				{
					1,
					2,
					3
				}, TextureImporterInspector.s_Styles.spriteMode, new GUILayoutOption[0]);
			}
			if (EditorGUI.EndChangeCheck())
			{
				GUIUtility.keyboardControl = 0;
			}
			EditorGUI.indentLevel++;
			this.m_ShowGenericSpriteSettings.target = (this.m_SpriteMode.intValue != 0);
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowGenericSpriteSettings.faded))
			{
				EditorGUILayout.PropertyField(this.m_SpritePackingTag, TextureImporterInspector.s_Styles.spritePackingTag, new GUILayoutOption[0]);
				EditorGUILayout.PropertyField(this.m_SpritePixelsToUnits, TextureImporterInspector.s_Styles.spritePixelsPerUnit, new GUILayoutOption[0]);
				if (this.textureType == TextureImporterType.Advanced)
				{
					EditorGUILayout.IntPopup(this.m_SpriteMeshType, TextureImporterInspector.s_Styles.spriteMeshTypeOptions, new int[]
					{
						0,
						1
					}, TextureImporterInspector.s_Styles.spriteMeshType, new GUILayoutOption[0]);
					EditorGUILayout.IntSlider(this.m_SpriteExtrude, 0, 32, TextureImporterInspector.s_Styles.spriteExtrude, new GUILayoutOption[0]);
				}
				if (this.m_SpriteMode.intValue == 1)
				{
					EditorGUILayout.Popup(this.m_Alignment, TextureImporterInspector.s_Styles.spriteAlignmentOptions, TextureImporterInspector.s_Styles.spriteAlignment, new GUILayoutOption[0]);
					if (this.m_Alignment.intValue == 9)
					{
						GUILayout.BeginHorizontal(new GUILayoutOption[0]);
						EditorGUILayout.PropertyField(this.m_SpritePivot, this.m_EmptyContent, new GUILayoutOption[0]);
						GUILayout.EndHorizontal();
					}
				}
				EditorGUI.BeginDisabledGroup(base.targets.Length != 1);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Sprite Editor", new GUILayoutOption[0]))
				{
					if (this.HasModified())
					{
						string text = "Unapplied import settings for '" + ((TextureImporter)this.target).assetPath + "'.\n";
						text += "Apply and continue to sprite editor or cancel.";
						if (EditorUtility.DisplayDialog("Unapplied import settings", text, "Apply", "Cancel"))
						{
							base.ApplyAndImport();
							SpriteEditorWindow.GetWindow();
							GUIUtility.ExitGUI();
						}
					}
					else
					{
						SpriteEditorWindow.GetWindow();
					}
				}
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.EndFadeGroup();
			EditorGUI.indentLevel--;
			if (showMipMapControls)
			{
				this.ToggleFromInt(this.m_EnableMipMap, TextureImporterInspector.s_Styles.generateMipMaps);
			}
		}

		private void CubemapGUI()
		{
			this.CubemapMappingGUI(false);
		}

		private void CubemapMappingGUI(bool advancedMode)
		{
			EditorGUI.showMixedValue = (this.m_GenerateCubemap.hasMultipleDifferentValues || this.m_SeamlessCubemap.hasMultipleDifferentValues);
			EditorGUI.BeginChangeCheck();
			int count = (!advancedMode) ? 1 : 0;
			List<GUIContent> list = new List<GUIContent>(TextureImporterInspector.s_Styles.cubemapOptions);
			list.RemoveRange(0, count);
			List<int> list2 = new List<int>(TextureImporterInspector.s_Styles.cubemapValues);
			list2.RemoveRange(0, count);
			int num = EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.cubemap, this.m_GenerateCubemap.intValue, list.ToArray(), list2.ToArray(), new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				this.m_GenerateCubemap.intValue = num;
			}
			EditorGUI.BeginDisabledGroup(num == 0);
			if (advancedMode)
			{
				EditorGUI.indentLevel++;
			}
			if (advancedMode)
			{
				EditorGUILayout.IntPopup(this.m_CubemapConvolution, TextureImporterInspector.s_Styles.cubemapConvolutionOptions, TextureImporterInspector.s_Styles.cubemapConvolutionValues.ToArray<int>(), TextureImporterInspector.s_Styles.cubemapConvolution, new GUILayoutOption[0]);
			}
			else
			{
				this.ToggleFromInt(this.m_CubemapConvolution, TextureImporterInspector.s_Styles.cubemapConvolutionSimple);
				if (this.m_CubemapConvolution.intValue != 0)
				{
					this.m_CubemapConvolution.intValue = 1;
				}
			}
			if (advancedMode)
			{
				this.m_ShowConvolutionCubeMapSettings.target = (this.m_CubemapConvolution.intValue == 1);
				if (EditorGUILayout.BeginFadeGroup(this.m_ShowConvolutionCubeMapSettings.faded))
				{
					EditorGUI.indentLevel++;
					this.m_CubemapConvolutionSteps.intValue = EditorGUILayout.IntField(TextureImporterInspector.s_Styles.cubemapConvolutionSteps, this.m_CubemapConvolutionSteps.intValue, new GUILayoutOption[0]);
					this.m_CubemapConvolutionExponent.floatValue = EditorGUILayout.FloatField(TextureImporterInspector.s_Styles.cubemapConvolutionExp, this.m_CubemapConvolutionExponent.floatValue, new GUILayoutOption[0]);
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFadeGroup();
			}
			this.ToggleFromInt(this.m_SeamlessCubemap, TextureImporterInspector.s_Styles.seamlessCubemap);
			if (advancedMode)
			{
				EditorGUI.indentLevel--;
			}
			EditorGUI.EndDisabledGroup();
			EditorGUI.showMixedValue = false;
		}

		private void CookieGUI()
		{
			EditorGUI.BeginChangeCheck();
			TextureImporterInspector.CookieMode cookieMode;
			if (this.m_BorderMipMap.intValue > 0)
			{
				cookieMode = TextureImporterInspector.CookieMode.Spot;
			}
			else if (this.m_GenerateCubemap.intValue != 0)
			{
				cookieMode = TextureImporterInspector.CookieMode.Point;
			}
			else
			{
				cookieMode = TextureImporterInspector.CookieMode.Directional;
			}
			cookieMode = (TextureImporterInspector.CookieMode)EditorGUILayout.Popup(TextureImporterInspector.s_Styles.cookieType, (int)cookieMode, TextureImporterInspector.s_Styles.cookieOptions, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				this.SetCookieMode(cookieMode);
			}
			this.m_ShowCookieCubeMapSettings.target = (cookieMode == TextureImporterInspector.CookieMode.Point);
			if (EditorGUILayout.BeginFadeGroup(this.m_ShowCookieCubeMapSettings.faded))
			{
				this.CubemapMappingGUI(false);
			}
			EditorGUILayout.EndFadeGroup();
			this.ToggleFromInt(this.m_GrayscaleToAlpha, TextureImporterInspector.s_Styles.generateAlphaFromGrayscale);
		}

		private void SetCookieMode(TextureImporterInspector.CookieMode cm)
		{
			switch (cm)
			{
			case TextureImporterInspector.CookieMode.Spot:
				this.m_BorderMipMap.intValue = 1;
				this.m_WrapMode.intValue = 1;
				this.m_GenerateCubemap.intValue = 0;
				break;
			case TextureImporterInspector.CookieMode.Directional:
				this.m_BorderMipMap.intValue = 0;
				this.m_WrapMode.intValue = 0;
				this.m_GenerateCubemap.intValue = 0;
				break;
			case TextureImporterInspector.CookieMode.Point:
				this.m_BorderMipMap.intValue = 0;
				this.m_WrapMode.intValue = 1;
				this.m_GenerateCubemap.intValue = 1;
				break;
			}
		}

		private void BeginToggleGroup(SerializedProperty property, GUIContent label)
		{
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			int intValue = (!EditorGUILayout.BeginToggleGroup(label, property.intValue > 0)) ? 0 : 1;
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = intValue;
			}
			EditorGUI.showMixedValue = false;
		}

		private void AdvancedGUI()
		{
			TextureImporter textureImporter = this.target as TextureImporter;
			if (textureImporter == null)
			{
				return;
			}
			int f = 0;
			int f2 = 0;
			textureImporter.GetWidthAndHeight(ref f2, ref f);
			bool flag = TextureImporterInspector.IsPowerOfTwo(f) && TextureImporterInspector.IsPowerOfTwo(f2);
			EditorGUI.BeginDisabledGroup(flag);
			this.EnumPopup(this.m_NPOTScale, typeof(TextureImporterNPOTScale), TextureImporterInspector.s_Styles.npot);
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!flag && this.m_NPOTScale.intValue == 0);
			this.CubemapMappingGUI(true);
			EditorGUI.EndDisabledGroup();
			this.ToggleFromInt(this.m_IsReadable, TextureImporterInspector.s_Styles.readWrite);
			TextureImporterInspector.AdvancedTextureType advancedTextureType = TextureImporterInspector.AdvancedTextureType.Default;
			if (this.m_NormalMap.intValue > 0)
			{
				advancedTextureType = TextureImporterInspector.AdvancedTextureType.NormalMap;
			}
			else if (this.m_Lightmap.intValue > 0)
			{
				advancedTextureType = TextureImporterInspector.AdvancedTextureType.LightMap;
			}
			EditorGUI.BeginChangeCheck();
			advancedTextureType = (TextureImporterInspector.AdvancedTextureType)EditorGUILayout.Popup("Import Type", (int)advancedTextureType, new string[]
			{
				"Default",
				"Normal Map",
				"Lightmap"
			}, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				switch (advancedTextureType)
				{
				case TextureImporterInspector.AdvancedTextureType.Default:
					this.m_NormalMap.intValue = 0;
					this.m_Lightmap.intValue = 0;
					this.m_ConvertToNormalMap.intValue = 0;
					break;
				case TextureImporterInspector.AdvancedTextureType.NormalMap:
					this.m_NormalMap.intValue = 1;
					this.m_Lightmap.intValue = 0;
					this.m_LinearTexture.intValue = 1;
					this.m_RGBM.intValue = 0;
					break;
				case TextureImporterInspector.AdvancedTextureType.LightMap:
					this.m_NormalMap.intValue = 0;
					this.m_Lightmap.intValue = 1;
					this.m_ConvertToNormalMap.intValue = 0;
					this.m_LinearTexture.intValue = 1;
					this.m_RGBM.intValue = 0;
					break;
				}
			}
			EditorGUI.indentLevel++;
			if (advancedTextureType == TextureImporterInspector.AdvancedTextureType.NormalMap)
			{
				EditorGUI.BeginChangeCheck();
				this.BumpGUI();
				if (EditorGUI.EndChangeCheck())
				{
					this.SyncPlatformSettings();
				}
			}
			else if (advancedTextureType == TextureImporterInspector.AdvancedTextureType.Default)
			{
				this.ToggleFromInt(this.m_GrayscaleToAlpha, TextureImporterInspector.s_Styles.generateAlphaFromGrayscale);
				this.DoAlphaIsTransparencyGUI();
				this.ToggleFromInt(this.m_LinearTexture, TextureImporterInspector.s_Styles.linearTexture);
				EditorGUILayout.Popup(this.m_RGBM, TextureImporterInspector.s_Styles.rgbmEncodingOptions, TextureImporterInspector.s_Styles.rgbmEncoding, new GUILayoutOption[0]);
				this.SpriteGUI(false);
			}
			EditorGUI.indentLevel--;
			this.ToggleFromInt(this.m_EnableMipMap, TextureImporterInspector.s_Styles.generateMipMaps);
			if (this.m_EnableMipMap.boolValue && !this.m_EnableMipMap.hasMultipleDifferentValues)
			{
				EditorGUI.indentLevel++;
				this.ToggleFromInt(this.m_GenerateMipsInLinearSpace, TextureImporterInspector.s_Styles.mipMapsInLinearSpace);
				this.ToggleFromInt(this.m_BorderMipMap, TextureImporterInspector.s_Styles.borderMipMaps);
				EditorGUILayout.Popup(this.m_MipMapMode, TextureImporterInspector.s_Styles.mipMapFilterOptions, TextureImporterInspector.s_Styles.mipMapFilter, new GUILayoutOption[0]);
				this.ToggleFromInt(this.m_FadeOut, TextureImporterInspector.s_Styles.mipmapFadeOutToggle);
				if (this.m_FadeOut.intValue > 0)
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					float f3 = (float)this.m_MipMapFadeDistanceStart.intValue;
					float f4 = (float)this.m_MipMapFadeDistanceEnd.intValue;
					EditorGUILayout.MinMaxSlider(TextureImporterInspector.s_Styles.mipmapFadeOut, ref f3, ref f4, 0f, 10f, new GUILayoutOption[0]);
					if (EditorGUI.EndChangeCheck())
					{
						this.m_MipMapFadeDistanceStart.intValue = Mathf.RoundToInt(f3);
						this.m_MipMapFadeDistanceEnd.intValue = Mathf.RoundToInt(f4);
					}
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}
		}

		private void SyncPlatformSettings()
		{
			foreach (TextureImporterInspector.PlatformSetting current in this.m_PlatformSettings)
			{
				current.Sync();
			}
		}

		private static string[] BuildTextureStrings(int[] texFormatValues)
		{
			string[] array = new string[texFormatValues.Length];
			int i = 0;
			while (i < texFormatValues.Length)
			{
				int num = texFormatValues[i];
				int num2 = num;
				switch (num2 + 5)
				{
				case 0:
					array[i] = "Automatic Crunched";
					break;
				case 1:
					goto IL_6B;
				case 2:
					array[i] = "Automatic Truecolor";
					break;
				case 3:
					array[i] = "Automatic 16 bits";
					break;
				case 4:
					array[i] = "Automatic Compressed";
					break;
				default:
					goto IL_6B;
				}
				IL_83:
				i++;
				continue;
				IL_6B:
				array[i] = " " + TextureUtil.GetTextureFormatString((TextureFormat)num);
				goto IL_83;
			}
			return array;
		}

		private static TextureImporterFormat MakeTextureFormatHaveAlpha(TextureImporterFormat format)
		{
			switch (format)
			{
			case TextureImporterFormat.RGB16:
				return TextureImporterFormat.ARGB16;
			case (TextureImporterFormat)8:
			case (TextureImporterFormat)9:
				IL_1A:
				switch (format)
				{
				case TextureImporterFormat.PVRTC_RGB2:
					return TextureImporterFormat.PVRTC_RGBA2;
				case TextureImporterFormat.PVRTC_RGBA2:
					IL_2F:
					if (format != TextureImporterFormat.RGB24)
					{
						return format;
					}
					return TextureImporterFormat.ARGB32;
				case TextureImporterFormat.PVRTC_RGB4:
					return TextureImporterFormat.PVRTC_RGBA4;
				}
				goto IL_2F;
			case TextureImporterFormat.DXT1:
				return TextureImporterFormat.DXT5;
			}
			goto IL_1A;
		}

		protected void SizeAndFormatGUI()
		{
			BuildPlayerWindow.BuildPlatform[] platforms = TextureImporterInspector.GetBuildPlayerValidPlatforms().ToArray<BuildPlayerWindow.BuildPlatform>();
			GUILayout.Space(10f);
			int num = EditorGUILayout.BeginPlatformGrouping(platforms, TextureImporterInspector.s_Styles.defaultPlatform);
			TextureImporterInspector.PlatformSetting platformSetting = this.m_PlatformSettings[num + 1];
			if (!platformSetting.isDefault)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = platformSetting.overriddenIsDifferent;
				bool overriddenForAll = EditorGUILayout.ToggleLeft("Override for " + platformSetting.name, platformSetting.overridden, new GUILayoutOption[0]);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					platformSetting.SetOverriddenForAll(overriddenForAll);
					this.SyncPlatformSettings();
				}
			}
			bool disabled = !platformSetting.isDefault && !platformSetting.allAreOverridden;
			EditorGUI.BeginDisabledGroup(disabled);
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = (platformSetting.overriddenIsDifferent || platformSetting.maxTextureSizeIsDifferent);
			int maxTextureSizeForAll = EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.maxSize.text, platformSetting.maxTextureSize, TextureImporterInspector.kMaxTextureSizeStrings, TextureImporterInspector.kMaxTextureSizeValues, new GUILayoutOption[0]);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				platformSetting.SetMaxTextureSizeForAll(maxTextureSizeForAll);
				this.SyncPlatformSettings();
			}
			int[] array = null;
			string[] array2 = null;
			bool flag = false;
			int num2 = 0;
			bool flag2 = false;
			int num3 = 0;
			bool flag3 = false;
			for (int i = 0; i < base.targets.Length; i++)
			{
				TextureImporter textureImporter = base.targets[i] as TextureImporter;
				TextureImporterType textureImporterType = (!this.textureTypeHasMultipleDifferentValues) ? this.textureType : textureImporter.textureType;
				TextureImporterSettings settings = platformSetting.GetSettings(textureImporter);
				int num4 = (int)platformSetting.textureFormats[i];
				int num5 = num4;
				if (!platformSetting.isDefault && num4 < 0)
				{
					num5 = (int)TextureImporter.SimpleToFullTextureFormat2((TextureImporterFormat)num5, textureImporterType, settings, textureImporter.DoesSourceTextureHaveAlpha(), textureImporter.IsSourceTextureHDR(), platformSetting.m_Target);
				}
				if (settings.normalMap && !TextureImporterInspector.IsGLESMobileTargetPlatform(platformSetting.m_Target))
				{
					num5 = (int)TextureImporterInspector.MakeTextureFormatHaveAlpha((TextureImporterFormat)num5);
				}
				TextureImporterType textureImporterType2 = textureImporterType;
				int[] array3;
				string[] array4;
				if (textureImporterType2 != TextureImporterType.Cookie)
				{
					if (textureImporterType2 != TextureImporterType.Advanced)
					{
						array3 = this.m_TextureFormatValues;
						array4 = (from content in TextureImporterInspector.s_Styles.textureFormatOptions
						select content.text).ToArray<string>();
						if (num4 >= 0)
						{
							num4 = (int)TextureImporter.FullToSimpleTextureFormat((TextureImporterFormat)num4);
						}
						num4 = -1 - num4;
					}
					else
					{
						num4 = num5;
						if (TextureImporterInspector.IsGLESMobileTargetPlatform(platformSetting.m_Target))
						{
							if (TextureImporterInspector.s_TextureFormatStringsiPhone == null)
							{
								TextureImporterInspector.s_TextureFormatStringsiPhone = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueiPhone);
							}
							if (TextureImporterInspector.s_TextureFormatStringstvOS == null)
							{
								TextureImporterInspector.s_TextureFormatStringstvOS = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValuetvOS);
							}
							if (TextureImporterInspector.s_TextureFormatStringsAndroid == null)
							{
								TextureImporterInspector.s_TextureFormatStringsAndroid = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueAndroid);
							}
							if (platformSetting.m_Target == BuildTarget.iOS)
							{
								array3 = TextureImporterInspector.kTextureFormatsValueiPhone;
								array4 = TextureImporterInspector.s_TextureFormatStringsiPhone;
							}
							else if (platformSetting.m_Target == BuildTarget.tvOS)
							{
								array3 = TextureImporterInspector.kTextureFormatsValuetvOS;
								array4 = TextureImporterInspector.s_TextureFormatStringstvOS;
							}
							else if (platformSetting.m_Target == BuildTarget.SamsungTV)
							{
								if (TextureImporterInspector.s_TextureFormatStringsSTV == null)
								{
									TextureImporterInspector.s_TextureFormatStringsSTV = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueSTV);
								}
								array3 = TextureImporterInspector.kTextureFormatsValueSTV;
								array4 = TextureImporterInspector.s_TextureFormatStringsSTV;
							}
							else
							{
								array3 = TextureImporterInspector.kTextureFormatsValueAndroid;
								array4 = TextureImporterInspector.s_TextureFormatStringsAndroid;
							}
						}
						else if (!settings.normalMap)
						{
							if (TextureImporterInspector.s_TextureFormatStringsAll == null)
							{
								TextureImporterInspector.s_TextureFormatStringsAll = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.TextureFormatsValueAll);
							}
							if (TextureImporterInspector.s_TextureFormatStringsWiiU == null)
							{
								TextureImporterInspector.s_TextureFormatStringsWiiU = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueWiiU);
							}
							if (TextureImporterInspector.s_TextureFormatStringsGLESEmu == null)
							{
								TextureImporterInspector.s_TextureFormatStringsGLESEmu = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueGLESEmu);
							}
							if (TextureImporterInspector.s_TextureFormatStringsWeb == null)
							{
								TextureImporterInspector.s_TextureFormatStringsWeb = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kTextureFormatsValueWeb);
							}
							if (platformSetting.isDefault)
							{
								array3 = TextureImporterInspector.TextureFormatsValueAll;
								array4 = TextureImporterInspector.s_TextureFormatStringsAll;
							}
							else if (platformSetting.m_Target == BuildTarget.WiiU)
							{
								array3 = TextureImporterInspector.kTextureFormatsValueWiiU;
								array4 = TextureImporterInspector.s_TextureFormatStringsWiiU;
							}
							else if (platformSetting.m_Target == BuildTarget.StandaloneGLESEmu)
							{
								array3 = TextureImporterInspector.kTextureFormatsValueGLESEmu;
								array4 = TextureImporterInspector.s_TextureFormatStringsGLESEmu;
							}
							else
							{
								array3 = TextureImporterInspector.kTextureFormatsValueWeb;
								array4 = TextureImporterInspector.s_TextureFormatStringsWeb;
							}
						}
						else
						{
							if (TextureImporterInspector.s_NormalFormatStringsAll == null)
							{
								TextureImporterInspector.s_NormalFormatStringsAll = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.NormalFormatsValueAll);
							}
							if (TextureImporterInspector.s_NormalFormatStringsWeb == null)
							{
								TextureImporterInspector.s_NormalFormatStringsWeb = TextureImporterInspector.BuildTextureStrings(TextureImporterInspector.kNormalFormatsValueWeb);
							}
							if (platformSetting.isDefault)
							{
								array3 = TextureImporterInspector.NormalFormatsValueAll;
								array4 = TextureImporterInspector.s_NormalFormatStringsAll;
							}
							else
							{
								array3 = TextureImporterInspector.kNormalFormatsValueWeb;
								array4 = TextureImporterInspector.s_NormalFormatStringsWeb;
							}
						}
					}
				}
				else
				{
					array3 = new int[1];
					array4 = new string[]
					{
						"8 Bit Alpha"
					};
					num4 = 0;
				}
				if (i == 0)
				{
					array = array3;
					array2 = array4;
					num2 = num4;
					num3 = num5;
				}
				else
				{
					if (num4 != num2)
					{
						flag2 = true;
					}
					if (num5 != num3)
					{
						flag3 = true;
					}
					if (!array3.SequenceEqual(array) || !array4.SequenceEqual(array2))
					{
						flag = true;
						break;
					}
				}
			}
			EditorGUI.BeginDisabledGroup(flag || array2.Length == 1);
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = (flag || flag2);
			num2 = EditorGUILayout.IntPopup(TextureImporterInspector.s_Styles.textureFormat, num2, EditorGUIUtility.TempContent(array2), array, new GUILayoutOption[0]);
			EditorGUI.showMixedValue = false;
			if (this.textureType != TextureImporterType.Advanced)
			{
				num2 = -1 - num2;
			}
			if (EditorGUI.EndChangeCheck())
			{
				platformSetting.SetTextureFormatForAll((TextureImporterFormat)num2);
				this.SyncPlatformSettings();
			}
			EditorGUI.EndDisabledGroup();
			if (num3 == -5 || (!flag3 && ArrayUtility.Contains<TextureImporterFormat>(TextureImporterInspector.kFormatsWithCompressionSettings, (TextureImporterFormat)num3)))
			{
				EditorGUI.BeginChangeCheck();
				EditorGUI.showMixedValue = (platformSetting.overriddenIsDifferent || platformSetting.compressionQualityIsDifferent);
				int compressionQualityForAll = this.EditCompressionQuality(platformSetting.m_Target, platformSetting.compressionQuality);
				EditorGUI.showMixedValue = false;
				if (EditorGUI.EndChangeCheck())
				{
					platformSetting.SetCompressionQualityForAll(compressionQualityForAll);
					this.SyncPlatformSettings();
				}
			}
			if (TextureImporter.FullToSimpleTextureFormat((TextureImporterFormat)num2) == TextureImporterFormat.AutomaticCrunched && TextureImporter.FullToSimpleTextureFormat((TextureImporterFormat)num3) != TextureImporterFormat.AutomaticCrunched)
			{
				EditorGUILayout.HelpBox("Crunched is not supported on this platform. Falling back to 'Compressed'.", MessageType.Warning);
			}
			if (platformSetting.overridden && platformSetting.m_Target == BuildTarget.Android && num2 == -1 && platformSetting.importers.Length > 0)
			{
				TextureImporter textureImporter2 = platformSetting.importers[0];
				EditorGUI.BeginChangeCheck();
				bool allowsAlphaSplitting = GUILayout.Toggle(textureImporter2.GetAllowsAlphaSplitting(), TextureImporterInspector.s_Styles.etc1Compression, new GUILayoutOption[0]);
				if (EditorGUI.EndChangeCheck())
				{
					TextureImporter[] importers = platformSetting.importers;
					for (int j = 0; j < importers.Length; j++)
					{
						TextureImporter textureImporter3 = importers[j];
						textureImporter3.SetAllowsAlphaSplitting(allowsAlphaSplitting);
					}
					platformSetting.SetChanged();
				}
			}
			if (!platformSetting.overridden && platformSetting.m_Target == BuildTarget.Android && platformSetting.importers.Length > 0 && platformSetting.importers[0].GetAllowsAlphaSplitting())
			{
				platformSetting.importers[0].SetAllowsAlphaSplitting(false);
				platformSetting.SetChanged();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndPlatformGrouping();
		}

		private int EditCompressionQuality(BuildTarget target, int compression)
		{
			if (target != BuildTarget.tvOS && target != BuildTarget.iOS && target != BuildTarget.Android && target != BuildTarget.BlackBerry && target != BuildTarget.Tizen && target != BuildTarget.SamsungTV)
			{
				compression = EditorGUILayout.IntSlider(TextureImporterInspector.s_Styles.compressionQualitySlider, compression, 0, 100, new GUILayoutOption[0]);
				return compression;
			}
			int selectedIndex = 1;
			if (compression == 0)
			{
				selectedIndex = 0;
			}
			else if (compression == 100)
			{
				selectedIndex = 2;
			}
			switch (EditorGUILayout.Popup(TextureImporterInspector.s_Styles.compressionQuality, selectedIndex, TextureImporterInspector.s_Styles.mobileCompressionQualityOptions, new GUILayoutOption[0]))
			{
			case 0:
				return 0;
			case 1:
				return 50;
			case 2:
				return 100;
			default:
				return 50;
			}
		}

		private static bool IsPowerOfTwo(int f)
		{
			return (f & f - 1) == 0;
		}

		public static BuildPlayerWindow.BuildPlatform[] GetBuildPlayerValidPlatforms()
		{
			List<BuildPlayerWindow.BuildPlatform> validPlatforms = BuildPlayerWindow.GetValidPlatforms();
			List<BuildPlayerWindow.BuildPlatform> list = new List<BuildPlayerWindow.BuildPlatform>();
			foreach (BuildPlayerWindow.BuildPlatform current in validPlatforms)
			{
				list.Add(current);
			}
			return list.ToArray();
		}

		public virtual void BuildTargetList()
		{
			BuildPlayerWindow.BuildPlatform[] buildPlayerValidPlatforms = TextureImporterInspector.GetBuildPlayerValidPlatforms();
			this.m_PlatformSettings = new List<TextureImporterInspector.PlatformSetting>();
			this.m_PlatformSettings.Add(new TextureImporterInspector.PlatformSetting(string.Empty, BuildTarget.StandaloneWindows, this));
			BuildPlayerWindow.BuildPlatform[] array = buildPlayerValidPlatforms;
			for (int i = 0; i < array.Length; i++)
			{
				BuildPlayerWindow.BuildPlatform buildPlatform = array[i];
				this.m_PlatformSettings.Add(new TextureImporterInspector.PlatformSetting(buildPlatform.name, buildPlatform.DefaultTarget, this));
			}
		}

		internal override bool HasModified()
		{
			if (base.HasModified())
			{
				return true;
			}
			foreach (TextureImporterInspector.PlatformSetting current in this.m_PlatformSettings)
			{
				if (current.HasChanged())
				{
					return true;
				}
			}
			return false;
		}

		public static void SelectMainAssets(UnityEngine.Object[] targets)
		{
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < targets.Length; i++)
			{
				AssetImporter assetImporter = (AssetImporter)targets[i];
				Texture texture = AssetDatabase.LoadMainAssetAtPath(assetImporter.assetPath) as Texture;
				if (texture)
				{
					arrayList.Add(texture);
				}
			}
			Selection.objects = (arrayList.ToArray(typeof(UnityEngine.Object)) as UnityEngine.Object[]);
		}

		internal override void ResetValues()
		{
			base.ResetValues();
			this.CacheSerializedProperties();
			this.BuildTargetList();
			this.ApplySettingsToTexture();
			TextureImporterInspector.SelectMainAssets(base.targets);
		}

		internal override void Apply()
		{
			SpriteEditorWindow.TextureImporterApply(base.serializedObject);
			base.Apply();
			this.SyncPlatformSettings();
			foreach (TextureImporterInspector.PlatformSetting current in this.m_PlatformSettings)
			{
				current.Apply();
			}
		}
	}
}
