using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine
{
	[RequiredByNativeCode]
	[StructLayout(LayoutKind.Sequential)]
	public class Object
	{
		private int m_InstanceID;

		private IntPtr m_CachedPtr;

		private string m_UnityRuntimeErrorString;

		public extern string name
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern HideFlags hideFlags
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
		private static extern Object Internal_CloneSingle(Object data);

		private static Object Internal_InstantiateSingle(Object data, Vector3 pos, Quaternion rot)
		{
			return Object.INTERNAL_CALL_Internal_InstantiateSingle(data, ref pos, ref rot);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern Object INTERNAL_CALL_Internal_InstantiateSingle(Object data, ref Vector3 pos, ref Quaternion rot);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void Destroy(Object obj, [DefaultValue("0.0F")] float t);

		[ExcludeFromDocs]
		public static void Destroy(Object obj)
		{
			float t = 0f;
			Object.Destroy(obj, t);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void DestroyImmediate(Object obj, [DefaultValue("false")] bool allowDestroyingAssets);

		[ExcludeFromDocs]
		public static void DestroyImmediate(Object obj)
		{
			bool allowDestroyingAssets = false;
			Object.DestroyImmediate(obj, allowDestroyingAssets);
		}

		[WrapperlessIcall, TypeInferenceRule(TypeInferenceRules.ArrayOfTypeReferencedByFirstArgument)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern Object[] FindObjectsOfType(Type type);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void DontDestroyOnLoad(Object target);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void DestroyObject(Object obj, [DefaultValue("0.0F")] float t);

		[ExcludeFromDocs]
		public static void DestroyObject(Object obj)
		{
			float t = 0f;
			Object.DestroyObject(obj, t);
		}

		[Obsolete("use Object.FindObjectsOfType instead."), WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern Object[] FindSceneObjectsOfType(Type type);

		[Obsolete("use Resources.FindObjectsOfTypeAll instead."), WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern Object[] FindObjectsOfTypeIncludingAssets(Type type);

		[Obsolete("Please use Resources.FindObjectsOfTypeAll instead")]
		public static Object[] FindObjectsOfTypeAll(Type type)
		{
			return Resources.FindObjectsOfTypeAll(type);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public override extern string ToString();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool DoesObjectWithInstanceIDExist(int instanceID);

		public override bool Equals(object o)
		{
			return Object.CompareBaseObjects(this, o as Object);
		}

		public override int GetHashCode()
		{
			return this.GetInstanceID();
		}

		private static bool CompareBaseObjects(Object lhs, Object rhs)
		{
			bool flag = lhs == null;
			bool flag2 = rhs == null;
			if (flag2 && flag)
			{
				return true;
			}
			if (flag2)
			{
				return !Object.IsNativeObjectAlive(lhs);
			}
			if (flag)
			{
				return !Object.IsNativeObjectAlive(rhs);
			}
			return lhs.m_InstanceID == rhs.m_InstanceID;
		}

		private static bool IsNativeObjectAlive(Object o)
		{
			return o.GetCachedPtr() != IntPtr.Zero || (!(o is MonoBehaviour) && !(o is ScriptableObject) && Object.DoesObjectWithInstanceIDExist(o.GetInstanceID()));
		}

		public int GetInstanceID()
		{
			return this.m_InstanceID;
		}

		private IntPtr GetCachedPtr()
		{
			return this.m_CachedPtr;
		}

		[TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
		public static Object Instantiate(Object original, Vector3 position, Quaternion rotation)
		{
			Object.CheckNullArgument(original, "The thing you want to instantiate is null.");
			return Object.Internal_InstantiateSingle(original, position, rotation);
		}

		[TypeInferenceRule(TypeInferenceRules.TypeOfFirstArgument)]
		public static Object Instantiate(Object original)
		{
			Object.CheckNullArgument(original, "The thing you want to instantiate is null.");
			return Object.Internal_CloneSingle(original);
		}

		public static T Instantiate<T>(T original) where T : Object
		{
			Object.CheckNullArgument(original, "The thing you want to instantiate is null.");
			return (T)((object)Object.Internal_CloneSingle(original));
		}

		private static void CheckNullArgument(object arg, string message)
		{
			if (arg == null)
			{
				throw new ArgumentException(message);
			}
		}

		public static T[] FindObjectsOfType<T>() where T : Object
		{
			return Resources.ConvertObjects<T>(Object.FindObjectsOfType(typeof(T)));
		}

		[TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
		public static Object FindObjectOfType(Type type)
		{
			Object[] array = Object.FindObjectsOfType(type);
			if (array.Length > 0)
			{
				return array[0];
			}
			return null;
		}

		public static T FindObjectOfType<T>() where T : Object
		{
			return (T)((object)Object.FindObjectOfType(typeof(T)));
		}

		public static implicit operator bool(Object exists)
		{
			return !Object.CompareBaseObjects(exists, null);
		}

		public static bool operator ==(Object x, Object y)
		{
			return Object.CompareBaseObjects(x, y);
		}

		public static bool operator !=(Object x, Object y)
		{
			return !Object.CompareBaseObjects(x, y);
		}
	}
}
