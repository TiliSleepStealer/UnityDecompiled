using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
	[Serializable]
	internal class AnimationWindowClipPopup
	{
		[SerializeField]
		public AnimationWindowState state;

		[SerializeField]
		private int selectedIndex;

		public bool creatingNewClipAllowed
		{
			get
			{
				return this.state.activeRootGameObject != null && this.state.animationIsEditable;
			}
		}

		public void OnGUI()
		{
			string[] clipMenuContent = this.GetClipMenuContent();
			EditorGUI.BeginChangeCheck();
			this.selectedIndex = EditorGUILayout.Popup(this.ClipToIndex(this.state.activeAnimationClip), clipMenuContent, EditorStyles.toolbarPopup, new GUILayoutOption[0]);
			if (EditorGUI.EndChangeCheck())
			{
				if (clipMenuContent[this.selectedIndex] == AnimationWindowStyles.createNewClip.text)
				{
					AnimationClip animationClip = AnimationWindowUtility.CreateNewClip(this.state.activeRootGameObject.name);
					if (animationClip)
					{
						AnimationWindowUtility.AddClipToAnimationPlayerComponent(this.state.activeAnimationPlayer, animationClip);
						this.state.activeAnimationClip = animationClip;
					}
				}
				else
				{
					this.state.activeAnimationClip = this.IndexToClip(this.selectedIndex);
				}
			}
		}

		private string[] GetClipMenuContent()
		{
			List<string> list = new List<string>();
			list.AddRange(this.GetClipNames());
			if (this.creatingNewClipAllowed)
			{
				list.Add(string.Empty);
				list.Add(AnimationWindowStyles.createNewClip.text);
			}
			return list.ToArray();
		}

		private string[] GetClipNames()
		{
			AnimationClip[] array = new AnimationClip[0];
			if (this.state.clipOnlyMode)
			{
				array = new AnimationClip[]
				{
					this.state.activeAnimationClip
				};
			}
			else if (this.state.activeRootGameObject != null)
			{
				array = AnimationUtility.GetAnimationClips(this.state.activeRootGameObject);
			}
			string[] array2 = new string[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = CurveUtility.GetClipName(array[i]);
			}
			return array2;
		}

		private AnimationClip IndexToClip(int index)
		{
			if (this.state.activeRootGameObject != null)
			{
				AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(this.state.activeRootGameObject);
				if (index >= 0 && index < animationClips.Length)
				{
					return AnimationUtility.GetAnimationClips(this.state.activeRootGameObject)[index];
				}
			}
			return null;
		}

		private int ClipToIndex(AnimationClip clip)
		{
			if (this.state.activeRootGameObject != null)
			{
				int num = 0;
				AnimationClip[] animationClips = AnimationUtility.GetAnimationClips(this.state.activeRootGameObject);
				for (int i = 0; i < animationClips.Length; i++)
				{
					AnimationClip y = animationClips[i];
					if (clip == y)
					{
						return num;
					}
					num++;
				}
			}
			return 0;
		}
	}
}
