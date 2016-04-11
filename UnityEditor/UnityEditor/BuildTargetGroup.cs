using System;

namespace UnityEditor
{
	public enum BuildTargetGroup
	{
		Unknown,
		Standalone,
		WebPlayer,
		[Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
		iPhone = 4,
		iOS = 4,
		PS3,
		XBOX360,
		Android,
		GLESEmu = 9,
		WebGL = 13,
		[Obsolete("Use WSA instead")]
		Metro,
		WSA = 14,
		WP8,
		BlackBerry,
		Tizen,
		PSP2,
		PS4,
		PSM,
		XboxOne,
		SamsungTV,
		Nintendo3DS,
		WiiU,
		tvOS
	}
}
