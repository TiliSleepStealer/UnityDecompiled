using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
	public sealed class ComputeShader : Object
	{
		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern int FindKernel(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetFloat(string name, float val);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetInt(string name, int val);

		public void SetVector(string name, Vector4 val)
		{
			ComputeShader.INTERNAL_CALL_SetVector(this, name, ref val);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_SetVector(ComputeShader self, string name, ref Vector4 val);

		public void SetFloats(string name, params float[] values)
		{
			this.Internal_SetFloats(name, values);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Internal_SetFloats(string name, float[] values);

		public void SetInts(string name, params int[] values)
		{
			this.Internal_SetInts(name, values);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Internal_SetInts(string name, int[] values);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetTexture(int kernelIndex, string name, Texture texture);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetBuffer(int kernelIndex, string name, ComputeBuffer buffer);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ);
	}
}
