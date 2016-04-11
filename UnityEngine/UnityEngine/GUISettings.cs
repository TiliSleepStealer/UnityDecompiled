using System;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
	[Serializable]
	public sealed class GUISettings
	{
		[SerializeField]
		private bool m_DoubleClickSelectsWord = true;

		[SerializeField]
		private bool m_TripleClickSelectsLine = true;

		[SerializeField]
		private Color m_CursorColor = Color.white;

		[SerializeField]
		private float m_CursorFlashSpeed = -1f;

		[SerializeField]
		private Color m_SelectionColor = new Color(0.5f, 0.5f, 1f);

		public bool doubleClickSelectsWord
		{
			get
			{
				return this.m_DoubleClickSelectsWord;
			}
			set
			{
				this.m_DoubleClickSelectsWord = value;
			}
		}

		public bool tripleClickSelectsLine
		{
			get
			{
				return this.m_TripleClickSelectsLine;
			}
			set
			{
				this.m_TripleClickSelectsLine = value;
			}
		}

		public Color cursorColor
		{
			get
			{
				return this.m_CursorColor;
			}
			set
			{
				this.m_CursorColor = value;
			}
		}

		public float cursorFlashSpeed
		{
			get
			{
				if (this.m_CursorFlashSpeed >= 0f)
				{
					return this.m_CursorFlashSpeed;
				}
				return GUISettings.Internal_GetCursorFlashSpeed();
			}
			set
			{
				this.m_CursorFlashSpeed = value;
			}
		}

		public Color selectionColor
		{
			get
			{
				return this.m_SelectionColor;
			}
			set
			{
				this.m_SelectionColor = value;
			}
		}

		[WrapperlessIcall]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern float Internal_GetCursorFlashSpeed();
	}
}
