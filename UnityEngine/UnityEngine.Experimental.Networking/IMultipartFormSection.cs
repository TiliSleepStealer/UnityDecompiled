using System;

namespace UnityEngine.Experimental.Networking
{
	public interface IMultipartFormSection
	{
		string sectionName
		{
			get;
		}

		byte[] sectionData
		{
			get;
		}

		string fileName
		{
			get;
		}

		string contentType
		{
			get;
		}
	}
}
