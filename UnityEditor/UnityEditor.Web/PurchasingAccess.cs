using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Connect;

namespace UnityEditor.Web
{
	[InitializeOnLoad]
	internal class PurchasingAccess : CloudServiceAccess
	{
		private const string kServiceName = "Purchasing";

		private const string kServiceDisplayName = "In App Purchasing";

		private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/purchasing";

		private static readonly Uri kPackageUri;

		private bool m_InstallInProgress;

		static PurchasingAccess()
		{
			PurchasingAccess.kPackageUri = new Uri("https://public-cdn.cloud.unity3d.com/UnityEngine.Cloud.Purchasing.unitypackage");
			UnityConnectServiceData cloudService = new UnityConnectServiceData("Purchasing", "https://public-cdn.cloud.unity3d.com/editor/5.3/production/cloud/purchasing", new PurchasingAccess(), "unity/project/cloud/purchasing");
			UnityConnectServiceCollection.instance.AddService(cloudService);
		}

		public override string GetServiceName()
		{
			return "Purchasing";
		}

		public override string GetServiceDisplayName()
		{
			return "In App Purchasing";
		}

		public override bool IsServiceEnabled()
		{
			return UnityPurchasingSettings.enabled;
		}

		public override void EnableService(bool enabled)
		{
			UnityPurchasingSettings.enabled = enabled;
		}

		public void InstallUnityPackage()
		{
			if (this.m_InstallInProgress)
			{
				return;
			}
			RemoteCertificateValidationCallback originalCallback = ServicePointManager.ServerCertificateValidationCallback;
			if (Application.platform != RuntimePlatform.OSXEditor)
			{
				ServicePointManager.ServerCertificateValidationCallback = ((object a, X509Certificate b, X509Chain c, SslPolicyErrors d) => true);
			}
			this.m_InstallInProgress = true;
			string location = FileUtil.GetUniqueTempPathInProject();
			location = Path.ChangeExtension(location, ".unitypackage");
			WebClient webClient = new WebClient();
			webClient.DownloadFileCompleted += delegate(object sender, AsyncCompletedEventArgs args)
			{
				EditorApplication.CallbackFunction handler = null;
				handler = delegate
				{
					ServicePointManager.ServerCertificateValidationCallback = originalCallback;
					EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, handler);
					this.m_InstallInProgress = false;
					if (args.Error != null)
					{
						this.ExecuteJSMethod("OnDownloadFailed", args.Error.Message);
					}
					else
					{
						AssetDatabase.ImportPackage(location, false);
						this.ExecuteJSMethod("OnDownloadComplete");
					}
				};
				EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, handler);
			};
			webClient.DownloadFileAsync(PurchasingAccess.kPackageUri, location);
		}

		private void ExecuteJSMethod(string name)
		{
			this.ExecuteJSMethod(name, null);
		}

		private void ExecuteJSMethod(string name, string arg)
		{
			string scriptCode = string.Format("UnityPurchasing.{0}({1})", name, (arg != null) ? string.Format("\"{0}\"", arg) : string.Empty);
			WebView webView = base.GetWebView();
			if (webView == null)
			{
				return;
			}
			webView.ExecuteJavascript(scriptCode);
		}
	}
}
