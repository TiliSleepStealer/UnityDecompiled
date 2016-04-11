using System;
using System.Collections.Generic;
using UnityEditor.Hardware;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	[CustomEditor(typeof(EditorSettings))]
	internal class EditorSettingsInspector : Editor
	{
		private struct PopupElement
		{
			public readonly string id;

			public readonly bool requiresTeamLicense;

			public readonly GUIContent content;

			public bool Enabled
			{
				get
				{
					return !this.requiresTeamLicense || InternalEditorUtility.HasTeamLicense();
				}
			}

			public PopupElement(string content)
			{
				this = new EditorSettingsInspector.PopupElement(content, false);
			}

			public PopupElement(string content, bool requiresTeamLicense)
			{
				this.id = content;
				this.content = new GUIContent(content);
				this.requiresTeamLicense = requiresTeamLicense;
			}
		}

		private EditorSettingsInspector.PopupElement[] vcDefaultPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement(ExternalVersionControl.Disabled),
			new EditorSettingsInspector.PopupElement(ExternalVersionControl.Generic),
			new EditorSettingsInspector.PopupElement(ExternalVersionControl.AssetServer, true)
		};

		private EditorSettingsInspector.PopupElement[] vcPopupList;

		private EditorSettingsInspector.PopupElement[] serializationPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Mixed"),
			new EditorSettingsInspector.PopupElement("Force Binary"),
			new EditorSettingsInspector.PopupElement("Force Text")
		};

		private EditorSettingsInspector.PopupElement[] behaviorPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("3D"),
			new EditorSettingsInspector.PopupElement("2D")
		};

		private EditorSettingsInspector.PopupElement[] spritePackerPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Disabled"),
			new EditorSettingsInspector.PopupElement("Enabled For Builds"),
			new EditorSettingsInspector.PopupElement("Always Enabled")
		};

		private EditorSettingsInspector.PopupElement[] spritePackerPaddingPowerPopupList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("1"),
			new EditorSettingsInspector.PopupElement("2"),
			new EditorSettingsInspector.PopupElement("3")
		};

		private EditorSettingsInspector.PopupElement[] remoteDevicePopupList;

		private DevDevice[] remoteDeviceList;

		private EditorSettingsInspector.PopupElement[] remoteCompressionList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("JPEG"),
			new EditorSettingsInspector.PopupElement("PNG")
		};

		private EditorSettingsInspector.PopupElement[] remoteResolutionList = new EditorSettingsInspector.PopupElement[]
		{
			new EditorSettingsInspector.PopupElement("Normal"),
			new EditorSettingsInspector.PopupElement("Downsize")
		};

		private string[] logLevelPopupList = new string[]
		{
			"Verbose",
			"Info",
			"Notice",
			"Fatal"
		};

		private string[] semanticMergePopupList = new string[]
		{
			"Off",
			"Premerge",
			"Ask"
		};

		public readonly GUIContent spritePaddingPower = EditorGUIUtility.TextContent("Padding Power|Set Padding Power if Atlas is enabled.");

		public void OnEnable()
		{
			Plugin[] availablePlugins = Plugin.availablePlugins;
			this.vcPopupList = new EditorSettingsInspector.PopupElement[availablePlugins.Length + this.vcDefaultPopupList.Length];
			Array.Copy(this.vcDefaultPopupList, this.vcPopupList, this.vcDefaultPopupList.Length);
			int num = 0;
			int i = this.vcDefaultPopupList.Length;
			while (i < this.vcPopupList.Length)
			{
				this.vcPopupList[i] = new EditorSettingsInspector.PopupElement(availablePlugins[num].name, true);
				i++;
				num++;
			}
			DevDeviceList.Changed += new DevDeviceList.OnChangedHandler(this.OnDeviceListChanged);
			this.BuildRemoteDeviceList();
		}

		public void OnDisable()
		{
			DevDeviceList.Changed -= new DevDeviceList.OnChangedHandler(this.OnDeviceListChanged);
		}

		private void OnDeviceListChanged()
		{
			this.BuildRemoteDeviceList();
		}

		private void BuildRemoteDeviceList()
		{
			List<DevDevice> list = new List<DevDevice>();
			List<EditorSettingsInspector.PopupElement> list2 = new List<EditorSettingsInspector.PopupElement>();
			list.Add(DevDevice.none);
			list2.Add(new EditorSettingsInspector.PopupElement("None"));
			list.Add(new DevDevice("Any Android Device", "Any Android Device", "Android", "Android", DevDeviceState.Connected, DevDeviceFeatures.RemoteConnection));
			list2.Add(new EditorSettingsInspector.PopupElement("Any Android Device"));
			DevDevice[] devices = DevDeviceList.GetDevices();
			for (int i = 0; i < devices.Length; i++)
			{
				DevDevice item = devices[i];
				bool flag = (item.features & DevDeviceFeatures.RemoteConnection) != DevDeviceFeatures.None;
				if (item.isConnected && flag)
				{
					list.Add(item);
					list2.Add(new EditorSettingsInspector.PopupElement(item.name));
				}
			}
			this.remoteDeviceList = list.ToArray();
			this.remoteDevicePopupList = list2.ToArray();
		}

		public override void OnInspectorGUI()
		{
			bool enabled = GUI.enabled;
			this.ShowUnityRemoteGUI(enabled);
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Version Control", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			ExternalVersionControl selvc = EditorSettings.externalVersionControl;
			int num = Array.FindIndex<EditorSettingsInspector.PopupElement>(this.vcPopupList, (EditorSettingsInspector.PopupElement cand) => cand.content.text == selvc);
			if (num < 0)
			{
				num = 0;
			}
			GUIContent content = new GUIContent(this.vcPopupList[num].content);
			Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Mode"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.vcPopupList, num, new GenericMenu.MenuFunction2(this.SetVersionControlSystem));
			}
			if (this.VersionControlSystemHasGUI())
			{
				GUI.enabled = true;
				bool enabled2 = false;
				if (EditorSettings.externalVersionControl == ExternalVersionControl.AssetServer)
				{
					EditorUserSettings.SetConfigValue("vcUsername", EditorGUILayout.TextField("User", EditorUserSettings.GetConfigValue("vcUsername"), new GUILayoutOption[0]));
					EditorUserSettings.SetConfigValue("vcPassword", EditorGUILayout.PasswordField("Password", EditorUserSettings.GetConfigValue("vcPassword"), new GUILayoutOption[0]));
				}
				else if (!(EditorSettings.externalVersionControl == ExternalVersionControl.Generic))
				{
					if (!(EditorSettings.externalVersionControl == ExternalVersionControl.Disabled))
					{
						ConfigField[] activeConfigFields = Provider.GetActiveConfigFields();
						enabled2 = true;
						ConfigField[] array = activeConfigFields;
						for (int i = 0; i < array.Length; i++)
						{
							ConfigField configField = array[i];
							string configValue = EditorUserSettings.GetConfigValue(configField.name);
							string text;
							if (configField.isPassword)
							{
								text = EditorGUILayout.PasswordField(configField.label, configValue, new GUILayoutOption[0]);
							}
							else
							{
								text = EditorGUILayout.TextField(configField.label, configValue, new GUILayoutOption[0]);
							}
							if (text != configValue)
							{
								EditorUserSettings.SetConfigValue(configField.name, text);
							}
							if (configField.isRequired && string.IsNullOrEmpty(text))
							{
								enabled2 = false;
							}
						}
					}
				}
				string logLevel = EditorUserSettings.GetConfigValue("vcSharedLogLevel");
				int num2 = Array.FindIndex<string>(this.logLevelPopupList, (string item) => item.ToLower() == logLevel);
				if (num2 == -1)
				{
					logLevel = "info";
				}
				int num3 = EditorGUILayout.Popup("Log Level", Math.Abs(num2), this.logLevelPopupList, new GUILayoutOption[0]);
				if (num3 != num2)
				{
					EditorUserSettings.SetConfigValue("vcSharedLogLevel", this.logLevelPopupList[num3].ToLower());
				}
				GUI.enabled = enabled;
				string label = "Connected";
				if (Provider.onlineState == OnlineState.Updating)
				{
					label = "Connecting...";
				}
				else if (Provider.onlineState == OnlineState.Offline)
				{
					label = "Disconnected";
				}
				EditorGUILayout.LabelField("Status", label, new GUILayoutOption[0]);
				if (Provider.onlineState != OnlineState.Online && !string.IsNullOrEmpty(Provider.offlineReason))
				{
					GUI.enabled = false;
					GUILayout.TextArea(Provider.offlineReason, new GUILayoutOption[0]);
					GUI.enabled = enabled;
				}
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				GUI.enabled = enabled2;
				if (GUILayout.Button("Connect", EditorStyles.miniButton, new GUILayoutOption[0]))
				{
					Provider.UpdateSettings();
				}
				GUILayout.EndHorizontal();
				EditorUserSettings.AutomaticAdd = EditorGUILayout.Toggle("Automatic add", EditorUserSettings.AutomaticAdd, new GUILayoutOption[0]);
				if (Provider.requiresNetwork)
				{
					bool flag = EditorGUILayout.Toggle("Work Offline", EditorUserSettings.WorkOffline, new GUILayoutOption[0]);
					if (flag != EditorUserSettings.WorkOffline)
					{
						if (flag && !EditorUtility.DisplayDialog("Confirm working offline", "Working offline and making changes to your assets means that you will have to manually integrate changes back into version control using your standard version control client before you stop working offline in Unity. Make sure you know what you are doing.", "Work offline", "Cancel"))
						{
							flag = false;
						}
						EditorUserSettings.WorkOffline = flag;
						EditorApplication.RequestRepaintAllViews();
					}
				}
				if (Provider.hasCheckoutSupport)
				{
					EditorUserSettings.showFailedCheckout = EditorGUILayout.Toggle("Show failed checkouts", EditorUserSettings.showFailedCheckout, new GUILayoutOption[0]);
				}
				GUI.enabled = enabled;
				EditorUserSettings.semanticMergeMode = (SemanticMergeMode)EditorGUILayout.Popup("Smart merge", (int)EditorUserSettings.semanticMergeMode, this.semanticMergePopupList, new GUILayoutOption[0]);
				this.DrawOverlayDescriptions();
			}
			GUILayout.Space(10f);
			GUILayout.Label("WWW Security Emulation", EditorStyles.boldLabel, new GUILayoutOption[0]);
			EditorSettings.webSecurityEmulationEnabled = EditorGUILayout.Toggle("Enable Webplayer Security Emulation", EditorSettings.webSecurityEmulationEnabled, new GUILayoutOption[0]);
			string text2 = EditorGUILayout.TextField("Host URL", EditorSettings.webSecurityEmulationHostUrl, new GUILayoutOption[0]);
			if (text2 != EditorSettings.webSecurityEmulationHostUrl)
			{
				EditorSettings.webSecurityEmulationHostUrl = text2;
			}
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Asset Serialization", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			content = new GUIContent(this.serializationPopupList[(int)EditorSettings.serializationMode].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Mode"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.serializationPopupList, (int)EditorSettings.serializationMode, new GenericMenu.MenuFunction2(this.SetAssetSerializationMode));
			}
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Default Behavior Mode", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			int num4 = Mathf.Clamp((int)EditorSettings.defaultBehaviorMode, 0, this.behaviorPopupList.Length - 1);
			content = new GUIContent(this.behaviorPopupList[num4].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Mode"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.behaviorPopupList, num4, new GenericMenu.MenuFunction2(this.SetDefaultBehaviorMode));
			}
			GUILayout.Space(10f);
			GUI.enabled = true;
			GUILayout.Label("Sprite Packer", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = enabled;
			num4 = Mathf.Clamp((int)EditorSettings.spritePackerMode, 0, this.spritePackerPopupList.Length - 1);
			content = new GUIContent(this.spritePackerPopupList[num4].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Mode"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.spritePackerPopupList, num4, new GenericMenu.MenuFunction2(this.SetSpritePackerMode));
			}
			num4 = Mathf.Clamp(EditorSettings.spritePackerPaddingPower - 1, 0, 2);
			content = new GUIContent(this.spritePackerPaddingPowerPopupList[num4].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Padding Power"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.spritePackerPaddingPowerPopupList, num4, new GenericMenu.MenuFunction2(this.SetSpritePackerPaddingPower));
			}
			this.DoProjectGenerationSettings();
		}

		private void DoProjectGenerationSettings()
		{
			GUILayout.Space(10f);
			GUILayout.Label("C# Project Generation", EditorStyles.boldLabel, new GUILayoutOption[0]);
			string text = EditorSettings.Internal_ProjectGenerationUserExtensions;
			string text2 = EditorGUILayout.TextField("Additional extensions to include", text, new GUILayoutOption[0]);
			if (text2 != text)
			{
				EditorSettings.Internal_ProjectGenerationUserExtensions = text2;
			}
			text = EditorSettings.projectGenerationRootNamespace;
			text2 = EditorGUILayout.TextField("Root namespace", text, new GUILayoutOption[0]);
			if (text2 != text)
			{
				EditorSettings.projectGenerationRootNamespace = text2;
			}
		}

		private static int GetIndexById(DevDevice[] elements, string id, int defaultIndex)
		{
			for (int i = 0; i < elements.Length; i++)
			{
				if (elements[i].id == id)
				{
					return i;
				}
			}
			return defaultIndex;
		}

		private static int GetIndexById(EditorSettingsInspector.PopupElement[] elements, string id, int defaultIndex)
		{
			for (int i = 0; i < elements.Length; i++)
			{
				if (elements[i].id == id)
				{
					return i;
				}
			}
			return defaultIndex;
		}

		private void ShowUnityRemoteGUI(bool editorEnabled)
		{
			GUI.enabled = true;
			GUILayout.Label("Unity Remote", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUI.enabled = editorEnabled;
			string unityRemoteDevice = EditorSettings.unityRemoteDevice;
			int indexById = EditorSettingsInspector.GetIndexById(this.remoteDeviceList, unityRemoteDevice, 0);
			GUIContent content = new GUIContent(this.remoteDevicePopupList[indexById].content);
			Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Device"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteDevicePopupList, indexById, new GenericMenu.MenuFunction2(this.SetUnityRemoteDevice));
			}
			int indexById2 = EditorSettingsInspector.GetIndexById(this.remoteCompressionList, EditorSettings.unityRemoteCompression, 0);
			content = new GUIContent(this.remoteCompressionList[indexById2].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Compression"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteCompressionList, indexById2, new GenericMenu.MenuFunction2(this.SetUnityRemoteCompression));
			}
			int indexById3 = EditorSettingsInspector.GetIndexById(this.remoteResolutionList, EditorSettings.unityRemoteResolution, 0);
			content = new GUIContent(this.remoteResolutionList[indexById3].content);
			rect = GUILayoutUtility.GetRect(content, EditorStyles.popup);
			rect = EditorGUI.PrefixLabel(rect, 0, new GUIContent("Resolution"));
			if (EditorGUI.ButtonMouseDown(rect, content, FocusType.Passive, EditorStyles.popup))
			{
				this.DoPopup(rect, this.remoteResolutionList, indexById3, new GenericMenu.MenuFunction2(this.SetUnityRemoteResolution));
			}
		}

		private void DrawOverlayDescriptions()
		{
			Texture2D overlayAtlas = Provider.overlayAtlas;
			if (overlayAtlas == null)
			{
				return;
			}
			GUILayout.Space(10f);
			GUILayout.Label("Overlay legends", EditorStyles.boldLabel, new GUILayoutOption[0]);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			this.DrawOverlayDescription(Asset.States.Local);
			this.DrawOverlayDescription(Asset.States.OutOfSync);
			this.DrawOverlayDescription(Asset.States.CheckedOutLocal);
			this.DrawOverlayDescription(Asset.States.CheckedOutRemote);
			this.DrawOverlayDescription(Asset.States.DeletedLocal);
			this.DrawOverlayDescription(Asset.States.DeletedRemote);
			GUILayout.EndVertical();
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			this.DrawOverlayDescription(Asset.States.AddedLocal);
			this.DrawOverlayDescription(Asset.States.AddedRemote);
			this.DrawOverlayDescription(Asset.States.Conflicted);
			this.DrawOverlayDescription(Asset.States.LockedLocal);
			this.DrawOverlayDescription(Asset.States.LockedRemote);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		private void DrawOverlayDescription(Asset.States state)
		{
			Rect atlasRectForState = Provider.GetAtlasRectForState((int)state);
			if (atlasRectForState.width == 0f)
			{
				return;
			}
			Texture2D overlayAtlas = Provider.overlayAtlas;
			if (overlayAtlas == null)
			{
				return;
			}
			GUILayout.Label("    " + Asset.StateToString(state), EditorStyles.miniLabel, new GUILayoutOption[0]);
			Rect lastRect = GUILayoutUtility.GetLastRect();
			lastRect.width = 16f;
			GUI.DrawTextureWithTexCoords(lastRect, overlayAtlas, atlasRectForState);
		}

		private void DoPopup(Rect popupRect, EditorSettingsInspector.PopupElement[] elements, int selectedIndex, GenericMenu.MenuFunction2 func)
		{
			GenericMenu genericMenu = new GenericMenu();
			for (int i = 0; i < elements.Length; i++)
			{
				EditorSettingsInspector.PopupElement popupElement = elements[i];
				if (popupElement.Enabled)
				{
					genericMenu.AddItem(popupElement.content, i == selectedIndex, func, i);
				}
				else
				{
					genericMenu.AddDisabledItem(popupElement.content);
				}
			}
			genericMenu.DropDown(popupRect);
		}

		private bool VersionControlSystemHasGUI()
		{
			ExternalVersionControl d = EditorSettings.externalVersionControl;
			return d != ExternalVersionControl.Disabled && d != ExternalVersionControl.AutoDetect && d != ExternalVersionControl.AssetServer && d != ExternalVersionControl.Generic;
		}

		private void SetVersionControlSystem(object data)
		{
			int num = (int)data;
			if (num < 0 && num >= this.vcPopupList.Length)
			{
				return;
			}
			EditorSettingsInspector.PopupElement popupElement = this.vcPopupList[num];
			string externalVersionControl = EditorSettings.externalVersionControl;
			EditorSettings.externalVersionControl = popupElement.content.text;
			Provider.UpdateSettings();
			AssetDatabase.Refresh();
			if (externalVersionControl != popupElement.content.text)
			{
				if (popupElement.content.text == ExternalVersionControl.AssetServer || popupElement.content.text == ExternalVersionControl.Disabled || popupElement.content.text == ExternalVersionControl.Generic)
				{
					WindowPending.CloseAllWindows();
				}
				else
				{
					ASMainWindow[] array = Resources.FindObjectsOfTypeAll(typeof(ASMainWindow)) as ASMainWindow[];
					ASMainWindow aSMainWindow = (array.Length <= 0) ? null : array[0];
					if (aSMainWindow != null)
					{
						aSMainWindow.Close();
					}
				}
			}
		}

		private void SetAssetSerializationMode(object data)
		{
			int serializationMode = (int)data;
			EditorSettings.serializationMode = (SerializationMode)serializationMode;
		}

		private void SetUnityRemoteDevice(object data)
		{
			EditorSettings.unityRemoteDevice = this.remoteDeviceList[(int)data].id;
		}

		private void SetUnityRemoteCompression(object data)
		{
			EditorSettings.unityRemoteCompression = this.remoteCompressionList[(int)data].id;
		}

		private void SetUnityRemoteResolution(object data)
		{
			EditorSettings.unityRemoteResolution = this.remoteResolutionList[(int)data].id;
		}

		private void SetDefaultBehaviorMode(object data)
		{
			int defaultBehaviorMode = (int)data;
			EditorSettings.defaultBehaviorMode = (EditorBehaviorMode)defaultBehaviorMode;
		}

		private void SetSpritePackerMode(object data)
		{
			int spritePackerMode = (int)data;
			EditorSettings.spritePackerMode = (SpritePackerMode)spritePackerMode;
		}

		private void SetSpritePackerPaddingPower(object data)
		{
			int num = (int)data;
			EditorSettings.spritePackerPaddingPower = num + 1;
		}
	}
}
