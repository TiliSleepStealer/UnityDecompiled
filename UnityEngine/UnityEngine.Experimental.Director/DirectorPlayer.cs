using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.Director
{
	public class DirectorPlayer : Behaviour
	{
		public void Play(Playable playable, object customData)
		{
			this.PlayInternal(playable, customData);
		}

		public void Play(Playable playable)
		{
			this.PlayInternal(playable, null);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void PlayInternal(Playable playable, object customData);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Stop();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetTime(double time);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern double GetTime();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetTimeUpdateMode(DirectorUpdateMode mode);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern DirectorUpdateMode GetTimeUpdateMode();
	}
}
