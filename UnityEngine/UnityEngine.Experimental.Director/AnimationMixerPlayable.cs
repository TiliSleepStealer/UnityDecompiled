using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.Director
{
	public class AnimationMixerPlayable : AnimationPlayable
	{
		public AnimationMixerPlayable() : base(false)
		{
			this.m_Ptr = IntPtr.Zero;
			this.InstantiateEnginePlayable();
		}

		public AnimationMixerPlayable(bool final) : base(false)
		{
			this.m_Ptr = IntPtr.Zero;
			if (final)
			{
				this.InstantiateEnginePlayable();
			}
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InstantiateEnginePlayable();

		public bool SetInputs(AnimationClip[] clips)
		{
			if (clips == null)
			{
				throw new NullReferenceException("Parameter clips was null. You need to pass in a valid array of clips.");
			}
			AnimationPlayable[] array = new AnimationPlayable[clips.Length];
			for (int i = 0; i < clips.Length; i++)
			{
				array[i] = new AnimationClipPlayable(clips[i]);
			}
			return base.SetInputs(array);
		}
	}
}
