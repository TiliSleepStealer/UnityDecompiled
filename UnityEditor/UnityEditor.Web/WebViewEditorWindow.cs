using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using UnityEngine;

namespace UnityEditor.Web
{
	internal class WebViewEditorWindow : EditorWindow, ISerializationCallbackReceiver, IHasCustomMenu
	{
		private const int k_RepaintTimerDelay = 30;

		[SerializeField]
		protected string m_InitialOpenURL;

		[SerializeField]
		protected string m_GlobalObjectTypeName;

		protected object m_GlobalObject;

		internal WebView webView;

		internal WebScriptObject scriptObject;

		[SerializeField]
		private List<string> m_RegisteredViewURLs;

		[SerializeField]
		private List<WebView> m_RegisteredViewInstances;

		private Dictionary<string, WebView> m_RegisteredViews;

		private bool m_SyncingFocus;

		private int m_RepeatedShow;

		private Timer m_PostLoadTimer;

		public string initialOpenUrl
		{
			get
			{
				return this.m_InitialOpenURL;
			}
			set
			{
				this.m_InitialOpenURL = value;
			}
		}

		protected WebViewEditorWindow()
		{
			this.m_RegisteredViewURLs = new List<string>();
			this.m_RegisteredViewInstances = new List<WebView>();
			this.m_RegisteredViews = new Dictionary<string, WebView>();
			this.m_GlobalObject = null;
			Resolution currentResolution = Screen.currentResolution;
			int num = (currentResolution.width < 1024) ? currentResolution.width : 1024;
			int num2 = (currentResolution.height < 896) ? (currentResolution.height - 96) : 800;
			int num3 = (currentResolution.width - num) / 2;
			int num4 = (currentResolution.height - num2) / 2;
			base.position = new Rect((float)num3, (float)num4, (float)num, (float)num2);
			this.m_RepeatedShow = 0;
		}

		public static WebViewEditorWindow Create<T>(string title, string sourcesPath, int minWidth, int minHeight, int maxWidth, int maxHeight) where T : new()
		{
			WebViewEditorWindow webViewEditorWindow = ScriptableObject.CreateInstance<WebViewEditorWindow>();
			webViewEditorWindow.titleContent = new GUIContent(title);
			webViewEditorWindow.minSize = new Vector2((float)minWidth, (float)minHeight);
			webViewEditorWindow.maxSize = new Vector2((float)maxWidth, (float)maxHeight);
			webViewEditorWindow.m_InitialOpenURL = sourcesPath;
			webViewEditorWindow.m_GlobalObjectTypeName = typeof(T).FullName;
			webViewEditorWindow.Init();
			webViewEditorWindow.Show();
			return webViewEditorWindow;
		}

		public static WebViewEditorWindow CreateBase(string title, string sourcesPath, int minWidth, int minHeight, int maxWidth, int maxHeight)
		{
			WebViewEditorWindow window = EditorWindow.GetWindow<WebViewEditorWindow>(title);
			window.minSize = new Vector2((float)minWidth, (float)minHeight);
			window.maxSize = new Vector2((float)maxWidth, (float)maxHeight);
			window.m_InitialOpenURL = sourcesPath;
			window.m_GlobalObjectTypeName = null;
			window.Init();
			window.Show();
			return window;
		}

		public virtual void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("Reload"), false, new GenericMenu.MenuFunction(this.Reload));
			if (Unsupported.IsDeveloperBuild())
			{
				menu.AddItem(new GUIContent("About"), false, new GenericMenu.MenuFunction(this.About));
			}
		}

		public void Logout()
		{
		}

		public void Reload()
		{
			if (this.webView == null)
			{
				return;
			}
			this.webView.Reload();
		}

		public void About()
		{
			if (this.webView == null)
			{
				return;
			}
			this.webView.LoadURL("chrome://version");
		}

		public void OnLoadError(string url)
		{
			if (!this.webView)
			{
				return;
			}
		}

		public void ToggleMaximize()
		{
			base.maximized = !base.maximized;
			this.Refresh();
			this.SetFocus(true);
		}

		public void Init()
		{
			if (this.m_GlobalObject == null && !string.IsNullOrEmpty(this.m_GlobalObjectTypeName))
			{
				Type type = Type.GetType(this.m_GlobalObjectTypeName);
				if (type != null)
				{
					this.m_GlobalObject = Activator.CreateInstance(type);
					JSProxyMgr.GetInstance().AddGlobalObject(this.m_GlobalObject.GetType().Name, this.m_GlobalObject);
				}
			}
		}

		public void OnGUI()
		{
			Rect webViewRect = GUIClip.Unclip(new Rect(0f, 0f, base.position.width, base.position.height));
			if (this.m_RepeatedShow-- > 0)
			{
				this.Refresh();
			}
			if (this.m_InitialOpenURL != null)
			{
				if (!this.webView)
				{
					this.InitWebView(webViewRect);
				}
				if (Event.current.type == EventType.Layout)
				{
					this.webView.SetSizeAndPosition((int)webViewRect.x, (int)webViewRect.y, (int)webViewRect.width, (int)webViewRect.height);
				}
			}
		}

		public void OnBatchMode()
		{
			Rect webViewRect = GUIClip.Unclip(new Rect(0f, 0f, base.position.width, base.position.height));
			if (this.m_InitialOpenURL != null && !this.webView)
			{
				this.InitWebView(webViewRect);
			}
		}

		public void Refresh()
		{
			if (this.webView == null)
			{
				return;
			}
			this.webView.Hide();
			this.webView.Show();
		}

		public void OnFocus()
		{
			this.SetFocus(true);
		}

		public void OnLostFocus()
		{
			this.SetFocus(false);
		}

		public void OnEnable()
		{
			this.Init();
		}

		public void OnBecameInvisible()
		{
			if (!this.webView)
			{
				return;
			}
			this.webView.SetHostView(null);
		}

		public void OnDestroy()
		{
			if (this.webView != null)
			{
				UnityEngine.Object.DestroyImmediate(this.webView);
			}
			this.m_GlobalObject = null;
			foreach (WebView current in this.m_RegisteredViews.Values)
			{
				if (current != null)
				{
					UnityEngine.Object.DestroyImmediate(current);
				}
			}
			this.m_RegisteredViews.Clear();
			this.m_RegisteredViewURLs.Clear();
			this.m_RegisteredViewInstances.Clear();
		}

		public void OnBeforeSerialize()
		{
			this.m_RegisteredViewURLs = new List<string>();
			this.m_RegisteredViewInstances = new List<WebView>();
			foreach (KeyValuePair<string, WebView> current in this.m_RegisteredViews)
			{
				this.m_RegisteredViewURLs.Add(current.Key);
				this.m_RegisteredViewInstances.Add(current.Value);
			}
		}

		public void OnAfterDeserialize()
		{
			this.m_RegisteredViews = new Dictionary<string, WebView>();
			for (int num = 0; num != Math.Min(this.m_RegisteredViewURLs.Count, this.m_RegisteredViewInstances.Count); num++)
			{
				this.m_RegisteredViews.Add(this.m_RegisteredViewURLs[num], this.m_RegisteredViewInstances[num]);
			}
		}

		private void DoPostLoadTask()
		{
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.DoPostLoadTask));
			base.RepaintImmediately();
		}

		private void RaisePostLoadCondition(object obj, ElapsedEventArgs args)
		{
			this.m_PostLoadTimer.Stop();
			this.m_PostLoadTimer = null;
			EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(this.DoPostLoadTask));
		}

		private static string MakeUrlKey(string webViewUrl)
		{
			int num = webViewUrl.IndexOf("#");
			string text;
			if (num != -1)
			{
				text = webViewUrl.Substring(0, num);
			}
			else
			{
				text = webViewUrl;
			}
			num = text.LastIndexOf("/");
			if (num == text.Length - 1)
			{
				return text.Substring(0, num);
			}
			return text;
		}

		protected void UnregisterWebviewUrl(string webViewUrl)
		{
			string key = WebViewEditorWindow.MakeUrlKey(webViewUrl);
			this.m_RegisteredViews[key] = null;
		}

		private void RegisterWebviewUrl(string webViewUrl, WebView view)
		{
			string key = WebViewEditorWindow.MakeUrlKey(webViewUrl);
			this.m_RegisteredViews[key] = view;
		}

		private bool FindWebView(string webViewUrl, out WebView webView)
		{
			webView = null;
			string key = WebViewEditorWindow.MakeUrlKey(webViewUrl);
			return this.m_RegisteredViews.TryGetValue(key, out webView);
		}

		private void InitWebView(Rect webViewRect)
		{
			if (!this.webView)
			{
				int x = (int)webViewRect.x;
				int y = (int)webViewRect.y;
				int width = (int)webViewRect.width;
				int height = (int)webViewRect.height;
				this.webView = ScriptableObject.CreateInstance<WebView>();
				this.RegisterWebviewUrl(this.m_InitialOpenURL, this.webView);
				this.webView.InitWebView(this.m_Parent, x, y, width, height, false);
				this.webView.hideFlags = HideFlags.HideAndDontSave;
				this.SetFocus(base.hasFocus);
			}
			this.webView.SetDelegateObject(this);
			if (this.m_InitialOpenURL.StartsWith("http"))
			{
				this.webView.LoadURL(this.m_InitialOpenURL);
				this.m_PostLoadTimer = new Timer(30.0);
				this.m_PostLoadTimer.Elapsed += new ElapsedEventHandler(this.RaisePostLoadCondition);
				this.m_PostLoadTimer.Enabled = true;
			}
			else if (this.m_InitialOpenURL.StartsWith("file"))
			{
				this.webView.LoadFile(this.m_InitialOpenURL);
			}
			else
			{
				string path = Path.Combine(Uri.EscapeUriString(Path.Combine(EditorApplication.applicationContentsPath, "Resources")), this.m_InitialOpenURL);
				this.webView.LoadFile(path);
			}
		}

		public void OnInitScripting()
		{
			this.SetScriptObject();
		}

		protected void NotifyVisibility(bool visible)
		{
			if (this.webView == null)
			{
				return;
			}
			string text = "document.dispatchEvent(new CustomEvent('showWebView',{ detail: { visible:";
			text += ((!visible) ? "false" : "true");
			text += "}, bubbles: true, cancelable: false }));";
			this.webView.ExecuteJavascript(text);
		}

		public WebView GetWebViewFromURL(string url)
		{
			string key = WebViewEditorWindow.MakeUrlKey(url);
			return this.m_RegisteredViews[key];
		}

		protected void LoadPage()
		{
			if (!this.webView)
			{
				return;
			}
			WebView webView;
			if (!this.FindWebView(this.m_InitialOpenURL, out webView) || webView == null)
			{
				this.NotifyVisibility(false);
				this.webView.SetHostView(null);
				this.webView = null;
				Rect webViewRect = GUIClip.Unclip(new Rect(0f, 0f, base.position.width, base.position.height));
				this.InitWebView(webViewRect);
				this.NotifyVisibility(true);
			}
			else if (webView != this.webView)
			{
				this.NotifyVisibility(false);
				webView.SetHostView(this.m_Parent);
				this.webView.SetHostView(null);
				this.webView = webView;
				this.NotifyVisibility(true);
				this.webView.Show();
			}
		}

		private void CreateScriptObject()
		{
			if (this.scriptObject != null)
			{
				return;
			}
			this.scriptObject = ScriptableObject.CreateInstance<WebScriptObject>();
			this.scriptObject.hideFlags = HideFlags.HideAndDontSave;
			this.scriptObject.webView = this.webView;
		}

		private void SetScriptObject()
		{
			if (!this.webView)
			{
				return;
			}
			this.CreateScriptObject();
			this.webView.DefineScriptObject("window.webScriptObject", this.scriptObject);
		}

		private void InvokeJSMethod(string objectName, string name, params object[] args)
		{
			if (!this.webView)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(objectName);
			stringBuilder.Append('.');
			stringBuilder.Append(name);
			stringBuilder.Append('(');
			bool flag = true;
			for (int i = 0; i < args.Length; i++)
			{
				object obj = args[i];
				if (!flag)
				{
					stringBuilder.Append(',');
				}
				bool flag2 = obj is string;
				if (flag2)
				{
					stringBuilder.Append('"');
				}
				stringBuilder.Append(obj);
				if (flag2)
				{
					stringBuilder.Append('"');
				}
				flag = false;
			}
			stringBuilder.Append(");");
			this.webView.ExecuteJavascript(stringBuilder.ToString());
		}

		private void SetFocus(bool value)
		{
			if (this.m_SyncingFocus)
			{
				return;
			}
			this.m_SyncingFocus = true;
			if (this.webView != null)
			{
				if (value)
				{
					this.webView.SetHostView(this.m_Parent);
					if (Application.platform != RuntimePlatform.WindowsEditor)
					{
						this.m_RepeatedShow = 15;
					}
					else
					{
						this.webView.Show();
					}
				}
				this.webView.SetApplicationFocus(this.m_Parent != null && this.m_Parent.hasFocus && base.hasFocus);
				this.webView.SetFocus(value);
			}
			this.m_SyncingFocus = false;
		}
	}
}
