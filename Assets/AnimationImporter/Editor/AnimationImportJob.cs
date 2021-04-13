using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

namespace AnimationImporter
{
	public class AnimationImportJob
	{
		private string _assetPath;

		public string name { get { return Path.GetFileNameWithoutExtension(fileName); } }
		public string fileName { get { return Path.GetFileName(_assetPath); } }
		public string assetDirectory { get { return GetBasePath(_assetPath); } }

		private string _directoryPathForSprites = "";
		public string directoryPathForSprites
		{
			get
			{
				if (!Directory.Exists(_directoryPathForSprites))
				{
					Directory.CreateDirectory(_directoryPathForSprites);
				}

				return _directoryPathForSprites;
			}
			set
			{
				_directoryPathForSprites = value;
			}
		}

		private string _directoryPathForAnimations = "";
		public string directoryPathForAnimations
		{
			get
			{
				if (!Directory.Exists(_directoryPathForAnimations))
				{
					Directory.CreateDirectory(_directoryPathForAnimations);
				}

				return _directoryPathForAnimations;
			}
			set
			{
				_directoryPathForAnimations = value;
			}
		}

		private string _directoryPathForAnimationControllers = "";
		public string directoryPathForAnimationControllers
		{
			get
			{
				if (!Directory.Exists(_directoryPathForAnimationControllers))
				{
					Directory.CreateDirectory(_directoryPathForAnimationControllers);
				}

				return _directoryPathForAnimationControllers;
			}
			set
			{
				_directoryPathForAnimationControllers = value;
			}
		}

		public string imageAssetFilename
		{
			get
			{
				return directoryPathForSprites + "/" + name + ".png";
			}
		}

		private string _sheetConfigParameter = "--sheet-pack";
		public string sheetConfigParameter
		{
			get
			{
				return _sheetConfigParameter;
			}
			set
			{
				_sheetConfigParameter = value;
			}
		}

		public PreviousImportSettings previousImportSettings = null;

		// additional import settings
		public string additionalCommandLineArguments = null;
		public bool createUnityAnimations = true;
		public ImportAnimatorController importAnimatorController = ImportAnimatorController.None;
		public bool useExistingAnimatorController = false;

		// ================================================================================
		//  constructor
		// --------------------------------------------------------------------------------

		public AnimationImportJob(string assetPath)
		{
			_assetPath = assetPath;
		}

		// ================================================================================
		//  progress
		// --------------------------------------------------------------------------------

		public delegate void ProgressUpdatedDelegate(float progress);
		public event ProgressUpdatedDelegate progressUpdated;

		private float _progress = 0;
		public float progress
		{
			get
			{
				return _progress;
			}
		}

		public void SetProgress(float progress)
		{
			_progress = progress;

			if (progressUpdated != null)
			{
				progressUpdated(_progress);
			}
		}

		// ================================================================================
		//  private methods
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
	}
}