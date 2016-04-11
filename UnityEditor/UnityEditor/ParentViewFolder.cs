using System;

namespace UnityEditor
{
	[Serializable]
	internal class ParentViewFolder
	{
		private const string rootDirText = "/";

		private const string assetsFolder = "Assets";

		private const string libraryFolder = "Library";

		public string guid;

		public string name;

		public ChangeFlags changeFlags;

		public ParentViewFile[] files;

		public ParentViewFolder(string name, string guid)
		{
			this.guid = guid;
			this.name = name;
			this.changeFlags = ChangeFlags.None;
			this.files = new ParentViewFile[0];
		}

		public ParentViewFolder(string name, string guid, ChangeFlags flags)
		{
			this.guid = guid;
			this.name = name;
			this.changeFlags = flags;
			this.files = new ParentViewFile[0];
		}

		public static string MakeNiceName(string name)
		{
			if (name.StartsWith("Assets"))
			{
				if (name != "Assets")
				{
					name = name.Substring("Assets".Length + 1);
					return (!(name == string.Empty)) ? name : "/";
				}
				return "/";
			}
			else
			{
				if (name.StartsWith("Library"))
				{
					return "../" + name;
				}
				return (!(name == string.Empty)) ? name : "/";
			}
		}

		public ParentViewFolder CloneWithoutFiles()
		{
			return new ParentViewFolder(this.name, this.guid, this.changeFlags);
		}
	}
}
