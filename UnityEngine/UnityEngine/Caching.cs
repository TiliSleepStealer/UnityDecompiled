using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
	public sealed class Caching
	{
		[Obsolete("this API is not for public use.")]
		public static extern CacheIndex[] index
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static extern long spaceFree
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static extern long maximumAvailableDiskSpace
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern long spaceOccupied
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		[Obsolete("Please use Caching.spaceFree instead")]
		public static extern int spaceAvailable
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		[Obsolete("Please use Caching.spaceOccupied instead")]
		public static extern int spaceUsed
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static extern int expirationDelay
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern bool enabled
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern bool compressionEnabled
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		public static extern bool ready
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public static bool Authorize(string name, string domain, long size, string signature)
		{
			return Caching.Authorize(name, domain, size, -1, signature);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool Authorize(string name, string domain, long size, int expiration, string signature);

		[Obsolete("Size is now specified as a long")]
		public static bool Authorize(string name, string domain, int size, int expiration, string signature)
		{
			return Caching.Authorize(name, domain, (long)size, expiration, signature);
		}

		[Obsolete("Size is now specified as a long")]
		public static bool Authorize(string name, string domain, int size, string signature)
		{
			return Caching.Authorize(name, domain, (long)size, signature);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool CleanCache();

		[Obsolete("this API is not for public use."), WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool CleanNamedCache(string name);

		[Obsolete("This function is obsolete and has no effect."), WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool DeleteFromCache(string url);

		[Obsolete("This function is obsolete and will always return -1. Use IsVersionCached instead."), WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern int GetVersionFromCache(string url);

		public static bool IsVersionCached(string url, int version)
		{
			Hash128 hash = new Hash128(0u, 0u, 0u, (uint)version);
			return Caching.IsVersionCached(url, hash);
		}

		public static bool IsVersionCached(string url, Hash128 hash)
		{
			return Caching.INTERNAL_CALL_IsVersionCached(url, ref hash);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_IsVersionCached(string url, ref Hash128 hash);

		public static bool MarkAsUsed(string url, int version)
		{
			Hash128 hash = new Hash128(0u, 0u, 0u, (uint)version);
			return Caching.MarkAsUsed(url, hash);
		}

		public static bool MarkAsUsed(string url, Hash128 hash)
		{
			return Caching.INTERNAL_CALL_MarkAsUsed(url, ref hash);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool INTERNAL_CALL_MarkAsUsed(string url, ref Hash128 hash);

		public static void SetNoBackupFlag(string url, int version)
		{
		}

		public static void SetNoBackupFlag(string url, Hash128 hash)
		{
			Caching.INTERNAL_CALL_SetNoBackupFlag(url, ref hash);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_SetNoBackupFlag(string url, ref Hash128 hash);

		public static void ResetNoBackupFlag(string url, int version)
		{
		}

		public static void ResetNoBackupFlag(string url, Hash128 hash)
		{
			Caching.INTERNAL_CALL_ResetNoBackupFlag(url, ref hash);
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void INTERNAL_CALL_ResetNoBackupFlag(string url, ref Hash128 hash);
	}
}
