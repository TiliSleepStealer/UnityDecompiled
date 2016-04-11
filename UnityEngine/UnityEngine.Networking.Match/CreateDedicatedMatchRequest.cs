using System;
using System.Collections.Generic;

namespace UnityEngine.Networking.Match
{
	public class CreateDedicatedMatchRequest : Request
	{
		public string name
		{
			get;
			set;
		}

		public uint size
		{
			get;
			set;
		}

		public bool advertise
		{
			get;
			set;
		}

		public string password
		{
			get;
			set;
		}

		public string publicAddress
		{
			get;
			set;
		}

		public string privateAddress
		{
			get;
			set;
		}

		public int eloScore
		{
			get;
			set;
		}

		public Dictionary<string, long> matchAttributes
		{
			get;
			set;
		}

		public override bool IsValid()
		{
			return this.matchAttributes == null || this.matchAttributes.Count <= 10;
		}
	}
}
