using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
	[Serializable]
	internal class AnimationWindowState : ScriptableObject, IPlayHead
	{
		public enum RefreshType
		{
			None,
			CurvesOnly,
			Everything
		}

		[SerializeField]
		public AnimationWindowHierarchyState hierarchyState;

		[SerializeField]
		public AnimEditor animEditor;

		[SerializeField]
		public bool showCurveEditor;

		[SerializeField]
		private float m_CurrentTime;

		[SerializeField]
		private TimeArea m_timeArea;

		[SerializeField]
		private AnimationClip m_ActiveAnimationClip;

		[SerializeField]
		private GameObject m_ActiveGameObject;

		[SerializeField]
		private HashSet<int> m_SelectedKeyHashes;

		[SerializeField]
		private int m_ActiveKeyframeHash;

		[SerializeField]
		private bool m_Locked;

		private static List<AnimationWindowKeyframe> s_KeyframeClipboard;

		[NonSerialized]
		public Action onClipSelectionChanged;

		[NonSerialized]
		public AnimationWindowHierarchyDataSource hierarchyData;

		[NonSerialized]
		public bool m_FrameCurveEditor;

		private List<AnimationWindowCurve> m_AllCurvesCache;

		private List<AnimationWindowCurve> m_ActiveCurvesCache;

		private List<DopeLine> m_dopelinesCache;

		private List<AnimationWindowKeyframe> m_SelectedKeysCache;

		private List<CurveWrapper> m_ActiveCurveWrappersCache;

		private AnimationWindowKeyframe m_ActiveKeyframeCache;

		private HashSet<int> m_ModifiedCurves = new HashSet<int>();

		private EditorCurveBinding? m_lastAddedCurveBinding;

		private int m_PreviousRefreshHash;

		private GameObject m_PreviousActiveRootGameObject;

		private AnimationWindowState.RefreshType m_Refresh;

		public Action<float> onFrameRateChange;

		public AnimationWindowState.RefreshType refresh
		{
			get
			{
				return this.m_Refresh;
			}
			set
			{
				if (this.m_Refresh < value)
				{
					this.m_Refresh = value;
				}
			}
		}

		public List<AnimationWindowCurve> allCurves
		{
			get
			{
				if (this.m_AllCurvesCache == null)
				{
					this.m_AllCurvesCache = new List<AnimationWindowCurve>();
					if (this.activeAnimationClip != null)
					{
						EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(this.activeAnimationClip);
						EditorCurveBinding[] objectReferenceCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(this.activeAnimationClip);
						EditorCurveBinding[] array = curveBindings;
						for (int i = 0; i < array.Length; i++)
						{
							EditorCurveBinding editorCurveBinding = array[i];
							if (AnimationWindowUtility.ShouldShowAnimationWindowCurve(editorCurveBinding))
							{
								this.m_AllCurvesCache.Add(new AnimationWindowCurve(this.activeAnimationClip, editorCurveBinding, CurveBindingUtility.GetEditorCurveValueType(this.activeRootGameObject, editorCurveBinding)));
							}
						}
						EditorCurveBinding[] array2 = objectReferenceCurveBindings;
						for (int j = 0; j < array2.Length; j++)
						{
							EditorCurveBinding editorCurveBinding2 = array2[j];
							this.m_AllCurvesCache.Add(new AnimationWindowCurve(this.activeAnimationClip, editorCurveBinding2, CurveBindingUtility.GetEditorCurveValueType(this.activeRootGameObject, editorCurveBinding2)));
						}
						this.m_AllCurvesCache.Sort();
					}
				}
				return this.m_AllCurvesCache;
			}
		}

		public List<AnimationWindowCurve> activeCurves
		{
			get
			{
				if (this.m_ActiveCurvesCache == null)
				{
					this.m_ActiveCurvesCache = new List<AnimationWindowCurve>();
					if (this.hierarchyState != null && this.hierarchyData != null)
					{
						foreach (int current in this.hierarchyState.selectedIDs)
						{
							TreeViewItem treeViewItem = this.hierarchyData.FindItem(current);
							AnimationWindowHierarchyNode animationWindowHierarchyNode = treeViewItem as AnimationWindowHierarchyNode;
							if (animationWindowHierarchyNode != null)
							{
								List<AnimationWindowCurve> curves = this.GetCurves(animationWindowHierarchyNode, true);
								foreach (AnimationWindowCurve current2 in curves)
								{
									if (!this.m_ActiveCurvesCache.Contains(current2))
									{
										this.m_ActiveCurvesCache.Add(current2);
									}
								}
							}
						}
					}
				}
				return this.m_ActiveCurvesCache;
			}
		}

		public List<CurveWrapper> activeCurveWrappers
		{
			get
			{
				if (this.m_ActiveCurveWrappersCache == null || this.m_ActiveCurvesCache == null)
				{
					this.m_ActiveCurveWrappersCache = new List<CurveWrapper>();
					foreach (AnimationWindowCurve current in this.activeCurves)
					{
						if (!current.isPPtrCurve)
						{
							this.m_ActiveCurveWrappersCache.Add(AnimationWindowUtility.GetCurveWrapper(current, this.activeAnimationClip));
						}
					}
					if (!this.m_ActiveCurveWrappersCache.Any<CurveWrapper>())
					{
						foreach (AnimationWindowCurve current2 in this.allCurves)
						{
							if (!current2.isPPtrCurve)
							{
								this.m_ActiveCurveWrappersCache.Add(AnimationWindowUtility.GetCurveWrapper(current2, this.activeAnimationClip));
							}
						}
					}
				}
				return this.m_ActiveCurveWrappersCache;
			}
		}

		public List<DopeLine> dopelines
		{
			get
			{
				if (this.m_dopelinesCache == null)
				{
					this.m_dopelinesCache = new List<DopeLine>();
					if (this.hierarchyData != null)
					{
						foreach (TreeViewItem current in this.hierarchyData.GetRows())
						{
							AnimationWindowHierarchyNode animationWindowHierarchyNode = current as AnimationWindowHierarchyNode;
							if (animationWindowHierarchyNode != null && !(animationWindowHierarchyNode is AnimationWindowHierarchyAddButtonNode))
							{
								List<AnimationWindowCurve> list;
								if (current is AnimationWindowHierarchyMasterNode)
								{
									list = this.allCurves;
								}
								else
								{
									list = this.GetCurves(animationWindowHierarchyNode, true);
								}
								DopeLine dopeLine = new DopeLine(current.id, list.ToArray());
								dopeLine.tallMode = this.hierarchyState.GetTallMode(animationWindowHierarchyNode);
								dopeLine.objectType = animationWindowHierarchyNode.animatableObjectType;
								dopeLine.hasChildren = !(animationWindowHierarchyNode is AnimationWindowHierarchyPropertyNode);
								dopeLine.isMasterDopeline = (current is AnimationWindowHierarchyMasterNode);
								this.m_dopelinesCache.Add(dopeLine);
							}
						}
					}
				}
				return this.m_dopelinesCache;
			}
		}

		public List<AnimationWindowHierarchyNode> selectedHierarchyNodes
		{
			get
			{
				List<AnimationWindowHierarchyNode> list = new List<AnimationWindowHierarchyNode>();
				if (this.hierarchyData != null)
				{
					foreach (int current in this.hierarchyState.selectedIDs)
					{
						AnimationWindowHierarchyNode animationWindowHierarchyNode = (AnimationWindowHierarchyNode)this.hierarchyData.FindItem(current);
						if (animationWindowHierarchyNode != null && !(animationWindowHierarchyNode is AnimationWindowHierarchyAddButtonNode))
						{
							list.Add(animationWindowHierarchyNode);
						}
					}
				}
				return list;
			}
		}

		public AnimationWindowKeyframe activeKeyframe
		{
			get
			{
				if (this.m_ActiveKeyframeCache == null)
				{
					foreach (AnimationWindowCurve current in this.allCurves)
					{
						foreach (AnimationWindowKeyframe current2 in current.m_Keyframes)
						{
							if (current2.GetHash() == this.m_ActiveKeyframeHash)
							{
								this.m_ActiveKeyframeCache = current2;
							}
						}
					}
				}
				return this.m_ActiveKeyframeCache;
			}
			set
			{
				this.m_ActiveKeyframeCache = null;
				this.m_ActiveKeyframeHash = ((value == null) ? 0 : value.GetHash());
			}
		}

		public List<AnimationWindowKeyframe> selectedKeys
		{
			get
			{
				if (this.m_SelectedKeysCache == null)
				{
					this.m_SelectedKeysCache = new List<AnimationWindowKeyframe>();
					foreach (AnimationWindowCurve current in this.allCurves)
					{
						foreach (AnimationWindowKeyframe current2 in current.m_Keyframes)
						{
							if (this.KeyIsSelected(current2))
							{
								this.m_SelectedKeysCache.Add(current2);
							}
						}
					}
				}
				return this.m_SelectedKeysCache;
			}
		}

		private HashSet<int> selectedKeyHashes
		{
			get
			{
				HashSet<int> arg_1B_0;
				if ((arg_1B_0 = this.m_SelectedKeyHashes) == null)
				{
					arg_1B_0 = (this.m_SelectedKeyHashes = new HashSet<int>());
				}
				return arg_1B_0;
			}
			set
			{
				this.m_SelectedKeyHashes = value;
			}
		}

		public AnimationClip activeAnimationClip
		{
			get
			{
				return this.m_ActiveAnimationClip;
			}
			set
			{
				if (this.m_ActiveAnimationClip != value && !this.m_Locked)
				{
					this.m_ActiveAnimationClip = value;
					if (this.onFrameRateChange != null)
					{
						this.onFrameRateChange(this.frameRate);
					}
					CurveBindingUtility.Cleanup();
					if (this.onClipSelectionChanged != null)
					{
						this.onClipSelectionChanged();
					}
				}
			}
		}

		public GameObject activeGameObject
		{
			get
			{
				if (!this.m_Locked && Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject))
				{
					this.m_ActiveGameObject = Selection.activeGameObject;
				}
				return this.m_ActiveGameObject;
			}
		}

		public GameObject activeRootGameObject
		{
			get
			{
				if (this.activeGameObject != null)
				{
					if (this.activeObjectIsPrefab)
					{
						return null;
					}
					Component closestAnimationPlayerComponentInParents = AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(this.activeGameObject.transform);
					if (closestAnimationPlayerComponentInParents != null)
					{
						return closestAnimationPlayerComponentInParents.gameObject;
					}
				}
				return null;
			}
		}

		public Component activeAnimationPlayer
		{
			get
			{
				if (this.activeGameObject != null)
				{
					return AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(this.activeGameObject.transform);
				}
				return null;
			}
		}

		public bool disabled
		{
			get
			{
				return !this.activeRootGameObject || !this.activeAnimationClip;
			}
		}

		public bool animationIsReadOnly
		{
			get
			{
				return !this.activeAnimationClip || (this.m_ActiveAnimationClip.hideFlags & HideFlags.NotEditable) != HideFlags.None || !this.animationIsEditable;
			}
		}

		public bool animationIsEditable
		{
			get
			{
				return this.activeGameObject && (!this.activeAnimationClip || (this.activeAnimationClip.hideFlags & HideFlags.NotEditable) == HideFlags.None) && !this.activeObjectIsPrefab;
			}
		}

		public bool clipIsEditable
		{
			get
			{
				return this.activeAnimationClip && (this.activeAnimationClip.hideFlags & HideFlags.NotEditable) == HideFlags.None && AssetDatabase.IsOpenForEdit(this.activeAnimationClip);
			}
		}

		public bool activeObjectIsPrefab
		{
			get
			{
				return this.activeGameObject && (EditorUtility.IsPersistent(this.activeGameObject) || (this.activeGameObject.hideFlags & HideFlags.NotEditable) != HideFlags.None);
			}
		}

		public bool animatorIsOptimized
		{
			get
			{
				if (!this.activeRootGameObject)
				{
					return false;
				}
				Animator component = this.activeRootGameObject.GetComponent<Animator>();
				return component != null && component.isOptimizable && !component.hasTransformHierarchy;
			}
		}

		public bool clipOnlyMode
		{
			get
			{
				return this.activeRootGameObject == null && this.activeAnimationClip != null;
			}
		}

		public bool locked
		{
			get
			{
				return this.m_Locked;
			}
			set
			{
				if (!this.disabled)
				{
					this.m_Locked = value;
					if (this.m_ActiveGameObject != Selection.activeGameObject)
					{
						this.OnSelectionChange();
					}
				}
			}
		}

		public float frameRate
		{
			get
			{
				if (this.activeAnimationClip == null)
				{
					return 60f;
				}
				return this.activeAnimationClip.frameRate;
			}
			set
			{
				if (this.activeAnimationClip != null && value > 0f && value <= 10000f)
				{
					foreach (AnimationWindowCurve current in this.allCurves)
					{
						foreach (AnimationWindowKeyframe current2 in current.m_Keyframes)
						{
							int frame = AnimationKeyTime.Time(current2.time, this.frameRate).frame;
							current2.time = AnimationKeyTime.Frame(frame, value).time;
						}
						this.SaveCurve(current);
					}
					AnimationEvent[] animationEvents = AnimationUtility.GetAnimationEvents(this.m_ActiveAnimationClip);
					AnimationEvent[] array = animationEvents;
					for (int i = 0; i < array.Length; i++)
					{
						AnimationEvent animationEvent = array[i];
						int frame2 = AnimationKeyTime.Time(animationEvent.time, this.frameRate).frame;
						animationEvent.time = AnimationKeyTime.Frame(frame2, value).time;
					}
					AnimationUtility.SetAnimationEvents(this.m_ActiveAnimationClip, animationEvents);
					this.m_ActiveAnimationClip.frameRate = value;
					if (this.onFrameRateChange != null)
					{
						this.onFrameRateChange(this.frameRate);
					}
				}
			}
		}

		public int frame
		{
			get
			{
				return this.TimeToFrameFloor(this.currentTime);
			}
			set
			{
				this.currentTime = this.FrameToTime((float)value);
			}
		}

		public float currentTime
		{
			get
			{
				return this.m_CurrentTime;
			}
			set
			{
				if (!Mathf.Approximately(this.m_CurrentTime, value))
				{
					this.m_CurrentTime = value;
					this.ResampleAnimation();
				}
			}
		}

		public AnimationKeyTime time
		{
			get
			{
				return AnimationKeyTime.Frame(this.frame, this.frameRate);
			}
		}

		public bool playing
		{
			get
			{
				return AnimationMode.InAnimationPlaybackMode();
			}
			set
			{
				if (value && !AnimationMode.InAnimationPlaybackMode())
				{
					AnimationMode.StartAnimationPlaybackMode();
					this.recording = true;
				}
				if (!value && AnimationMode.InAnimationPlaybackMode())
				{
					AnimationMode.StopAnimationPlaybackMode();
					this.currentTime = this.FrameToTime((float)this.frame);
				}
			}
		}

		public bool recording
		{
			get
			{
				return AnimationMode.InAnimationMode();
			}
			set
			{
				if (value && !AnimationMode.InAnimationMode())
				{
					AnimationMode.StartAnimationMode();
					Undo.postprocessModifications = (Undo.PostprocessModifications)Delegate.Combine(Undo.postprocessModifications, new Undo.PostprocessModifications(this.PostprocessAnimationRecordingModifications));
				}
				else if (!value)
				{
					AnimationMode.StopAnimationMode();
					Undo.postprocessModifications = (Undo.PostprocessModifications)Delegate.Remove(Undo.postprocessModifications, new Undo.PostprocessModifications(this.PostprocessAnimationRecordingModifications));
				}
			}
		}

		public TimeArea timeArea
		{
			get
			{
				return this.m_timeArea;
			}
			set
			{
				this.m_timeArea = value;
			}
		}

		public float pixelPerSecond
		{
			get
			{
				return this.timeArea.m_Scale.x;
			}
		}

		public float zeroTimePixel
		{
			get
			{
				return this.timeArea.shownArea.xMin * this.timeArea.m_Scale.x * -1f;
			}
		}

		public float minVisibleTime
		{
			get
			{
				return this.m_timeArea.shownArea.xMin;
			}
		}

		public float maxVisibleTime
		{
			get
			{
				return this.m_timeArea.shownArea.xMax;
			}
		}

		public float visibleTimeSpan
		{
			get
			{
				return this.maxVisibleTime - this.minVisibleTime;
			}
		}

		public float minVisibleFrame
		{
			get
			{
				return this.minVisibleTime * this.frameRate;
			}
		}

		public float maxVisibleFrame
		{
			get
			{
				return this.maxVisibleTime * this.frameRate;
			}
		}

		public float visibleFrameSpan
		{
			get
			{
				return this.visibleTimeSpan * this.frameRate;
			}
		}

		public float minTime
		{
			get
			{
				return (!(this.m_ActiveAnimationClip != null)) ? 0f : this.m_ActiveAnimationClip.startTime;
			}
		}

		public float maxTime
		{
			get
			{
				return (!(this.m_ActiveAnimationClip != null) || this.m_ActiveAnimationClip.stopTime <= 0f) ? 1f : this.m_ActiveAnimationClip.stopTime;
			}
		}

		public int minFrame
		{
			get
			{
				return this.TimeToFrameRound(this.minTime);
			}
		}

		public int maxFrame
		{
			get
			{
				return this.TimeToFrameRound(this.maxTime);
			}
		}

		public void OnGUI()
		{
			this.RefreshHashCheck();
			this.Refresh();
		}

		private void RefreshHashCheck()
		{
			int refreshHash = this.GetRefreshHash();
			if (this.m_PreviousRefreshHash != refreshHash)
			{
				this.refresh = AnimationWindowState.RefreshType.Everything;
				this.m_PreviousRefreshHash = refreshHash;
			}
		}

		private void Refresh()
		{
			if (this.refresh == AnimationWindowState.RefreshType.Everything)
			{
				CurveRendererCache.ClearCurveRendererCache();
				this.m_ActiveKeyframeCache = null;
				this.m_AllCurvesCache = null;
				this.m_ActiveCurvesCache = null;
				this.m_dopelinesCache = null;
				this.m_SelectedKeysCache = null;
				this.m_ActiveCurveWrappersCache = null;
				if (this.hierarchyData != null)
				{
					this.hierarchyData.UpdateData();
				}
				EditorCurveBinding? lastAddedCurveBinding = this.m_lastAddedCurveBinding;
				if (lastAddedCurveBinding.HasValue)
				{
					EditorCurveBinding? lastAddedCurveBinding2 = this.m_lastAddedCurveBinding;
					this.OnNewCurveAdded(lastAddedCurveBinding2.Value);
				}
				if (this.activeCurves.Count == 0 && this.dopelines.Count > 0)
				{
					this.SelectHierarchyItem(this.dopelines[0], false, false);
				}
				this.m_Refresh = AnimationWindowState.RefreshType.None;
			}
			else if (this.refresh == AnimationWindowState.RefreshType.CurvesOnly)
			{
				CurveRendererCache.ClearCurveRendererCache();
				this.m_ActiveKeyframeCache = null;
				this.m_ActiveCurvesCache = null;
				this.m_ActiveCurveWrappersCache = null;
				this.m_SelectedKeysCache = null;
				this.ReloadModifiedAnimationCurveCache();
				this.ReloadModifiedDopelineCache();
				this.m_Refresh = AnimationWindowState.RefreshType.None;
				this.m_ModifiedCurves.Clear();
			}
			if (this.disabled && this.recording)
			{
				this.recording = false;
			}
		}

		private int GetRefreshHash()
		{
			return ((!(this.activeAnimationClip != null)) ? 0 : this.activeAnimationClip.GetHashCode()) ^ ((!(this.activeRootGameObject != null)) ? 0 : this.activeRootGameObject.GetHashCode()) ^ ((this.hierarchyState == null) ? 0 : this.hierarchyState.expandedIDs.Count) ^ ((this.hierarchyState == null) ? 0 : this.hierarchyState.GetTallInstancesCount()) ^ ((!this.showCurveEditor) ? 0 : 1);
		}

		public void OnSelectionChange()
		{
			if (this.m_Locked)
			{
				return;
			}
			AnimationClip[] array = new AnimationClip[0];
			if (this.activeRootGameObject != null && !(Selection.activeObject is AnimationClip))
			{
				array = AnimationUtility.GetAnimationClips(this.activeRootGameObject);
				if (this.activeAnimationClip == null && this.activeGameObject != null)
				{
					this.activeAnimationClip = ((array.Length <= 0) ? null : array[0]);
				}
				else if (!Array.Exists<AnimationClip>(array, (AnimationClip x) => x == this.activeAnimationClip))
				{
					this.activeAnimationClip = ((array.Length <= 0) ? null : array[0]);
				}
			}
			else if (this.activeRootGameObject == null)
			{
				this.m_ActiveAnimationClip = null;
				this.onFrameRateChange(this.frameRate);
			}
			if (this.m_PreviousActiveRootGameObject != this.activeRootGameObject)
			{
				this.recording = false;
			}
			this.m_PreviousActiveRootGameObject = this.activeRootGameObject;
		}

		public void OnControllerChange()
		{
			AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(this.activeRootGameObject);
			bool flag = animationClips != null && animationClips.Length > 0;
			this.activeAnimationClip = ((!flag) ? null : animationClips[0]);
			this.refresh = AnimationWindowState.RefreshType.Everything;
		}

		public void OnEnable()
		{
			base.hideFlags = HideFlags.HideAndDontSave;
			AnimationUtility.onCurveWasModified = (AnimationUtility.OnCurveWasModified)Delegate.Combine(AnimationUtility.onCurveWasModified, new AnimationUtility.OnCurveWasModified(this.CurveWasModified));
			Undo.undoRedoPerformed = (Undo.UndoRedoCallback)Delegate.Combine(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.UndoRedoPerformed));
		}

		public void OnDisable()
		{
			CurveBindingUtility.Cleanup();
			this.recording = false;
			this.playing = false;
			AnimationUtility.onCurveWasModified = (AnimationUtility.OnCurveWasModified)Delegate.Remove(AnimationUtility.onCurveWasModified, new AnimationUtility.OnCurveWasModified(this.CurveWasModified));
			Undo.undoRedoPerformed = (Undo.UndoRedoCallback)Delegate.Remove(Undo.undoRedoPerformed, new Undo.UndoRedoCallback(this.UndoRedoPerformed));
		}

		public void UndoRedoPerformed()
		{
			this.refresh = AnimationWindowState.RefreshType.Everything;
		}

		private void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
		{
			if (clip != this.activeAnimationClip)
			{
				return;
			}
			if (type == AnimationUtility.CurveModifiedType.CurveModified)
			{
				bool flag = false;
				int hashCode = binding.GetHashCode();
				foreach (AnimationWindowCurve current in this.allCurves)
				{
					int hashCode2 = current.binding.GetHashCode();
					if (hashCode2 == hashCode)
					{
						this.m_ModifiedCurves.Add(hashCode2);
						flag = true;
					}
				}
				if (flag)
				{
					this.refresh = AnimationWindowState.RefreshType.CurvesOnly;
				}
				else
				{
					this.m_lastAddedCurveBinding = new EditorCurveBinding?(binding);
					this.refresh = AnimationWindowState.RefreshType.Everything;
				}
			}
			else
			{
				this.refresh = AnimationWindowState.RefreshType.Everything;
			}
		}

		public void SaveCurve(AnimationWindowCurve curve)
		{
			curve.m_Keyframes.Sort((AnimationWindowKeyframe a, AnimationWindowKeyframe b) => a.time.CompareTo(b.time));
			Undo.RegisterCompleteObjectUndo(this.activeAnimationClip, "Edit Curve");
			if (curve.isPPtrCurve)
			{
				ObjectReferenceKeyframe[] array = curve.ToObjectCurve();
				if (array.Length == 0)
				{
					array = null;
				}
				AnimationUtility.SetObjectReferenceCurve(this.activeAnimationClip, curve.binding, array);
			}
			else
			{
				AnimationCurve animationCurve = curve.ToAnimationCurve();
				if (animationCurve.keys.Length == 0)
				{
					animationCurve = null;
				}
				else
				{
					QuaternionCurveTangentCalculation.UpdateTangentsFromMode(animationCurve, this.activeAnimationClip, curve.binding);
				}
				AnimationUtility.SetEditorCurve(this.activeAnimationClip, curve.binding, animationCurve);
			}
			this.Repaint();
		}

		public void SaveSelectedKeys(List<AnimationWindowKeyframe> currentSelectedKeys)
		{
			List<AnimationWindowCurve> list = new List<AnimationWindowCurve>();
			foreach (AnimationWindowKeyframe current in currentSelectedKeys)
			{
				if (!list.Contains(current.curve))
				{
					list.Add(current.curve);
				}
				List<AnimationWindowKeyframe> list2 = new List<AnimationWindowKeyframe>();
				foreach (AnimationWindowKeyframe current2 in current.curve.m_Keyframes)
				{
					if (!currentSelectedKeys.Contains(current2) && AnimationKeyTime.Time(current.time, this.frameRate).frame == AnimationKeyTime.Time(current2.time, this.frameRate).frame)
					{
						list2.Add(current2);
					}
				}
				foreach (AnimationWindowKeyframe current3 in list2)
				{
					current.curve.m_Keyframes.Remove(current3);
				}
			}
			foreach (AnimationWindowCurve current4 in list)
			{
				this.SaveCurve(current4);
			}
		}

		public void RemoveCurve(AnimationWindowCurve curve)
		{
			Undo.RegisterCompleteObjectUndo(this.activeAnimationClip, "Remove Curve");
			if (curve.isPPtrCurve)
			{
				AnimationUtility.SetObjectReferenceCurve(this.activeAnimationClip, curve.binding, null);
			}
			else
			{
				AnimationUtility.SetEditorCurve(this.activeAnimationClip, curve.binding, null);
			}
		}

		public bool AnyKeyIsSelected(DopeLine dopeline)
		{
			foreach (AnimationWindowKeyframe current in dopeline.keys)
			{
				if (this.KeyIsSelected(current))
				{
					return true;
				}
			}
			return false;
		}

		public bool KeyIsSelected(AnimationWindowKeyframe keyframe)
		{
			return this.selectedKeyHashes.Contains(keyframe.GetHash());
		}

		public void SelectKey(AnimationWindowKeyframe keyframe)
		{
			int hash = keyframe.GetHash();
			if (!this.selectedKeyHashes.Contains(hash))
			{
				this.selectedKeyHashes.Add(hash);
			}
			this.m_SelectedKeysCache = null;
		}

		public void SelectKeysFromDopeline(DopeLine dopeline)
		{
			if (dopeline == null)
			{
				return;
			}
			foreach (AnimationWindowKeyframe current in dopeline.keys)
			{
				this.SelectKey(current);
			}
		}

		public void UnselectKey(AnimationWindowKeyframe keyframe)
		{
			int hash = keyframe.GetHash();
			if (this.selectedKeyHashes.Contains(hash))
			{
				this.selectedKeyHashes.Remove(hash);
			}
			this.m_SelectedKeysCache = null;
		}

		public void UnselectKeysFromDopeline(DopeLine dopeline)
		{
			if (dopeline == null)
			{
				return;
			}
			foreach (AnimationWindowKeyframe current in dopeline.keys)
			{
				this.UnselectKey(current);
			}
		}

		public void DeleteSelectedKeys()
		{
			if (this.selectedKeys.Count == 0)
			{
				return;
			}
			foreach (AnimationWindowKeyframe current in this.selectedKeys)
			{
				this.UnselectKey(current);
				current.curve.m_Keyframes.Remove(current);
				this.SaveCurve(current.curve);
			}
		}

		public void MoveSelectedKeys(float deltaTime)
		{
			this.MoveSelectedKeys(deltaTime, false);
		}

		public void MoveSelectedKeys(float deltaTime, bool snapToFrame)
		{
			this.MoveSelectedKeys(deltaTime, snapToFrame, true);
		}

		public void MoveSelectedKeys(float deltaTime, bool snapToFrame, bool saveToClip)
		{
			List<AnimationWindowKeyframe> list = new List<AnimationWindowKeyframe>(this.selectedKeys);
			foreach (AnimationWindowKeyframe current in list)
			{
				current.time += deltaTime;
				if (snapToFrame)
				{
					current.time = this.SnapToFrame(current.time, !saveToClip);
				}
			}
			if (saveToClip)
			{
				this.SaveSelectedKeys(list);
			}
			this.ClearKeySelections();
			foreach (AnimationWindowKeyframe current2 in list)
			{
				this.SelectKey(current2);
			}
		}

		public void CopyKeys()
		{
			if (AnimationWindowState.s_KeyframeClipboard == null)
			{
				AnimationWindowState.s_KeyframeClipboard = new List<AnimationWindowKeyframe>();
			}
			float num = 3.40282347E+38f;
			AnimationWindowState.s_KeyframeClipboard.Clear();
			foreach (AnimationWindowKeyframe current in this.selectedKeys)
			{
				AnimationWindowState.s_KeyframeClipboard.Add(new AnimationWindowKeyframe(current));
				if (current.time < num)
				{
					num = current.time;
				}
			}
			if (AnimationWindowState.s_KeyframeClipboard.Count > 0)
			{
				foreach (AnimationWindowKeyframe current2 in AnimationWindowState.s_KeyframeClipboard)
				{
					current2.time -= num;
				}
			}
			else
			{
				this.CopyAllActiveCurves();
			}
		}

		public void CopyAllActiveCurves()
		{
			foreach (AnimationWindowCurve current in this.activeCurves)
			{
				foreach (AnimationWindowKeyframe current2 in current.m_Keyframes)
				{
					AnimationWindowState.s_KeyframeClipboard.Add(new AnimationWindowKeyframe(current2));
				}
			}
		}

		public void PasteKeys()
		{
			if (AnimationWindowState.s_KeyframeClipboard == null)
			{
				AnimationWindowState.s_KeyframeClipboard = new List<AnimationWindowKeyframe>();
			}
			HashSet<int> selectedKeyHashes = new HashSet<int>(this.m_SelectedKeyHashes);
			this.ClearSelections();
			AnimationWindowCurve animationWindowCurve = null;
			AnimationWindowCurve animationWindowCurve2 = null;
			float startTime = 0f;
			List<AnimationWindowCurve> list = new List<AnimationWindowCurve>();
			foreach (AnimationWindowKeyframe current in AnimationWindowState.s_KeyframeClipboard)
			{
				if (!list.Any<AnimationWindowCurve>() || list.Last<AnimationWindowCurve>() != current.curve)
				{
					list.Add(current.curve);
				}
			}
			bool flag = list.Count<AnimationWindowCurve>() == this.activeCurves.Count<AnimationWindowCurve>();
			int num = 0;
			foreach (AnimationWindowKeyframe current2 in AnimationWindowState.s_KeyframeClipboard)
			{
				if (animationWindowCurve2 != null && current2.curve != animationWindowCurve2)
				{
					num++;
				}
				AnimationWindowKeyframe animationWindowKeyframe = new AnimationWindowKeyframe(current2);
				if (flag)
				{
					animationWindowKeyframe.curve = this.activeCurves[num];
				}
				else
				{
					animationWindowKeyframe.curve = AnimationWindowUtility.BestMatchForPaste(animationWindowKeyframe.curve.binding, this.activeCurves);
				}
				if (animationWindowKeyframe.curve == null)
				{
					animationWindowKeyframe.curve = new AnimationWindowCurve(this.activeAnimationClip, current2.curve.binding, current2.curve.type);
					animationWindowKeyframe.time = current2.time;
				}
				animationWindowKeyframe.time += this.time.time;
				if (animationWindowKeyframe.curve != null)
				{
					if (animationWindowKeyframe.curve.HasKeyframe(AnimationKeyTime.Time(animationWindowKeyframe.time, this.frameRate)))
					{
						animationWindowKeyframe.curve.RemoveKeyframe(AnimationKeyTime.Time(animationWindowKeyframe.time, this.frameRate));
					}
					if (animationWindowCurve == animationWindowKeyframe.curve)
					{
						animationWindowKeyframe.curve.RemoveKeysAtRange(startTime, animationWindowKeyframe.time);
					}
					animationWindowKeyframe.curve.m_Keyframes.Add(animationWindowKeyframe);
					this.SelectKey(animationWindowKeyframe);
					this.SaveCurve(animationWindowKeyframe.curve);
					animationWindowCurve = animationWindowKeyframe.curve;
					startTime = animationWindowKeyframe.time;
				}
				animationWindowCurve2 = current2.curve;
			}
			if (this.m_SelectedKeyHashes.Count == 0)
			{
				this.m_SelectedKeyHashes = selectedKeyHashes;
			}
			else
			{
				this.ResampleAnimation();
			}
		}

		public void ClearSelections()
		{
			this.ClearKeySelections();
			this.ClearHierarchySelection();
		}

		public void ClearKeySelections()
		{
			this.selectedKeyHashes.Clear();
			this.m_SelectedKeysCache = null;
		}

		public void ClearHierarchySelection()
		{
			this.hierarchyState.selectedIDs.Clear();
		}

		private void ReloadModifiedDopelineCache()
		{
			if (this.m_dopelinesCache == null)
			{
				return;
			}
			foreach (DopeLine current in this.m_dopelinesCache)
			{
				AnimationWindowCurve[] curves = current.m_Curves;
				for (int i = 0; i < curves.Length; i++)
				{
					AnimationWindowCurve animationWindowCurve = curves[i];
					if (this.m_ModifiedCurves.Contains(animationWindowCurve.binding.GetHashCode()))
					{
						current.LoadKeyframes();
					}
				}
			}
		}

		private void ReloadModifiedAnimationCurveCache()
		{
			if (this.m_AllCurvesCache == null)
			{
				return;
			}
			foreach (AnimationWindowCurve current in this.m_AllCurvesCache)
			{
				if (this.m_ModifiedCurves.Contains(current.binding.GetHashCode()))
				{
					current.LoadKeyframes(this.activeAnimationClip);
				}
			}
		}

		public void ResampleAnimation()
		{
			if (this.activeAnimationClip == null)
			{
				return;
			}
			if (!this.recording)
			{
				this.recording = true;
			}
			Undo.FlushUndoRecordObjects();
			AnimationMode.BeginSampling();
			CurveBindingUtility.SampleAnimationClip(this.activeRootGameObject, this.activeAnimationClip, this.currentTime);
			AnimationMode.EndSampling();
			SceneView.RepaintAll();
			ParticleSystemWindow instance = ParticleSystemWindow.GetInstance();
			if (instance)
			{
				instance.Repaint();
			}
		}

		private void OnNewCurveAdded(EditorCurveBinding newCurve)
		{
			string propertyGroupName = AnimationWindowUtility.GetPropertyGroupName(newCurve.propertyName);
			int propertyNodeID = AnimationWindowUtility.GetPropertyNodeID(newCurve.path, newCurve.type, propertyGroupName);
			this.SelectHierarchyItem(propertyNodeID, false, false);
			if (newCurve.isPPtrCurve)
			{
				this.hierarchyState.AddTallInstance(propertyNodeID);
			}
			this.m_lastAddedCurveBinding = null;
		}

		public void Repaint()
		{
			if (this.animEditor != null)
			{
				this.animEditor.Repaint();
			}
		}

		public List<AnimationWindowCurve> GetCurves(AnimationWindowHierarchyNode hierarchyNode, bool entireHierarchy)
		{
			return AnimationWindowUtility.FilterCurves(this.allCurves.ToArray(), hierarchyNode.path, hierarchyNode.animatableObjectType, hierarchyNode.propertyName);
		}

		public List<AnimationWindowKeyframe> GetAggregateKeys(AnimationWindowHierarchyNode hierarchyNode)
		{
			DopeLine dopeLine = this.dopelines.FirstOrDefault((DopeLine e) => e.m_HierarchyNodeID == hierarchyNode.id);
			if (dopeLine == null)
			{
				return null;
			}
			return dopeLine.keys;
		}

		public void OnHierarchySelectionChanged(int[] selectedInstanceIDs)
		{
			this.HandleHierarchySelectionChanged(selectedInstanceIDs, true);
			foreach (DopeLine current in this.dopelines)
			{
				bool flag = selectedInstanceIDs.Contains(current.m_HierarchyNodeID);
				if (flag)
				{
					this.SelectKeysFromDopeline(current);
				}
				else
				{
					this.UnselectKeysFromDopeline(current);
				}
			}
		}

		public void HandleHierarchySelectionChanged(int[] selectedInstanceIDs, bool triggerSceneSelectionSync)
		{
			this.m_ActiveCurvesCache = null;
			this.m_FrameCurveEditor = true;
			if (triggerSceneSelectionSync)
			{
				this.SyncSceneSelection(selectedInstanceIDs);
			}
		}

		public void SelectHierarchyItem(DopeLine dopeline, bool additive)
		{
			this.SelectHierarchyItem(dopeline.m_HierarchyNodeID, additive, true);
		}

		public void SelectHierarchyItem(DopeLine dopeline, bool additive, bool triggerSceneSelectionSync)
		{
			this.SelectHierarchyItem(dopeline.m_HierarchyNodeID, additive, triggerSceneSelectionSync);
		}

		public void SelectHierarchyItem(int hierarchyNodeID, bool additive, bool triggerSceneSelectionSync)
		{
			if (!additive)
			{
				this.ClearHierarchySelection();
			}
			this.hierarchyState.selectedIDs.Add(hierarchyNodeID);
			int[] selectedInstanceIDs = this.hierarchyState.selectedIDs.ToArray();
			this.HandleHierarchySelectionChanged(selectedInstanceIDs, triggerSceneSelectionSync);
		}

		public void UnSelectHierarchyItem(DopeLine dopeline)
		{
			this.UnSelectHierarchyItem(dopeline.m_HierarchyNodeID);
		}

		public void UnSelectHierarchyItem(int hierarchyNodeID)
		{
			this.hierarchyState.selectedIDs.Remove(hierarchyNodeID);
		}

		public List<int> GetAffectedHierarchyIDs(List<AnimationWindowKeyframe> keyframes)
		{
			List<int> list = new List<int>();
			foreach (DopeLine current in this.GetAffectedDopelines(keyframes))
			{
				if (!list.Contains(current.m_HierarchyNodeID))
				{
					list.Add(current.m_HierarchyNodeID);
				}
			}
			return list;
		}

		public List<DopeLine> GetAffectedDopelines(List<AnimationWindowKeyframe> keyframes)
		{
			List<DopeLine> list = new List<DopeLine>();
			foreach (AnimationWindowCurve current in this.GetAffectedCurves(keyframes))
			{
				foreach (DopeLine current2 in this.dopelines)
				{
					if (!list.Contains(current2) && current2.m_Curves.Contains(current))
					{
						list.Add(current2);
					}
				}
			}
			return list;
		}

		public List<AnimationWindowCurve> GetAffectedCurves(List<AnimationWindowKeyframe> keyframes)
		{
			List<AnimationWindowCurve> list = new List<AnimationWindowCurve>();
			foreach (AnimationWindowKeyframe current in keyframes)
			{
				if (!list.Contains(current.curve))
				{
					list.Add(current.curve);
				}
			}
			return list;
		}

		public DopeLine GetDopeline(int selectedInstanceID)
		{
			foreach (DopeLine current in this.dopelines)
			{
				if (current.m_HierarchyNodeID == selectedInstanceID)
				{
					return current;
				}
			}
			return null;
		}

		private void SyncSceneSelection(int[] selectedNodeIDs)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < selectedNodeIDs.Length; i++)
			{
				int id = selectedNodeIDs[i];
				AnimationWindowHierarchyNode animationWindowHierarchyNode = this.hierarchyData.FindItem(id) as AnimationWindowHierarchyNode;
				if (!(this.activeRootGameObject == null) && animationWindowHierarchyNode != null)
				{
					if (!(animationWindowHierarchyNode is AnimationWindowHierarchyMasterNode))
					{
						Transform transform = this.activeRootGameObject.transform.Find(animationWindowHierarchyNode.path);
						if (transform != null && this.activeRootGameObject != null && this.activeAnimationPlayer == AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(transform))
						{
							list.Add(transform.gameObject.GetInstanceID());
						}
					}
				}
			}
			Selection.instanceIDs = list.ToArray();
		}

		private UndoPropertyModification[] PostprocessAnimationRecordingModifications(UndoPropertyModification[] modifications)
		{
			return AnimationRecording.Process(this, modifications);
		}

		public float PixelToTime(float pixel)
		{
			return this.PixelToTime(pixel, false);
		}

		public float PixelToTime(float pixel, bool snapToFrame)
		{
			float num = pixel - this.zeroTimePixel;
			if (snapToFrame)
			{
				return this.SnapToFrame(num / this.pixelPerSecond);
			}
			return num / this.pixelPerSecond;
		}

		public float TimeToPixel(float time)
		{
			return this.TimeToPixel(time, false);
		}

		public float TimeToPixel(float time, bool snapToFrame)
		{
			return ((!snapToFrame) ? time : this.SnapToFrame(time)) * this.pixelPerSecond + this.zeroTimePixel;
		}

		public float SnapToFrame(float time)
		{
			return Mathf.Round(time * this.frameRate) / this.frameRate;
		}

		public float SnapToFrame(float time, bool preventHashCollision)
		{
			if (preventHashCollision)
			{
				return Mathf.Round(time * this.frameRate) / this.frameRate + 0.01f / this.frameRate;
			}
			return this.SnapToFrame(time);
		}

		public string FormatFrame(int frame, int frameDigits)
		{
			return (frame / (int)this.frameRate).ToString() + ":" + ((float)frame % this.frameRate).ToString().PadLeft(frameDigits, '0');
		}

		public float TimeToFrame(float time)
		{
			return time * this.frameRate;
		}

		public float FrameToTime(float frame)
		{
			return frame / this.frameRate;
		}

		public float FrameToTimeFloor(float frame)
		{
			return (frame - 0.5f) / this.frameRate;
		}

		public float FrameToTimeCeiling(float frame)
		{
			return (frame + 0.5f) / this.frameRate;
		}

		public int TimeToFrameFloor(float time)
		{
			return Mathf.FloorToInt(this.TimeToFrame(time));
		}

		public int TimeToFrameRound(float time)
		{
			return Mathf.RoundToInt(this.TimeToFrame(time));
		}

		public float FrameToPixel(float i, Rect rect)
		{
			return (i - this.minVisibleFrame) * rect.width / this.visibleFrameSpan;
		}

		public float FrameDeltaToPixel(Rect rect)
		{
			return rect.width / this.visibleFrameSpan;
		}

		public float TimeToPixel(float time, Rect rect)
		{
			return this.FrameToPixel(time * this.frameRate, rect);
		}

		public float PixelToTime(float pixelX, Rect rect)
		{
			return pixelX * this.visibleTimeSpan / rect.width + this.minVisibleTime;
		}

		public float PixelDeltaToTime(Rect rect)
		{
			return this.visibleTimeSpan / rect.width;
		}

		public float SnapTimeToWholeFPS(float time)
		{
			return Mathf.Round(time * this.frameRate) / this.frameRate;
		}
	}
}
