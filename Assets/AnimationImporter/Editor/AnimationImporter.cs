using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using UnityEditor.Animations;
using System.Linq;
using AnimationImporter.Aseprite;

namespace AnimationImporter
{
	public class AnimationImporter
	{
		// ================================================================================
		//	Singleton
		// --------------------------------------------------------------------------------

		private static AnimationImporter _instance = null;
		public static AnimationImporter Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new AnimationImporter();
				}

				return _instance;
			}
		}

		// ================================================================================
		//  delegates
		// --------------------------------------------------------------------------------

		public delegate ImportedAnimationSheet ImportDelegate(AnimationImportJob job, AnimationImporterSharedConfig config);

		public delegate bool CustomReImportDelegate(string fileName);
		public static CustomReImportDelegate HasCustomReImport = null;
		public static CustomReImportDelegate HandleCustomReImport = null;

		// ================================================================================
		//  const
		// --------------------------------------------------------------------------------

		private const string PREFS_PREFIX = "ANIMATION_IMPORTER_";
		private const string SHARED_CONFIG_PATH = "Assets/Resources/AnimationImporter/AnimationImporterConfig.asset";

		// ================================================================================
		//  user values
		// --------------------------------------------------------------------------------

		string _asepritePath = "";
		public string asepritePath
		{
			get
			{
				return _asepritePath;
			}
			set
			{
				if (_asepritePath != value)
				{
					_asepritePath = value;
					SaveUserConfig();
				}
			}
		}

		private RuntimeAnimatorController _baseController = null;
		public RuntimeAnimatorController baseController
		{
			get
			{
				return _baseController;
			}
			set
			{
				if (_baseController != value)
				{
					_baseController = value;
					SaveUserConfig();
				}
			}
		}

		private AnimationImporterSharedConfig _sharedData;
		public AnimationImporterSharedConfig sharedData
		{
			get
			{
				return _sharedData;
			}
		}

		// ================================================================================
		//  Importer Plugins
		// --------------------------------------------------------------------------------

		private static Dictionary<string, IAnimationImporterPlugin> _importerPlugins = new Dictionary<string, IAnimationImporterPlugin>();

		public static void RegisterImporter(IAnimationImporterPlugin importer, params string[] extensions)
		{
			foreach (var extension in extensions)
			{
				_importerPlugins[extension] = importer;
			}
		}

		// ================================================================================
		//  validation
		// --------------------------------------------------------------------------------

		// this was used in the past, might be again in the future, so leave it here
		public bool canImportAnimations
		{
			get
			{
				return true;
			}
		}
		public bool canImportAnimationsForOverrideController
		{
			get
			{
				return canImportAnimations && _baseController != null;
			}
		}

		// ================================================================================
		//  save and load user values
		// --------------------------------------------------------------------------------

		public void LoadOrCreateUserConfig()
		{
			LoadPreferences();

			_sharedData = ScriptableObjectUtility.LoadOrCreateSaveData<AnimationImporterSharedConfig>(SHARED_CONFIG_PATH);
		}

		public void LoadUserConfig()
		{
			LoadPreferences();

			_sharedData = ScriptableObjectUtility.LoadSaveData<AnimationImporterSharedConfig>(SHARED_CONFIG_PATH);
		}

		private void LoadPreferences()
		{
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "asepritePath"))
			{
				_asepritePath = PlayerPrefs.GetString(PREFS_PREFIX + "asepritePath");
			}
			else
			{
				_asepritePath = AsepriteImporter.standardApplicationPath;

				if (!File.Exists(_asepritePath))
					_asepritePath = "";
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "baseControllerPath"))
			{
				string baseControllerPath = PlayerPrefs.GetString(PREFS_PREFIX + "baseControllerPath");
				if (!string.IsNullOrEmpty(baseControllerPath))
				{
					_baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
				}
			}
		}

		private void SaveUserConfig()
		{
			PlayerPrefs.SetString(PREFS_PREFIX + "asepritePath", _asepritePath);

			if (_baseController != null)
			{
				PlayerPrefs.SetString(PREFS_PREFIX + "baseControllerPath", AssetDatabase.GetAssetPath(_baseController));
			}
			else
			{
				PlayerPrefs.SetString(PREFS_PREFIX + "baseControllerPath", "");
			}
		}

		// ================================================================================
		//  import methods
		// --------------------------------------------------------------------------------

		public ImportedAnimationSheet CreateAnimationsForAssetFile(DefaultAsset droppedAsset)
		{
			return CreateAnimationsForAssetFile(AssetDatabase.GetAssetPath(droppedAsset));
		}

		public ImportedAnimationSheet CreateAnimationsForAssetFile(string assetPath, string additionalCommandLineArguments = null)
		{
			ImportedAnimationSheet animationSheet = ImportSpritesAndAnimationSheet(assetPath, additionalCommandLineArguments);

			if (animationSheet != null)
			{
				CreateAnimations(animationSheet);
			}

			return animationSheet;
		}

		public ImportedAnimationSheet ImportSpritesAndAnimationSheet(string assetPath, string additionalCommandLineArguments = null)
		{
			if (!IsValidAsset(assetPath))
			{
				return null;
			}

			// making sure config is valid
			if (sharedData == null)
			{
				LoadOrCreateUserConfig();
			}

			// create a job
			AnimationImportJob job = CreateAnimationImportJob(assetPath, additionalCommandLineArguments);

			IAnimationImporterPlugin importer = _importerPlugins[GetExtension(assetPath)];
			ImportedAnimationSheet animationSheet = importer.Import(job, sharedData);

			animationSheet.assetDirectory = job.assetDirectory;
			animationSheet.name = job.name;

			CreateSprites(animationSheet);

			return animationSheet;
		}

		public void CreateAnimatorController(ImportedAnimationSheet animations)
		{
			AnimatorController controller;

			// check if controller already exists; use this to not loose any references to this in other assets
			string pathForAnimatorController = animations.assetDirectory + "/" + animations.name + ".controller";
			controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForAnimatorController);

			if (controller == null)
			{
				// create a new controller and place every animation as a state on the first layer
				controller = AnimatorController.CreateAnimatorControllerAtPath(animations.assetDirectory + "/" + animations.name + ".controller");
				controller.AddLayer("Default");

				foreach (var animation in animations.animations)
				{
					AnimatorState state = controller.layers[0].stateMachine.AddState(animation.name);
					state.motion = animation.animationClip;
				}
			}
			else
			{
				// look at all states on the first layer and replace clip if state has the same name
				var childStates = controller.layers[0].stateMachine.states;
				foreach (var childState in childStates)
				{
					AnimationClip clip = animations.GetClip(childState.state.name);
					if (clip != null)
						childState.state.motion = clip;
				}
			}

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
		}

		public void CreateAnimatorOverrideController(ImportedAnimationSheet animations, bool useExistingBaseController = false)
		{
			AnimatorOverrideController overrideController;

			// check if override controller already exists; use this to not loose any references to this in other assets
			string pathForOverrideController = animations.assetDirectory + "/" + animations.name + ".overrideController";
			overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForOverrideController);

			RuntimeAnimatorController baseController = _baseController;
			if (useExistingBaseController && overrideController.runtimeAnimatorController != null)
			{
				baseController = overrideController.runtimeAnimatorController;
			}

			if (baseController != null)
			{
				if (overrideController == null)
				{
					overrideController = new AnimatorOverrideController();
					AssetDatabase.CreateAsset(overrideController, pathForOverrideController);
				}

				overrideController.runtimeAnimatorController = baseController;

				// set override clips
				var clipPairs = overrideController.clips;
				for (int i = 0; i < clipPairs.Length; i++)
				{
					string animationName = clipPairs[i].originalClip.name;
					AnimationClip clip = animations.GetClipOrSimilar(animationName);
					clipPairs[i].overrideClip = clip;
				}
				overrideController.clips = clipPairs;

				EditorUtility.SetDirty(overrideController);
			}
			else
			{
				Debug.LogWarning("No Animator Controller found as a base for the Override Controller");
			}
		}

		// ================================================================================
		//  create sprites and animations
		// --------------------------------------------------------------------------------

		private void CreateAnimations(ImportedAnimationSheet animationSheet)
		{
			if (animationSheet == null)
			{
				return;
			}

			string imageAssetFilename = GetImageAssetFilename(animationSheet.assetDirectory, animationSheet.name);

			if (animationSheet.hasAnimations)
			{
				if (sharedData.saveAnimationsToSubfolder)
				{
					string path = animationSheet.assetDirectory + "/Animations";
					if (!Directory.Exists(path))
					{
						Directory.CreateDirectory(path);
					}

					CreateAnimationAssets(animationSheet, imageAssetFilename, path);
				}
				else
				{
					CreateAnimationAssets(animationSheet, imageAssetFilename, animationSheet.assetDirectory);
				}
			}
		}

		private void CreateAnimationAssets(ImportedAnimationSheet animationInfo, string imageAssetFilename, string pathForAnimations)
		{
			string masterName = Path.GetFileNameWithoutExtension(imageAssetFilename);

			foreach (var animation in animationInfo.animations)
			{
				animationInfo.CreateAnimation(animation, pathForAnimations, masterName, sharedData.targetObjectType);
			}
		}

		private void CreateSprites(ImportedAnimationSheet animationSheet)
		{
			if (animationSheet == null)
			{
				return;
			}

			string imageFile = GetImageAssetFilename(animationSheet.assetDirectory, animationSheet.name);

			TextureImporter importer = AssetImporter.GetAtPath(imageFile) as TextureImporter;

			// apply texture import settings if there are no previous ones
			if (!animationSheet.hasPreviousTextureImportSettings)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spritePixelsPerUnit = sharedData.spritePixelsPerUnit;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Point;
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			}

			// create sub sprites for this file according to the AsepriteAnimationInfo 
			importer.spritesheet = animationSheet.GetSpriteSheet(
				sharedData.spriteAlignment,
				sharedData.spriteAlignmentCustomX,
				sharedData.spriteAlignmentCustomY);

			// reapply old import settings (pivot settings for sprites)
			if (animationSheet.hasPreviousTextureImportSettings)
			{
				animationSheet.previousImportSettings.ApplyPreviousTextureImportSettings(importer);
			}

			// these values will be set in any case, not influenced by previous import settings
			importer.spriteImportMode = SpriteImportMode.Multiple;
			importer.maxTextureSize = animationSheet.maxTextureSize;

			EditorUtility.SetDirty(importer);

			try
			{
				importer.SaveAndReimport();
			}
			catch (Exception e)
			{
				Debug.LogWarning("There was a problem with applying settings to the generated sprite file: " + e.ToString());
			}

			AssetDatabase.ImportAsset(imageFile, ImportAssetOptions.ForceUpdate);

			Sprite[] createdSprites = GetAllSpritesFromAssetFile(imageFile);
			animationSheet.ApplyCreatedSprites(createdSprites);
		}

		private static Sprite[] GetAllSpritesFromAssetFile(string imageFilename)
		{
			var assets = AssetDatabase.LoadAllAssetsAtPath(imageFilename);
			List<Sprite> sprites = new List<Sprite>();
			foreach (var item in assets)
			{
				if (item is Sprite)
				{
					sprites.Add(item as Sprite);
				}
			}

			// we order the sprites by name here because the LoadAllAssets above does not necessarily return the sprites in correct order
			// the OrderBy is fed with the last word of the name, which is an int from 0 upwards
			Sprite[] orderedSprites = sprites
									 .OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf(' ')).TrimStart()))
									 .ToArray();

			return orderedSprites;
		}

		// ================================================================================
		//  querying existing assets
		// --------------------------------------------------------------------------------

		// check if this is a valid file; we are only looking at the file extension here
		public static bool IsValidAsset(string path)
		{
			string extension = GetExtension(path);

			if (!string.IsNullOrEmpty(path))
			{
				if (_importerPlugins.ContainsKey(extension))
				{
					IAnimationImporterPlugin importer = _importerPlugins[extension];
					if (importer != null)
					{
						return importer.IsValid();
					}
				}
			}

			return false;
		}

		private static string GetExtension(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			string extension = Path.GetExtension(path);
			if (extension.StartsWith("."))
			{
				extension = extension.Remove(0, 1);
			}

			return extension;
		}

		public bool HasExistingRuntimeAnimatorController(string assetPath)
		{
			return HasExistingAnimatorController(assetPath) || HasExistingAnimatorOverrideController(assetPath);
		}

		public bool HasExistingAnimatorController(string assetPath)
		{
			return GetExistingAnimatorController(assetPath) != null;
		}

		public bool HasExistingAnimatorOverrideController(string assetPath)
		{
			return GetExistingAnimatorOverrideController(assetPath) != null;
		}

		public RuntimeAnimatorController GetExistingRuntimeAnimatorController(string assetPath)
		{
			AnimatorController animatorController = GetExistingAnimatorController(assetPath);
			if (animatorController != null)
			{
				return animatorController;
			}

			return GetExistingAnimatorOverrideController(assetPath);
		}

		public AnimatorController GetExistingAnimatorController(string assetPath)
		{
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);

			string pathForController = basePath + "/" + name + ".controller";
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForController);

			return controller;
		}

		public AnimatorOverrideController GetExistingAnimatorOverrideController(string assetPath)
		{
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);

			string pathForController = basePath + "/" + name + ".overrideController";
			AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForController);

			return controller;
		}

		// ================================================================================
		//  automatic ReImport
		// --------------------------------------------------------------------------------

		/// <summary>
		/// will be called by the AssetPostProcessor
		/// </summary>
		public void AutomaticReImport(string filePath)
		{
			if (filePath == null)
			{
				return;
			}

			// check if file is handled by other Importers
			if (HandleCustomReImport != null && HandleCustomReImport(filePath))
			{
				return;
			}

			HandleReImport(filePath);
		}

		/// <summary>
		/// can be used for manually handling ReImport
		/// </summary>
		public void HandleReImport(string filePath, string additionalCommandLineArguments = null)
		{
			if (filePath == null)
			{
				return;
			}

			if (sharedData == null)
			{
				LoadOrCreateUserConfig();
			}

			if (HasExistingAnimatorController(filePath))
			{
				var animationInfo = CreateAnimationsForAssetFile(filePath, additionalCommandLineArguments);

				if (animationInfo != null)
				{
					CreateAnimatorController(animationInfo);
				}
			}
			else if (HasExistingAnimatorOverrideController(filePath))
			{
				var animationInfo = CreateAnimationsForAssetFile(filePath, additionalCommandLineArguments);

				if (animationInfo != null)
				{
					CreateAnimatorOverrideController(animationInfo, true);
				}
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private AnimationImportJob CreateAnimationImportJob(string assetPath, string additionalCommandLineArguments = "")
		{
			AnimationImportJob importJob = new AnimationImportJob(assetPath);

			importJob.additionalCommandLineArguments = additionalCommandLineArguments;

			if (_sharedData.saveSpritesToSubfolder)
			{
				importJob.directoryPathForSprites = importJob.assetDirectory + "/Sprites";
			}
			else
			{
				importJob.directoryPathForSprites = importJob.assetDirectory;
			}

			if (_sharedData.saveAnimationsToSubfolder)
			{
				importJob.directoryPathForAnimations = importJob.assetDirectory + "/Animations";
			}
			else
			{
				importJob.directoryPathForAnimations = importJob.assetDirectory;
			}

			// we analyze import settings on existing files
			importJob.previousImportSettings = CollectPreviousImportSettings(importJob);

			return importJob;
		}

		private PreviousImportSettings CollectPreviousImportSettings(AnimationImportJob importJob)
		{
			PreviousImportSettings previousImportSettings = new PreviousImportSettings();

			previousImportSettings.GetTextureImportSettings(importJob.imageAssetFilename);

			return previousImportSettings;
		}

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

		private string GetImageAssetFilename(string basePath, string name)
		{
			if (sharedData.saveSpritesToSubfolder)
				basePath += "/Sprites";

			return basePath + "/" + name + ".png";
		}
	}
}
