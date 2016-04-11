using System;
using UnityEditor.Connect;

namespace UnityEditor.Web
{
	[InitializeOnLoad]
	internal class BuildAccess : CloudServiceAccess
	{
		private const string kServiceName = "Build";

		private const string kServiceDisplayName = "Unity Build";

		private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/build";

		static BuildAccess()
		{
			UnityConnectServiceData cloudService = new UnityConnectServiceData("Build", "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/build", new BuildAccess(), "unity/project/cloud/build");
			UnityConnectServiceCollection.instance.AddService(cloudService);
		}

		public override string GetServiceName()
		{
			return "Build";
		}

		public override string GetServiceDisplayName()
		{
			return "Unity Build";
		}
	}
}
