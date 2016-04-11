using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
	public sealed class LightmapEditorSettings
	{
		public static extern int maxAtlasWidth
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern int maxAtlasHeight
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern float resolution
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern float bakeResolution
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern bool textureCompression
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern ReflectionCubemapCompression reflectionCubemapCompression
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern float aoMaxDistance
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern int padding
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[Obsolete("LightmapEditorSettings.aoContrast has been deprecated.", false)]
		public static float aoContrast
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.aoAmount has been deprecated.", false)]
		public static float aoAmount
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.lockAtlas has been deprecated.", false)]
		public static bool lockAtlas
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.skyLightColor has been deprecated.", false)]
		public static Color skyLightColor
		{
			get
			{
				return Color.black;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.skyLightIntensity has been deprecated.", false)]
		public static float skyLightIntensity
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.quality has been deprecated.", false)]
		public static LightmapBakeQuality quality
		{
			get
			{
				return LightmapBakeQuality.High;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.bounceBoost has been deprecated.", false)]
		public static float bounceBoost
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.finalGatherRays has been deprecated.", false)]
		public static int finalGatherRays
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.finalGatherContrastThreshold has been deprecated.", false)]
		public static float finalGatherContrastThreshold
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.finalGatherGradientThreshold has been deprecated.", false)]
		public static float finalGatherGradientThreshold
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.finalGatherInterpolationPoints has been deprecated.", false)]
		public static int finalGatherInterpolationPoints
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.lastUsedResolution has been deprecated.", false)]
		public static float lastUsedResolution
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.bounces has been deprecated.", false)]
		public static int bounces
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		[Obsolete("LightmapEditorSettings.bounceIntensity has been deprecated.", false)]
		public static float bounceIntensity
		{
			get
			{
				return 0f;
			}
			set
			{
			}
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void Reset();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsLightmappedOrDynamicLightmappedForRendering(Renderer renderer);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool HasZeroAreaMesh(Renderer renderer);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool HasClampedResolution(Renderer renderer);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetSystemResolution(Renderer renderer, out int width, out int height);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetTerrainSystemResolution(Terrain terrain, out int width, out int height, out int numChunksInX, out int numChunksInY);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetInstanceResolution(Renderer renderer, out int width, out int height);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetInputSystemHash(Renderer renderer, out Hash128 inputSystemHash);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetInstanceHash(Renderer renderer, out Hash128 instanceHash);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetGeometryHash(Renderer renderer, out Hash128 geometryHash);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern UnityEngine.Object GetLightmapSettings();
	}
}
