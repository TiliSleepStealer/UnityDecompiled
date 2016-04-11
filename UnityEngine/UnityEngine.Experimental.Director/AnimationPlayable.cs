using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.Director
{
	public class AnimationPlayable : Playable
	{
		public AnimationPlayable() : base(false)
		{
			this.m_Ptr = IntPtr.Zero;
			this.InstantiateEnginePlayable();
		}

		public AnimationPlayable(bool final) : base(false)
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

		public virtual int AddInput(AnimationPlayable source)
		{
			Playable.Connect(source, this, -1, -1);
			Playable[] inputs = base.GetInputs();
			return inputs.Length - 1;
		}

		public virtual bool SetInput(AnimationPlayable source, int index)
		{
			if (!base.CheckInputBounds(index))
			{
				return false;
			}
			Playable[] inputs = base.GetInputs();
			if (inputs[index] != null)
			{
				Playable.Disconnect(this, index);
			}
			return Playable.Connect(source, this, -1, index);
		}

		public virtual bool SetInputs(IEnumerable<AnimationPlayable> sources)
		{
			Playable[] inputs = base.GetInputs();
			int num = inputs.Length;
			for (int i = 0; i < num; i++)
			{
				Playable.Disconnect(this, i);
			}
			bool flag = false;
			int num2 = 0;
			foreach (AnimationPlayable current in sources)
			{
				if (num2 < num)
				{
					flag |= Playable.Connect(current, this, -1, num2);
				}
				else
				{
					flag |= Playable.Connect(current, this, -1, -1);
				}
				base.SetInputWeight(num2, 1f);
				num2++;
			}
			for (int j = num2; j < num; j++)
			{
				base.SetInputWeight(j, 0f);
			}
			return flag;
		}

		public virtual bool RemoveInput(int index)
		{
			if (!base.CheckInputBounds(index))
			{
				return false;
			}
			Playable.Disconnect(this, index);
			return true;
		}

		public virtual bool RemoveInput(AnimationPlayable playable)
		{
			if (!Playable.CheckPlayableValidity(playable, "playable"))
			{
				return false;
			}
			Playable[] inputs = base.GetInputs();
			for (int i = 0; i < inputs.Length; i++)
			{
				if (inputs[i] == playable)
				{
					Playable.Disconnect(this, i);
					return true;
				}
			}
			return false;
		}

		public virtual bool RemoveAllInputs()
		{
			Playable[] inputs = base.GetInputs();
			for (int i = 0; i < inputs.Length; i++)
			{
				this.RemoveInput(inputs[i] as AnimationPlayable);
			}
			return true;
		}
	}
}
