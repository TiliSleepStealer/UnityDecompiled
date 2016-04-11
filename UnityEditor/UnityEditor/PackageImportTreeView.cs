using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	internal class PackageImportTreeView
	{
		public enum EnabledState
		{
			NotSet = -1,
			None,
			All,
			Mixed
		}

		private class PackageImportTreeViewItem : TreeViewItem
		{
			public ImportPackageItem item
			{
				get;
				set;
			}

			public PackageImportTreeViewItem(ImportPackageItem itemIn, int id, int depth, TreeViewItem parent, string displayName) : base(id, depth, parent, displayName)
			{
				this.item = itemIn;
			}
		}

		private class PackageImportTreeViewGUI : TreeViewGUI
		{
			internal static class Constants
			{
				public static Texture2D folderIcon;

				public static GUIContent badgeNew;

				public static GUIContent badgeDelete;

				public static GUIContent badgeWarn;

				public static GUIContent badgeChange;

				public static GUIStyle paddinglessStyle;

				static Constants()
				{
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.folderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName);
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeNew = EditorGUIUtility.IconContent("AS Badge New", "|This is a new Asset");
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeDelete = EditorGUIUtility.IconContent("AS Badge Delete", "|These files will be deleted!");
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeWarn = EditorGUIUtility.IconContent("console.warnicon", "|Warning: File exists in project, but with different GUID. Will override existing asset which may be undesired.");
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeChange = EditorGUIUtility.IconContent("playLoopOff", "|This file is new or has changed.");
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.paddinglessStyle = new GUIStyle();
					PackageImportTreeView.PackageImportTreeViewGUI.Constants.paddinglessStyle.padding = new RectOffset(0, 0, 0, 0);
				}
			}

			public Action<PackageImportTreeView.PackageImportTreeViewItem> itemWasToggled;

			private PackageImportTreeView m_PackageImportView;

			public int showPreviewForID
			{
				get;
				set;
			}

			public PackageImportTreeViewGUI(TreeView treeView, PackageImportTreeView view) : base(treeView)
			{
				this.m_PackageImportView = view;
				this.k_BaseIndent = 4f;
				if (!PackageImportTreeView.s_UseFoldouts)
				{
					this.k_FoldoutWidth = 0f;
				}
			}

			public override void OnRowGUI(Rect rowRect, TreeViewItem tvItem, int row, bool selected, bool focused)
			{
				this.k_IndentWidth = 18f;
				this.k_FoldoutWidth = 18f;
				PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = tvItem as PackageImportTreeView.PackageImportTreeViewItem;
				ImportPackageItem item = packageImportTreeViewItem.item;
				bool flag = Event.current.type == EventType.Repaint;
				if (selected && flag)
				{
					TreeViewGUI.s_Styles.selectionStyle.Draw(rowRect, false, false, true, focused);
				}
				bool flag2 = item != null;
				bool flag3 = item == null || item.isFolder;
				bool flag4 = item != null && item.assetChanged;
				bool flag5 = item != null && item.pathConflict;
				bool flag6 = item == null || item.exists;
				bool flag7 = item != null && item.projectAsset;
				bool doReInstall = this.m_PackageImportView.doReInstall;
				if (this.m_TreeView.data.IsExpandable(tvItem))
				{
					this.DoFoldout(rowRect, tvItem, row);
				}
				EditorGUI.BeginDisabledGroup(!flag2 || flag7);
				Rect toggleRect = new Rect(this.k_BaseIndent + (float)tvItem.depth * base.indentWidth + this.k_FoldoutWidth, rowRect.y, 18f, rowRect.height);
				if (flag2 && !flag7 && (flag3 || flag4 || doReInstall))
				{
					this.DoToggle(packageImportTreeViewItem, toggleRect);
				}
				Rect contentRect = new Rect(toggleRect.xMax, rowRect.y, rowRect.width, rowRect.height);
				this.DoIconAndText(tvItem, contentRect, selected, focused);
				this.DoPreviewPopup(packageImportTreeViewItem, rowRect);
				if (flag && flag2 && flag5)
				{
					Rect position = new Rect(rowRect.xMax - 58f, rowRect.y, rowRect.height, rowRect.height);
					EditorGUIUtility.SetIconSize(new Vector2(rowRect.height, rowRect.height));
					GUI.Label(position, PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeWarn);
					EditorGUIUtility.SetIconSize(Vector2.zero);
				}
				if (flag && flag2 && !flag6 && !flag5)
				{
					Texture image = PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeNew.image;
					Rect position2 = new Rect(rowRect.xMax - (float)image.width - 6f, rowRect.y + (rowRect.height - (float)image.height) / 2f, (float)image.width, (float)image.height);
					GUI.Label(position2, PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeNew, PackageImportTreeView.PackageImportTreeViewGUI.Constants.paddinglessStyle);
				}
				if (flag && doReInstall && flag7)
				{
					Texture image2 = PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeDelete.image;
					Rect position3 = new Rect(rowRect.xMax - (float)image2.width - 6f, rowRect.y + (rowRect.height - (float)image2.height) / 2f, (float)image2.width, (float)image2.height);
					GUI.Label(position3, PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeDelete, PackageImportTreeView.PackageImportTreeViewGUI.Constants.paddinglessStyle);
				}
				if (flag && flag2 && (flag6 || flag5) && flag4)
				{
					Texture image3 = PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeChange.image;
					Rect position4 = new Rect(rowRect.xMax - (float)image3.width - 6f, rowRect.y, rowRect.height, rowRect.height);
					GUI.Label(position4, PackageImportTreeView.PackageImportTreeViewGUI.Constants.badgeChange, PackageImportTreeView.PackageImportTreeViewGUI.Constants.paddinglessStyle);
				}
				EditorGUI.EndDisabledGroup();
			}

			private static void Toggle(ImportPackageItem[] items, PackageImportTreeView.PackageImportTreeViewItem pitem, Rect toggleRect)
			{
				ImportPackageItem item = pitem.item;
				if (item != null)
				{
					bool flag = item.enabledStatus > 0;
					GUIStyle style = EditorStyles.toggle;
					bool flag2 = item.isFolder && item.enabledStatus == 2;
					if (flag2)
					{
						style = EditorStyles.toggleMixed;
					}
					bool flag3 = GUI.Toggle(toggleRect, flag, GUIContent.none, style);
					if (flag3 != flag)
					{
						item.enabledStatus = ((!flag3) ? 0 : 1);
					}
				}
			}

			private void DoToggle(PackageImportTreeView.PackageImportTreeViewItem pitem, Rect toggleRect)
			{
				EditorGUI.BeginChangeCheck();
				PackageImportTreeView.PackageImportTreeViewGUI.Toggle(this.m_PackageImportView.packageItems, pitem, toggleRect);
				if (EditorGUI.EndChangeCheck())
				{
					if (this.m_TreeView.GetSelection().Length <= 1 || !this.m_TreeView.GetSelection().Contains(pitem.id))
					{
						this.m_TreeView.SetSelection(new int[]
						{
							pitem.id
						}, false);
						this.m_TreeView.NotifyListenersThatSelectionChanged();
					}
					if (this.itemWasToggled != null)
					{
						this.itemWasToggled(pitem);
					}
					Event.current.Use();
				}
			}

			private void DoPreviewPopup(PackageImportTreeView.PackageImportTreeViewItem pitem, Rect rowRect)
			{
				ImportPackageItem item = pitem.item;
				if (item != null)
				{
					if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition) && !PopupWindowWithoutFocus.IsVisible())
					{
						this.showPreviewForID = pitem.id;
					}
					if (pitem.id == this.showPreviewForID && Event.current.type != EventType.Layout)
					{
						this.showPreviewForID = 0;
						if (!string.IsNullOrEmpty(item.previewPath))
						{
							Texture2D preview = PackageImport.GetPreview(item.previewPath);
							Rect rect = rowRect;
							rect.width = EditorGUIUtility.currentViewWidth;
							Rect arg_AF_0 = rect;
							PopupWindowContent arg_AF_1 = new PackageImportTreeView.PreviewPopup(preview);
							PopupLocationHelper.PopupLocation[] expr_A7 = new PopupLocationHelper.PopupLocation[3];
							expr_A7[0] = PopupLocationHelper.PopupLocation.Right;
							expr_A7[1] = PopupLocationHelper.PopupLocation.Left;
							PopupWindowWithoutFocus.Show(arg_AF_0, arg_AF_1, expr_A7);
						}
					}
				}
			}

			private void DoIconAndText(TreeViewItem item, Rect contentRect, bool selected, bool focused)
			{
				EditorGUIUtility.SetIconSize(new Vector2(this.k_IconWidth, this.k_IconWidth));
				GUIStyle lineStyle = TreeViewGUI.s_Styles.lineStyle;
				lineStyle.padding.left = 0;
				if (Event.current.type == EventType.Repaint)
				{
					lineStyle.Draw(contentRect, GUIContent.Temp(item.displayName, this.GetIconForNode(item)), false, false, selected, focused);
				}
				EditorGUIUtility.SetIconSize(Vector2.zero);
			}

			protected override Texture GetIconForNode(TreeViewItem tvItem)
			{
				PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = tvItem as PackageImportTreeView.PackageImportTreeViewItem;
				ImportPackageItem item = packageImportTreeViewItem.item;
				if (item == null || item.isFolder)
				{
					return PackageImportTreeView.PackageImportTreeViewGUI.Constants.folderIcon;
				}
				Texture cachedIcon = AssetDatabase.GetCachedIcon(item.destinationAssetPath);
				if (cachedIcon != null)
				{
					return cachedIcon;
				}
				return InternalEditorUtility.GetIconForFile(item.destinationAssetPath);
			}

			protected override void RenameEnded()
			{
			}
		}

		private class PackageImportTreeViewDataSource : TreeViewDataSource
		{
			private PackageImportTreeView m_PackageImportView;

			public PackageImportTreeViewDataSource(TreeView treeView, PackageImportTreeView view) : base(treeView)
			{
				this.m_PackageImportView = view;
				base.rootIsCollapsable = false;
				base.showRootNode = false;
			}

			public override bool IsRenamingItemAllowed(TreeViewItem item)
			{
				return false;
			}

			public override bool IsExpandable(TreeViewItem item)
			{
				return PackageImportTreeView.s_UseFoldouts && base.IsExpandable(item);
			}

			public override void FetchData()
			{
				int depth = -1;
				this.m_RootItem = new PackageImportTreeView.PackageImportTreeViewItem(null, "Assets".GetHashCode(), depth, null, "InvisibleAssetsFolder");
				bool flag = true;
				if (flag)
				{
					this.m_TreeView.state.expandedIDs.Add(this.m_RootItem.id);
				}
				ImportPackageItem[] packageItems = this.m_PackageImportView.packageItems;
				Dictionary<string, PackageImportTreeView.PackageImportTreeViewItem> dictionary = new Dictionary<string, PackageImportTreeView.PackageImportTreeViewItem>();
				for (int i = 0; i < packageItems.Length; i++)
				{
					ImportPackageItem importPackageItem = packageItems[i];
					if (!PackageImport.HasInvalidCharInFilePath(importPackageItem.destinationAssetPath))
					{
						string fileName = Path.GetFileName(importPackageItem.destinationAssetPath);
						string directoryName = Path.GetDirectoryName(importPackageItem.destinationAssetPath);
						TreeViewItem treeViewItem = this.EnsureFolderPath(directoryName, dictionary, flag);
						if (treeViewItem != null)
						{
							int hashCode = importPackageItem.destinationAssetPath.GetHashCode();
							PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = new PackageImportTreeView.PackageImportTreeViewItem(importPackageItem, hashCode, treeViewItem.depth + 1, treeViewItem, fileName);
							treeViewItem.AddChild(packageImportTreeViewItem);
							if (flag)
							{
								this.m_TreeView.state.expandedIDs.Add(hashCode);
							}
							if (importPackageItem.isFolder)
							{
								dictionary[importPackageItem.destinationAssetPath] = packageImportTreeViewItem;
							}
						}
					}
				}
				if (flag)
				{
					this.m_TreeView.state.expandedIDs.Sort();
				}
			}

			private TreeViewItem EnsureFolderPath(string folderPath, Dictionary<string, PackageImportTreeView.PackageImportTreeViewItem> treeViewFolders, bool initExpandedState)
			{
				if (folderPath == string.Empty)
				{
					return this.m_RootItem;
				}
				int hashCode = folderPath.GetHashCode();
				TreeViewItem treeViewItem = TreeViewUtility.FindItem(hashCode, this.m_RootItem);
				if (treeViewItem != null)
				{
					return treeViewItem;
				}
				string[] array = folderPath.Split(new char[]
				{
					'/'
				});
				string text = string.Empty;
				TreeViewItem treeViewItem2 = this.m_RootItem;
				int num = -1;
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i];
					if (text != string.Empty)
					{
						text += '/';
					}
					text += text2;
					if (i != 0 || !(text == "Assets"))
					{
						num++;
						hashCode = text.GetHashCode();
						PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem;
						if (treeViewFolders.TryGetValue(text, out packageImportTreeViewItem))
						{
							treeViewItem2 = packageImportTreeViewItem;
						}
						else
						{
							PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem2 = new PackageImportTreeView.PackageImportTreeViewItem(null, hashCode, num, treeViewItem2, text2);
							treeViewItem2.AddChild(packageImportTreeViewItem2);
							treeViewItem2 = packageImportTreeViewItem2;
							if (initExpandedState)
							{
								this.m_TreeView.state.expandedIDs.Add(hashCode);
							}
							treeViewFolders[text] = packageImportTreeViewItem2;
						}
					}
				}
				return treeViewItem2;
			}
		}

		private class PreviewPopup : PopupWindowContent
		{
			private readonly Texture2D m_Preview;

			private readonly Vector2 kPreviewSize = new Vector2(128f, 128f);

			public PreviewPopup(Texture2D preview)
			{
				this.m_Preview = preview;
			}

			public override void OnGUI(Rect rect)
			{
				PackageImport.DrawTexture(rect, this.m_Preview, false);
			}

			public override Vector2 GetWindowSize()
			{
				return this.kPreviewSize;
			}
		}

		private TreeView m_TreeView;

		private List<PackageImportTreeView.PackageImportTreeViewItem> m_Selection = new List<PackageImportTreeView.PackageImportTreeViewItem>();

		private static readonly bool s_UseFoldouts = true;

		private PackageImport m_PackageImport;

		public bool canReInstall
		{
			get
			{
				return this.m_PackageImport.canReInstall;
			}
		}

		public bool doReInstall
		{
			get
			{
				return this.m_PackageImport.doReInstall;
			}
		}

		public ImportPackageItem[] packageItems
		{
			get
			{
				return this.m_PackageImport.packageItems;
			}
		}

		public PackageImportTreeView(PackageImport packageImport, TreeViewState treeViewState, Rect startRect)
		{
			this.m_PackageImport = packageImport;
			this.m_TreeView = new TreeView(this.m_PackageImport, treeViewState);
			PackageImportTreeView.PackageImportTreeViewDataSource data = new PackageImportTreeView.PackageImportTreeViewDataSource(this.m_TreeView, this);
			PackageImportTreeView.PackageImportTreeViewGUI packageImportTreeViewGUI = new PackageImportTreeView.PackageImportTreeViewGUI(this.m_TreeView, this);
			this.m_TreeView.Init(startRect, data, packageImportTreeViewGUI, null);
			this.m_TreeView.ReloadData();
			TreeView expr_64 = this.m_TreeView;
			expr_64.selectionChangedCallback = (Action<int[]>)Delegate.Combine(expr_64.selectionChangedCallback, new Action<int[]>(this.SelectionChanged));
			PackageImportTreeView.PackageImportTreeViewGUI expr_86 = packageImportTreeViewGUI;
			expr_86.itemWasToggled = (Action<PackageImportTreeView.PackageImportTreeViewItem>)Delegate.Combine(expr_86.itemWasToggled, new Action<PackageImportTreeView.PackageImportTreeViewItem>(this.ItemWasToggled));
			this.ComputeEnabledStateForFolders();
		}

		private void ComputeEnabledStateForFolders()
		{
			PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = this.m_TreeView.data.root as PackageImportTreeView.PackageImportTreeViewItem;
			this.RecursiveComputeEnabledStateForFolders(packageImportTreeViewItem, new HashSet<PackageImportTreeView.PackageImportTreeViewItem>
			{
				packageImportTreeViewItem
			});
		}

		private void RecursiveComputeEnabledStateForFolders(PackageImportTreeView.PackageImportTreeViewItem pitem, HashSet<PackageImportTreeView.PackageImportTreeViewItem> done)
		{
			ImportPackageItem item = pitem.item;
			if (item != null && !item.isFolder)
			{
				return;
			}
			if (pitem.hasChildren)
			{
				foreach (TreeViewItem current in pitem.children)
				{
					this.RecursiveComputeEnabledStateForFolders(current as PackageImportTreeView.PackageImportTreeViewItem, done);
				}
			}
			if (item != null && !done.Contains(pitem))
			{
				PackageImportTreeView.EnabledState folderChildrenEnabledState = this.GetFolderChildrenEnabledState(pitem);
				item.enabledStatus = (int)folderChildrenEnabledState;
				if (folderChildrenEnabledState == PackageImportTreeView.EnabledState.Mixed)
				{
					done.Add(pitem);
					for (PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = pitem.parent as PackageImportTreeView.PackageImportTreeViewItem; packageImportTreeViewItem != null; packageImportTreeViewItem = (packageImportTreeViewItem.parent as PackageImportTreeView.PackageImportTreeViewItem))
					{
						ImportPackageItem item2 = packageImportTreeViewItem.item;
						if (item2 != null && !done.Contains(packageImportTreeViewItem))
						{
							item2.enabledStatus = 2;
							done.Add(packageImportTreeViewItem);
						}
					}
				}
			}
		}

		private PackageImportTreeView.EnabledState GetFolderChildrenEnabledState(PackageImportTreeView.PackageImportTreeViewItem folder)
		{
			ImportPackageItem item = folder.item;
			if (item != null && !item.isFolder)
			{
				Debug.LogError("Should be a folder item!");
			}
			if (!folder.hasChildren)
			{
				return PackageImportTreeView.EnabledState.None;
			}
			PackageImportTreeView.EnabledState enabledState = PackageImportTreeView.EnabledState.NotSet;
			PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = folder.children[0] as PackageImportTreeView.PackageImportTreeViewItem;
			ImportPackageItem item2 = packageImportTreeViewItem.item;
			int num = (item2 != null) ? item2.enabledStatus : 1;
			for (int i = 1; i < folder.children.Count; i++)
			{
				item2 = (folder.children[i] as PackageImportTreeView.PackageImportTreeViewItem).item;
				if (num != item2.enabledStatus)
				{
					enabledState = PackageImportTreeView.EnabledState.Mixed;
					break;
				}
			}
			if (enabledState == PackageImportTreeView.EnabledState.NotSet)
			{
				enabledState = ((num != 1) ? PackageImportTreeView.EnabledState.None : PackageImportTreeView.EnabledState.All);
			}
			return enabledState;
		}

		private void SelectionChanged(int[] selectedIDs)
		{
			this.m_Selection = new List<PackageImportTreeView.PackageImportTreeViewItem>();
			List<TreeViewItem> rows = this.m_TreeView.data.GetRows();
			foreach (TreeViewItem current in rows)
			{
				if (selectedIDs.Contains(current.id))
				{
					PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = current as PackageImportTreeView.PackageImportTreeViewItem;
					if (packageImportTreeViewItem != null)
					{
						this.m_Selection.Add(packageImportTreeViewItem);
					}
				}
			}
			ImportPackageItem item = this.m_Selection[0].item;
			if (this.m_Selection.Count == 1 && item != null && !string.IsNullOrEmpty(item.previewPath))
			{
				PackageImportTreeView.PackageImportTreeViewGUI packageImportTreeViewGUI = this.m_TreeView.gui as PackageImportTreeView.PackageImportTreeViewGUI;
				packageImportTreeViewGUI.showPreviewForID = this.m_Selection[0].id;
			}
			else
			{
				PopupWindowWithoutFocus.Hide();
			}
		}

		public void OnGUI(Rect rect)
		{
			if (Event.current.type == EventType.ScrollWheel)
			{
				PopupWindowWithoutFocus.Hide();
			}
			int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
			this.m_TreeView.OnGUI(rect, controlID);
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space && this.m_Selection != null && this.m_Selection.Count > 0 && GUIUtility.keyboardControl == controlID)
			{
				ImportPackageItem item = this.m_Selection[0].item;
				if (item != null)
				{
					int enabledStatus = (item.enabledStatus != 0) ? 0 : 1;
					item.enabledStatus = enabledStatus;
					this.ItemWasToggled(this.m_Selection[0]);
				}
				Event.current.Use();
			}
		}

		public void SetAllEnabled(int enabled)
		{
			this.EnableChildrenRecursive(this.m_TreeView.data.root, enabled);
			this.ComputeEnabledStateForFolders();
		}

		private void ItemWasToggled(PackageImportTreeView.PackageImportTreeViewItem pitem)
		{
			ImportPackageItem item = pitem.item;
			if (item != null)
			{
				if (this.m_Selection.Count <= 1)
				{
					this.EnableChildrenRecursive(pitem, item.enabledStatus);
				}
				else
				{
					foreach (PackageImportTreeView.PackageImportTreeViewItem current in this.m_Selection)
					{
						ImportPackageItem item2 = current.item;
						item2.enabledStatus = item.enabledStatus;
					}
				}
				this.ComputeEnabledStateForFolders();
			}
		}

		private void EnableChildrenRecursive(TreeViewItem parentItem, int enabled)
		{
			if (!parentItem.hasChildren)
			{
				return;
			}
			foreach (TreeViewItem current in parentItem.children)
			{
				PackageImportTreeView.PackageImportTreeViewItem packageImportTreeViewItem = current as PackageImportTreeView.PackageImportTreeViewItem;
				ImportPackageItem item = packageImportTreeViewItem.item;
				if (item != null)
				{
					item.enabledStatus = enabled;
				}
				this.EnableChildrenRecursive(packageImportTreeViewItem, enabled);
			}
		}
	}
}
