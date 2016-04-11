using System;
using UnityEngine.Networking.Types;

namespace UnityEngine.Networking.Match
{
	public abstract class Request
	{
		public int version = 2;

		public SourceID sourceId
		{
			get;
			set;
		}

		public string projectId
		{
			get;
			set;
		}

		public AppID appId
		{
			get;
			set;
		}

		public string accessTokenString
		{
			get;
			set;
		}

		public int domain
		{
			get;
			set;
		}

		public virtual bool IsValid()
		{
			return this.appId != AppID.Invalid && this.sourceId != SourceID.Invalid;
		}

		public override string ToString()
		{
			return UnityString.Format("[{0}]-SourceID:0x{1},AppID:0x{2},domain:{3}", new object[]
			{
				base.ToString(),
				this.sourceId.ToString("X"),
				this.appId.ToString("X"),
				this.domain
			});
		}
	}
}
