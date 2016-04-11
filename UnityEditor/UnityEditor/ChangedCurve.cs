using System;
using UnityEngine;

namespace UnityEditor
{
	internal class ChangedCurve
	{
		public AnimationCurve curve;

		public EditorCurveBinding binding;

		public ChangedCurve(AnimationCurve curve, EditorCurveBinding binding)
		{
			this.curve = curve;
			this.binding = binding;
		}

		public override int GetHashCode()
		{
			int hashCode = this.curve.GetHashCode();
			return 33 * hashCode + this.binding.GetHashCode();
		}
	}
}
