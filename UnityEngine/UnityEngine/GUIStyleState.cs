using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public sealed class GUIStyleState
	{
		[NonSerialized]
		internal IntPtr m_Ptr;

		private readonly GUIStyle m_SourceStyle;

		[NonSerialized]
		private Texture2D m_Background;

		public Texture2D background
		{
			get
			{
				return this.GetBackgroundInternal();
			}
			set
			{
				this.SetBackgroundInternal(value);
				this.m_Background = value;
			}
		}

		public Color textColor
		{
			get
			{
				Color result;
				this.INTERNAL_get_textColor(out result);
				return result;
			}
			set
			{
				this.INTERNAL_set_textColor(ref value);
			}
		}

		public GUIStyleState()
		{
			this.Init();
		}

		private GUIStyleState(GUIStyle sourceStyle, IntPtr source)
		{
			this.m_SourceStyle = sourceStyle;
			this.m_Ptr = source;
		}

		internal static GUIStyleState ProduceGUIStyleStateFromDeserialization(GUIStyle sourceStyle, IntPtr source)
		{
			GUIStyleState gUIStyleState = new GUIStyleState(sourceStyle, source);
			gUIStyleState.m_Background = gUIStyleState.GetBackgroundInternalFromDeserialization();
			return gUIStyleState;
		}

		internal static GUIStyleState GetGUIStyleState(GUIStyle sourceStyle, IntPtr source)
		{
			GUIStyleState gUIStyleState = new GUIStyleState(sourceStyle, source);
			gUIStyleState.m_Background = gUIStyleState.GetBackgroundInternal();
			return gUIStyleState;
		}

		~GUIStyleState()
		{
			if (this.m_SourceStyle == null)
			{
				this.Cleanup();
			}
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Init();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Cleanup();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetBackgroundInternal(Texture2D value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern Texture2D GetBackgroundInternalFromDeserialization();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern Texture2D GetBackgroundInternal();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_textColor(out Color value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_set_textColor(ref Color value);
	}
}
