using System.IO;
using UnityEngine;

namespace AnimationImporter
{
	[System.Serializable]
	public class AssetTargetLocation
	{
		[SerializeField]
		private AssetTargetLocationType _locationType;
		public AssetTargetLocationType locationType
		{
			get { return _locationType; }
			set { _locationType = value; }
		}

		[SerializeField]
		private string _globalDirectory = "Assets";
		public string globalDirectory
		{
			get { return _globalDirectory; }
			set { _globalDirectory = value; }
		}
		
		private string _subDirectoryName;
		public string subDirectoryName
		{
			get {return _subDirectoryName; }
		}

		// ================================================================================
		//  constructor
		// --------------------------------------------------------------------------------

		public AssetTargetLocation(AssetTargetLocationType type, string subFolderName) : this(type)
		{
			_subDirectoryName = subFolderName;
		}

		public AssetTargetLocation(AssetTargetLocationType type)
		{
			locationType = type;
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		public string GetAndEnsureTargetDirectory(string assetDirectory)
		{
			string directory = GetTargetDirectory(assetDirectory);

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return directory;
		}

		public string GetTargetDirectory(string assetDirectory)
		{
			if (locationType == AssetTargetLocationType.GlobalDirectory)
			{
				return globalDirectory;
			}
			else if (locationType == AssetTargetLocationType.SubDirectory)
			{
				return Path.Combine(assetDirectory, subDirectoryName);
			}

			return assetDirectory;
		}
	}
}