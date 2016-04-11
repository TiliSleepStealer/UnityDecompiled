using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
	[EditorWindowTitle(title = "Hierarchy", useTypeNameAsIconName = true)]
	internal class SceneHierarchyWindow : SearchableEditorWindow, IHasCustomMenu
	{
		private class Styles
		{
			private const string kCustomSorting = "CustomSorting";

			private const string kWarningSymbol = "console.warnicon.sml";

			private const string kWarningMessage = "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.";

			public GUIContent defaultSortingContent = new GUIContent(EditorGUIUtility.FindTexture("CustomSorting"));

			public GUIContent createContent = new GUIContent("Create");

			public GUIContent fetchWarning = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("console.warnicon.sml"), "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.");

			public GUIStyle MiniButton;

			public GUIStyle lockButton = "IN LockButton";

			public Styles()
			{
				this.MiniButton = "ToolbarButton";
			}
		}

		private const int kInvalidSceneHandle = 0;

		private const float toolbarHeight = 17f;

		private static SceneHierarchyWindow s_LastInteractedHierarchy;

		private static SceneHierarchyWindow.Styles s_Styles;

		private static List<SceneHierarchyWindow> s_SceneHierarchyWindow = new List<SceneHierarchyWindow>();

		private TreeView m_TreeView;

		[SerializeField]
		private TreeViewState m_TreeViewState;

		private int m_TreeViewKeyboardControlID;

		[SerializeField]
		private int m_CurrenRootInstanceID;

		[SerializeField]
		private bool m_Locked;

		[SerializeField]
		private string m_CurrentSortMethod = string.Empty;

		[NonSerialized]
		private int m_LastFramedID = -1;

		[NonSerialized]
		private bool m_TreeViewReloadNeeded;

		[NonSerialized]
		private bool m_SelectionSyncNeeded;

		[NonSerialized]
		private bool m_FrameOnSelectionSync;

		[NonSerialized]
		private bool m_DidSelectSearchResult;

		private Dictionary<string, BaseHierarchySort> m_SortingObjects;

		private bool m_AllowAlphaNumericalSort;

		[NonSerialized]
		private double m_LastUserInteractionTime;

		private bool m_Debug;

		public static bool s_Debug = SessionState.GetBool("HierarchyWindowDebug", false);

		public static SceneHierarchyWindow lastInteractedHierarchyWindow
		{
			get
			{
				return SceneHierarchyWindow.s_LastInteractedHierarchy;
			}
		}

		internal static bool debug
		{
			get
			{
				return SceneHierarchyWindow.lastInteractedHierarchyWindow.m_Debug;
			}
			set
			{
				SceneHierarchyWindow.lastInteractedHierarchyWindow.m_Debug = value;
			}
		}

		private bool treeViewReloadNeeded
		{
			get
			{
				return this.m_TreeViewReloadNeeded;
			}
			set
			{
				this.m_TreeViewReloadNeeded = value;
				if (value)
				{
					base.Repaint();
					if (SceneHierarchyWindow.s_Debug)
					{
						Debug.Log("Reload treeview on next event");
					}
				}
			}
		}

		private bool selectionSyncNeeded
		{
			get
			{
				return this.m_SelectionSyncNeeded;
			}
			set
			{
				this.m_SelectionSyncNeeded = value;
				if (value)
				{
					base.Repaint();
					if (SceneHierarchyWindow.s_Debug)
					{
						Debug.Log("Selection sync and frameing on next event");
					}
				}
			}
		}

		private string currentSortMethod
		{
			get
			{
				return this.m_CurrentSortMethod;
			}
			set
			{
				this.m_CurrentSortMethod = value;
				if (!this.m_SortingObjects.ContainsKey(this.m_CurrentSortMethod))
				{
					this.m_CurrentSortMethod = this.GetNameForType(typeof(TransformSort));
				}
				GameObjectTreeViewDataSource gameObjectTreeViewDataSource = (GameObjectTreeViewDataSource)this.treeView.data;
				gameObjectTreeViewDataSource.sortingState.sortingObject = this.m_SortingObjects[this.m_CurrentSortMethod];
				GameObjectsTreeViewDragging gameObjectsTreeViewDragging = (GameObjectsTreeViewDragging)this.treeView.dragging;
				gameObjectsTreeViewDragging.allowDragBetween = !gameObjectTreeViewDataSource.sortingState.implementsCompare;
			}
		}

		private bool hasSortMethods
		{
			get
			{
				return this.m_SortingObjects.Count > 1;
			}
		}

		private Rect treeViewRect
		{
			get
			{
				return new Rect(0f, 17f, base.position.width, base.position.height - 17f);
			}
		}

		private TreeView treeView
		{
			get
			{
				if (this.m_TreeView == null)
				{
					this.Init();
				}
				return this.m_TreeView;
			}
		}

		public static List<SceneHierarchyWindow> GetAllSceneHierarchyWindows()
		{
			return SceneHierarchyWindow.s_SceneHierarchyWindow;
		}

		private void Init()
		{
			if (this.m_TreeViewState == null)
			{
				this.m_TreeViewState = new TreeViewState();
			}
			this.m_TreeView = new TreeView(this, this.m_TreeViewState);
			TreeView expr_2E = this.m_TreeView;
			expr_2E.itemDoubleClickedCallback = (Action<int>)Delegate.Combine(expr_2E.itemDoubleClickedCallback, new Action<int>(this.TreeViewItemDoubleClicked));
			TreeView expr_55 = this.m_TreeView;
			expr_55.selectionChangedCallback = (Action<int[]>)Delegate.Combine(expr_55.selectionChangedCallback, new Action<int[]>(this.TreeViewSelectionChanged));
			TreeView expr_7C = this.m_TreeView;
			expr_7C.onGUIRowCallback = (Action<int, Rect>)Delegate.Combine(expr_7C.onGUIRowCallback, new Action<int, Rect>(this.OnGUIAssetCallback));
			TreeView expr_A3 = this.m_TreeView;
			expr_A3.dragEndedCallback = (Action<int[], bool>)Delegate.Combine(expr_A3.dragEndedCallback, new Action<int[], bool>(this.OnDragEndedCallback));
			TreeView expr_CA = this.m_TreeView;
			expr_CA.contextClickItemCallback = (Action<int>)Delegate.Combine(expr_CA.contextClickItemCallback, new Action<int>(this.ItemContextClick));
			TreeView expr_F1 = this.m_TreeView;
			expr_F1.contextClickOutsideItemsCallback = (Action)Delegate.Combine(expr_F1.contextClickOutsideItemsCallback, new Action(this.ContextClickOutsideItems));
			this.m_TreeView.deselectOnUnhandledMouseDown = true;
			bool showRootNode = false;
			bool rootNodeIsCollapsable = false;
			GameObjectTreeViewDataSource gameObjectTreeViewDataSource = new GameObjectTreeViewDataSource(this.m_TreeView, this.m_CurrenRootInstanceID, showRootNode, rootNodeIsCollapsable);
			GameObjectsTreeViewDragging dragging = new GameObjectsTreeViewDragging(this.m_TreeView);
			GameObjectTreeViewGUI gui = new GameObjectTreeViewGUI(this.m_TreeView, false);
			this.m_TreeView.Init(this.treeViewRect, gameObjectTreeViewDataSource, gui, dragging);
			gameObjectTreeViewDataSource.searchMode = (int)this.m_SearchMode;
			gameObjectTreeViewDataSource.searchString = this.m_SearchFilter;
			this.m_AllowAlphaNumericalSort = (EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false) || InternalEditorUtility.inBatchMode);
			this.SetUpSortMethodLists();
			this.m_TreeView.ReloadData();
		}

		public void SetCurrentRootInstanceID(int instanceID)
		{
			this.m_CurrenRootInstanceID = instanceID;
			this.Init();
			GUIUtility.ExitGUI();
		}

		public UnityEngine.Object[] GetCurrentVisibleObjects()
		{
			List<TreeViewItem> rows = this.m_TreeView.data.GetRows();
			UnityEngine.Object[] array = new UnityEngine.Object[rows.Count];
			for (int i = 0; i < rows.Count; i++)
			{
				array[i] = ((GameObjectTreeViewItem)rows[i]).objectPPTR;
			}
			return array;
		}

		internal void SelectPrevious()
		{
			this.m_TreeView.OffsetSelection(-1);
		}

		internal void SelectNext()
		{
			this.m_TreeView.OffsetSelection(1);
		}

		private void Awake()
		{
			this.m_HierarchyType = HierarchyType.GameObjects;
			if (this.m_TreeViewState != null)
			{
				this.m_TreeViewState.OnAwake();
			}
		}

		private void OnBecameVisible()
		{
			if (SceneManager.sceneCount > 0)
			{
				this.treeViewReloadNeeded = true;
			}
		}

		public override void OnEnable()
		{
			base.OnEnable();
			base.titleContent = base.GetLocalizedTitleContent();
			SceneHierarchyWindow.s_SceneHierarchyWindow.Add(this);
			EditorApplication.projectWindowChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.projectWindowChanged, new EditorApplication.CallbackFunction(this.ReloadData));
			EditorApplication.searchChanged = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.searchChanged, new EditorApplication.CallbackFunction(this.SearchChanged));
			SceneHierarchyWindow.s_LastInteractedHierarchy = this;
		}

		public override void OnDisable()
		{
			EditorApplication.projectWindowChanged = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.projectWindowChanged, new EditorApplication.CallbackFunction(this.ReloadData));
			EditorApplication.searchChanged = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.searchChanged, new EditorApplication.CallbackFunction(this.SearchChanged));
			SceneHierarchyWindow.s_SceneHierarchyWindow.Remove(this);
		}

		public void OnDestroy()
		{
			if (SceneHierarchyWindow.s_LastInteractedHierarchy == this)
			{
				SceneHierarchyWindow.s_LastInteractedHierarchy = null;
				foreach (SceneHierarchyWindow current in SceneHierarchyWindow.s_SceneHierarchyWindow)
				{
					if (current != this)
					{
						SceneHierarchyWindow.s_LastInteractedHierarchy = current;
					}
				}
			}
		}

		private void SetAsLastInteractedHierarchy()
		{
			SceneHierarchyWindow.s_LastInteractedHierarchy = this;
		}

		private void SyncIfNeeded()
		{
			if (this.treeViewReloadNeeded)
			{
				this.treeViewReloadNeeded = false;
				this.ReloadData();
			}
			if (this.selectionSyncNeeded)
			{
				this.selectionSyncNeeded = false;
				bool flag = EditorApplication.timeSinceStartup - this.m_LastUserInteractionTime < 0.2;
				bool flag2 = !this.m_Locked || this.m_FrameOnSelectionSync || flag;
				bool animatedFraming = flag && flag2;
				this.m_FrameOnSelectionSync = false;
				this.treeView.SetSelection(Selection.instanceIDs, flag2, animatedFraming);
			}
		}

		private void DetectUserInteraction()
		{
			Event current = Event.current;
			if (current.type != EventType.Layout && current.type != EventType.Repaint)
			{
				this.m_LastUserInteractionTime = EditorApplication.timeSinceStartup;
			}
		}

		private void OnGUI()
		{
			if (SceneHierarchyWindow.s_Styles == null)
			{
				SceneHierarchyWindow.s_Styles = new SceneHierarchyWindow.Styles();
			}
			this.DetectUserInteraction();
			this.SyncIfNeeded();
			this.m_TreeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
			this.OnEvent();
			Rect rect = new Rect(0f, 0f, base.position.width, base.position.height);
			Event current = Event.current;
			if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
			{
				this.treeView.EndPing();
				this.SetAsLastInteractedHierarchy();
			}
			this.DoToolbar();
			float searchPathHeight = this.DoSearchResultPathGUI();
			this.DoTreeView(searchPathHeight);
			this.ExecuteCommands();
		}

		private void OnLostFocus()
		{
			this.treeView.EndNameEditing(true);
			EditorGUI.EndEditingActiveTextField();
		}

		public static bool IsSceneHeaderInHierarchyWindow(Scene scene)
		{
			return scene.IsValid();
		}

		private void TreeViewItemDoubleClicked(int instanceID)
		{
			Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(instanceID);
			if (SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(sceneByHandle))
			{
				if (sceneByHandle.isLoaded)
				{
					SceneManager.SetActiveScene(sceneByHandle);
				}
			}
			else
			{
				SceneView.FrameLastActiveSceneView();
			}
		}

		public void SetExpandedRecursive(int id, bool expand)
		{
			TreeViewItem treeViewItem = this.treeView.data.FindItem(id);
			if (treeViewItem == null)
			{
				this.ReloadData();
				treeViewItem = this.treeView.data.FindItem(id);
			}
			if (treeViewItem != null)
			{
				this.treeView.data.SetExpandedWithChildren(treeViewItem, expand);
			}
		}

		private void OnGUIAssetCallback(int instanceID, Rect rect)
		{
			if (EditorApplication.hierarchyWindowItemOnGUI != null)
			{
				EditorApplication.hierarchyWindowItemOnGUI(instanceID, rect);
			}
		}

		private void OnDragEndedCallback(int[] draggedInstanceIds, bool draggedItemsFromOwnTreeView)
		{
			if (draggedInstanceIds != null && draggedItemsFromOwnTreeView)
			{
				this.ReloadData();
				this.treeView.SetSelection(draggedInstanceIds, true);
				this.treeView.NotifyListenersThatSelectionChanged();
				base.Repaint();
				GUIUtility.ExitGUI();
			}
		}

		public void ReloadData()
		{
			if (this.m_TreeView == null)
			{
				this.Init();
			}
			else
			{
				this.m_TreeView.ReloadData();
			}
		}

		public void SearchChanged()
		{
			GameObjectTreeViewDataSource gameObjectTreeViewDataSource = (GameObjectTreeViewDataSource)this.treeView.data;
			if (gameObjectTreeViewDataSource.searchMode == (int)base.searchMode && gameObjectTreeViewDataSource.searchString == this.m_SearchFilter)
			{
				return;
			}
			gameObjectTreeViewDataSource.searchMode = (int)base.searchMode;
			gameObjectTreeViewDataSource.searchString = this.m_SearchFilter;
			if (this.m_SearchFilter == string.Empty)
			{
				this.treeView.Frame(Selection.activeInstanceID, true, false);
			}
			this.ReloadData();
		}

		private void TreeViewSelectionChanged(int[] ids)
		{
			Selection.instanceIDs = ids;
			this.m_DidSelectSearchResult = !string.IsNullOrEmpty(this.m_SearchFilter);
		}

		private bool IsTreeViewSelectionInSyncWithBackend()
		{
			return this.m_TreeView != null && this.m_TreeView.state.selectedIDs.SequenceEqual(Selection.instanceIDs);
		}

		private void OnSelectionChange()
		{
			if (!this.IsTreeViewSelectionInSyncWithBackend())
			{
				this.selectionSyncNeeded = true;
			}
			else if (SceneHierarchyWindow.s_Debug)
			{
				Debug.Log("OnSelectionChange: Selection is already in sync so no framing will happen");
			}
		}

		private void OnHierarchyChange()
		{
			if (this.m_TreeView != null)
			{
				this.m_TreeView.EndNameEditing(false);
			}
			this.treeViewReloadNeeded = true;
		}

		private float DoSearchResultPathGUI()
		{
			if (!base.hasSearchFilter)
			{
				return 0f;
			}
			GUILayout.FlexibleSpace();
			Rect rect = EditorGUILayout.BeginVertical(EditorStyles.inspectorBig, new GUILayoutOption[0]);
			GUILayout.Label("Path:", new GUILayoutOption[0]);
			if (this.m_TreeView.HasSelection())
			{
				int instanceID = this.m_TreeView.GetSelection()[0];
				IHierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
				hierarchyProperty.Find(instanceID, null);
				if (hierarchyProperty.isValid)
				{
					do
					{
						EditorGUILayout.BeginHorizontal(new GUILayoutOption[0]);
						GUILayout.Label(hierarchyProperty.icon, new GUILayoutOption[0]);
						GUILayout.Label(hierarchyProperty.name, new GUILayoutOption[0]);
						GUILayout.FlexibleSpace();
						EditorGUILayout.EndHorizontal();
					}
					while (hierarchyProperty.Parent());
				}
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(0f);
			return rect.height;
		}

		private void OnEvent()
		{
			this.treeView.OnEvent();
		}

		private void DoTreeView(float searchPathHeight)
		{
			Rect treeViewRect = this.treeViewRect;
			treeViewRect.height -= searchPathHeight;
			this.treeView.OnGUI(treeViewRect, this.m_TreeViewKeyboardControlID);
		}

		private void DoToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar, new GUILayoutOption[0]);
			this.CreateGameObjectPopup();
			GUILayout.Space(6f);
			if (SceneHierarchyWindow.s_Debug)
			{
				int num;
				int num2;
				this.m_TreeView.gui.GetFirstAndLastRowVisible(out num, out num2);
				GUILayout.Label(string.Format("{0} ({1}, {2})", this.m_TreeView.data.rowCount, num, num2), EditorStyles.miniLabel, new GUILayoutOption[0]);
				GUILayout.Space(6f);
			}
			GUILayout.FlexibleSpace();
			Event current = Event.current;
			if (base.hasSearchFilterFocus && current.type == EventType.KeyDown && (current.keyCode == KeyCode.DownArrow || current.keyCode == KeyCode.UpArrow))
			{
				GUIUtility.keyboardControl = this.m_TreeViewKeyboardControlID;
				if (this.treeView.IsLastClickedPartOfRows())
				{
					this.treeView.Frame(this.treeView.state.lastClickedID, true, false);
					this.m_DidSelectSearchResult = !string.IsNullOrEmpty(this.m_SearchFilter);
				}
				else
				{
					this.treeView.OffsetSelection(1);
				}
				current.Use();
			}
			base.SearchFieldGUI();
			GUILayout.Space(6f);
			if (this.hasSortMethods)
			{
				if (Application.isPlaying && ((GameObjectTreeViewDataSource)this.treeView.data).isFetchAIssue)
				{
					GUILayout.Toggle(false, SceneHierarchyWindow.s_Styles.fetchWarning, SceneHierarchyWindow.s_Styles.MiniButton, new GUILayoutOption[0]);
				}
				this.SortMethodsDropDown();
			}
			GUILayout.EndHorizontal();
		}

		internal override void SetSearchFilter(string searchFilter, SearchableEditorWindow.SearchMode searchMode, bool setAll)
		{
			base.SetSearchFilter(searchFilter, searchMode, setAll);
			if (this.m_DidSelectSearchResult && string.IsNullOrEmpty(searchFilter))
			{
				this.m_DidSelectSearchResult = false;
				this.FrameObjectPrivate(Selection.activeInstanceID, true, false, false);
				if (GUIUtility.keyboardControl == 0)
				{
					GUIUtility.keyboardControl = this.m_TreeViewKeyboardControlID;
				}
			}
		}

		private void AddCreateGameObjectItemsToMenu(GenericMenu menu, UnityEngine.Object[] context, bool includeCreateEmptyChild, bool includeGameObjectInPath, int targetSceneHandle)
		{
			string[] submenus = Unsupported.GetSubmenus("GameObject");
			string[] array = submenus;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				UnityEngine.Object[] temporaryContext = context;
				if (includeCreateEmptyChild || !(text.ToLower() == "GameObject/Create Empty Child".ToLower()))
				{
					if (text.EndsWith("..."))
					{
						temporaryContext = null;
					}
					if (text.ToLower() == "GameObject/Center On Children".ToLower())
					{
						return;
					}
					string replacementMenuString = text;
					if (!includeGameObjectInPath)
					{
						replacementMenuString = text.Substring(11);
					}
					MenuUtils.ExtractMenuItemWithPath(text, menu, replacementMenuString, temporaryContext, targetSceneHandle, new Action<string, UnityEngine.Object[], int>(this.BeforeCreateGameObjectMenuItemWasExecuted), new Action<string, UnityEngine.Object[], int>(this.AfterCreateGameObjectMenuItemWasExecuted));
				}
			}
		}

		private void BeforeCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, int userData)
		{
			EditorSceneManager.SetTargetSceneForNewGameObjects(userData);
		}

		private void AfterCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, int userData)
		{
			EditorSceneManager.SetTargetSceneForNewGameObjects(0);
			if (this.m_Locked)
			{
				this.m_FrameOnSelectionSync = true;
			}
		}

		private void CreateGameObjectPopup()
		{
			Rect rect = GUILayoutUtility.GetRect(SceneHierarchyWindow.s_Styles.createContent, EditorStyles.toolbarDropDown, null);
			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.toolbarDropDown.Draw(rect, SceneHierarchyWindow.s_Styles.createContent, false, false, false, false);
			}
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl = 0;
				GenericMenu genericMenu = new GenericMenu();
				this.AddCreateGameObjectItemsToMenu(genericMenu, null, true, false, 0);
				genericMenu.DropDown(rect);
				Event.current.Use();
			}
		}

		private void SortMethodsDropDown()
		{
			if (this.hasSortMethods)
			{
				GUIContent gUIContent = this.m_SortingObjects[this.currentSortMethod].content;
				if (gUIContent == null)
				{
					gUIContent = SceneHierarchyWindow.s_Styles.defaultSortingContent;
					gUIContent.tooltip = this.currentSortMethod;
				}
				Rect rect = GUILayoutUtility.GetRect(gUIContent, EditorStyles.toolbarButton);
				if (EditorGUI.ButtonMouseDown(rect, gUIContent, FocusType.Passive, EditorStyles.toolbarButton))
				{
					List<SceneHierarchySortingWindow.InputData> list = new List<SceneHierarchySortingWindow.InputData>();
					foreach (KeyValuePair<string, BaseHierarchySort> current in this.m_SortingObjects)
					{
						list.Add(new SceneHierarchySortingWindow.InputData
						{
							m_TypeName = current.Key,
							m_Name = ObjectNames.NicifyVariableName(current.Key),
							m_Selected = (current.Key == this.m_CurrentSortMethod)
						});
					}
					if (SceneHierarchySortingWindow.ShowAtPosition(new Vector2(rect.x, rect.y + rect.height), list, new SceneHierarchySortingWindow.OnSelectCallback(this.SortFunctionCallback)))
					{
						GUIUtility.ExitGUI();
					}
				}
			}
		}

		private void SetUpSortMethodLists()
		{
			this.m_SortingObjects = new Dictionary<string, BaseHierarchySort>();
			Assembly[] loadedAssemblies = EditorAssemblies.loadedAssemblies;
			for (int i = 0; i < loadedAssemblies.Length; i++)
			{
				Assembly assembly = loadedAssemblies[i];
				foreach (BaseHierarchySort current in AssemblyHelper.FindImplementors<BaseHierarchySort>(assembly))
				{
					if (current.GetType() != typeof(AlphabeticalSort) || this.m_AllowAlphaNumericalSort)
					{
						string nameForType = this.GetNameForType(current.GetType());
						this.m_SortingObjects.Add(nameForType, current);
					}
				}
			}
			this.currentSortMethod = this.m_CurrentSortMethod;
		}

		private string GetNameForType(Type type)
		{
			return type.Name;
		}

		private void SortFunctionCallback(SceneHierarchySortingWindow.InputData data)
		{
			this.SetSortFunction(data.m_TypeName);
		}

		public void SetSortFunction(Type sortType)
		{
			this.SetSortFunction(this.GetNameForType(sortType));
		}

		private void SetSortFunction(string sortTypeName)
		{
			if (!this.m_SortingObjects.ContainsKey(sortTypeName))
			{
				Debug.LogError("Invalid search type name: " + sortTypeName);
				return;
			}
			this.currentSortMethod = sortTypeName;
			if (this.treeView.GetSelection().Any<int>())
			{
				this.treeView.Frame(this.treeView.GetSelection().First<int>(), true, false);
			}
			this.treeView.ReloadData();
		}

		public void DirtySortingMethods()
		{
			this.m_AllowAlphaNumericalSort = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);
			this.SetUpSortMethodLists();
			this.treeView.SetSelection(this.treeView.GetSelection(), true);
			this.treeView.ReloadData();
		}

		private void ExecuteCommands()
		{
			Event current = Event.current;
			if (current.type != EventType.ExecuteCommand && current.type != EventType.ValidateCommand)
			{
				return;
			}
			bool flag = current.type == EventType.ExecuteCommand;
			if (current.commandName == "Delete" || current.commandName == "SoftDelete")
			{
				if (flag)
				{
					this.DeleteGO();
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "Duplicate")
			{
				if (flag)
				{
					this.DuplicateGO();
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "Copy")
			{
				if (flag)
				{
					this.CopyGO();
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "Paste")
			{
				if (flag)
				{
					this.PasteGO();
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "SelectAll")
			{
				if (flag)
				{
					this.SelectAll();
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "FrameSelected")
			{
				if (current.type == EventType.ExecuteCommand)
				{
					this.FrameObjectPrivate(Selection.activeInstanceID, true, true, true);
				}
				current.Use();
				GUIUtility.ExitGUI();
			}
			else if (current.commandName == "Find")
			{
				if (current.type == EventType.ExecuteCommand)
				{
					base.FocusSearchField();
				}
				current.Use();
			}
		}

		private void CreateGameObjectContextClick(GenericMenu menu, int contextClickedItemID)
		{
			menu.AddItem(EditorGUIUtility.TextContent("Copy"), false, new GenericMenu.MenuFunction(this.CopyGO));
			menu.AddItem(EditorGUIUtility.TextContent("Paste"), false, new GenericMenu.MenuFunction(this.PasteGO));
			menu.AddSeparator(string.Empty);
			if (!base.hasSearchFilter && this.m_TreeViewState.selectedIDs.Count == 1)
			{
				menu.AddItem(EditorGUIUtility.TextContent("Rename"), false, new GenericMenu.MenuFunction(this.RenameGO));
			}
			else
			{
				menu.AddDisabledItem(EditorGUIUtility.TextContent("Rename"));
			}
			menu.AddItem(EditorGUIUtility.TextContent("Duplicate"), false, new GenericMenu.MenuFunction(this.DuplicateGO));
			menu.AddItem(EditorGUIUtility.TextContent("Delete"), false, new GenericMenu.MenuFunction(this.DeleteGO));
			menu.AddSeparator(string.Empty);
			bool flag = false;
			if (this.m_TreeViewState.selectedIDs.Count == 1)
			{
				GameObjectTreeViewItem gameObjectTreeViewItem = this.treeView.FindNode(this.m_TreeViewState.selectedIDs[0]) as GameObjectTreeViewItem;
				if (gameObjectTreeViewItem != null)
				{
					UnityEngine.Object prefab = PrefabUtility.GetPrefabParent(gameObjectTreeViewItem.objectPPTR);
					if (prefab != null)
					{
						menu.AddItem(EditorGUIUtility.TextContent("Select Prefab"), false, delegate
						{
							Selection.activeObject = prefab;
							EditorGUIUtility.PingObject(prefab.GetInstanceID());
						});
						flag = true;
					}
				}
			}
			if (!flag)
			{
				menu.AddDisabledItem(EditorGUIUtility.TextContent("Select Prefab"));
			}
			menu.AddSeparator(string.Empty);
			this.AddCreateGameObjectItemsToMenu(menu, (from t in Selection.transforms
			select t.gameObject).ToArray<GameObject>(), false, false, 0);
			menu.ShowAsContext();
		}

		private void CreateMultiSceneHeaderContextClick(GenericMenu menu, int contextClickedItemID)
		{
			Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(contextClickedItemID);
			if (!SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(sceneByHandle))
			{
				Debug.LogError("Context clicked item is not a scene");
				return;
			}
			if (sceneByHandle.isLoaded)
			{
				menu.AddItem(EditorGUIUtility.TextContent("Set Active Scene"), false, new GenericMenu.MenuFunction2(this.SetSceneActive), contextClickedItemID);
				menu.AddSeparator(string.Empty);
			}
			if (sceneByHandle.isLoaded)
			{
				if (!EditorApplication.isPlaying)
				{
					menu.AddItem(EditorGUIUtility.TextContent("Save Scene"), false, new GenericMenu.MenuFunction2(this.SaveSelectedScenes), contextClickedItemID);
					menu.AddItem(EditorGUIUtility.TextContent("Save Scene As"), false, new GenericMenu.MenuFunction2(this.SaveSceneAs), contextClickedItemID);
					menu.AddItem(EditorGUIUtility.TextContent("Save All"), false, new GenericMenu.MenuFunction2(this.SaveAllScenes), contextClickedItemID);
				}
				else
				{
					menu.AddDisabledItem(EditorGUIUtility.TextContent("Save Scene"));
					menu.AddDisabledItem(EditorGUIUtility.TextContent("Save Scene As"));
					menu.AddDisabledItem(EditorGUIUtility.TextContent("Save All"));
				}
				menu.AddSeparator(string.Empty);
			}
			bool flag = EditorSceneManager.loadedSceneCount != this.GetNumLoadedScenesInSelection();
			if (sceneByHandle.isLoaded)
			{
				bool flag2 = flag && !EditorApplication.isPlaying && !string.IsNullOrEmpty(sceneByHandle.path);
				if (flag2)
				{
					menu.AddItem(EditorGUIUtility.TextContent("Unload Scene"), false, new GenericMenu.MenuFunction2(this.UnloadSelectedScenes), contextClickedItemID);
				}
				else
				{
					menu.AddDisabledItem(EditorGUIUtility.TextContent("Unload Scene"));
				}
			}
			else
			{
				bool flag3 = !EditorApplication.isPlaying;
				if (flag3)
				{
					menu.AddItem(EditorGUIUtility.TextContent("Load Scene"), false, new GenericMenu.MenuFunction2(this.LoadSelectedScenes), contextClickedItemID);
				}
				else
				{
					menu.AddDisabledItem(EditorGUIUtility.TextContent("Load Scene"));
				}
			}
			bool flag4 = this.GetSelectedScenes().Count == SceneManager.sceneCount;
			bool flag5 = flag && !flag4 && !EditorApplication.isPlaying;
			if (flag5)
			{
				menu.AddItem(EditorGUIUtility.TextContent("Remove Scene"), false, new GenericMenu.MenuFunction2(this.RemoveSelectedScenes), contextClickedItemID);
			}
			else
			{
				menu.AddDisabledItem(EditorGUIUtility.TextContent("Remove Scene"));
			}
			menu.AddSeparator(string.Empty);
			if (!string.IsNullOrEmpty(sceneByHandle.path))
			{
				menu.AddItem(EditorGUIUtility.TextContent("Select Scene Asset"), false, new GenericMenu.MenuFunction2(this.SelectSceneAsset), contextClickedItemID);
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Select Scene Asset"));
			}
			if (sceneByHandle.isLoaded)
			{
				menu.AddSeparator(string.Empty);
				this.AddCreateGameObjectItemsToMenu(menu, (from t in Selection.transforms
				select t.gameObject).ToArray<GameObject>(), false, true, sceneByHandle.handle);
			}
		}

		private int GetNumLoadedScenesInSelection()
		{
			int num = 0;
			foreach (int current in this.GetSelectedScenes())
			{
				if (EditorSceneManager.GetSceneByHandle(current).isLoaded)
				{
					num++;
				}
			}
			return num;
		}

		private List<int> GetSelectedScenes()
		{
			List<int> list = new List<int>();
			int[] selection = this.m_TreeView.GetSelection();
			int[] array = selection;
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				if (SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(num)))
				{
					list.Add(num);
				}
			}
			return list;
		}

		private List<int> GetSelectedGameObjects()
		{
			List<int> list = new List<int>();
			int[] selection = this.m_TreeView.GetSelection();
			int[] array = selection;
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				if (!SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(num)))
				{
					list.Add(num);
				}
			}
			return list;
		}

		private void ContextClickOutsideItems()
		{
			Event current = Event.current;
			current.Use();
			GenericMenu genericMenu = new GenericMenu();
			this.CreateGameObjectContextClick(genericMenu, 0);
			genericMenu.ShowAsContext();
		}

		private void ItemContextClick(int contextClickedItemID)
		{
			Event current = Event.current;
			current.Use();
			GenericMenu genericMenu = new GenericMenu();
			bool flag = SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(contextClickedItemID));
			if (flag)
			{
				this.CreateMultiSceneHeaderContextClick(genericMenu, contextClickedItemID);
			}
			else
			{
				this.CreateGameObjectContextClick(genericMenu, contextClickedItemID);
			}
			genericMenu.ShowAsContext();
		}

		private void CopyGO()
		{
			Unsupported.CopyGameObjectsToPasteboard();
		}

		private void PasteGO()
		{
			Unsupported.PasteGameObjectsFromPasteboard();
		}

		private void DuplicateGO()
		{
			Unsupported.DuplicateGameObjectsUsingPasteboard();
		}

		private void RenameGO()
		{
			this.treeView.BeginNameEditing(0f);
		}

		private void DeleteGO()
		{
			Unsupported.DeleteGameObjectSelection();
		}

		private void SetSceneActive(object userData)
		{
			int handle = (int)userData;
			SceneManager.SetActiveScene(EditorSceneManager.GetSceneByHandle(handle));
		}

		private void LoadSelectedScenes(object userdata)
		{
			List<int> selectedScenes = this.GetSelectedScenes();
			foreach (int current in selectedScenes)
			{
				Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(current);
				if (!sceneByHandle.isLoaded)
				{
					EditorSceneManager.OpenScene(sceneByHandle.path, OpenSceneMode.Additive);
				}
			}
			EditorApplication.RequestRepaintAllViews();
		}

		private void SaveSceneAs(object userdata)
		{
			int handle = (int)userdata;
			Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(handle);
			if (sceneByHandle.isLoaded)
			{
				EditorSceneManager.SaveSceneAs(sceneByHandle);
			}
		}

		private void SaveAllScenes(object userdata)
		{
			EditorSceneManager.SaveOpenScenes();
		}

		private void SaveSelectedScenes(object userdata)
		{
			List<int> selectedScenes = this.GetSelectedScenes();
			foreach (int current in selectedScenes)
			{
				Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(current);
				if (sceneByHandle.isLoaded)
				{
					EditorSceneManager.SaveScene(sceneByHandle);
				}
			}
		}

		private void UnloadSelectedScenes(object userdata)
		{
			this.CloseSelectedScenes(false);
		}

		private void RemoveSelectedScenes(object userData)
		{
			this.CloseSelectedScenes(true);
		}

		private Scene[] GetModifiedScenes(List<int> handles)
		{
			return (from handle in handles
			select EditorSceneManager.GetSceneByHandle(handle) into scene
			where scene.isDirty
			select scene).ToArray<Scene>();
		}

		private void CloseSelectedScenes(bool removeScenes)
		{
			List<int> selectedScenes = this.GetSelectedScenes();
			Scene[] modifiedScenes = this.GetModifiedScenes(selectedScenes);
			bool flag = !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(modifiedScenes);
			if (flag)
			{
				return;
			}
			foreach (int current in selectedScenes)
			{
				EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByHandle(current), removeScenes);
			}
			EditorApplication.RequestRepaintAllViews();
		}

		private void SelectSceneAsset(object userData)
		{
			int handle = (int)userData;
			string guid = AssetDatabase.AssetPathToGUID(EditorSceneManager.GetSceneByHandle(handle).path);
			int instanceIDFromGUID = AssetDatabase.GetInstanceIDFromGUID(guid);
			Selection.activeInstanceID = instanceIDFromGUID;
			EditorGUIUtility.PingObject(instanceIDFromGUID);
		}

		private void SelectAll()
		{
			int[] rowIDs = this.treeView.GetRowIDs();
			this.treeView.SetSelection(rowIDs, false);
			this.TreeViewSelectionChanged(rowIDs);
		}

		private static void ToggleDebugMode()
		{
			SceneHierarchyWindow.s_Debug = !SceneHierarchyWindow.s_Debug;
			SessionState.SetBool("HierarchyWindowDebug", SceneHierarchyWindow.s_Debug);
		}

		public virtual void AddItemsToMenu(GenericMenu menu)
		{
			if (Unsupported.IsDeveloperBuild())
			{
				menu.AddItem(new GUIContent("DEVELOPER/Toggle DebugMode"), false, new GenericMenu.MenuFunction(SceneHierarchyWindow.ToggleDebugMode));
			}
		}

		public void FrameObject(int instanceID, bool ping)
		{
			this.FrameObjectPrivate(instanceID, true, ping, true);
		}

		private void FrameObjectPrivate(int instanceID, bool frame, bool ping, bool animatedFraming)
		{
			if (instanceID == 0)
			{
				return;
			}
			if (this.m_LastFramedID != instanceID)
			{
				this.treeView.EndPing();
			}
			this.SetSearchFilter(string.Empty, SearchableEditorWindow.SearchMode.All, true);
			this.m_LastFramedID = instanceID;
			this.treeView.Frame(instanceID, frame, ping, animatedFraming);
			this.FrameObjectPrivate(InternalEditorUtility.GetGameObjectInstanceIDFromComponent(instanceID), frame, ping, animatedFraming);
		}

		protected virtual void ShowButton(Rect r)
		{
			if (SceneHierarchyWindow.s_Styles == null)
			{
				SceneHierarchyWindow.s_Styles = new SceneHierarchyWindow.Styles();
			}
			this.m_Locked = GUI.Toggle(r, this.m_Locked, GUIContent.none, SceneHierarchyWindow.s_Styles.lockButton);
		}
	}
}
