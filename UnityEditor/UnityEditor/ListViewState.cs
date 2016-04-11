using System;
using UnityEngine;

namespace UnityEditor
{
	[Serializable]
	internal class ListViewState
	{
		private const int c_rowHeight = 16;

		public int row;

		public int column;

		public Vector2 scrollPos;

		public int totalRows;

		public int rowHeight;

		public int ID;

		public bool selectionChanged;

		public int draggedFrom;

		public int draggedTo;

		public bool drawDropHere;

		public Rect dropHereRect = new Rect(0f, 0f, 0f, 0f);

		public string[] fileNames;

		public int customDraggedFromID;

		internal ListViewShared.InternalLayoutedListViewState ilvState = new ListViewShared.InternalLayoutedListViewState();

		public ListViewState()
		{
			this.Init(0, 16);
		}

		public ListViewState(int totalRows)
		{
			this.Init(totalRows, 16);
		}

		public ListViewState(int totalRows, int rowHeight)
		{
			this.Init(totalRows, rowHeight);
		}

		private void Init(int totalRows, int rowHeight)
		{
			this.row = -1;
			this.column = 0;
			this.scrollPos = Vector2.zero;
			this.totalRows = totalRows;
			this.rowHeight = rowHeight;
			this.selectionChanged = false;
		}
	}
}
