using System;
using UnityEditor.Audio;

namespace UnityEditor
{
	internal class AudioMixerTreeViewNode : TreeViewItem
	{
		public AudioMixerGroupController group
		{
			get;
			set;
		}

		public AudioMixerTreeViewNode(int instanceID, int depth, TreeViewItem parent, string displayName, AudioMixerGroupController group) : base(instanceID, depth, parent, displayName)
		{
			this.group = group;
		}
	}
}
