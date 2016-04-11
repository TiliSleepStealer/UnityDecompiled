using System;

namespace UnityEditor.RestService
{
	internal delegate void WriteResponse(HttpStatusCode code, string payload);
}
