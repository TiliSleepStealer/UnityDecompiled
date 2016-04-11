using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Tizen
{
	public sealed class Window
	{
		public static extern IntPtr windowHandle
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}
	}
}
