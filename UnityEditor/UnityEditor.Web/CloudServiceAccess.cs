using System;
using UnityEditor.Connect;

namespace UnityEditor.Web
{
	internal abstract class CloudServiceAccess
	{
		private const string kServiceEnabled = "ServiceEnabled";

		public abstract string GetServiceName();

		protected WebView GetWebView()
		{
			return UnityConnectServiceCollection.instance.GetWebViewFromServiceName(this.GetServiceName());
		}

		protected string GetSafeServiceName()
		{
			return this.GetServiceName().Replace(' ', '_');
		}

		public virtual string GetServiceDisplayName()
		{
			return this.GetServiceName();
		}

		public virtual bool IsServiceEnabled()
		{
			bool result;
			bool.TryParse(this.GetServiceConfig("ServiceEnabled"), out result);
			return result;
		}

		public virtual void EnableService(bool enabled)
		{
			this.SetServiceConfig("ServiceEnabled", enabled.ToString());
		}

		public string GetServiceConfig(string key)
		{
			string name = this.GetSafeServiceName() + "_" + key;
			string empty = string.Empty;
			if (PlayerSettings.GetPropertyOptionalString(name, ref empty))
			{
				return empty;
			}
			return string.Empty;
		}

		public void SetServiceConfig(string key, string value)
		{
			string name = this.GetSafeServiceName() + "_" + key;
			string empty = string.Empty;
			if (!PlayerSettings.GetPropertyOptionalString(name, ref empty))
			{
				PlayerSettings.InitializePropertyString(name, value);
			}
			else
			{
				PlayerSettings.SetPropertyString(name, value);
			}
		}

		public void GoBackToHub()
		{
			UnityConnectServiceCollection.instance.ShowService("Hub", true);
		}
	}
}
