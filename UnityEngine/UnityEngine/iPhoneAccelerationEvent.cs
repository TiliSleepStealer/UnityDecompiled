using System;

namespace UnityEngine
{
	[Obsolete("iPhoneAccelerationEvent struct is deprecated. Please use AccelerationEvent instead (UnityUpgradable) -> AccelerationEvent", true)]
	public struct iPhoneAccelerationEvent
	{
		[Obsolete("timeDelta property is deprecated. Please use AccelerationEvent.deltaTime instead (UnityUpgradable) -> AccelerationEvent.deltaTime", true)]
		public float timeDelta
		{
			get
			{
				return 0f;
			}
		}

		public Vector3 acceleration
		{
			get
			{
				return default(Vector3);
			}
		}

		public float deltaTime
		{
			get
			{
				return -1f;
			}
		}
	}
}
