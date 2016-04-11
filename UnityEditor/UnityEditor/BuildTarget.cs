using System;

namespace UnityEditor
{
	public enum BuildTarget
	{
		StandaloneOSXUniversal = 2,
		StandaloneOSXIntel = 4,
		StandaloneWindows,
		WebPlayer,
		WebPlayerStreamed,
		iOS = 9,
		PS3,
		XBOX360,
		Android = 13,
		StandaloneGLESEmu,
		StandaloneLinux = 17,
		StandaloneWindows64 = 19,
		WebGL,
		WSAPlayer,
		StandaloneLinux64 = 24,
		StandaloneLinuxUniversal,
		WP8Player,
		StandaloneOSXIntel64,
		BlackBerry,
		Tizen,
		PSP2,
		PS4,
		PSM,
		XboxOne,
		SamsungTV,
		Nintendo3DS,
		WiiU,
		tvOS,
		[Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
		iPhone = -1,
		[Obsolete("Use BlackBerry instead (UnityUpgradable) -> BlackBerry", true)]
		BB10 = -1,
		[Obsolete("Use WSAPlayer instead (UnityUpgradable) -> WSAPlayer", true)]
		MetroPlayer = -1
	}
}
