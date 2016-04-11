using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
	[UsedByNativeCode]
	[StructLayout(LayoutKind.Sequential)]
	public sealed class TextGenerator : IDisposable
	{
		internal IntPtr m_Ptr;

		private string m_LastString;

		private TextGenerationSettings m_LastSettings;

		private bool m_HasGenerated;

		private bool m_LastValid;

		private readonly List<UIVertex> m_Verts;

		private readonly List<UICharInfo> m_Characters;

		private readonly List<UILineInfo> m_Lines;

		private bool m_CachedVerts;

		private bool m_CachedCharacters;

		private bool m_CachedLines;

		private static int s_NextId = 0;

		private int m_Id;

		private static readonly Dictionary<int, WeakReference> s_Instances = new Dictionary<int, WeakReference>();

		public IList<UIVertex> verts
		{
			get
			{
				if (!this.m_CachedVerts)
				{
					this.GetVertices(this.m_Verts);
					this.m_CachedVerts = true;
				}
				return this.m_Verts;
			}
		}

		public IList<UICharInfo> characters
		{
			get
			{
				if (!this.m_CachedCharacters)
				{
					this.GetCharacters(this.m_Characters);
					this.m_CachedCharacters = true;
				}
				return this.m_Characters;
			}
		}

		public IList<UILineInfo> lines
		{
			get
			{
				if (!this.m_CachedLines)
				{
					this.GetLines(this.m_Lines);
					this.m_CachedLines = true;
				}
				return this.m_Lines;
			}
		}

		public Rect rectExtents
		{
			get
			{
				Rect result;
				this.INTERNAL_get_rectExtents(out result);
				return result;
			}
		}

		public extern int vertexCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern int characterCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public int characterCountVisible
		{
			get
			{
				return (!string.IsNullOrEmpty(this.m_LastString)) ? Mathf.Min(this.m_LastString.Length, Mathf.Max(0, (this.vertexCount - 4) / 4)) : 0;
			}
		}

		public extern int lineCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern int fontSizeUsedForBestFit
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public TextGenerator() : this(50)
		{
		}

		public TextGenerator(int initialCapacity)
		{
			this.m_Verts = new List<UIVertex>((initialCapacity + 1) * 4);
			this.m_Characters = new List<UICharInfo>(initialCapacity + 1);
			this.m_Lines = new List<UILineInfo>(20);
			this.Init();
			Dictionary<int, WeakReference> obj = TextGenerator.s_Instances;
			lock (obj)
			{
				this.m_Id = TextGenerator.s_NextId++;
				TextGenerator.s_Instances.Add(this.m_Id, new WeakReference(this));
			}
		}

		void IDisposable.Dispose()
		{
			Dictionary<int, WeakReference> obj = TextGenerator.s_Instances;
			lock (obj)
			{
				TextGenerator.s_Instances.Remove(this.m_Id);
			}
			this.Dispose_cpp();
		}

		~TextGenerator()
		{
			((IDisposable)this).Dispose();
		}

		[RequiredByNativeCode]
		internal static void InvalidateAll()
		{
			Dictionary<int, WeakReference> obj = TextGenerator.s_Instances;
			lock (obj)
			{
				foreach (KeyValuePair<int, WeakReference> current in TextGenerator.s_Instances)
				{
					WeakReference value = current.Value;
					if (value.IsAlive)
					{
						(value.Target as TextGenerator).Invalidate();
					}
				}
			}
		}

		private TextGenerationSettings ValidatedSettings(TextGenerationSettings settings)
		{
			if (settings.font != null && settings.font.dynamic)
			{
				return settings;
			}
			if (settings.fontSize != 0 || settings.fontStyle != FontStyle.Normal)
			{
				Debug.LogWarning("Font size and style overrides are only supported for dynamic fonts.");
				settings.fontSize = 0;
				settings.fontStyle = FontStyle.Normal;
			}
			if (settings.resizeTextForBestFit)
			{
				Debug.LogWarning("BestFit is only supported for dynamic fonts.");
				settings.resizeTextForBestFit = false;
			}
			return settings;
		}

		public void Invalidate()
		{
			this.m_HasGenerated = false;
		}

		public void GetCharacters(List<UICharInfo> characters)
		{
			this.GetCharactersInternal(characters);
		}

		public void GetLines(List<UILineInfo> lines)
		{
			this.GetLinesInternal(lines);
		}

		public void GetVertices(List<UIVertex> vertices)
		{
			this.GetVerticesInternal(vertices);
		}

		public float GetPreferredWidth(string str, TextGenerationSettings settings)
		{
			settings.horizontalOverflow = HorizontalWrapMode.Overflow;
			settings.verticalOverflow = VerticalWrapMode.Overflow;
			settings.updateBounds = true;
			this.Populate(str, settings);
			return this.rectExtents.width;
		}

		public float GetPreferredHeight(string str, TextGenerationSettings settings)
		{
			settings.verticalOverflow = VerticalWrapMode.Overflow;
			settings.updateBounds = true;
			this.Populate(str, settings);
			return this.rectExtents.height;
		}

		public bool Populate(string str, TextGenerationSettings settings)
		{
			if (this.m_HasGenerated && str == this.m_LastString && settings.Equals(this.m_LastSettings))
			{
				return this.m_LastValid;
			}
			return this.PopulateAlways(str, settings);
		}

		private bool PopulateAlways(string str, TextGenerationSettings settings)
		{
			this.m_LastString = str;
			this.m_HasGenerated = true;
			this.m_CachedVerts = false;
			this.m_CachedCharacters = false;
			this.m_CachedLines = false;
			this.m_LastSettings = settings;
			TextGenerationSettings textGenerationSettings = this.ValidatedSettings(settings);
			this.m_LastValid = this.Populate_Internal(str, textGenerationSettings.font, textGenerationSettings.color, textGenerationSettings.fontSize, textGenerationSettings.scaleFactor, textGenerationSettings.lineSpacing, textGenerationSettings.fontStyle, textGenerationSettings.richText, textGenerationSettings.resizeTextForBestFit, textGenerationSettings.resizeTextMinSize, textGenerationSettings.resizeTextMaxSize, textGenerationSettings.verticalOverflow, textGenerationSettings.horizontalOverflow, textGenerationSettings.updateBounds, textGenerationSettings.textAnchor, textGenerationSettings.generationExtents, textGenerationSettings.pivot, textGenerationSettings.generateOutOfBounds, textGenerationSettings.alignByGeometry);
			return this.m_LastValid;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Init();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Dispose_cpp();

		internal bool Populate_Internal(string str, Font font, Color color, int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText, bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize, VerticalWrapMode verticalOverFlow, HorizontalWrapMode horizontalOverflow, bool updateBounds, TextAnchor anchor, Vector2 extents, Vector2 pivot, bool generateOutOfBounds, bool alignByGeometry)
		{
			return this.Populate_Internal_cpp(str, font, color, fontSize, scaleFactor, lineSpacing, style, richText, resizeTextForBestFit, resizeTextMinSize, resizeTextMaxSize, (int)verticalOverFlow, (int)horizontalOverflow, updateBounds, anchor, extents.x, extents.y, pivot.x, pivot.y, generateOutOfBounds, alignByGeometry);
		}

		internal bool Populate_Internal_cpp(string str, Font font, Color color, int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText, bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize, int verticalOverFlow, int horizontalOverflow, bool updateBounds, TextAnchor anchor, float extentsX, float extentsY, float pivotX, float pivotY, bool generateOutOfBounds, bool alignByGeometry)
		{
			return TextGenerator.INTERNAL_CALL_Populate_Internal_cpp(this, str, font, ref color, fontSize, scaleFactor, lineSpacing, style, richText, resizeTextForBestFit, resizeTextMinSize, resizeTextMaxSize, verticalOverFlow, horizontalOverflow, updateBounds, anchor, extentsX, extentsY, pivotX, pivotY, generateOutOfBounds, alignByGeometry);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_Populate_Internal_cpp(TextGenerator self, string str, Font font, ref Color color, int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText, bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize, int verticalOverFlow, int horizontalOverflow, bool updateBounds, TextAnchor anchor, float extentsX, float extentsY, float pivotX, float pivotY, bool generateOutOfBounds, bool alignByGeometry);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_rectExtents(out Rect value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void GetVerticesInternal(object vertices);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern UIVertex[] GetVerticesArray();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void GetCharactersInternal(object characters);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern UICharInfo[] GetCharactersArray();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void GetLinesInternal(object lines);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern UILineInfo[] GetLinesArray();
	}
}
