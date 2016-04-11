using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Networking
{
	[StructLayout(LayoutKind.Sequential)]
	public sealed class DownloadHandlerAssetBundle : DownloadHandler
	{
		public extern AssetBundle assetBundle
		{
			[WrapperlessIcall]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		public DownloadHandlerAssetBundle(string url, uint crc)
		{
			base.InternalCreateWebStream(url, crc);
		}

		public DownloadHandlerAssetBundle(string url, uint version, uint crc)
		{
			Hash128 hash = new Hash128(0u, 0u, 0u, version);
			base.InternalCreateWebStream(url, hash, crc);
		}

		public DownloadHandlerAssetBundle(string url, Hash128 hash, uint crc)
		{
			base.InternalCreateWebStream(url, hash, crc);
		}

		protected override byte[] GetData()
		{
			throw new NotSupportedException("Raw data access is not supported for asset bundles");
		}

		protected override string GetText()
		{
			throw new NotSupportedException("String access is not supported for asset bundles");
		}

		public static AssetBundle GetContent(UnityWebRequest www)
		{
			return DownloadHandler.GetCheckedDownloader<DownloadHandlerAssetBundle>(www).assetBundle;
		}
	}
}
