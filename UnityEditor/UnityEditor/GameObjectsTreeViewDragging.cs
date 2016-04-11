using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
	internal class GameObjectsTreeViewDragging : TreeViewDragging
	{
		private const string kSceneHeaderDragString = "SceneHeaderList";

		public bool allowDragBetween
		{
			get;
			set;
		}

		public GameObjectsTreeViewDragging(TreeView treeView) : base(treeView)
		{
			this.allowDragBetween = false;
		}

		public override bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition)
		{
			return string.IsNullOrEmpty(((GameObjectTreeViewDataSource)this.m_TreeView.data).searchString);
		}

		public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
		{
			DragAndDrop.PrepareStartDrag();
			draggedItemIDs = this.m_TreeView.SortIDsInVisiblityOrder(draggedItemIDs);
			if (!draggedItemIDs.Contains(draggedItem.id))
			{
				draggedItemIDs = new List<int>
				{
					draggedItem.id
				};
			}
			UnityEngine.Object[] dragAndDropObjects = ProjectWindowUtil.GetDragAndDropObjects(draggedItem.id, draggedItemIDs);
			DragAndDrop.objectReferences = dragAndDropObjects;
			List<Scene> draggedScenes = this.GetDraggedScenes(draggedItemIDs);
			if (draggedScenes != null)
			{
				DragAndDrop.SetGenericData("SceneHeaderList", draggedScenes);
				List<string> list = new List<string>();
				foreach (Scene current in draggedScenes)
				{
					if (current.path.Length > 0)
					{
						list.Add(current.path);
					}
				}
				DragAndDrop.paths = list.ToArray();
			}
			else
			{
				DragAndDrop.paths = new string[0];
			}
			string title;
			if (draggedItemIDs.Count > 1)
			{
				title = "<Multiple>";
			}
			else if (dragAndDropObjects.Length == 1)
			{
				title = ObjectNames.GetDragAndDropTitle(dragAndDropObjects[0]);
			}
			else if (draggedScenes != null && draggedScenes.Count == 1)
			{
				title = draggedScenes[0].path;
			}
			else
			{
				title = "Unhandled dragged item";
				Debug.LogError("Unhandled dragged item");
			}
			DragAndDrop.StartDrag(title);
			if (this.m_TreeView.data is GameObjectTreeViewDataSource)
			{
				((GameObjectTreeViewDataSource)this.m_TreeView.data).SetupChildParentReferencesIfNeeded();
			}
		}

		public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, TreeViewDragging.DropPosition dropPos)
		{
			DragAndDropVisualMode dragAndDropVisualMode = this.DoDragScenes(parentItem as GameObjectTreeViewItem, targetItem as GameObjectTreeViewItem, perform, dropPos);
			if (dragAndDropVisualMode != DragAndDropVisualMode.None)
			{
				return dragAndDropVisualMode;
			}
			if (parentItem == null || targetItem == null)
			{
				return InternalEditorUtility.HierarchyWindowDrag(null, perform, InternalEditorUtility.HierarchyDropMode.kHierarchyDropUpon);
			}
			HierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.GameObjects);
			if (this.allowDragBetween)
			{
				if (dropPos == TreeViewDragging.DropPosition.Above || !hierarchyProperty.Find(targetItem.id, null))
				{
					hierarchyProperty = null;
				}
			}
			else if (dropPos == TreeViewDragging.DropPosition.Above || !hierarchyProperty.Find(parentItem.id, null))
			{
				hierarchyProperty = null;
			}
			InternalEditorUtility.HierarchyDropMode hierarchyDropMode = InternalEditorUtility.HierarchyDropMode.kHierarchyDragNormal;
			if (this.allowDragBetween)
			{
				hierarchyDropMode = ((dropPos != TreeViewDragging.DropPosition.Upon) ? InternalEditorUtility.HierarchyDropMode.kHierarchyDropBetween : InternalEditorUtility.HierarchyDropMode.kHierarchyDropUpon);
			}
			if (parentItem != null && parentItem == targetItem && dropPos != TreeViewDragging.DropPosition.Above)
			{
				hierarchyDropMode |= InternalEditorUtility.HierarchyDropMode.kHierarchyDropAfterParent;
			}
			return InternalEditorUtility.HierarchyWindowDrag(hierarchyProperty, perform, hierarchyDropMode);
		}

		public override void DragCleanup(bool revertExpanded)
		{
			DragAndDrop.SetGenericData("SceneHeaderList", null);
			base.DragCleanup(revertExpanded);
		}

		private List<Scene> GetDraggedScenes(List<int> draggedItemIDs)
		{
			List<Scene> list = new List<Scene>();
			foreach (int current in draggedItemIDs)
			{
				Scene sceneByHandle = EditorSceneManager.GetSceneByHandle(current);
				if (!SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(sceneByHandle))
				{
					return null;
				}
				list.Add(sceneByHandle);
			}
			return list;
		}

		private DragAndDropVisualMode DoDragScenes(GameObjectTreeViewItem parentItem, GameObjectTreeViewItem targetItem, bool perform, TreeViewDragging.DropPosition dropPos)
		{
			List<Scene> list = DragAndDrop.GetGenericData("SceneHeaderList") as List<Scene>;
			bool flag = list != null;
			bool flag2 = false;
			if (!flag && DragAndDrop.objectReferences.Length > 0)
			{
				int num = 0;
				UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
				for (int i = 0; i < objectReferences.Length; i++)
				{
					UnityEngine.Object @object = objectReferences[i];
					if (@object is SceneAsset)
					{
						num++;
					}
				}
				flag2 = (num == DragAndDrop.objectReferences.Length);
			}
			if (!flag && !flag2)
			{
				return DragAndDropVisualMode.None;
			}
			if (perform)
			{
				List<Scene> list2 = null;
				if (flag2)
				{
					List<Scene> list3 = new List<Scene>();
					UnityEngine.Object[] objectReferences2 = DragAndDrop.objectReferences;
					for (int j = 0; j < objectReferences2.Length; j++)
					{
						UnityEngine.Object assetObject = objectReferences2[j];
						string assetPath = AssetDatabase.GetAssetPath(assetObject);
						Scene scene = SceneManager.GetSceneByPath(assetPath);
						if (SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(scene))
						{
							this.m_TreeView.Frame(scene.handle, true, true);
						}
						else
						{
							bool alt = Event.current.alt;
							if (alt)
							{
								scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.AdditiveWithoutLoading);
							}
							else
							{
								scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
							}
							if (SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(scene))
							{
								list3.Add(scene);
							}
						}
					}
					if (targetItem != null)
					{
						list2 = list3;
					}
					bool flag3 = SceneManager.sceneCount - list3.Count == 1;
					if (flag3)
					{
						Scene sceneAt = SceneManager.GetSceneAt(0);
						((TreeViewDataSource)this.m_TreeView.data).SetExpanded(sceneAt.handle, true);
					}
					if (list3.Count > 0)
					{
						Selection.instanceIDs = (from x in list3
						select x.handle).ToArray<int>();
						this.m_TreeView.Frame(list3.Last<Scene>().handle, true, false);
					}
				}
				else
				{
					list2 = list;
				}
				if (list2 != null)
				{
					if (targetItem != null)
					{
						Scene scene2 = targetItem.scene;
						if (SceneHierarchyWindow.IsSceneHeaderInHierarchyWindow(scene2))
						{
							if (!targetItem.isSceneHeader || dropPos == TreeViewDragging.DropPosition.Upon)
							{
								dropPos = TreeViewDragging.DropPosition.Below;
							}
							if (dropPos == TreeViewDragging.DropPosition.Above)
							{
								for (int k = 0; k < list2.Count; k++)
								{
									EditorSceneManager.MoveSceneBefore(list2[k], scene2);
								}
							}
							else if (dropPos == TreeViewDragging.DropPosition.Below)
							{
								for (int l = list2.Count - 1; l >= 0; l--)
								{
									EditorSceneManager.MoveSceneAfter(list2[l], scene2);
								}
							}
						}
					}
					else
					{
						Scene sceneAt2 = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
						for (int m = list2.Count - 1; m >= 0; m--)
						{
							EditorSceneManager.MoveSceneAfter(list2[m], sceneAt2);
						}
					}
				}
			}
			return DragAndDropVisualMode.Move;
		}
	}
}
