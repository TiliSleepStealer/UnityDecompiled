using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Director
{
	[UsedByNativeCode]
	public sealed class AnimatorControllerPlayable : AnimationPlayable, IAnimatorControllerPlayable
	{
		public extern RuntimeAnimatorController animatorController
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern int layerCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern int parameterCount
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		private extern AnimatorControllerParameter[] parameters
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public AnimatorControllerPlayable(RuntimeAnimatorController controller) : base(false)
		{
			this.m_Ptr = IntPtr.Zero;
			this.InstantiateEnginePlayable(controller);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InstantiateEnginePlayable(RuntimeAnimatorController controller);

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

		public float GetFloat(string name)
		{
			return this.GetFloatString(name);
		}

		public float GetFloat(int id)
		{
			return this.GetFloatID(id);
		}

		public void SetFloat(string name, float value)
		{
			this.SetFloatString(name, value);
		}

		public void SetFloat(int id, float value)
		{
			this.SetFloatID(id, value);
		}

		public bool GetBool(string name)
		{
			return this.GetBoolString(name);
		}

		public bool GetBool(int id)
		{
			return this.GetBoolID(id);
		}

		public void SetBool(string name, bool value)
		{
			this.SetBoolString(name, value);
		}

		public void SetBool(int id, bool value)
		{
			this.SetBoolID(id, value);
		}

		public int GetInteger(string name)
		{
			return this.GetIntegerString(name);
		}

		public int GetInteger(int id)
		{
			return this.GetIntegerID(id);
		}

		public void SetInteger(string name, int value)
		{
			this.SetIntegerString(name, value);
		}

		public void SetInteger(int id, int value)
		{
			this.SetIntegerID(id, value);
		}

		public void SetTrigger(string name)
		{
			this.SetTriggerString(name);
		}

		public void SetTrigger(int id)
		{
			this.SetTriggerID(id);
		}

		public void ResetTrigger(string name)
		{
			this.ResetTriggerString(name);
		}

		public void ResetTrigger(int id)
		{
			this.ResetTriggerID(id);
		}

		public bool IsParameterControlledByCurve(string name)
		{
			return this.IsParameterControlledByCurveString(name);
		}

		public bool IsParameterControlledByCurve(int id)
		{
			return this.IsParameterControlledByCurveID(id);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern string GetLayerName(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern int GetLayerIndex(string layerName);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern float GetLayerWeight(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetLayerWeight(int layerIndex, float weight);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern string ResolveHash(int hash);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool IsInTransition(int layerIndex);

		public AnimatorControllerParameter GetParameter(int index)
		{
			AnimatorControllerParameter[] parameters = this.parameters;
			if (index < 0 && index >= this.parameters.Length)
			{
				throw new IndexOutOfRangeException("index");
			}
			return parameters[index];
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int StringToHash(string name);

		[ExcludeFromDocs]
		public void CrossFadeInFixedTime(string stateName, float transitionDuration, int layer)
		{
			float fixedTime = 0f;
			this.CrossFadeInFixedTime(stateName, transitionDuration, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void CrossFadeInFixedTime(string stateName, float transitionDuration)
		{
			float fixedTime = 0f;
			int layer = -1;
			this.CrossFadeInFixedTime(stateName, transitionDuration, layer, fixedTime);
		}

		public void CrossFadeInFixedTime(string stateName, float transitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("0.0f")] float fixedTime)
		{
			this.CrossFadeInFixedTime(AnimatorControllerPlayable.StringToHash(stateName), transitionDuration, layer, fixedTime);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("0.0f")] float fixedTime);

		[ExcludeFromDocs]
		public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, int layer)
		{
			float fixedTime = 0f;
			this.CrossFadeInFixedTime(stateNameHash, transitionDuration, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration)
		{
			float fixedTime = 0f;
			int layer = -1;
			this.CrossFadeInFixedTime(stateNameHash, transitionDuration, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void CrossFade(string stateName, float transitionDuration, int layer)
		{
			float normalizedTime = float.NegativeInfinity;
			this.CrossFade(stateName, transitionDuration, layer, normalizedTime);
		}

		[ExcludeFromDocs]
		public void CrossFade(string stateName, float transitionDuration)
		{
			float normalizedTime = float.NegativeInfinity;
			int layer = -1;
			this.CrossFade(stateName, transitionDuration, layer, normalizedTime);
		}

		public void CrossFade(string stateName, float transitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime)
		{
			this.CrossFade(AnimatorControllerPlayable.StringToHash(stateName), transitionDuration, layer, normalizedTime);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void CrossFade(int stateNameHash, float transitionDuration, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime);

		[ExcludeFromDocs]
		public void CrossFade(int stateNameHash, float transitionDuration, int layer)
		{
			float normalizedTime = float.NegativeInfinity;
			this.CrossFade(stateNameHash, transitionDuration, layer, normalizedTime);
		}

		[ExcludeFromDocs]
		public void CrossFade(int stateNameHash, float transitionDuration)
		{
			float normalizedTime = float.NegativeInfinity;
			int layer = -1;
			this.CrossFade(stateNameHash, transitionDuration, layer, normalizedTime);
		}

		[ExcludeFromDocs]
		public void PlayInFixedTime(string stateName, int layer)
		{
			float fixedTime = float.NegativeInfinity;
			this.PlayInFixedTime(stateName, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void PlayInFixedTime(string stateName)
		{
			float fixedTime = float.NegativeInfinity;
			int layer = -1;
			this.PlayInFixedTime(stateName, layer, fixedTime);
		}

		public void PlayInFixedTime(string stateName, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime)
		{
			this.PlayInFixedTime(AnimatorControllerPlayable.StringToHash(stateName), layer, fixedTime);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void PlayInFixedTime(int stateNameHash, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float fixedTime);

		[ExcludeFromDocs]
		public void PlayInFixedTime(int stateNameHash, int layer)
		{
			float fixedTime = float.NegativeInfinity;
			this.PlayInFixedTime(stateNameHash, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void PlayInFixedTime(int stateNameHash)
		{
			float fixedTime = float.NegativeInfinity;
			int layer = -1;
			this.PlayInFixedTime(stateNameHash, layer, fixedTime);
		}

		[ExcludeFromDocs]
		public void Play(string stateName, int layer)
		{
			float normalizedTime = float.NegativeInfinity;
			this.Play(stateName, layer, normalizedTime);
		}

		[ExcludeFromDocs]
		public void Play(string stateName)
		{
			float normalizedTime = float.NegativeInfinity;
			int layer = -1;
			this.Play(stateName, layer, normalizedTime);
		}

		public void Play(string stateName, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime)
		{
			this.Play(AnimatorControllerPlayable.StringToHash(stateName), layer, normalizedTime);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Play(int stateNameHash, [DefaultValue("-1")] int layer, [DefaultValue("float.NegativeInfinity")] float normalizedTime);

		[ExcludeFromDocs]
		public void Play(int stateNameHash, int layer)
		{
			float normalizedTime = float.NegativeInfinity;
			this.Play(stateNameHash, layer, normalizedTime);
		}

		[ExcludeFromDocs]
		public void Play(int stateNameHash)
		{
			float normalizedTime = float.NegativeInfinity;
			int layer = -1;
			this.Play(stateNameHash, layer, normalizedTime);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool HasState(int layerIndex, int stateID);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetFloatString(string name, float value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetFloatID(int id, float value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern float GetFloatString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern float GetFloatID(int id);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetBoolString(string name, bool value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetBoolID(int id, bool value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool GetBoolString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool GetBoolID(int id);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetIntegerString(string name, int value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetIntegerID(int id, int value);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int GetIntegerString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int GetIntegerID(int id);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetTriggerString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void SetTriggerID(int id);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void ResetTriggerString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void ResetTriggerID(int id);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool IsParameterControlledByCurveString(string name);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool IsParameterControlledByCurveID(int id);
	}
}
