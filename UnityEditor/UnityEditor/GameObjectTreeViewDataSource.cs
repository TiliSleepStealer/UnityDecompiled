using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
	internal class GameObjectTreeViewDataSource : LazyTreeViewDataSource
	{
		public class SortingState
		{
			private BaseHierarchySort m_HierarchySort;

			private bool m_ImplementsCompare;

			public BaseHierarchySort sortingObject
			{
				get
				{
					return this.m_HierarchySort;
				}
				set
				{
					this.m_HierarchySort = value;
					if (this.m_HierarchySort != null)
					{
						this.m_ImplementsCompare = (this.m_HierarchySort.GetType().GetMethod("Compare").DeclaringType != typeof(BaseHierarchySort));
					}
				}
			}

			public bool implementsCompare
			{
				get
				{
					return this.m_ImplementsCompare;
				}
			}
		}

		private const double k_LongFetchTime = 0.05;

		private const double k_FetchDelta = 0.1;

		private const int k_MaxDelayedFetch = 5;

		private const HierarchyType k_HierarchyType = HierarchyType.GameObjects;

		private const int k_DefaultStartCapacity = 1000;

		private readonly int kGameObjectClassID = BaseObjectTools.StringToClassID("GameObject");

		private int m_RootInstanceID;

		private string m_SearchString = string.Empty;

		private int m_SearchMode;

		private double m_LastFetchTime;

		private int m_DelayedFetches;

		private bool m_NeedsChildParentReferenceSetup;

		private bool m_RowsPartiallyInitialized;

		private int m_RowCount;

		private List<GameObjectTreeViewItem> m_StickySceneHeaderItems = new List<GameObjectTreeViewItem>();

		public GameObjectTreeViewDataSource.SortingState sortingState = new GameObjectTreeViewDataSource.SortingState();

		public List<GameObjectTreeViewItem> sceneHeaderItems
		{
			get
			{
				return this.m_StickySceneHeaderItems;
			}
		}

		public string searchString
		{
			get
			{
				return this.m_SearchString;
			}
			set
			{
				this.m_SearchString = value;
			}
		}

		public int searchMode
		{
			get
			{
				return this.m_SearchMode;
			}
			set
			{
				this.m_SearchMode = value;
			}
		}

		public bool isFetchAIssue
		{
			get
			{
				return this.m_DelayedFetches >= 5;
			}
		}

		public override int rowCount
		{
			get
			{
				return this.m_RowCount;
			}
		}

		public GameObjectTreeViewDataSource(TreeView treeView, int rootInstanceID, bool showRootNode, bool rootNodeIsCollapsable) : base(treeView)
		{
			this.m_RootInstanceID = rootInstanceID;
			this.showRootNode = showRootNode;
			base.rootIsCollapsable = rootNodeIsCollapsable;
		}

		public override void OnInitialize()
		{
			base.OnInitialize();
			GameObjectTreeViewGUI gameObjectTreeViewGUI = (GameObjectTreeViewGUI)this.m_TreeView.gui;
			GameObjectTreeViewGUI expr_18 = gameObjectTreeViewGUI;
			expr_18.scrollHeightChanged = (Action)Delegate.Combine(expr_18.scrollHeightChanged, new Action(this.EnsureFullyInitialized));
			GameObjectTreeViewGUI expr_3A = gameObjectTreeViewGUI;
			expr_3A.scrollPositionChanged = (Action)Delegate.Combine(expr_3A.scrollPositionChanged, new Action(this.EnsureFullyInitialized));
			GameObjectTreeViewGUI expr_5C = gameObjectTreeViewGUI;
			expr_5C.mouseDownInTreeViewRect = (Action)Delegate.Combine(expr_5C.mouseDownInTreeViewRect, new Action(this.EnsureFullyInitialized));
		}

		internal void SetupChildParentReferencesIfNeeded()
		{
			if (this.m_NeedsChildParentReferenceSetup)
			{
				this.m_NeedsChildParentReferenceSetup = false;
				TreeViewUtility.SetChildParentReferences(this.GetRows(), this.m_RootItem);
			}
		}

		public void EnsureFullyInitialized()
		{
			if (this.m_RowsPartiallyInitialized)
			{
				this.InitializeFull();
				this.m_RowsPartiallyInitialized = false;
			}
		}

		public override void RevealItem(int itemID)
		{
			if (this.IsValidHierarchyInstanceID(itemID))
			{
				base.RevealItem(itemID);
			}
		}

		public override bool IsRevealed(int id)
		{
			return this.GetRow(id) != -1;
		}

		private bool IsValidHierarchyInstanceID(int instanceID)
		{
			bool flag = SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(instanceID));
			bool flag2 = InternalEditorUtility.GetClassIDWithoutLoadingObject(instanceID) == this.kGameObjectClassID;
			return flag || flag2;
		}

		private HierarchyProperty FindHierarchyProperty(int instanceID)
		{
			if (!this.IsValidHierarchyInstanceID(instanceID))
			{
				return null;
			}
			HierarchyProperty hierarchyProperty = this.CreateHierarchyProperty();
			if (hierarchyProperty.Find(instanceID, this.m_TreeView.state.expandedIDs.ToArray()))
			{
				return hierarchyProperty;
			}
			return null;
		}

		public override int GetRow(int id)
		{
			HierarchyProperty hierarchyProperty = this.FindHierarchyProperty(id);
			if (hierarchyProperty != null)
			{
				return hierarchyProperty.row;
			}
			return -1;
		}

		public override TreeViewItem GetItem(int row)
		{
			return this.m_VisibleRows[row];
		}

		public override List<TreeViewItem> GetRows()
		{
			this.InitIfNeeded();
			this.EnsureFullyInitialized();
			return this.m_VisibleRows;
		}

		public override TreeViewItem FindItem(int id)
		{
			this.RevealItem(id);
			this.SetupChildParentReferencesIfNeeded();
			return base.FindItem(id);
		}

		private HierarchyProperty CreateHierarchyProperty()
		{
			HierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			hierarchyProperty.Reset();
			hierarchyProperty.alphaSorted = this.IsUsingAlphaSort();
			return hierarchyProperty;
		}

		private void CreateRootItem(HierarchyProperty property)
		{
			int depth = 0;
			if (property.isValid)
			{
				this.m_RootItem = new GameObjectTreeViewItem(this.m_RootInstanceID, depth, null, property.name);
			}
			else
			{
				this.m_RootItem = new GameObjectTreeViewItem(this.m_RootInstanceID, depth, null, "RootOfAll");
			}
			if (!base.showRootNode)
			{
				this.SetExpanded(this.m_RootItem, true);
			}
		}

		public override void FetchData()
		{
			Profiler.BeginSample("SceneHierarchyWindow.FetchData");
			this.m_RowsPartiallyInitialized = false;
			double timeSinceStartup = EditorApplication.timeSinceStartup;
			HierarchyProperty hierarchyProperty = this.CreateHierarchyProperty();
			if (this.m_RootInstanceID != 0 && !hierarchyProperty.Find(this.m_RootInstanceID, null))
			{
				Debug.LogError("Root gameobject with id " + this.m_RootInstanceID + " not found!!");
				this.m_RootInstanceID = 0;
				hierarchyProperty.Reset();
			}
			this.CreateRootItem(hierarchyProperty);
			this.m_NeedRefreshVisibleFolders = false;
			this.m_NeedsChildParentReferenceSetup = true;
			bool flag = this.m_RootInstanceID != 0;
			bool flag2 = !string.IsNullOrEmpty(this.m_SearchString);
			bool flag3 = this.sortingState.sortingObject != null && this.sortingState.implementsCompare;
			if (flag2 || flag3 || flag)
			{
				if (flag2)
				{
					hierarchyProperty.SetSearchFilter(this.m_SearchString, this.m_SearchMode);
				}
				this.InitializeProgressivly(hierarchyProperty, flag, flag2);
				if (flag3)
				{
					this.SortVisibleRows();
				}
			}
			else
			{
				this.InitializeMinimal();
			}
			double timeSinceStartup2 = EditorApplication.timeSinceStartup;
			double num = timeSinceStartup2 - timeSinceStartup;
			double num2 = timeSinceStartup2 - this.m_LastFetchTime;
			if (num2 > 0.1 && num > 0.05)
			{
				this.m_DelayedFetches++;
			}
			else
			{
				this.m_DelayedFetches = 0;
			}
			this.m_LastFetchTime = timeSinceStartup;
			this.m_TreeView.SetSelection(Selection.instanceIDs, false);
			this.CreateSceneHeaderItems();
			if (SceneHierarchyWindow.s_Debug)
			{
				Debug.Log(string.Concat(new object[]
				{
					"Fetch time: ",
					num * 1000.0,
					" ms, alphaSort = ",
					this.IsUsingAlphaSort()
				}));
			}
			Profiler.EndSample();
		}

		public override bool CanBeParent(TreeViewItem item)
		{
			this.SetupChildParentReferencesIfNeeded();
			return base.CanBeParent(item);
		}

		private bool IsUsingAlphaSort()
		{
			return this.sortingState.sortingObject.GetType() == typeof(AlphabeticalSort);
		}

		private static void Resize(List<TreeViewItem> list, int count)
		{
			int count2 = list.Count;
			if (count < count2)
			{
				list.RemoveRange(count, count2 - count);
			}
			else if (count > count2)
			{
				if (count > list.Capacity)
				{
					list.Capacity = count + 20;
				}
				list.AddRange(Enumerable.Repeat<TreeViewItem>(null, count - count2));
			}
		}

		private void ResizeItemList(int count)
		{
			this.AllocateBackingArrayIfNeeded();
			if (this.m_VisibleRows.Count != count)
			{
				GameObjectTreeViewDataSource.Resize(this.m_VisibleRows, count);
			}
		}

		private void AllocateBackingArrayIfNeeded()
		{
			if (this.m_VisibleRows == null)
			{
				int capacity = (this.m_RowCount <= 1000) ? 1000 : this.m_RowCount;
				this.m_VisibleRows = new List<TreeViewItem>(capacity);
			}
		}

		private void InitializeMinimal()
		{
			int[] expanded = this.m_TreeView.state.expandedIDs.ToArray();
			HierarchyProperty hierarchyProperty = this.CreateHierarchyProperty();
			this.m_RowCount = hierarchyProperty.CountRemaining(expanded);
			this.ResizeItemList(this.m_RowCount);
			hierarchyProperty.Reset();
			if (SceneHierarchyWindow.debug)
			{
				GameObjectTreeViewDataSource.Log("Init minimal (" + this.m_RowCount + ")");
			}
			int firstRow;
			int lastRow;
			this.m_TreeView.gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);
			this.InitializeRows(hierarchyProperty, firstRow, lastRow);
			this.m_RowsPartiallyInitialized = true;
		}

		private void InitializeFull()
		{
			if (SceneHierarchyWindow.debug)
			{
				GameObjectTreeViewDataSource.Log("Init full (" + this.m_RowCount + ")");
			}
			HierarchyProperty property = this.CreateHierarchyProperty();
			this.InitializeRows(property, 0, this.m_RowCount - 1);
		}

		private void InitializeProgressivly(HierarchyProperty property, bool subTreeWanted, bool isSearching)
		{
			this.AllocateBackingArrayIfNeeded();
			int num = (!subTreeWanted) ? 0 : (property.depth + 1);
			if (!isSearching)
			{
				int num2 = 0;
				int[] expanded = base.expandedIDs.ToArray();
				int num3 = (!subTreeWanted) ? 0 : (property.depth + 1);
				while (property.NextWithDepthCheck(expanded, num))
				{
					GameObjectTreeViewItem item = this.EnsureCreatedItem(num2);
					this.InitTreeViewItem(item, property, property.hasChildren, property.depth - num3);
					num2++;
				}
				this.m_RowCount = num2;
			}
			else
			{
				this.m_RowCount = this.InitializeSearchResults(property, num);
			}
			this.ResizeItemList(this.m_RowCount);
		}

		private int InitializeSearchResults(HierarchyProperty property, int minAllowedDepth)
		{
			int num = -1;
			int num2 = 0;
			while (property.NextWithDepthCheck(null, minAllowedDepth))
			{
				GameObjectTreeViewItem item = this.EnsureCreatedItem(num2);
				if (this.AddSceneHeaderToSearchIfNeeded(item, property, ref num))
				{
					num2++;
					if (this.IsSceneHeader(property))
					{
						continue;
					}
					item = this.EnsureCreatedItem(num2);
				}
				this.InitTreeViewItem(item, property, false, 0);
				num2++;
			}
			return num2;
		}

		private bool AddSceneHeaderToSearchIfNeeded(GameObjectTreeViewItem item, HierarchyProperty property, ref int currentSceneHandle)
		{
			if (SceneManager.sceneCount <= 1)
			{
				return false;
			}
			Scene scene = property.GetScene();
			if (currentSceneHandle != scene.handle)
			{
				currentSceneHandle = scene.handle;
				this.InitTreeViewItem(item, scene.handle, scene, true, 0, null, false, 0);
				return true;
			}
			return false;
		}

		private GameObjectTreeViewItem EnsureCreatedItem(int row)
		{
			if (row >= this.m_VisibleRows.Count)
			{
				this.m_VisibleRows.Add(null);
			}
			GameObjectTreeViewItem gameObjectTreeViewItem = (GameObjectTreeViewItem)this.m_VisibleRows[row];
			if (gameObjectTreeViewItem == null)
			{
				gameObjectTreeViewItem = new GameObjectTreeViewItem(0, 0, null, null);
				this.m_VisibleRows[row] = gameObjectTreeViewItem;
			}
			return gameObjectTreeViewItem;
		}

		private void InitializeRows(HierarchyProperty property, int firstRow, int lastRow)
		{
			property.Reset();
			int[] expanded = base.expandedIDs.ToArray();
			if (firstRow > 0 && !property.Skip(firstRow, expanded))
			{
				Debug.LogError("Failed to skip " + firstRow);
			}
			int num = firstRow;
			while (property.Next(expanded) && num <= lastRow)
			{
				GameObjectTreeViewItem item = this.EnsureCreatedItem(num);
				this.InitTreeViewItem(item, property, property.hasChildren, property.depth);
				num++;
			}
		}

		private void InitTreeViewItem(GameObjectTreeViewItem item, HierarchyProperty property, bool itemHasChildren, int itemDepth)
		{
			this.InitTreeViewItem(item, property.instanceID, property.GetScene(), this.IsSceneHeader(property), property.colorCode, property.pptrValue, itemHasChildren, itemDepth);
		}

		private void InitTreeViewItem(GameObjectTreeViewItem item, int itemID, Scene scene, bool isSceneHeader, int colorCode, UnityEngine.Object pptrObject, bool hasChildren, int depth)
		{
			item.children = null;
			item.userData = null;
			item.id = itemID;
			item.depth = depth;
			item.parent = null;
			if (isSceneHeader)
			{
				item.displayName = ((!string.IsNullOrEmpty(scene.path)) ? scene.name : "Untitled");
			}
			else
			{
				item.displayName = null;
			}
			item.colorCode = colorCode;
			item.objectPPTR = pptrObject;
			item.shouldDisplay = true;
			item.isSceneHeader = isSceneHeader;
			item.scene = scene;
			item.icon = ((!isSceneHeader) ? null : EditorGUIUtility.FindTexture("SceneAsset Icon"));
			if (hasChildren)
			{
				item.children = LazyTreeViewDataSource.CreateChildListForCollapsedParent();
			}
		}

		private bool IsSceneHeader(HierarchyProperty property)
		{
			return property.pptrValue == null;
		}

		protected override HashSet<int> GetParentsAbove(int id)
		{
			HashSet<int> hashSet = new HashSet<int>();
			if (!this.IsValidHierarchyInstanceID(id))
			{
				return hashSet;
			}
			IHierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			if (hierarchyProperty.Find(id, null))
			{
				while (hierarchyProperty.Parent())
				{
					hashSet.Add(hierarchyProperty.instanceID);
				}
			}
			return hashSet;
		}

		protected override HashSet<int> GetParentsBelow(int id)
		{
			HashSet<int> hashSet = new HashSet<int>();
			if (!this.IsValidHierarchyInstanceID(id))
			{
				return hashSet;
			}
			IHierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			if (hierarchyProperty.Find(id, null))
			{
				hashSet.Add(id);
				int depth = hierarchyProperty.depth;
				while (hierarchyProperty.Next(null) && hierarchyProperty.depth > depth)
				{
					if (hierarchyProperty.hasChildren)
					{
						hashSet.Add(hierarchyProperty.instanceID);
					}
				}
			}
			return hashSet;
		}

		private void SortVisibleRows()
		{
			this.SetupChildParentReferencesIfNeeded();
			this.SortChildrenRecursively(this.m_RootItem, this.sortingState.sortingObject);
			this.m_VisibleRows.Clear();
			this.RebuildVisibilityTree(this.m_RootItem, this.m_VisibleRows);
		}

		private void SortChildrenRecursively(TreeViewItem item, BaseHierarchySort comparer)
		{
			if (item == null || !item.hasChildren)
			{
				return;
			}
			item.children = item.children.OrderBy((TreeViewItem x) => (x as GameObjectTreeViewItem).objectPPTR as GameObject, comparer).ToList<TreeViewItem>();
			for (int i = 0; i < item.children.Count; i++)
			{
				this.SortChildrenRecursively(item.children[i], comparer);
			}
		}

		private void RebuildVisibilityTree(TreeViewItem item, List<TreeViewItem> visibleItems)
		{
			if (item == null || !item.hasChildren)
			{
				return;
			}
			for (int i = 0; i < item.children.Count; i++)
			{
				if (item.children[i] != null)
				{
					visibleItems.Add(item.children[i]);
					this.RebuildVisibilityTree(item.children[i], visibleItems);
				}
			}
		}

		private static void Log(string text)
		{
			Debug.Log(text);
		}

		private void CreateSceneHeaderItems()
		{
			this.m_StickySceneHeaderItems.Clear();
			int sceneCount = SceneManager.sceneCount;
			for (int i = 0; i < sceneCount; i++)
			{
				Scene sceneAt = SceneManager.GetSceneAt(i);
				GameObjectTreeViewItem item = new GameObjectTreeViewItem(0, 0, null, null);
				this.InitTreeViewItem(item, sceneAt.handle, sceneAt, true, 0, null, false, 0);
				this.m_StickySceneHeaderItems.Add(item);
			}
		}
	}
}
