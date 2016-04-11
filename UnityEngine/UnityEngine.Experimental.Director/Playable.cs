using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Director
{
	[RequiredByNativeCode]
	public class Playable : IDisposable
	{
		internal IntPtr m_Ptr;

		internal int m_UniqueId;

		public extern double time
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern PlayState state
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern int inputCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern int outputCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public Playable()
		{
			this.m_Ptr = IntPtr.Zero;
			this.m_UniqueId = this.GenerateUniqueId();
			this.InstantiateEnginePlayable();
		}

		internal Playable(bool callCPPConstructor)
		{
			this.m_Ptr = IntPtr.Zero;
			this.m_UniqueId = this.GenerateUniqueId();
			if (callCPPConstructor)
			{
				this.InstantiateEnginePlayable();
			}
		}

		private void Dispose(bool disposing)
		{
			this.ReleaseEnginePlayable();
			this.m_Ptr = IntPtr.Zero;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern int GetUniqueIDInternal();

		public static bool Connect(Playable source, Playable target)
		{
			return Playable.Connect(source, target, -1, -1);
		}

		public static bool Connect(Playable source, Playable target, int sourceOutputPort, int targetInputPort)
		{
			return (Playable.CheckPlayableValidity(source, "source") || Playable.CheckPlayableValidity(target, "target")) && (!(source != null) || source.CheckInputBounds(sourceOutputPort, true)) && target.CheckInputBounds(targetInputPort, true) && Playable.ConnectInternal(source, target, sourceOutputPort, targetInputPort);
		}

		public static void Disconnect(Playable target, int inputPort)
		{
			if (!Playable.CheckPlayableValidity(target, "target"))
			{
				return;
			}
			if (!target.CheckInputBounds(inputPort))
			{
				return;
			}
			Playable.DisconnectInternal(target, inputPort);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void ReleaseEnginePlayable();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InstantiateEnginePlayable();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int GenerateUniqueId();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool SetInputWeightInternal(int inputIndex, float weight);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern float GetInputWeightInternal(int inputIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool ConnectInternal(Playable source, Playable target, int sourceOutputPort, int targetInputPort);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void DisconnectInternal(Playable target, int inputPort);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Playable GetInput(int inputPort);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Playable[] GetInputs();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void ClearInputs();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Playable GetOutput(int outputPort);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern Playable[] GetOutputs();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void GetInputsInternal(object list);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void GetOutputsInternal(object list);

		~Playable()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public override bool Equals(object p)
		{
			return Playable.CompareIntPtr(this, p as Playable);
		}

		public override int GetHashCode()
		{
			return this.m_UniqueId;
		}

		internal static bool CompareIntPtr(Playable lhs, Playable rhs)
		{
			bool flag = lhs == null || !Playable.IsNativePlayableAlive(lhs);
			bool flag2 = rhs == null || !Playable.IsNativePlayableAlive(rhs);
			if (flag2 && flag)
			{
				return true;
			}
			if (flag2)
			{
				return !Playable.IsNativePlayableAlive(lhs);
			}
			if (flag)
			{
				return !Playable.IsNativePlayableAlive(rhs);
			}
			return lhs.GetUniqueIDInternal() == rhs.GetUniqueIDInternal();
		}

		internal static bool IsNativePlayableAlive(Playable p)
		{
			return p.m_Ptr != IntPtr.Zero;
		}

		internal static bool CheckPlayableValidity(Playable playable, string name)
		{
			if (playable == null)
			{
				throw new NullReferenceException("Playable " + name + "is null");
			}
			return true;
		}

		internal bool CheckInputBounds(int inputIndex)
		{
			return this.CheckInputBounds(inputIndex, false);
		}

		internal bool CheckInputBounds(int inputIndex, bool acceptAny)
		{
			if (inputIndex == -1 && acceptAny)
			{
				return true;
			}
			if (inputIndex < 0)
			{
				throw new IndexOutOfRangeException("Index must be greater than 0");
			}
			Playable[] inputs = this.GetInputs();
			if (inputs.Length <= inputIndex)
			{
				throw new IndexOutOfRangeException(string.Concat(new object[]
				{
					"inputIndex ",
					inputIndex,
					" is greater than the number of available inputs (",
					inputs.Length,
					")."
				}));
			}
			return true;
		}

		public float GetInputWeight(int inputIndex)
		{
			if (this.CheckInputBounds(inputIndex))
			{
				return this.GetInputWeightInternal(inputIndex);
			}
			return -1f;
		}

		public bool SetInputWeight(int inputIndex, float weight)
		{
			return this.CheckInputBounds(inputIndex) && this.SetInputWeightInternal(inputIndex, weight);
		}

		public void GetInputs(List<Playable> inputList)
		{
			inputList.Clear();
			this.GetInputsInternal(inputList);
		}

		public void GetOutputs(List<Playable> outputList)
		{
			outputList.Clear();
			this.GetOutputsInternal(outputList);
		}

		public virtual void PrepareFrame(FrameData info)
		{
		}

		public virtual void ProcessFrame(FrameData info, object playerData)
		{
		}

		public virtual void OnSetTime(float localTime)
		{
		}

		public virtual void OnSetPlayState(PlayState newState)
		{
		}

		public static bool operator ==(Playable x, Playable y)
		{
			return Playable.CompareIntPtr(x, y);
		}

		public static bool operator !=(Playable x, Playable y)
		{
			return !Playable.CompareIntPtr(x, y);
		}

		public static implicit operator bool(Playable exists)
		{
			return !Playable.CompareIntPtr(exists, null);
		}
	}
}
