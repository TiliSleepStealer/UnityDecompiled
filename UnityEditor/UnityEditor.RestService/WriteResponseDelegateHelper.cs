using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.RestService
{
	internal sealed class WriteResponseDelegateHelper
	{
		internal static WriteResponse MakeDelegateFor(IntPtr response, IntPtr callbackData)
		{
			return delegate(HttpStatusCode resultCode, string payload)
			{
				WriteResponseDelegateHelper.DoWriteResponse(response, resultCode, payload, callbackData);
			};
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void DoWriteResponse(IntPtr cppResponse, HttpStatusCode resultCode, string payload, IntPtr callbackData);
	}
}
