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

        private string GetBasePath(string path)
        {
            string extension = Path.GetExtension(path);
            if (extension.Length > 0 && extension[0] == '.')
            {
                extension = extension.Remove(0, 1);
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            string lastPart = "/" + fileName + "." + extension;

            return path.Replace(lastPart, "");
        }

        public string GetAndEnsureTargetDirectory(string assetPath)
		{
			string directory = GetTargetDirectory(assetPath);

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			return directory;
		}

		public string GetTargetDirectory(string assetPath)
		{
            var basePath = GetBasePath(assetPath);
            
            switch (locationType)
            {
                case AssetTargetLocationType.GlobalDirectory:
                    return globalDirectory;
                case AssetTargetLocationType.SubDirectory:
                    return Path.Combine(basePath, subDirectoryName);
                case AssetTargetLocationType.FileNameDirectory:
                    return Path.Combine(basePath, Path.GetFileNameWithoutExtension(assetPath));
            }
			
			return basePath;
		}
	}
}