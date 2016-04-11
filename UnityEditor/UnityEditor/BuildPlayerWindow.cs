using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Connect;
using UnityEditor.Modules;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
	internal class BuildPlayerWindow : EditorWindow
	{
		public class SceneSorter : IComparer
		{
			int IComparer.Compare(object x, object y)
			{
				return new CaseInsensitiveComparer().Compare(y, x);
			}
		}

		public class BuildPlatform
		{
			public string name;

			public GUIContent title;

			public Texture2D smallIcon;

			public BuildTargetGroup targetGroup;

			public bool forceShowTarget;

			public string tooltip;

			public BuildTarget DefaultTarget
			{
				get
				{
					switch (this.targetGroup)
					{
					case BuildTargetGroup.Standalone:
						return BuildTarget.StandaloneWindows;
					case BuildTargetGroup.WebPlayer:
						return BuildTarget.WebPlayer;
					case BuildTargetGroup.iPhone:
						return BuildTarget.iOS;
					case BuildTargetGroup.PS3:
						return BuildTarget.PS3;
					case BuildTargetGroup.XBOX360:
						return BuildTarget.XBOX360;
					case BuildTargetGroup.Android:
						return BuildTarget.Android;
					case BuildTargetGroup.GLESEmu:
						return BuildTarget.StandaloneGLESEmu;
					case BuildTargetGroup.WebGL:
						return BuildTarget.WebGL;
					case BuildTargetGroup.Metro:
						return BuildTarget.WSAPlayer;
					case BuildTargetGroup.WP8:
						return BuildTarget.WP8Player;
					case BuildTargetGroup.BlackBerry:
						return BuildTarget.BlackBerry;
					case BuildTargetGroup.Tizen:
						return BuildTarget.Tizen;
					case BuildTargetGroup.PSP2:
						return BuildTarget.PSP2;
					case BuildTargetGroup.PS4:
						return BuildTarget.PS4;
					case BuildTargetGroup.XboxOne:
						return BuildTarget.XboxOne;
					case BuildTargetGroup.SamsungTV:
						return BuildTarget.SamsungTV;
					case BuildTargetGroup.Nintendo3DS:
						return BuildTarget.Nintendo3DS;
					case BuildTargetGroup.WiiU:
						return BuildTarget.WiiU;
					case BuildTargetGroup.tvOS:
						return BuildTarget.tvOS;
					}
					return BuildTarget.iPhone;
				}
			}

			public BuildPlatform(string locTitle, string iconId, BuildTargetGroup targetGroup, bool forceShowTarget) : this(locTitle, string.Empty, iconId, targetGroup, forceShowTarget)
			{
			}

			public BuildPlatform(string locTitle, string tooltip, string iconId, BuildTargetGroup targetGroup, bool forceShowTarget)
			{
				this.targetGroup = targetGroup;
				this.name = ((targetGroup == BuildTargetGroup.Unknown) ? string.Empty : BuildPipeline.GetBuildTargetGroupName(this.DefaultTarget));
				this.title = EditorGUIUtility.TextContentWithIcon(locTitle, iconId);
				this.smallIcon = (EditorGUIUtility.IconContent(iconId + ".Small").image as Texture2D);
				this.tooltip = tooltip;
				this.forceShowTarget = forceShowTarget;
			}
		}

		private class BuildPlatforms
		{
			public BuildPlayerWindow.BuildPlatform[] buildPlatforms;

			public BuildTarget[] standaloneSubtargets;

			public GUIContent[] standaloneSubtargetStrings;

			internal BuildPlatforms()
			{
				List<BuildPlayerWindow.BuildPlatform> list = new List<BuildPlayerWindow.BuildPlatform>();
				list.Add(new BuildPlayerWindow.BuildPlatform("Web Player", "BuildSettings.Web", BuildTargetGroup.WebPlayer, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("PC, Mac & Linux Standalone", "BuildSettings.Standalone", BuildTargetGroup.Standalone, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("iOS", "BuildSettings.iPhone", BuildTargetGroup.iPhone, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("tvOS", "BuildSettings.tvOS", BuildTargetGroup.tvOS, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Android", "BuildSettings.Android", BuildTargetGroup.Android, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Tizen", "BuildSettings.Tizen", BuildTargetGroup.Tizen, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Xbox 360", "BuildSettings.XBox360", BuildTargetGroup.XBOX360, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Xbox One", "BuildSettings.XboxOne", BuildTargetGroup.XboxOne, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("PS3", "BuildSettings.PS3", BuildTargetGroup.PS3, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("PS Vita", "BuildSettings.PSP2", BuildTargetGroup.PSP2, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("PS4", "BuildSettings.PS4", BuildTargetGroup.PS4, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("GLES Emulator", "BuildSettings.StandaloneGLESEmu", BuildTargetGroup.GLESEmu, false));
				list.Add(new BuildPlayerWindow.BuildPlatform("Wii U", "BuildSettings.WiiU", BuildTargetGroup.WiiU, false));
				list.Add(new BuildPlayerWindow.BuildPlatform("Windows Store", "BuildSettings.Metro", BuildTargetGroup.Metro, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("WebGL", "BuildSettings.WebGL", BuildTargetGroup.WebGL, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Samsung TV", "BuildSettings.SamsungTV", BuildTargetGroup.SamsungTV, true));
				list.Add(new BuildPlayerWindow.BuildPlatform("Nintendo 3DS", "BuildSettings.N3DS", BuildTargetGroup.Nintendo3DS, false));
				foreach (BuildPlayerWindow.BuildPlatform current in list)
				{
					current.tooltip = BuildPipeline.GetBuildTargetGroupDisplayName(current.targetGroup) + " settings";
				}
				this.buildPlatforms = list.ToArray();
				this.SetupStandaloneSubtargets();
			}

			private void SetupStandaloneSubtargets()
			{
				List<BuildTarget> list = new List<BuildTarget>();
				List<GUIContent> list2 = new List<GUIContent>();
				if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows)))
				{
					list.Add(BuildTarget.StandaloneWindows);
					list2.Add(EditorGUIUtility.TextContent("Windows"));
				}
				if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)))
				{
					list.Add(BuildTarget.StandaloneOSXIntel);
					list2.Add(EditorGUIUtility.TextContent("Mac OS X"));
				}
				if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)))
				{
					list.Add(BuildTarget.StandaloneLinux);
					list2.Add(EditorGUIUtility.TextContent("Linux"));
				}
				this.standaloneSubtargets = list.ToArray();
				this.standaloneSubtargetStrings = list2.ToArray();
			}

			public string GetBuildTargetDisplayName(BuildTarget target)
			{
				BuildPlayerWindow.BuildPlatform[] array = this.buildPlatforms;
				for (int i = 0; i < array.Length; i++)
				{
					BuildPlayerWindow.BuildPlatform buildPlatform = array[i];
					if (buildPlatform.DefaultTarget == target)
					{
						return buildPlatform.title.text;
					}
				}
				if (target == BuildTarget.WebPlayerStreamed)
				{
					return this.BuildPlatformFromTargetGroup(BuildTargetGroup.WebPlayer).title.text;
				}
				for (int j = 0; j < this.standaloneSubtargets.Length; j++)
				{
					if (this.standaloneSubtargets[j] == BuildPlayerWindow.BuildPlatforms.DefaultTargetForPlatform(target))
					{
						return this.standaloneSubtargetStrings[j].text;
					}
				}
				return "Unsupported Target";
			}

			public static Dictionary<GUIContent, BuildTarget> GetArchitecturesForPlatform(BuildTarget target)
			{
				switch (target)
				{
				case BuildTarget.StandaloneOSXUniversal:
				case BuildTarget.StandaloneOSXIntel:
					goto IL_B6;
				case (BuildTarget)3:
					IL_1A:
					switch (target)
					{
					case BuildTarget.StandaloneLinux64:
					case BuildTarget.StandaloneLinuxUniversal:
						goto IL_78;
					case BuildTarget.WP8Player:
						IL_33:
						switch (target)
						{
						case BuildTarget.StandaloneLinux:
							goto IL_78;
						case BuildTarget.StandaloneWindows64:
							goto IL_4D;
						}
						return null;
					case BuildTarget.StandaloneOSXIntel64:
						goto IL_B6;
					}
					goto IL_33;
					IL_78:
					return new Dictionary<GUIContent, BuildTarget>
					{
						{
							EditorGUIUtility.TextContent("x86"),
							BuildTarget.StandaloneLinux
						},
						{
							EditorGUIUtility.TextContent("x86_64"),
							BuildTarget.StandaloneLinux64
						},
						{
							EditorGUIUtility.TextContent("x86 + x86_64 (Universal)"),
							BuildTarget.StandaloneLinuxUniversal
						}
					};
				case BuildTarget.StandaloneWindows:
					goto IL_4D;
				}
				goto IL_1A;
				IL_4D:
				return new Dictionary<GUIContent, BuildTarget>
				{
					{
						EditorGUIUtility.TextContent("x86"),
						BuildTarget.StandaloneWindows
					},
					{
						EditorGUIUtility.TextContent("x86_64"),
						BuildTarget.StandaloneWindows64
					}
				};
				IL_B6:
				return new Dictionary<GUIContent, BuildTarget>
				{
					{
						EditorGUIUtility.TextContent("x86"),
						BuildTarget.StandaloneOSXIntel
					},
					{
						EditorGUIUtility.TextContent("x86_64"),
						BuildTarget.StandaloneOSXIntel64
					},
					{
						EditorGUIUtility.TextContent("Universal"),
						BuildTarget.StandaloneOSXUniversal
					}
				};
			}

			public static BuildTarget DefaultTargetForPlatform(BuildTarget target)
			{
				switch (target)
				{
				case BuildTarget.StandaloneLinux:
				case BuildTarget.StandaloneLinux64:
				case BuildTarget.StandaloneLinuxUniversal:
					return BuildTarget.StandaloneLinux;
				case (BuildTarget)18:
				case BuildTarget.WebGL:
				case (BuildTarget)22:
				case (BuildTarget)23:
					IL_37:
					switch (target)
					{
					case BuildTarget.StandaloneOSXUniversal:
					case BuildTarget.StandaloneOSXIntel:
						return BuildTarget.StandaloneOSXIntel;
					case BuildTarget.StandaloneWindows:
						return BuildTarget.StandaloneWindows;
					}
					return target;
				case BuildTarget.StandaloneWindows64:
					return BuildTarget.StandaloneWindows;
				case BuildTarget.WSAPlayer:
					return BuildTarget.WSAPlayer;
				case BuildTarget.WP8Player:
					return BuildTarget.WP8Player;
				case BuildTarget.StandaloneOSXIntel64:
					return BuildTarget.StandaloneOSXIntel;
				}
				goto IL_37;
			}

			public int BuildPlatformIndexFromTargetGroup(BuildTargetGroup group)
			{
				for (int i = 0; i < this.buildPlatforms.Length; i++)
				{
					if (group == this.buildPlatforms[i].targetGroup)
					{
						return i;
					}
				}
				return -1;
			}

			public BuildPlayerWindow.BuildPlatform BuildPlatformFromTargetGroup(BuildTargetGroup group)
			{
				int num = this.BuildPlatformIndexFromTargetGroup(group);
				return (num == -1) ? null : this.buildPlatforms[num];
			}
		}

		private class Styles
		{
			public const float kButtonWidth = 110f;

			private const string kShopURL = "https://store.unity3d.com/shop/";

			private const string kDownloadURL = "http://unity3d.com/unity/download/";

			private const string kMailURL = "http://unity3d.com/company/sales?type=sales";

			public GUIStyle selected = "ServerUpdateChangesetOn";

			public GUIStyle box = "OL Box";

			public GUIStyle title = "OL title";

			public GUIStyle evenRow = "CN EntryBackEven";

			public GUIStyle oddRow = "CN EntryBackOdd";

			public GUIStyle platformSelector = "PlayerSettingsPlatform";

			public GUIStyle toggle = "Toggle";

			public GUIStyle levelString = "PlayerSettingsLevel";

			public GUIStyle levelStringCounter = new GUIStyle("Label");

			public Vector2 toggleSize;

			public GUIContent noSessionDialogText = EditorGUIUtility.TextContent("In order to publish your build to UDN, you need to sign in via the AssetStore and tick the 'Stay signed in' checkbox.");

			public GUIContent platformTitle = EditorGUIUtility.TextContent("Platform|Which platform to build for");

			public GUIContent switchPlatform = EditorGUIUtility.TextContent("Switch Platform");

			public GUIContent build = EditorGUIUtility.TextContent("Build");

			public GUIContent export = EditorGUIUtility.TextContent("Export");

			public GUIContent buildAndRun = EditorGUIUtility.TextContent("Build And Run");

			public GUIContent scenesInBuild = EditorGUIUtility.TextContent("Scenes In Build|Which scenes to include in the build");

			public Texture2D activePlatformIcon = EditorGUIUtility.IconContent("BuildSettings.SelectedIcon").image as Texture2D;

			public GUIContent[,] notLicensedMessages;

			private GUIContent[,] buildTargetNotInstalled;

			public GUIContent standaloneTarget;

			public GUIContent architecture;

			public GUIContent webPlayerStreamed;

			public GUIContent webPlayerOfflineDeployment;

			public GUIContent debugBuild;

			public GUIContent profileBuild;

			public GUIContent allowDebugging;

			public GUIContent symlinkiOSLibraries;

			public GUIContent explicitNullChecks;

			public GUIContent enableHeadlessMode;

			public GUIContent buildScriptsOnly;

			public GUIContent forceOptimizeScriptCompilation;

			public GUIContent learnAboutUnityCloudBuild;

			public Styles()
			{
				GUIContent[,] expr_128 = new GUIContent[18, 3];
				expr_128[0, 0] = EditorGUIUtility.TextContent("Unity Web Player building is disabled during the public preview beta. It will be enabled when Unity ships.");
				expr_128[0, 1] = new GUIContent(string.Empty);
				expr_128[0, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[1, 0] = EditorGUIUtility.TextContent("Your license does not cover Standalone Publishing.");
				expr_128[1, 1] = new GUIContent(string.Empty);
				expr_128[1, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[2, 0] = EditorGUIUtility.TextContent("Your license does not cover iOS Publishing.");
				expr_128[2, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[2, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[3, 0] = EditorGUIUtility.TextContent("Your license does not cover Apple TV Publishing.");
				expr_128[3, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[3, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[4, 0] = EditorGUIUtility.TextContent("Your license does not cover Android Publishing.");
				expr_128[4, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[4, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[5, 0] = EditorGUIUtility.TextContent("Your license does not cover Tizen Publishing.");
				expr_128[5, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[5, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[6, 0] = EditorGUIUtility.TextContent("Your license does not cover Xbox 360 Publishing.");
				expr_128[6, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[6, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[7, 0] = EditorGUIUtility.TextContent("Your license does not cover Xbox One Publishing.");
				expr_128[7, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[7, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[8, 0] = EditorGUIUtility.TextContent("Your license does not cover PS3 Publishing.");
				expr_128[8, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[8, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[9, 0] = EditorGUIUtility.TextContent("Your license does not cover PS Vita Publishing.");
				expr_128[9, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[9, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[10, 0] = EditorGUIUtility.TextContent("Your license does not cover PS4 Publishing.");
				expr_128[10, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[10, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[11, 0] = EditorGUIUtility.TextContent("Your license does not cover GLESEmu Publishing");
				expr_128[11, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[11, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[12, 0] = EditorGUIUtility.TextContent("Your license does not cover Wii U Publishing.");
				expr_128[12, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[12, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				expr_128[13, 0] = EditorGUIUtility.TextContent("Your license does not cover Flash Publishing");
				expr_128[13, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[13, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[14, 0] = EditorGUIUtility.TextContent("Your license does not cover Windows Store Publishing.");
				expr_128[14, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[14, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[15, 0] = EditorGUIUtility.TextContent("Your license does not cover Windows Phone 8 Publishing.");
				expr_128[15, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[15, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[16, 0] = EditorGUIUtility.TextContent("Your license does not cover SamsungTV Publishing");
				expr_128[16, 1] = EditorGUIUtility.TextContent("Go to Our Online Store");
				expr_128[16, 2] = new GUIContent("https://store.unity3d.com/shop/");
				expr_128[17, 0] = EditorGUIUtility.TextContent("Your license does not cover Nintendo 3DS Publishing");
				expr_128[17, 1] = EditorGUIUtility.TextContent("Contact sales");
				expr_128[17, 2] = new GUIContent("http://unity3d.com/company/sales?type=sales");
				this.notLicensedMessages = expr_128;
				GUIContent[,] expr_51D = new GUIContent[18, 3];
				expr_51D[0, 0] = EditorGUIUtility.TextContent("Web Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[0, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[1, 0] = EditorGUIUtility.TextContent("Standalone Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[1, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[2, 0] = EditorGUIUtility.TextContent("iOS Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[2, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[3, 0] = EditorGUIUtility.TextContent("Apple TV Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[3, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[4, 0] = EditorGUIUtility.TextContent("Android Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[4, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[5, 0] = EditorGUIUtility.TextContent("Tizen is not supported in this build.\nDownload a build that supports it.");
				expr_51D[5, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[6, 0] = EditorGUIUtility.TextContent("Xbox 360 Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[6, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[7, 0] = EditorGUIUtility.TextContent("Xbox One Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[7, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[8, 0] = EditorGUIUtility.TextContent("PS3 Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[8, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[9, 0] = EditorGUIUtility.TextContent("PS Vita Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[9, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[10, 0] = EditorGUIUtility.TextContent("PS4 Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[10, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[11, 0] = EditorGUIUtility.TextContent("GLESEmu Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[11, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[12, 0] = EditorGUIUtility.TextContent("Wii U Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[12, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[13, 0] = EditorGUIUtility.TextContent("Flash Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[13, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[14, 0] = EditorGUIUtility.TextContent("Windows Store Player is not supported in\nthis build.\n\nDownload a build that supports it.");
				expr_51D[14, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[15, 0] = EditorGUIUtility.TextContent("Windows Phone 8 Player is not supported\nin this build.\n\nDownload a build that supports it.");
				expr_51D[15, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[16, 0] = EditorGUIUtility.TextContent("SamsungTV Player is not supported in this build.\nDownload a build that supports it.");
				expr_51D[16, 2] = new GUIContent("http://unity3d.com/unity/download/");
				expr_51D[17, 0] = EditorGUIUtility.TextContent("Ninteno 3DS is not supported in this build.\nDownload a build that supports it.");
				expr_51D[17, 2] = new GUIContent("http://unity3d.com/unity/download/");
				this.buildTargetNotInstalled = expr_51D;
				this.standaloneTarget = EditorGUIUtility.TextContent("Target Platform|Destination platform for standalone build");
				this.architecture = EditorGUIUtility.TextContent("Architecture|Build architecture for standalone");
				this.webPlayerStreamed = EditorGUIUtility.TextContent("Streamed|Is the web player streamed in?");
				this.webPlayerOfflineDeployment = EditorGUIUtility.TextContent("Offline Deployment|Web Player will not reference online resources");
				this.debugBuild = EditorGUIUtility.TextContent("Development Build");
				this.profileBuild = EditorGUIUtility.TextContent("Autoconnect Profiler");
				this.allowDebugging = EditorGUIUtility.TextContent("Script Debugging");
				this.symlinkiOSLibraries = EditorGUIUtility.TextContent("Symlink Unity libraries");
				this.explicitNullChecks = EditorGUIUtility.TextContent("Explicit Null Checks");
				this.enableHeadlessMode = EditorGUIUtility.TextContent("Headless Mode");
				this.buildScriptsOnly = EditorGUIUtility.TextContent("Scripts Only Build");
				this.forceOptimizeScriptCompilation = EditorGUIUtility.TextContent("Build Optimized Scripts|Compile IL2CPP using full compiler optimizations. Note this will obfuscate callstack output.");
				this.learnAboutUnityCloudBuild = EditorGUIUtility.TextContent("Learn about Unity Cloud Build");
				base..ctor();
				this.levelStringCounter.alignment = TextAnchor.MiddleRight;
			}

			public GUIContent GetTargetNotInstalled(int index, int item)
			{
				if (index >= this.buildTargetNotInstalled.GetLength(0))
				{
					index = 0;
				}
				return this.buildTargetNotInstalled[index, item];
			}

			public GUIContent GetDownloadErrorForTarget(BuildTarget target)
			{
				return null;
			}
		}

		private class ScenePostprocessor : AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
			{
				EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
				for (int i = 0; i < movedAssets.Length; i++)
				{
					string path = movedAssets[i];
					if (Path.GetExtension(path) == ".unity")
					{
						EditorBuildSettingsScene[] array = scenes;
						for (int j = 0; j < array.Length; j++)
						{
							EditorBuildSettingsScene editorBuildSettingsScene = array[j];
							if (editorBuildSettingsScene.path.ToLower() == movedFromPath[i].ToLower())
							{
								editorBuildSettingsScene.path = path;
							}
						}
					}
				}
				EditorBuildSettings.scenes = scenes;
			}
		}

		private const string kAssetsFolder = "Assets/";

		private const string kEditorBuildSettingsPath = "ProjectSettings/EditorBuildSettings.asset";

		private static BuildPlayerWindow.BuildPlatforms s_BuildPlatforms;

		private ListViewState lv = new ListViewState();

		private bool[] selectedLVItems = new bool[0];

		private bool[] selectedBeforeDrag;

		private int initialSelectedLVItem = -1;

		private Vector2 scrollPosition = new Vector2(0f, 0f);

		private static BuildPlayerWindow.Styles styles;

		public BuildPlayerWindow()
		{
			base.position = new Rect(50f, 50f, 540f, 530f);
			base.minSize = new Vector2(550f, 580f);
			base.titleContent = new GUIContent("Build Settings");
		}

		private static void ShowBuildPlayerWindow()
		{
			EditorUserBuildSettings.selectedBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
			EditorWindow.GetWindow<BuildPlayerWindow>(true, "Build Settings");
		}

		private static void BuildPlayerAndRun()
		{
			if (!BuildPlayerWindow.BuildPlayerWithDefaultSettings(false, BuildOptions.AutoRunPlayer))
			{
				BuildPlayerWindow.ShowBuildPlayerWindow();
			}
		}

		private static void BuildPlayerAndSelect()
		{
			if (!BuildPlayerWindow.BuildPlayerWithDefaultSettings(false, BuildOptions.ShowBuiltPlayer))
			{
				BuildPlayerWindow.ShowBuildPlayerWindow();
			}
		}

		private static bool BuildPlayerWithDefaultSettings(bool askForBuildLocation, BuildOptions forceOptions)
		{
			return BuildPlayerWindow.BuildPlayerWithDefaultSettings(askForBuildLocation, forceOptions, true);
		}

		private static bool IsMetroPlayer(BuildTarget target)
		{
			return target == BuildTarget.WSAPlayer;
		}

		private static bool IsWP8Player(BuildTarget target)
		{
			return target == BuildTarget.WP8Player;
		}

		private static bool BuildPlayerWithDefaultSettings(bool askForBuildLocation, BuildOptions forceOptions, bool first)
		{
			bool flag = false;
			BuildPlayerWindow.InitBuildPlatforms();
			if (!UnityConnect.instance.canBuildWithUPID && !EditorUtility.DisplayDialog("Missing Project ID", "Because you are not a member of this project this build will not access Unity services.\nDo you want to continue?", "Yes", "No"))
			{
				return false;
			}
			BuildTarget buildTarget = BuildPlayerWindow.CalculateSelectedBuildTarget();
			if (!BuildPipeline.IsBuildTargetSupported(buildTarget))
			{
				return false;
			}
			string targetStringFromBuildTargetGroup = ModuleManager.GetTargetStringFromBuildTargetGroup(BuildPlayerWindow.s_BuildPlatforms.BuildPlatformFromTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup).targetGroup);
			IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(targetStringFromBuildTargetGroup);
			if (buildWindowExtension != null && (forceOptions & BuildOptions.AutoRunPlayer) != BuildOptions.None && !buildWindowExtension.EnabledBuildAndRunButton())
			{
				return false;
			}
			if (Unsupported.IsBleedingEdgeBuild())
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("This version of Unity is a BleedingEdge build that has not seen any manual testing.");
				stringBuilder.AppendLine("You should consider this build unstable.");
				stringBuilder.AppendLine("We strongly recommend that you use a normal version of Unity instead.");
				if (EditorUtility.DisplayDialog("BleedingEdge Build", stringBuilder.ToString(), "Cancel", "OK"))
				{
					return false;
				}
			}
			if (buildTarget == BuildTarget.BlackBerry && (forceOptions & BuildOptions.AutoRunPlayer) != BuildOptions.None && (string.IsNullOrEmpty(PlayerSettings.BlackBerry.deviceAddress) || string.IsNullOrEmpty(PlayerSettings.BlackBerry.devicePassword)))
			{
				Debug.LogError(EditorGUIUtility.TextContent("Author Id, Device Address and Device Password must all be set in order to use Build and Run").text);
				return false;
			}
			string text = string.Empty;
			bool flag2 = EditorUserBuildSettings.installInBuildFolder && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTarget) && (Unsupported.IsDeveloperBuild() || BuildPlayerWindow.IsMetroPlayer(buildTarget) || BuildPlayerWindow.IsWP8Player(buildTarget));
			BuildOptions buildOptions = forceOptions;
			bool development = EditorUserBuildSettings.development;
			if (development)
			{
				buildOptions |= BuildOptions.Development;
			}
			if (EditorUserBuildSettings.allowDebugging && development)
			{
				buildOptions |= BuildOptions.AllowDebugging;
			}
			if (EditorUserBuildSettings.symlinkLibraries)
			{
				buildOptions |= BuildOptions.SymlinkLibraries;
			}
			if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
			{
				buildOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
			}
			if (EditorUserBuildSettings.webPlayerOfflineDeployment)
			{
				buildOptions |= BuildOptions.WebPlayerOfflineDeployment;
			}
			if (EditorUserBuildSettings.enableHeadlessMode)
			{
				buildOptions |= BuildOptions.EnableHeadlessMode;
			}
			if (EditorUserBuildSettings.connectProfiler && (development || buildTarget == BuildTarget.WSAPlayer || BuildPlayerWindow.IsWP8Player(buildTarget)))
			{
				buildOptions |= BuildOptions.ConnectWithProfiler;
			}
			if (EditorUserBuildSettings.buildScriptsOnly)
			{
				buildOptions |= BuildOptions.BuildScriptsOnly;
			}
			if (EditorUserBuildSettings.forceOptimizeScriptCompilation)
			{
				buildOptions |= BuildOptions.ForceOptimizeScriptCompilation;
			}
			if (flag2)
			{
				buildOptions |= BuildOptions.InstallInBuildFolder;
			}
			if (!flag2)
			{
				if (askForBuildLocation && !BuildPlayerWindow.PickBuildLocation(buildTarget, buildOptions, out flag))
				{
					return false;
				}
				text = EditorUserBuildSettings.GetBuildLocation(buildTarget);
				if (text.Length == 0)
				{
					return false;
				}
				if (!askForBuildLocation)
				{
					switch (InternalEditorUtility.BuildCanBeAppended(buildTarget, text))
					{
					case CanAppendBuild.Yes:
						flag = true;
						break;
					case CanAppendBuild.No:
						if (!BuildPlayerWindow.PickBuildLocation(buildTarget, buildOptions, out flag))
						{
							return false;
						}
						text = EditorUserBuildSettings.GetBuildLocation(buildTarget);
						if (text.Length == 0 || !Directory.Exists(FileUtil.DeleteLastPathNameComponent(text)))
						{
							return false;
						}
						break;
					}
				}
			}
			if (flag)
			{
				buildOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
			}
			ArrayList arrayList = new ArrayList();
			EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
			EditorBuildSettingsScene[] array = scenes;
			for (int i = 0; i < array.Length; i++)
			{
				EditorBuildSettingsScene editorBuildSettingsScene = array[i];
				if (editorBuildSettingsScene.enabled)
				{
					arrayList.Add(editorBuildSettingsScene.path);
				}
			}
			string[] levels = arrayList.ToArray(typeof(string)) as string[];
			bool delayToAfterScriptReload = false;
			if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
			{
				if (!EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget))
				{
					Debug.LogErrorFormat("Could not switch to build target '{0}'.", new object[]
					{
						BuildPlayerWindow.s_BuildPlatforms.GetBuildTargetDisplayName(buildTarget)
					});
					return false;
				}
				if (EditorApplication.isCompiling)
				{
					delayToAfterScriptReload = true;
				}
			}
			uint num = 0u;
			string text2 = BuildPipeline.BuildPlayerInternalNoCheck(levels, text, buildTarget, buildOptions, delayToAfterScriptReload, out num);
			return text2.Length == 0;
		}

		private void ActiveScenesGUI()
		{
			int num = 0;
			int row = this.lv.row;
			bool shift = Event.current.shift;
			bool actionKey = EditorGUI.actionKey;
			Event current = Event.current;
			Rect rect = GUILayoutUtility.GetRect(BuildPlayerWindow.styles.scenesInBuild, BuildPlayerWindow.styles.title);
			ArrayList arrayList = new ArrayList(EditorBuildSettings.scenes);
			this.lv.totalRows = arrayList.Count;
			if (this.selectedLVItems.Length != arrayList.Count)
			{
				Array.Resize<bool>(ref this.selectedLVItems, arrayList.Count);
			}
			int[] array = new int[arrayList.Count];
			for (int i = 0; i < array.Length; i++)
			{
				EditorBuildSettingsScene editorBuildSettingsScene = (EditorBuildSettingsScene)arrayList[i];
				array[i] = num;
				if (editorBuildSettingsScene.enabled)
				{
					num++;
				}
			}
			foreach (ListViewElement listViewElement in ListViewGUILayout.ListView(this.lv, (ListViewOptions)3, BuildPlayerWindow.styles.box, new GUILayoutOption[0]))
			{
				EditorBuildSettingsScene editorBuildSettingsScene2 = (EditorBuildSettingsScene)arrayList[listViewElement.row];
				bool flag = File.Exists(editorBuildSettingsScene2.path);
				EditorGUI.BeginDisabledGroup(!flag);
				bool flag2 = this.selectedLVItems[listViewElement.row];
				if (flag2 && current.type == EventType.Repaint)
				{
					BuildPlayerWindow.styles.selected.Draw(listViewElement.position, false, false, false, false);
				}
				if (!flag)
				{
					editorBuildSettingsScene2.enabled = false;
				}
				Rect position = new Rect(listViewElement.position.x + 4f, listViewElement.position.y, BuildPlayerWindow.styles.toggleSize.x, BuildPlayerWindow.styles.toggleSize.y);
				EditorGUI.BeginChangeCheck();
				editorBuildSettingsScene2.enabled = GUI.Toggle(position, editorBuildSettingsScene2.enabled, string.Empty);
				if (EditorGUI.EndChangeCheck() && flag2)
				{
					for (int j = 0; j < arrayList.Count; j++)
					{
						if (this.selectedLVItems[j])
						{
							((EditorBuildSettingsScene)arrayList[j]).enabled = editorBuildSettingsScene2.enabled;
						}
					}
				}
				GUILayout.Space(BuildPlayerWindow.styles.toggleSize.x);
				string text = editorBuildSettingsScene2.path;
				if (text.StartsWith("Assets/"))
				{
					text = text.Substring("Assets/".Length);
				}
				if (text.EndsWith(".unity"))
				{
					text = text.Substring(0, text.Length - ".unity".Length);
				}
				Rect rect2 = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(text), BuildPlayerWindow.styles.levelString);
				if (Event.current.type == EventType.Repaint)
				{
					BuildPlayerWindow.styles.levelString.Draw(rect2, EditorGUIUtility.TempContent(text), false, false, flag2, false);
				}
				GUILayout.Label((!editorBuildSettingsScene2.enabled) ? string.Empty : array[listViewElement.row].ToString(), BuildPlayerWindow.styles.levelStringCounter, new GUILayoutOption[]
				{
					GUILayout.MaxWidth(36f)
				});
				EditorGUI.EndDisabledGroup();
				if (ListViewGUILayout.HasMouseUp(listViewElement.position) && !shift && !actionKey)
				{
					if (!shift && !actionKey)
					{
						ListViewGUILayout.MultiSelection(row, listViewElement.row, ref this.initialSelectedLVItem, ref this.selectedLVItems);
					}
				}
				else if (ListViewGUILayout.HasMouseDown(listViewElement.position))
				{
					if (!this.selectedLVItems[listViewElement.row] || shift || actionKey)
					{
						ListViewGUILayout.MultiSelection(row, listViewElement.row, ref this.initialSelectedLVItem, ref this.selectedLVItems);
					}
					this.lv.row = listViewElement.row;
					this.selectedBeforeDrag = new bool[this.selectedLVItems.Length];
					this.selectedLVItems.CopyTo(this.selectedBeforeDrag, 0);
					this.selectedBeforeDrag[this.lv.row] = true;
				}
			}
			GUI.Label(rect, BuildPlayerWindow.styles.scenesInBuild, BuildPlayerWindow.styles.title);
			if (GUIUtility.keyboardControl == this.lv.ID)
			{
				if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SelectAll")
				{
					Event.current.Use();
				}
				else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SelectAll")
				{
					for (int i = 0; i < this.selectedLVItems.Length; i++)
					{
						this.selectedLVItems[i] = true;
					}
					this.lv.selectionChanged = true;
					Event.current.Use();
					GUIUtility.ExitGUI();
				}
			}
			if (this.lv.selectionChanged)
			{
				ListViewGUILayout.MultiSelection(row, this.lv.row, ref this.initialSelectedLVItem, ref this.selectedLVItems);
			}
			if (this.lv.fileNames != null)
			{
				Array.Sort<string>(this.lv.fileNames);
				int num2 = 0;
				for (int i = 0; i < this.lv.fileNames.Length; i++)
				{
					if (this.lv.fileNames[i].EndsWith("unity"))
					{
						EditorBuildSettingsScene editorBuildSettingsScene3 = new EditorBuildSettingsScene();
						editorBuildSettingsScene3.path = FileUtil.GetProjectRelativePath(this.lv.fileNames[i]);
						if (editorBuildSettingsScene3.path == string.Empty)
						{
							editorBuildSettingsScene3.path = this.lv.fileNames[i];
						}
						editorBuildSettingsScene3.enabled = true;
						arrayList.Insert(this.lv.draggedTo + num2++, editorBuildSettingsScene3);
					}
				}
				if (num2 != 0)
				{
					Array.Resize<bool>(ref this.selectedLVItems, arrayList.Count);
					for (int i = 0; i < this.selectedLVItems.Length; i++)
					{
						this.selectedLVItems[i] = (i >= this.lv.draggedTo && i < this.lv.draggedTo + num2);
					}
				}
				this.lv.draggedTo = -1;
			}
			if (this.lv.draggedTo != -1)
			{
				ArrayList arrayList2 = new ArrayList();
				int num3 = 0;
				int i = 0;
				while (i < this.selectedLVItems.Length)
				{
					if (this.selectedBeforeDrag[i])
					{
						arrayList2.Add(arrayList[num3]);
						arrayList.RemoveAt(num3);
						num3--;
						if (this.lv.draggedTo >= i)
						{
							this.lv.draggedTo--;
						}
					}
					i++;
					num3++;
				}
				this.lv.draggedTo = ((this.lv.draggedTo <= arrayList.Count && this.lv.draggedTo >= 0) ? this.lv.draggedTo : arrayList.Count);
				arrayList.InsertRange(this.lv.draggedTo, arrayList2);
				for (i = 0; i < this.selectedLVItems.Length; i++)
				{
					this.selectedLVItems[i] = (i >= this.lv.draggedTo && i < this.lv.draggedTo + arrayList2.Count);
				}
			}
			if (current.type == EventType.KeyDown && (current.keyCode == KeyCode.Backspace || current.keyCode == KeyCode.Delete) && GUIUtility.keyboardControl == this.lv.ID)
			{
				int num3 = 0;
				int i = 0;
				while (i < this.selectedLVItems.Length)
				{
					if (this.selectedLVItems[i])
					{
						arrayList.RemoveAt(num3);
						num3--;
					}
					this.selectedLVItems[i] = false;
					i++;
					num3++;
				}
				this.lv.row = 0;
				current.Use();
			}
			EditorBuildSettings.scenes = (arrayList.ToArray(typeof(EditorBuildSettingsScene)) as EditorBuildSettingsScene[]);
		}

		private void AddOpenScenes()
		{
			List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
			bool flag = false;
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.path.Length != 0 || EditorSceneManager.SaveScene(scene, string.Empty, false))
				{
					if (!list.Any((EditorBuildSettingsScene s) => s.path == scene.path))
					{
						list.Add(new EditorBuildSettingsScene
						{
							path = scene.path,
							enabled = true
						});
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			EditorBuildSettings.scenes = list.ToArray();
			base.Repaint();
			GUIUtility.ExitGUI();
		}

		private static BuildTarget CalculateSelectedBuildTarget()
		{
			BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			BuildTargetGroup buildTargetGroup = selectedBuildTargetGroup;
			if (buildTargetGroup == BuildTargetGroup.Standalone)
			{
				return EditorUserBuildSettings.selectedStandaloneTarget;
			}
			if (buildTargetGroup == BuildTargetGroup.WebPlayer)
			{
				return (!EditorUserBuildSettings.webPlayerStreamed) ? BuildTarget.WebPlayer : BuildTarget.WebPlayerStreamed;
			}
			if (BuildPlayerWindow.s_BuildPlatforms == null)
			{
				throw new Exception("Build platforms are not initialized.");
			}
			BuildPlayerWindow.BuildPlatform buildPlatform = BuildPlayerWindow.s_BuildPlatforms.BuildPlatformFromTargetGroup(selectedBuildTargetGroup);
			if (buildPlatform == null)
			{
				throw new Exception("Could not find build platform for target group " + selectedBuildTargetGroup);
			}
			return buildPlatform.DefaultTarget;
		}

		private void ActiveBuildTargetsGUI()
		{
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			GUILayout.BeginVertical(new GUILayoutOption[]
			{
				GUILayout.Width(255f)
			});
			GUILayout.Label(BuildPlayerWindow.styles.platformTitle, BuildPlayerWindow.styles.title, new GUILayoutOption[0]);
			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, "OL Box");
			for (int i = 0; i < 2; i++)
			{
				bool flag = i == 0;
				bool flag2 = false;
				BuildPlayerWindow.BuildPlatform[] buildPlatforms = BuildPlayerWindow.s_BuildPlatforms.buildPlatforms;
				for (int j = 0; j < buildPlatforms.Length; j++)
				{
					BuildPlayerWindow.BuildPlatform buildPlatform = buildPlatforms[j];
					if (BuildPlayerWindow.IsBuildTargetGroupSupported(buildPlatform.DefaultTarget) == flag)
					{
						if (BuildPlayerWindow.IsBuildTargetGroupSupported(buildPlatform.DefaultTarget) || buildPlatform.forceShowTarget)
						{
							this.ShowOption(buildPlatform, buildPlatform.title, (!flag2) ? BuildPlayerWindow.styles.oddRow : BuildPlayerWindow.styles.evenRow);
							flag2 = !flag2;
						}
					}
				}
				GUI.contentColor = Color.white;
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.Space(10f);
			BuildTarget buildTarget = BuildPlayerWindow.CalculateSelectedBuildTarget();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUI.enabled = (BuildPipeline.IsBuildTargetSupported(buildTarget) && BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) != BuildPipeline.GetBuildTargetGroup(buildTarget));
			if (GUILayout.Button(BuildPlayerWindow.styles.switchPlatform, new GUILayoutOption[]
			{
				GUILayout.Width(110f)
			}))
			{
				EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget);
				GUIUtility.ExitGUI();
			}
			GUI.enabled = BuildPipeline.IsBuildTargetSupported(buildTarget);
			if (GUILayout.Button(new GUIContent("Player Settings..."), new GUILayoutOption[]
			{
				GUILayout.Width(110f)
			}))
			{
				Selection.activeObject = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
			}
			GUILayout.EndHorizontal();
			GUI.enabled = true;
			GUILayout.EndVertical();
		}

		private void ShowAlert()
		{
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Space(10f);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			EditorGUILayout.HelpBox("Because you are not a member of this project this build will not access Unity services.", MessageType.Warning);
			GUILayout.EndVertical();
			GUILayout.Space(5f);
			GUILayout.EndHorizontal();
		}

		private void ShowOption(BuildPlayerWindow.BuildPlatform bp, GUIContent title, GUIStyle background)
		{
			Rect rect = GUILayoutUtility.GetRect(50f, 36f);
			rect.x += 1f;
			rect.y += 1f;
			bool flag = BuildPipeline.LicenseCheck(bp.DefaultTarget);
			GUI.contentColor = new Color(1f, 1f, 1f, (!flag) ? 0.7f : 1f);
			bool flag2 = EditorUserBuildSettings.selectedBuildTargetGroup == bp.targetGroup;
			if (Event.current.type == EventType.Repaint)
			{
				background.Draw(rect, GUIContent.none, false, false, flag2, false);
				GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, 32f, 32f), title.image, GUIStyle.none);
				if (BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget) == bp.targetGroup)
				{
					GUI.Label(new Rect(rect.xMax - (float)BuildPlayerWindow.styles.activePlatformIcon.width - 8f, rect.y + 3f + (float)((32 - BuildPlayerWindow.styles.activePlatformIcon.height) / 2), (float)BuildPlayerWindow.styles.activePlatformIcon.width, (float)BuildPlayerWindow.styles.activePlatformIcon.height), BuildPlayerWindow.styles.activePlatformIcon, GUIStyle.none);
				}
			}
			if (GUI.Toggle(rect, flag2, title.text, BuildPlayerWindow.styles.platformSelector) && EditorUserBuildSettings.selectedBuildTargetGroup != bp.targetGroup)
			{
				EditorUserBuildSettings.selectedBuildTargetGroup = bp.targetGroup;
				UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(InspectorWindow));
				for (int i = 0; i < array.Length; i++)
				{
					InspectorWindow inspectorWindow = array[i] as InspectorWindow;
					if (inspectorWindow != null)
					{
						inspectorWindow.Repaint();
					}
				}
			}
		}

		private void OnGUI()
		{
			if (BuildPlayerWindow.styles == null)
			{
				BuildPlayerWindow.styles = new BuildPlayerWindow.Styles();
				BuildPlayerWindow.styles.toggleSize = BuildPlayerWindow.styles.toggle.CalcSize(new GUIContent("X"));
				this.lv.rowHeight = (int)BuildPlayerWindow.styles.levelString.CalcHeight(new GUIContent("X"), 100f);
			}
			BuildPlayerWindow.InitBuildPlatforms();
			if (!UnityConnect.instance.canBuildWithUPID)
			{
				this.ShowAlert();
			}
			GUILayout.Space(5f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Space(10f);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			string empty = string.Empty;
			bool flag = !AssetDatabase.IsOpenForEdit("ProjectSettings/EditorBuildSettings.asset", out empty);
			EditorGUI.BeginDisabledGroup(flag);
			this.ActiveScenesGUI();
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			if (flag)
			{
				GUI.enabled = true;
				if (Provider.enabled && GUILayout.Button("Check out", new GUILayoutOption[0]))
				{
					Asset assetByPath = Provider.GetAssetByPath("ProjectSettings/EditorBuildSettings.asset");
					Provider.Checkout(new AssetList
					{
						assetByPath
					}, CheckoutMode.Asset);
				}
				GUILayout.Label(empty, new GUILayoutOption[0]);
				GUI.enabled = false;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Add Open Scenes", new GUILayoutOption[0]))
			{
				this.AddOpenScenes();
			}
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			GUILayout.Space(10f);
			GUILayout.BeginHorizontal(new GUILayoutOption[]
			{
				GUILayout.Height(301f)
			});
			this.ActiveBuildTargetsGUI();
			GUILayout.Space(10f);
			GUILayout.BeginVertical(new GUILayoutOption[0]);
			this.ShowBuildTargetSettings();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.Space(10f);
			GUILayout.EndVertical();
			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
		}

		private static BuildTarget RestoreLastKnownPlatformsBuildTarget(BuildPlayerWindow.BuildPlatform bp)
		{
			BuildTargetGroup targetGroup = bp.targetGroup;
			if (targetGroup == BuildTargetGroup.Standalone)
			{
				return EditorUserBuildSettings.selectedStandaloneTarget;
			}
			if (targetGroup != BuildTargetGroup.WebPlayer)
			{
				return bp.DefaultTarget;
			}
			return (!EditorUserBuildSettings.webPlayerStreamed) ? BuildTarget.WebPlayer : BuildTarget.WebPlayerStreamed;
		}

		private static void InitBuildPlatforms()
		{
			if (BuildPlayerWindow.s_BuildPlatforms == null)
			{
				BuildPlayerWindow.s_BuildPlatforms = new BuildPlayerWindow.BuildPlatforms();
				BuildPlayerWindow.RepairSelectedBuildTargetGroup();
			}
		}

		internal static List<BuildPlayerWindow.BuildPlatform> GetValidPlatforms()
		{
			BuildPlayerWindow.InitBuildPlatforms();
			List<BuildPlayerWindow.BuildPlatform> list = new List<BuildPlayerWindow.BuildPlatform>();
			BuildPlayerWindow.BuildPlatform[] buildPlatforms = BuildPlayerWindow.s_BuildPlatforms.buildPlatforms;
			for (int i = 0; i < buildPlatforms.Length; i++)
			{
				BuildPlayerWindow.BuildPlatform buildPlatform = buildPlatforms[i];
				if (buildPlatform.targetGroup == BuildTargetGroup.Standalone || BuildPipeline.IsBuildTargetSupported(buildPlatform.DefaultTarget))
				{
					list.Add(buildPlatform);
				}
			}
			return list;
		}

		internal static bool IsBuildTargetGroupSupported(BuildTarget target)
		{
			return target == BuildTarget.StandaloneWindows || BuildPipeline.IsBuildTargetSupported(target);
		}

		private static void RepairSelectedBuildTargetGroup()
		{
			BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			if (selectedBuildTargetGroup == BuildTargetGroup.Unknown || BuildPlayerWindow.s_BuildPlatforms == null || BuildPlayerWindow.s_BuildPlatforms.BuildPlatformIndexFromTargetGroup(selectedBuildTargetGroup) < 0)
			{
				EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.WebPlayer;
			}
		}

		private static bool IsAnyStandaloneModuleLoaded()
		{
			return ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)) || ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)) || ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows));
		}

		private static BuildTarget GetBestStandaloneTarget(BuildTarget selectedTarget)
		{
			if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(selectedTarget)))
			{
				return selectedTarget;
			}
			if (Application.platform == RuntimePlatform.WindowsEditor && ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows)))
			{
				return BuildTarget.StandaloneWindows;
			}
			if (Application.platform == RuntimePlatform.OSXEditor && ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)))
			{
				return BuildTarget.StandaloneOSXIntel;
			}
			if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)))
			{
				return BuildTarget.StandaloneOSXIntel;
			}
			if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)))
			{
				return BuildTarget.StandaloneLinux;
			}
			return BuildTarget.StandaloneWindows;
		}

		private void ShowBuildTargetSettings()
		{
			EditorGUIUtility.labelWidth = Mathf.Min(180f, (base.position.width - 265f) * 0.47f);
			BuildTarget buildTarget = BuildPlayerWindow.CalculateSelectedBuildTarget();
			BuildPlayerWindow.BuildPlatform buildPlatform = BuildPlayerWindow.s_BuildPlatforms.BuildPlatformFromTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			bool flag = BuildPipeline.LicenseCheck(buildTarget);
			GUILayout.Space(18f);
			Rect rect = GUILayoutUtility.GetRect(50f, 36f);
			rect.x += 1f;
			GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, 32f, 32f), buildPlatform.title.image, GUIStyle.none);
			GUI.Toggle(rect, false, buildPlatform.title.text, BuildPlayerWindow.styles.platformSelector);
			GUILayout.Space(10f);
			if (buildPlatform.targetGroup == BuildTargetGroup.WebGL && !BuildPipeline.IsBuildTargetSupported(buildTarget) && IntPtr.Size == 4)
			{
				GUILayout.Label("Building for WebGL requires a 64-bit Unity editor.", new GUILayoutOption[0]);
				BuildPlayerWindow.GUIBuildButtons(false, false, false, buildPlatform);
				return;
			}
			if (flag && !string.IsNullOrEmpty(ModuleManager.GetTargetStringFromBuildTarget(buildTarget)) && ModuleManager.GetBuildPostProcessor(buildTarget) == null && (EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Standalone || !BuildPlayerWindow.IsAnyStandaloneModuleLoaded()))
			{
				GUILayout.Label("No " + BuildPlayerWindow.s_BuildPlatforms.GetBuildTargetDisplayName(buildTarget) + " module loaded.", new GUILayoutOption[0]);
				if (GUILayout.Button("Module Manager", EditorStyles.miniButton, new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(false)
				}))
				{
					InternalEditorUtility.ShowPackageManagerWindow();
				}
				BuildPlayerWindow.GUIBuildButtons(false, false, false, buildPlatform);
				return;
			}
			if (Application.HasProLicense() && !InternalEditorUtility.HasAdvancedLicenseOnBuildTarget(buildTarget))
			{
				string text = string.Format("{0} is not included in your Unity Pro license. Your {0} build will include a Unity Personal Edition splash screen.\n\nYou must be eligible to use Unity Personal Edition to use this build option. Please refer to our EULA for further information.", BuildPlayerWindow.s_BuildPlatforms.GetBuildTargetDisplayName(buildTarget));
				GUILayout.BeginVertical(EditorStyles.helpBox, new GUILayoutOption[0]);
				GUILayout.Label(text, EditorStyles.wordWrappedMiniLabel, new GUILayoutOption[0]);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				if (GUILayout.Button("EULA", EditorStyles.miniButton, new GUILayoutOption[0]))
				{
					Application.OpenURL("http://unity3d.com/legal/eula");
				}
				if (GUILayout.Button(string.Format("Add {0} to your Unity Pro license", BuildPlayerWindow.s_BuildPlatforms.GetBuildTargetDisplayName(buildTarget)), EditorStyles.miniButton, new GUILayoutOption[0]))
				{
					Application.OpenURL("http://unity3d.com/get-unity");
				}
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
			}
			GUIContent downloadErrorForTarget = BuildPlayerWindow.styles.GetDownloadErrorForTarget(buildTarget);
			if (downloadErrorForTarget != null)
			{
				GUILayout.Label(downloadErrorForTarget, EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
				BuildPlayerWindow.GUIBuildButtons(false, false, false, buildPlatform);
				return;
			}
			if (!flag)
			{
				int num = BuildPlayerWindow.s_BuildPlatforms.BuildPlatformIndexFromTargetGroup(buildPlatform.targetGroup);
				GUILayout.Label(BuildPlayerWindow.styles.notLicensedMessages[num, 0], EditorStyles.wordWrappedLabel, new GUILayoutOption[0]);
				GUILayout.Space(5f);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				if (BuildPlayerWindow.styles.notLicensedMessages[num, 1].text.Length != 0 && GUILayout.Button(BuildPlayerWindow.styles.notLicensedMessages[num, 1], new GUILayoutOption[0]))
				{
					Application.OpenURL(BuildPlayerWindow.styles.notLicensedMessages[num, 2].text);
				}
				GUILayout.EndHorizontal();
				BuildPlayerWindow.GUIBuildButtons(false, false, false, buildPlatform);
				return;
			}
			string targetStringFromBuildTargetGroup = ModuleManager.GetTargetStringFromBuildTargetGroup(buildPlatform.targetGroup);
			IBuildWindowExtension buildWindowExtension = ModuleManager.GetBuildWindowExtension(targetStringFromBuildTargetGroup);
			if (buildWindowExtension != null)
			{
				buildWindowExtension.ShowPlatformBuildOptions();
			}
			GUI.changed = false;
			BuildTargetGroup targetGroup = buildPlatform.targetGroup;
			switch (targetGroup)
			{
			case BuildTargetGroup.Standalone:
			{
				BuildTarget bestStandaloneTarget = BuildPlayerWindow.GetBestStandaloneTarget(EditorUserBuildSettings.selectedStandaloneTarget);
				BuildTarget buildTarget2 = EditorUserBuildSettings.selectedStandaloneTarget;
				int num2 = Math.Max(0, Array.IndexOf<BuildTarget>(BuildPlayerWindow.s_BuildPlatforms.standaloneSubtargets, BuildPlayerWindow.BuildPlatforms.DefaultTargetForPlatform(bestStandaloneTarget)));
				int num3 = EditorGUILayout.Popup(BuildPlayerWindow.styles.standaloneTarget, num2, BuildPlayerWindow.s_BuildPlatforms.standaloneSubtargetStrings, new GUILayoutOption[0]);
				if (num3 == num2)
				{
					Dictionary<GUIContent, BuildTarget> architecturesForPlatform = BuildPlayerWindow.BuildPlatforms.GetArchitecturesForPlatform(bestStandaloneTarget);
					if (architecturesForPlatform != null)
					{
						GUIContent[] array = new List<GUIContent>(architecturesForPlatform.Keys).ToArray();
						int num4 = 0;
						if (num3 == num2)
						{
							foreach (KeyValuePair<GUIContent, BuildTarget> current in architecturesForPlatform)
							{
								if (current.Value == bestStandaloneTarget)
								{
									num4 = Math.Max(0, Array.IndexOf<GUIContent>(array, current.Key));
									break;
								}
							}
						}
						num4 = EditorGUILayout.Popup(BuildPlayerWindow.styles.architecture, num4, array, new GUILayoutOption[0]);
						buildTarget2 = architecturesForPlatform[array[num4]];
					}
				}
				else
				{
					buildTarget2 = BuildPlayerWindow.s_BuildPlatforms.standaloneSubtargets[num3];
				}
				if (buildTarget2 != EditorUserBuildSettings.selectedStandaloneTarget)
				{
					EditorUserBuildSettings.selectedStandaloneTarget = buildTarget2;
					GUIUtility.ExitGUI();
				}
				goto IL_577;
			}
			case BuildTargetGroup.WebPlayer:
			{
				GUI.enabled = BuildPipeline.LicenseCheck(BuildTarget.WebPlayerStreamed);
				bool webPlayerStreamed = EditorGUILayout.Toggle(BuildPlayerWindow.styles.webPlayerStreamed, EditorUserBuildSettings.webPlayerStreamed, new GUILayoutOption[0]);
				if (GUI.changed)
				{
					EditorUserBuildSettings.webPlayerStreamed = webPlayerStreamed;
				}
				EditorUserBuildSettings.webPlayerOfflineDeployment = EditorGUILayout.Toggle(BuildPlayerWindow.styles.webPlayerOfflineDeployment, EditorUserBuildSettings.webPlayerOfflineDeployment, new GUILayoutOption[0]);
				goto IL_577;
			}
			case (BuildTargetGroup)3:
				IL_39B:
				if (targetGroup != BuildTargetGroup.tvOS)
				{
					goto IL_577;
				}
				goto IL_549;
			case BuildTargetGroup.iPhone:
				goto IL_549;
			}
			goto IL_39B;
			IL_549:
			if (Application.platform == RuntimePlatform.OSXEditor)
			{
				EditorUserBuildSettings.symlinkLibraries = EditorGUILayout.Toggle(BuildPlayerWindow.styles.symlinkiOSLibraries, EditorUserBuildSettings.symlinkLibraries, new GUILayoutOption[0]);
			}
			IL_577:
			GUI.enabled = true;
			bool flag2 = buildWindowExtension == null || buildWindowExtension.EnabledBuildButton();
			bool enableBuildAndRunButton = false;
			bool flag3 = buildWindowExtension == null || buildWindowExtension.ShouldDrawScriptDebuggingCheckbox();
			bool flag4 = buildWindowExtension != null && buildWindowExtension.ShouldDrawExplicitNullCheckbox();
			bool flag5 = buildWindowExtension == null || buildWindowExtension.ShouldDrawDevelopmentPlayerCheckbox();
			bool flag6 = buildTarget == BuildTarget.StandaloneLinux || buildTarget == BuildTarget.StandaloneLinux64 || buildTarget == BuildTarget.StandaloneLinuxUniversal;
			IBuildPostprocessor buildPostProcessor = ModuleManager.GetBuildPostProcessor(buildTarget);
			bool flag7 = buildPostProcessor != null && buildPostProcessor.SupportsScriptsOnlyBuild();
			bool canInstallInBuildFolder = false;
			if (BuildPipeline.IsBuildTargetSupported(buildTarget))
			{
				bool flag8 = buildWindowExtension == null || buildWindowExtension.ShouldDrawProfilerCheckbox();
				GUI.enabled = flag5;
				if (flag5)
				{
					EditorUserBuildSettings.development = EditorGUILayout.Toggle(BuildPlayerWindow.styles.debugBuild, EditorUserBuildSettings.development, new GUILayoutOption[0]);
				}
				bool development = EditorUserBuildSettings.development;
				GUI.enabled = development;
				GUI.enabled = (GUI.enabled && buildPlatform.targetGroup != BuildTargetGroup.WebPlayer);
				if (flag8)
				{
					if (!GUI.enabled)
					{
						if (!development)
						{
							BuildPlayerWindow.styles.profileBuild.tooltip = "Profiling only enabled in Development Player";
						}
						else if (buildPlatform.targetGroup == BuildTargetGroup.WebPlayer)
						{
							BuildPlayerWindow.styles.profileBuild.tooltip = "Autoconnect not available from webplayer. Manually connect in Profiler";
						}
					}
					else
					{
						BuildPlayerWindow.styles.profileBuild.tooltip = string.Empty;
					}
					EditorUserBuildSettings.connectProfiler = EditorGUILayout.Toggle(BuildPlayerWindow.styles.profileBuild, EditorUserBuildSettings.connectProfiler, new GUILayoutOption[0]);
				}
				GUI.enabled = development;
				if (flag3)
				{
					EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle(BuildPlayerWindow.styles.allowDebugging, EditorUserBuildSettings.allowDebugging, new GUILayoutOption[0]);
				}
				bool flag9 = false;
				int num5 = 0;
				if (PlayerSettings.GetPropertyOptionalInt("ScriptingBackend", ref num5, buildPlatform.targetGroup))
				{
					flag9 = (num5 == 1);
				}
				bool flag10 = buildWindowExtension != null && development && flag9 && buildWindowExtension.ShouldDrawForceOptimizeScriptsCheckbox();
				if (flag10)
				{
					EditorUserBuildSettings.forceOptimizeScriptCompilation = EditorGUILayout.Toggle(BuildPlayerWindow.styles.forceOptimizeScriptCompilation, EditorUserBuildSettings.forceOptimizeScriptCompilation, new GUILayoutOption[0]);
				}
				if (flag4)
				{
					GUI.enabled = !development;
					if (!GUI.enabled)
					{
						EditorUserBuildSettings.explicitNullChecks = true;
					}
					EditorUserBuildSettings.explicitNullChecks = EditorGUILayout.Toggle(BuildPlayerWindow.styles.explicitNullChecks, EditorUserBuildSettings.explicitNullChecks, new GUILayoutOption[0]);
					GUI.enabled = development;
				}
				if (flag7)
				{
					EditorUserBuildSettings.buildScriptsOnly = EditorGUILayout.Toggle(BuildPlayerWindow.styles.buildScriptsOnly, EditorUserBuildSettings.buildScriptsOnly, new GUILayoutOption[0]);
				}
				GUI.enabled = !development;
				if (flag6)
				{
					EditorUserBuildSettings.enableHeadlessMode = EditorGUILayout.Toggle(BuildPlayerWindow.styles.enableHeadlessMode, EditorUserBuildSettings.enableHeadlessMode && !development, new GUILayoutOption[0]);
				}
				GUI.enabled = true;
				GUILayout.FlexibleSpace();
				canInstallInBuildFolder = (Unsupported.IsDeveloperBuild() && PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTarget));
				if (flag2)
				{
					enableBuildAndRunButton = ((buildWindowExtension == null) ? (!EditorUserBuildSettings.installInBuildFolder) : (buildWindowExtension.EnabledBuildAndRunButton() && !EditorUserBuildSettings.installInBuildFolder));
				}
				if (buildPlatform.targetGroup == BuildTargetGroup.WebPlayer)
				{
					string message = string.Format("Please note that the Unity Web Player is deprecated. Building for Web Player will no longer be supported in future versions of Unity.", new object[0]);
					GUILayout.BeginVertical(new GUILayoutOption[0]);
					EditorGUILayout.HelpBox(message, MessageType.Warning);
					GUILayout.EndVertical();
				}
			}
			else
			{
				GUILayout.BeginHorizontal(new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(true)
				});
				GUILayout.BeginVertical(new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(true)
				});
				int index = BuildPlayerWindow.s_BuildPlatforms.BuildPlatformIndexFromTargetGroup(buildPlatform.targetGroup);
				GUILayout.Label(BuildPlayerWindow.styles.GetTargetNotInstalled(index, 0), new GUILayoutOption[0]);
				if (BuildPlayerWindow.styles.GetTargetNotInstalled(index, 1) != null && GUILayout.Button(BuildPlayerWindow.styles.GetTargetNotInstalled(index, 1), new GUILayoutOption[0]))
				{
					Application.OpenURL(BuildPlayerWindow.styles.GetTargetNotInstalled(index, 2).text);
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
			}
			BuildPlayerWindow.GUIBuildButtons(buildWindowExtension, flag2, enableBuildAndRunButton, canInstallInBuildFolder, buildPlatform);
		}

		private static void GUIBuildButtons(bool enableBuildButton, bool enableBuildAndRunButton, bool canInstallInBuildFolder, BuildPlayerWindow.BuildPlatform platform)
		{
			BuildPlayerWindow.GUIBuildButtons(null, enableBuildButton, enableBuildAndRunButton, canInstallInBuildFolder, platform);
		}

		private static void GUIBuildButtons(IBuildWindowExtension buildWindowExtension, bool enableBuildButton, bool enableBuildAndRunButton, bool canInstallInBuildFolder, BuildPlayerWindow.BuildPlatform platform)
		{
			GUILayout.FlexibleSpace();
			if (canInstallInBuildFolder)
			{
				EditorUserBuildSettings.installInBuildFolder = GUILayout.Toggle(EditorUserBuildSettings.installInBuildFolder, "Install in Builds folder\n(for debugging with source code)", new GUILayoutOption[]
				{
					GUILayout.ExpandWidth(false)
				});
			}
			else
			{
				EditorUserBuildSettings.installInBuildFolder = false;
			}
			if (buildWindowExtension != null && Unsupported.IsDeveloperBuild())
			{
				buildWindowExtension.ShowInternalPlatformBuildOptions();
			}
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			if (EditorGUILayout.LinkLabel(BuildPlayerWindow.styles.learnAboutUnityCloudBuild, new GUILayoutOption[0]))
			{
				Application.OpenURL(string.Format("{0}/from/editor/buildsettings?upid={1}&pid={2}&currentplatform={3}&selectedplatform={4}&unityversion={5}", new object[]
				{
					WebURLs.cloudBuildPage,
					PlayerSettings.cloudProjectId,
					PlayerSettings.productGUID,
					EditorUserBuildSettings.activeBuildTarget,
					BuildPlayerWindow.CalculateSelectedBuildTarget(),
					Application.unityVersion
				}));
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(6f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			GUIContent content = BuildPlayerWindow.styles.build;
			if (platform.targetGroup == BuildTargetGroup.Android && EditorUserBuildSettings.exportAsGoogleAndroidProject)
			{
				content = BuildPlayerWindow.styles.export;
			}
			if (platform.targetGroup == BuildTargetGroup.iPhone && Application.platform != RuntimePlatform.OSXEditor)
			{
				enableBuildAndRunButton = false;
			}
			GUI.enabled = enableBuildButton;
			if (GUILayout.Button(content, new GUILayoutOption[]
			{
				GUILayout.Width(110f)
			}))
			{
				BuildPlayerWindow.BuildPlayerWithDefaultSettings(true, BuildOptions.ShowBuiltPlayer);
				GUIUtility.ExitGUI();
			}
			GUI.enabled = enableBuildAndRunButton;
			if (GUILayout.Button(BuildPlayerWindow.styles.buildAndRun, new GUILayoutOption[]
			{
				GUILayout.Width(110f)
			}))
			{
				BuildPlayerWindow.BuildPlayerWithDefaultSettings(true, BuildOptions.AutoRunPlayer);
				GUIUtility.ExitGUI();
			}
			GUILayout.EndHorizontal();
		}

		private static bool PickBuildLocation(BuildTarget target, BuildOptions options, out bool updateExistingBuild)
		{
			updateExistingBuild = false;
			string buildLocation = EditorUserBuildSettings.GetBuildLocation(target);
			if (target == BuildTarget.Android && EditorUserBuildSettings.exportAsGoogleAndroidProject)
			{
				string title = "Export Google Android Project";
				string location = EditorUtility.SaveFolderPanel(title, buildLocation, string.Empty);
				EditorUserBuildSettings.SetBuildLocation(target, location);
				return true;
			}
			string extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(target, options);
			string directory = FileUtil.DeleteLastPathNameComponent(buildLocation);
			string lastPathNameComponent = FileUtil.GetLastPathNameComponent(buildLocation);
			string title2 = "Build " + BuildPlayerWindow.s_BuildPlatforms.GetBuildTargetDisplayName(target);
			string text = EditorUtility.SaveBuildPanel(target, title2, directory, lastPathNameComponent, extensionForBuildTarget, out updateExistingBuild);
			if (text == string.Empty)
			{
				return false;
			}
			if (extensionForBuildTarget != string.Empty && FileUtil.GetPathExtension(text).ToLower() != extensionForBuildTarget)
			{
				text = text + '.' + extensionForBuildTarget;
			}
			string lastPathNameComponent2 = FileUtil.GetLastPathNameComponent(text);
			if (lastPathNameComponent2 == string.Empty)
			{
				return false;
			}
			string path = (!(extensionForBuildTarget != string.Empty)) ? text : FileUtil.DeleteLastPathNameComponent(text);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			if (target == BuildTarget.iOS && Application.platform != RuntimePlatform.OSXEditor && !BuildPlayerWindow.FolderIsEmpty(text) && !BuildPlayerWindow.UserWantsToDeleteFiles(text))
			{
				return false;
			}
			EditorUserBuildSettings.SetBuildLocation(target, text);
			return true;
		}

		private static bool FolderIsEmpty(string path)
		{
			return !Directory.Exists(path) || (Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0);
		}

		private static bool UserWantsToDeleteFiles(string path)
		{
			string message = "WARNING: all files and folders located in target folder: '" + path + "' will be deleted by build process.";
			return EditorUtility.DisplayDialog("Deleting existing files", message, "OK", "Cancel");
		}
	}
}
