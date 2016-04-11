using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	internal class PackageExport : EditorWindow
	{
		internal static class Styles
		{
			public static GUIStyle title;

			public static GUIStyle bottomBarBg;

			public static GUIStyle topBarBg;

			public static GUIStyle loadingTextStyle;

			public static GUIContent allText;

			public static GUIContent noneText;

			public static GUIContent includeDependenciesText;

			public static GUIContent header;

			static Styles()
			{
				PackageExport.Styles.title = new GUIStyle(EditorStyles.largeLabel);
				PackageExport.Styles.bottomBarBg = "ProjectBrowserBottomBarBg";
				PackageExport.Styles.topBarBg = new GUIStyle("ProjectBrowserHeaderBgTop");
				PackageExport.Styles.loadingTextStyle = new GUIStyle(EditorStyles.label);
				PackageExport.Styles.allText = EditorGUIUtility.TextContent("All");
				PackageExport.Styles.noneText = EditorGUIUtility.TextContent("None");
				PackageExport.Styles.includeDependenciesText = EditorGUIUtility.TextContent("Include dependencies");
				PackageExport.Styles.header = new GUIContent("Items to Export");
				PackageExport.Styles.topBarBg.fixedHeight = 0f;
				RectOffset arg_A9_0 = PackageExport.Styles.topBarBg.border;
				int num = 2;
				PackageExport.Styles.topBarBg.border.bottom = num;
				arg_A9_0.top = num;
				PackageExport.Styles.title.fontStyle = FontStyle.Bold;
				PackageExport.Styles.title.alignment = TextAnchor.MiddleLeft;
				PackageExport.Styles.loadingTextStyle.alignment = TextAnchor.MiddleCenter;
			}
		}

		[SerializeField]
		private ExportPackageItem[] m_ExportPackageItems;

		[SerializeField]
		private IncrementalInitialize m_IncrementalInitialize = new IncrementalInitialize();

		[SerializeField]
		private bool m_IncludeDependencies = true;

		[SerializeField]
		private TreeViewState m_TreeViewState;

		[NonSerialized]
		private PackageExportTreeView m_Tree;

		public ExportPackageItem[] items
		{
			get
			{
				return this.m_ExportPackageItems;
			}
		}

		public PackageExport()
		{
			base.position = new Rect(100f, 100f, 400f, 300f);
			base.minSize = new Vector2(350f, 350f);
		}

		internal static void ShowExportPackage()
		{
			EditorWindow.GetWindow<PackageExport>(true, "Exporting package");
		}

		internal static IEnumerable<ExportPackageItem> GetAssetItemsForExport(ICollection<string> guids, bool includeDependencies)
		{
			if (guids.Count == 0)
			{
				string[] collection = new string[0];
				guids = new HashSet<string>(AssetServer.CollectAllChildren(AssetServer.GetRootGUID(), collection));
			}
			ExportPackageItem[] array = PackageUtility.BuildExportPackageItemsList(guids.ToArray<string>(), includeDependencies);
			array = (from val in array
			where val.assetPath != "Assets"
			select val).ToArray<ExportPackageItem>();
			if (includeDependencies)
			{
				if (array.Any((ExportPackageItem asset) => InternalEditorUtility.IsScriptOrAssembly(asset.assetPath)))
				{
					array = PackageUtility.BuildExportPackageItemsList(guids.Union(InternalEditorUtility.GetAllScriptGUIDs()).ToArray<string>(), includeDependencies);
				}
			}
			return array;
		}

		private void RefreshAssetList()
		{
			this.m_IncrementalInitialize.Restart();
			this.m_ExportPackageItems = null;
		}

		private bool HasValidAssetList()
		{
			return this.m_ExportPackageItems != null;
		}

		private bool CheckAssetExportList()
		{
			if (this.m_ExportPackageItems == null || this.m_ExportPackageItems.Length == 0)
			{
				GUILayout.Space(20f);
				GUILayout.BeginVertical(EditorStyles.helpBox, new GUILayoutOption[0]);
				GUILayout.Label("Nothing to import!", EditorStyles.boldLabel, new GUILayoutOption[0]);
				GUILayout.Label("All assets from this package are already in your project.", "WordWrappedLabel", new GUILayoutOption[0]);
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("OK", new GUILayoutOption[0]))
				{
					base.Close();
					GUIUtility.ExitGUI();
				}
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				return true;
			}
			return false;
		}

		public void OnGUI()
		{
			this.m_IncrementalInitialize.OnEvent();
			bool showLoadingScreen = false;
			IncrementalInitialize.State state = this.m_IncrementalInitialize.state;
			if (state != IncrementalInitialize.State.PreInitialize)
			{
				if (state == IncrementalInitialize.State.Initialize)
				{
					this.BuildAssetList();
				}
			}
			else
			{
				showLoadingScreen = true;
			}
			if (this.CheckAssetExportList())
			{
				return;
			}
			EditorGUI.BeginDisabledGroup(!this.HasValidAssetList());
			this.TopArea();
			EditorGUI.EndDisabledGroup();
			this.TreeViewArea(showLoadingScreen);
			EditorGUI.BeginDisabledGroup(!this.HasValidAssetList());
			this.BottomArea();
			EditorGUI.EndDisabledGroup();
		}

		private void TopArea()
		{
			float height = 53f;
			Rect rect = GUILayoutUtility.GetRect(base.position.width, height);
			GUI.Label(rect, GUIContent.none, PackageExport.Styles.topBarBg);
			Rect position = new Rect(rect.x + 5f, rect.yMin, rect.width, rect.height);
			GUI.Label(position, PackageExport.Styles.header, PackageExport.Styles.title);
		}

		private void BottomArea()
		{
			GUILayout.BeginVertical(PackageExport.Styles.bottomBarBg, new GUILayoutOption[0]);
			GUILayout.Space(8f);
			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.Space(10f);
			if (GUILayout.Button(PackageExport.Styles.allText, new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			}))
			{
				this.m_Tree.SetAllEnabled(1);
			}
			if (GUILayout.Button(PackageExport.Styles.noneText, new GUILayoutOption[]
			{
				GUILayout.Width(50f)
			}))
			{
				this.m_Tree.SetAllEnabled(0);
			}
			GUILayout.Space(10f);
			EditorGUI.BeginChangeCheck();
			this.m_IncludeDependencies = GUILayout.Toggle(this.m_IncludeDependencies, PackageExport.Styles.includeDependenciesText, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				this.RefreshAssetList();
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(EditorGUIUtility.TextContent("Export..."), new GUILayoutOption[0]))
			{
				this.Export();
				GUIUtility.ExitGUI();
			}
			GUILayout.Space(10f);
			GUILayout.EndHorizontal();
			GUILayout.Space(5f);
			GUILayout.EndVertical();
		}

		private void TreeViewArea(bool showLoadingScreen)
		{
			Rect rect = GUILayoutUtility.GetRect(1f, 9999f, 1f, 99999f);
			if (showLoadingScreen)
			{
				GUI.Label(rect, "Loading...", PackageExport.Styles.loadingTextStyle);
				return;
			}
			if (this.m_ExportPackageItems != null && this.m_ExportPackageItems.Length > 0)
			{
				if (this.m_TreeViewState == null)
				{
					this.m_TreeViewState = new TreeViewState();
				}
				if (this.m_Tree == null)
				{
					this.m_Tree = new PackageExportTreeView(this, this.m_TreeViewState, default(Rect));
				}
				this.m_Tree.OnGUI(rect);
			}
		}

		private void Export()
		{
			string text = EditorUtility.SaveFilePanel("Export package ...", string.Empty, string.Empty, "unitypackage");
			if (text != string.Empty)
			{
				List<string> list = new List<string>();
				ExportPackageItem[] exportPackageItems = this.m_ExportPackageItems;
				for (int i = 0; i < exportPackageItems.Length; i++)
				{
					ExportPackageItem exportPackageItem = exportPackageItems[i];
					if (exportPackageItem.enabledStatus > 0)
					{
						list.Add(exportPackageItem.guid);
					}
				}
				PackageUtility.ExportPackage(list.ToArray(), text);
				base.Close();
				GUIUtility.ExitGUI();
			}
		}

		private void BuildAssetList()
		{
			this.m_ExportPackageItems = PackageExport.GetAssetItemsForExport(Selection.assetGUIDsDeepSelection, this.m_IncludeDependencies).ToArray<ExportPackageItem>();
			this.m_Tree = null;
			this.m_TreeViewState = null;
		}
	}
}
