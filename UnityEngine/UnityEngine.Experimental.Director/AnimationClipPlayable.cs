using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.Director
{
	public sealed class AnimationClipPlayable : AnimationPlayable
	{
		public extern AnimationClip clip
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern float speed
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public AnimationClipPlayable(AnimationClip clip) : base(false)
		{
			this.m_Ptr = IntPtr.Zero;
			this.InstantiateEnginePlayable(clip);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InstantiateEnginePlayable(AnimationClip clip);

		public override int AddInput(AnimationPlayable source)
		{
			Debug.LogError("AnimationClipPlayable doesn't support adding inputs");
			return -1;
		}

		public override bool SetInput(AnimationPlayable source, int index)
		{
			Debug.LogError("AnimationClipPlayable doesn't support setting inputs");
			return false;
		}

		public override bool SetInputs(IEnumerable<AnimationPlayable> sources)
		{
			Debug.LogError("AnimationClipPlayable doesn't support setting inputs");
			return false;
		}

		public override bool RemoveInput(int index)
		{
			Debug.LogError("AnimationClipPlayable doesn't support removing inputs");
			return false;
		}

		public override bool RemoveInput(AnimationPlayable playable)
		{
			Debug.LogError("AnimationClipPlayable doesn't support removing inputs");
			return false;
		}

		public override bool RemoveAllInputs()
		{
			Debug.LogError("AnimationClipPlayable doesn't support removing inputs");
			return false;
		}
	}
}
