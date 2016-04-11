using System;
using UnityEditor.Connect;
using UnityEngine;

namespace UnityEditor.Web
{
	[InitializeOnLoad]
	internal class UnetAccess : CloudServiceAccess
	{
		private const string kServiceName = "UNet";

		private const string kServiceDisplayName = "Multiplayer";

		private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/unet";

		private const string kMultiplayerNetworkingIdKey = "CloudNetworkingId";

		static UnetAccess()
		{
			UnityConnectServiceData cloudService = new UnityConnectServiceData("UNet", "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/unet", new UnetAccess(), "unity/project/cloud/networking");
			UnityConnectServiceCollection.instance.AddService(cloudService);
		}

		public override string GetServiceName()
		{
			return "UNet";
		}

		public override string GetServiceDisplayName()
		{
			return "Multiplayer";
		}

		public void SetMultiplayerId(int id)
		{
			PlayerSettings.InitializePropertyInt("CloudNetworkingId", id);
			PlayerPrefs.SetString("CloudNetworkingId", id.ToString());
			PlayerPrefs.Save();
		}
	}
}
