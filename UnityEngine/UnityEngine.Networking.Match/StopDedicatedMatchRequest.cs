using System;
using UnityEngine.Networking.Types;

namespace UnityEngine.Networking.Match
{
	public class StopDedicatedMatchRequest : Request
	{
		public NetworkID networkId
		{
			get;
			set;
		}
	}
}
