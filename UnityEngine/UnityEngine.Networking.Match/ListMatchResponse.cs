using System;
using System.Collections.Generic;

namespace UnityEngine.Networking.Match
{
	public class ListMatchResponse : BasicResponse
	{
		public List<MatchDesc> matches
		{
			get;
			set;
		}

		public ListMatchResponse()
		{
		}

		public ListMatchResponse(List<MatchDesc> otherMatches)
		{
			this.matches = otherMatches;
		}

		public override string ToString()
		{
			return UnityString.Format("[{0}]-matches.Count:{1}", new object[]
			{
				base.ToString(),
				this.matches.Count
			});
		}

		public override void Parse(object obj)
		{
			base.Parse(obj);
			IDictionary<string, object> dictionary = obj as IDictionary<string, object>;
			if (dictionary != null)
			{
				this.matches = base.ParseJSONList<MatchDesc>("matches", obj, dictionary);
				return;
			}
			throw new FormatException("While parsing JSON response, found obj is not of type IDictionary<string,object>:" + obj.ToString());
		}
	}
}
