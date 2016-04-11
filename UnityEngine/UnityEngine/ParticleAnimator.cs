using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
	public sealed class ParticleAnimator : Component
	{
		public extern bool doesAnimateColor
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public Vector3 worldRotationAxis
		{
			get
			{
				Vector3 result;
				this.INTERNAL_get_worldRotationAxis(out result);
				return result;
			}
			set
			{
				this.INTERNAL_set_worldRotationAxis(ref value);
			}
		}

		public Vector3 localRotationAxis
		{
			get
			{
				Vector3 result;
				this.INTERNAL_get_localRotationAxis(out result);
				return result;
			}
			set
			{
				this.INTERNAL_set_localRotationAxis(ref value);
			}
		}

		public extern float sizeGrow
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public Vector3 rndForce
		{
			get
			{
				Vector3 result;
				this.INTERNAL_get_rndForce(out result);
				return result;
			}
			set
			{
				this.INTERNAL_set_rndForce(ref value);
			}
		}

		public Vector3 force
		{
			get
			{
				Vector3 result;
				this.INTERNAL_get_force(out result);
				return result;
			}
			set
			{
				this.INTERNAL_set_force(ref value);
			}
		}

		public extern float damping
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern bool autodestruct
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern Color[] colorAnimation
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_worldRotationAxis(out Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_set_worldRotationAxis(ref Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_localRotationAxis(out Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_set_localRotationAxis(ref Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_rndForce(out Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_set_rndForce(ref Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_get_force(out Vector3 value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void INTERNAL_set_force(ref Vector3 value);
	}
}
