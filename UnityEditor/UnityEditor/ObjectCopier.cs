using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityEditor
{
	internal static class ObjectCopier
	{
		public static T DeepClone<T>(T source)
		{
			if (!typeof(T).IsSerializable)
			{
				throw new ArgumentException("The type must be serializable.", "source");
			}
			if (object.ReferenceEquals(source, null))
			{
				return default(T);
			}
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			T result;
			using (stream)
			{
				formatter.Serialize(stream, source);
				stream.Seek(0L, SeekOrigin.Begin);
				result = (T)((object)formatter.Deserialize(stream));
			}
			return result;
		}
	}
}
