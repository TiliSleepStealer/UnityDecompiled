using System;

namespace UnityEngine.Networking.Match
{
	public interface IResponse
	{
		void SetSuccess();

		void SetFailure(string info);
	}
}
