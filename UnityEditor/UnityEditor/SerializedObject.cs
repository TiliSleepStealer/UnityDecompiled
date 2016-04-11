using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor
{
	public sealed class SerializedObject
	{
		private IntPtr m_Property;

		public extern UnityEngine.Object targetObject
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public extern UnityEngine.Object[] targetObjects
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		internal extern bool hasModifiedProperties
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		internal extern InspectorMode inspectorMode
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public extern bool isEditingMultipleObjects
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public SerializedObject(UnityEngine.Object obj)
		{
			this.InternalCreate(new UnityEngine.Object[]
			{
				obj
			});
		}

		public SerializedObject(UnityEngine.Object[] objs)
		{
			this.InternalCreate(objs);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void InternalCreate(UnityEngine.Object[] monoObjs);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Update();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SetIsDifferentCacheDirty();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void UpdateIfDirtyOrScript();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Dispose();

		~SerializedObject()
		{
			this.Dispose();
		}

		public SerializedProperty GetIterator()
		{
			SerializedProperty iterator_Internal = this.GetIterator_Internal();
			iterator_Internal.m_SerializedObject = this;
			return iterator_Internal;
		}

		public SerializedProperty FindProperty(string propertyPath)
		{
			SerializedProperty iterator_Internal = this.GetIterator_Internal();
			iterator_Internal.m_SerializedObject = this;
			if (iterator_Internal.FindPropertyInternal(propertyPath))
			{
				return iterator_Internal;
			}
			return null;
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern SerializedProperty GetIterator_Internal();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern void Cache(int instanceID);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern SerializedObject LoadFromCache(int instanceID);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern PropertyModification ExtractPropertyModification(string propertyPath);

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool ApplyModifiedProperties();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool ApplyModifiedPropertiesWithoutUndo();

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void CopyFromSerializedProperty(SerializedProperty prop);
	}
}
