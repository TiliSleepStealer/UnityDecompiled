using System;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.VersionControl;
using UnityEditorInternal.VersionControl;
using UnityEngine;

namespace UnityEditor
{
	internal class AssetsTreeViewGUI : TreeViewGUI
	{
		private const float k_IconOverlayPadding = 7f;

		private static bool s_VCEnabled;

		public AssetsTreeViewGUI(TreeView treeView) : base(treeView)
		{
			base.iconOverlayGUI = (Action<TreeViewItem, Rect>)Delegate.Combine(base.iconOverlayGUI, new Action<TreeViewItem, Rect>(this.OnIconOverlayGUI));
			this.k_TopRowMargin = 4f;
		}

		public override void BeginRowGUI()
		{
			AssetsTreeViewGUI.s_VCEnabled = Provider.isActive;
			float num = (!AssetsTreeViewGUI.s_VCEnabled) ? 0f : 7f;
			base.iconRightPadding = num;
			base.iconLeftPadding = num;
			base.BeginRowGUI();
		}

		protected CreateAssetUtility GetCreateAssetUtility()
		{
			return this.m_TreeView.state.createAssetUtility;
		}

		protected virtual bool IsCreatingNewAsset(int instanceID)
		{
			return this.GetCreateAssetUtility().IsCreatingNewAsset() && this.IsRenaming(instanceID);
		}

		protected override void ClearRenameAndNewNodeState()
		{
			this.GetCreateAssetUtility().Clear();
			base.ClearRenameAndNewNodeState();
		}

		protected override void RenameEnded()
		{
			string name = (!string.IsNullOrEmpty(base.GetRenameOverlay().name)) ? base.GetRenameOverlay().name : base.GetRenameOverlay().originalName;
			int userData = base.GetRenameOverlay().userData;
			bool flag = this.GetCreateAssetUtility().IsCreatingNewAsset();
			bool userAcceptedRename = base.GetRenameOverlay().userAcceptedRename;
			if (userAcceptedRename)
			{
				if (flag)
				{
					this.GetCreateAssetUtility().EndNewAssetCreation(name);
				}
				else
				{
					ObjectNames.SetNameSmartWithInstanceID(userData, name);
				}
			}
		}

		protected override void SyncFakeItem()
		{
			if (!this.m_TreeView.data.HasFakeItem() && this.GetCreateAssetUtility().IsCreatingNewAsset())
			{
				int mainAssetInstanceID = AssetDatabase.GetMainAssetInstanceID(this.GetCreateAssetUtility().folder);
				this.m_TreeView.data.InsertFakeItem(this.GetCreateAssetUtility().instanceID, mainAssetInstanceID, this.GetCreateAssetUtility().originalName, this.GetCreateAssetUtility().icon);
			}
			if (this.m_TreeView.data.HasFakeItem() && !this.GetCreateAssetUtility().IsCreatingNewAsset())
			{
				this.m_TreeView.data.RemoveFakeItem();
			}
		}

		public virtual void BeginCreateNewAsset(int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile)
		{
			this.ClearRenameAndNewNodeState();
			if (this.GetCreateAssetUtility().BeginNewAssetCreation(instanceID, endAction, pathName, icon, resourceFile))
			{
				this.SyncFakeItem();
				if (!base.GetRenameOverlay().BeginRename(this.GetCreateAssetUtility().originalName, instanceID, 0f))
				{
					Debug.LogError("Rename not started (when creating new asset)");
				}
			}
		}

		protected override Texture GetIconForNode(TreeViewItem item)
		{
			if (item == null)
			{
				return null;
			}
			Texture texture = null;
			if (this.IsCreatingNewAsset(item.id))
			{
				texture = this.GetCreateAssetUtility().icon;
			}
			if (texture == null)
			{
				texture = item.icon;
			}
			if (texture == null && item.id != 0)
			{
				string assetPath = AssetDatabase.GetAssetPath(item.id);
				texture = AssetDatabase.GetCachedIcon(assetPath);
			}
			return texture;
		}

		private void OnIconOverlayGUI(TreeViewItem item, Rect overlayRect)
		{
			if (AssetsTreeViewGUI.s_VCEnabled && AssetDatabase.IsMainAsset(item.id))
			{
				string assetPath = AssetDatabase.GetAssetPath(item.id);
				string guid = AssetDatabase.AssetPathToGUID(assetPath);
				ProjectHooks.OnProjectWindowItem(guid, overlayRect);
			}
		}
	}
}
