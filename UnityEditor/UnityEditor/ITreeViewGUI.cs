using System;
using UnityEngine;

namespace UnityEditor
{
	internal interface ITreeViewGUI
	{
		float halfDropBetweenHeight
		{
			get;
		}

		float topRowMargin
		{
			get;
		}

		float bottomRowMargin
		{
			get;
		}

		void OnInitialize();

		Vector2 GetTotalSize();

		void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible);

		Rect GetRowRect(int row, float rowWidth);

		Rect GetRectForFraming(int row);

		int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView);

		void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused);

		void BeginRowGUI();

		void EndRowGUI();

		void BeginPingNode(TreeViewItem item, float topPixelOfRow, float availableWidth);

		void EndPingNode();

		bool BeginRename(TreeViewItem item, float delay);

		void EndRename();

		float GetContentIndent(TreeViewItem item);
	}
}
