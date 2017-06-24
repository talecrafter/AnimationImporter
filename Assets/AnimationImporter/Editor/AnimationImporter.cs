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

		public delegate void ChangeImportJob(AnimationImportJob job);

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

		public void ImportAssets(DefaultAsset[] assets, ImportAnimatorController importAnimatorController = ImportAnimatorController.None)
		{
			List<AnimationImportJob> jobs = new List<AnimationImportJob>();

			foreach (var asset in assets)
			{
				string assetPath = AssetDatabase.GetAssetPath(asset);
				if (!IsValidAsset(assetPath))
				{
					continue;
				}

				AnimationImportJob job = CreateAnimationImportJob(assetPath);
				job.importAnimatorController = importAnimatorController;
				jobs.Add(job);
			}

			Import(jobs.ToArray());
		}

		/// <summary>
		/// can be used by custom import pipeline
		/// </summary>
		public ImportedAnimationSheet ImportSpritesAndAnimationSheet(
			string assetPath,
			ChangeImportJob changeImportJob = null,
			string additionalCommandLineArguments = null
		)
		{
			// making sure config is valid
			if (sharedData == null)
			{
				LoadOrCreateUserConfig();
			}

			if (!IsValidAsset(assetPath))
			{
				return null;
			}

			// create a job
			AnimationImportJob job = CreateAnimationImportJob(assetPath, additionalCommandLineArguments);
			job.createUnityAnimations = false;

			if (changeImportJob != null)
			{
				changeImportJob(job);
			}

			return ImportJob(job);
		}

		private void Import(AnimationImportJob[] jobs)
		{
			if (jobs == null || jobs.Length == 0)
			{
				return;
			}

			float progressPerJob = 1f / jobs.Length;

			try
			{
				for (int i = 0; i < jobs.Length; i++)
				{
					AnimationImportJob job = jobs[i];

					job.progressUpdated += (float progress) => {							
							float completeProgress = i * progressPerJob + progress * progressPerJob;
							EditorUtility.DisplayProgressBar("Import", job.name, completeProgress);
						};
					ImportJob(job);
				}
				AssetDatabase.Refresh();
			}
			catch (Exception error)
			{
				Debug.LogWarning(error.ToString());
				throw;
			}

			EditorUtility.ClearProgressBar();
		}

		private ImportedAnimationSheet ImportJob(AnimationImportJob job)
		{
			job.SetProgress(0);

			IAnimationImporterPlugin importer = _importerPlugins[GetExtension(job.fileName)];
			ImportedAnimationSheet animationSheet = importer.Import(job, sharedData);

			job.SetProgress(0.3f);

			if (animationSheet != null)
			{
				animationSheet.assetDirectory = job.assetDirectory;
				animationSheet.name = job.name;

				animationSheet.ApplySpriteNamingScheme(sharedData.spriteNamingScheme);

				CreateSprites(animationSheet, job);

				job.SetProgress(0.6f);

				if (job.createUnityAnimations)
				{
					CreateAnimations(animationSheet, job);

					job.SetProgress(0.8f);

					if (job.importAnimatorController == ImportAnimatorController.AnimatorController)
					{
						CreateAnimatorController(animationSheet);
					}
					else if (job.importAnimatorController == ImportAnimatorController.AnimatorOverrideController)
					{
						CreateAnimatorOverrideController(animationSheet, job.useExistingAnimatorController);
					}
				}
			}

			return animationSheet;
		}

		// ================================================================================
		//  create animator controllers
		// --------------------------------------------------------------------------------

		private void CreateAnimatorController(ImportedAnimationSheet animations)
		{
			AnimatorController controller;

			string directory = sharedData.animationControllersTargetLocation.GetAndEnsureTargetDirectory(animations.assetDirectory);

			// check if controller already exists; use this to not loose any references to this in other assets
			string pathForAnimatorController = directory + "/" + animations.name + ".controller";
			controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForAnimatorController);

			if (controller == null)
			{
				// create a new controller and place every animation as a state on the first layer
				controller = AnimatorController.CreateAnimatorControllerAtPath(pathForAnimatorController);
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

		private void CreateAnimatorOverrideController(ImportedAnimationSheet animations, bool useExistingBaseController = false)
		{
			AnimatorOverrideController overrideController;

			string directory = sharedData.animationControllersTargetLocation.GetAndEnsureTargetDirectory(animations.assetDirectory);

			// check if override controller already exists; use this to not loose any references to this in other assets
			string pathForOverrideController = directory + "/" + animations.name + ".overrideController";
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
#if UNITY_5_6_OR_NEWER
				var clipPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
				overrideController.GetOverrides(clipPairs);

				foreach (var pair in clipPairs)
				{
					string animationName = pair.Key.name;
					AnimationClip clip = animations.GetClipOrSimilar(animationName);
					overrideController[animationName] = clip;
				}
#else
				var clipPairs = overrideController.clips;
				for (int i = 0; i < clipPairs.Length; i++)
				{
					string animationName = clipPairs[i].originalClip.name;
					AnimationClip clip = animations.GetClipOrSimilar(animationName);
					clipPairs[i].overrideClip = clip;
				}
				overrideController.clips = clipPairs;
#endif

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

		private void CreateAnimations(ImportedAnimationSheet animationSheet, AnimationImportJob job)
		{
			if (animationSheet == null)
			{
				return;
			}

			string imageAssetFilename = job.imageAssetFilename;

			if (animationSheet.hasAnimations)
			{
				string targetPath = _sharedData.animationsTargetLocation.GetAndEnsureTargetDirectory(animationSheet.assetDirectory);
				CreateAnimationAssets(animationSheet, imageAssetFilename, targetPath);
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

		private void CreateSprites(ImportedAnimationSheet animationSheet, AnimationImportJob job)
		{
			if (animationSheet == null)
			{
				return;
			}

			string imageAssetFile = job.imageAssetFilename;

			TextureImporter importer = AssetImporter.GetAtPath(imageAssetFile) as TextureImporter;

			// apply texture import settings if there are no previous ones
			if (!animationSheet.hasPreviousTextureImportSettings)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spritePixelsPerUnit = sharedData.spritePixelsPerUnit;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Point;
#if UNITY_5_5_OR_NEWER
				importer.textureCompression = TextureImporterCompression.Uncompressed;
#else
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
#endif
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

			AssetDatabase.ImportAsset(imageAssetFile, ImportAssetOptions.ForceUpdate);

			Sprite[] createdSprites = GetAllSpritesFromAssetFile(imageAssetFile);
			animationSheet.ApplyCreatedSprites(createdSprites);
		}

		private static Sprite[] GetAllSpritesFromAssetFile(string imageFilename)
		{
			var assets = AssetDatabase.LoadAllAssetsAtPath(imageFilename);

			// make sure we only grab valid sprites here
			List<Sprite> sprites = new List<Sprite>();
			foreach (var item in assets)
			{
				if (item is Sprite)
				{
					sprites.Add(item as Sprite);
				}
			}

			return sprites.ToArray();
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
			string targetDirectory = sharedData.animationControllersTargetLocation.GetTargetDirectory(basePath);

			string pathForController = targetDirectory + "/" + name + ".controller";
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForController);

			return controller;
		}

		public AnimatorOverrideController GetExistingAnimatorOverrideController(string assetPath)
		{
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);
			string targetDirectory = sharedData.animationControllersTargetLocation.GetTargetDirectory(basePath);

			string pathForController = targetDirectory + "/" + name + ".overrideController";
			AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForController);

			return controller;
		}

		// ================================================================================
		//  automatic ReImport
		// --------------------------------------------------------------------------------

		/// <summary>
		/// will be called by the AssetPostProcessor
		/// </summary>
		public void AutomaticReImport(string[] assetPaths)
		{
			if (sharedData == null)
			{
				LoadOrCreateUserConfig();
			}

			List<AnimationImportJob> jobs = new List<AnimationImportJob>();

			foreach (var assetPath in assetPaths)
			{
				if (string.IsNullOrEmpty(assetPath))
				{
					continue;
				}

				if (HandleCustomReImport != null && HandleCustomReImport(assetPath))
				{
					continue;
				}

				AnimationImportJob job = CreateAnimationImportJob(assetPath);
				if (job != null)
				{
					if (HasExistingAnimatorController(assetPath))
					{
						job.importAnimatorController = ImportAnimatorController.AnimatorController;
					}
					else if (HasExistingAnimatorOverrideController(assetPath))
					{
						job.importAnimatorController = ImportAnimatorController.AnimatorOverrideController;
						job.useExistingAnimatorController = true;
					}

					jobs.Add(job);
				}
			}

			Import(jobs.ToArray());
		}		

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private AnimationImportJob CreateAnimationImportJob(string assetPath, string additionalCommandLineArguments = "")
		{
			AnimationImportJob importJob = new AnimationImportJob(assetPath);

			importJob.additionalCommandLineArguments = additionalCommandLineArguments;

			importJob.directoryPathForSprites = _sharedData.spritesTargetLocation.GetTargetDirectory(importJob.assetDirectory);
			importJob.directoryPathForAnimations = _sharedData.animationsTargetLocation.GetTargetDirectory(importJob.assetDirectory);
			importJob.directoryPathForAnimationControllers = _sharedData.animationControllersTargetLocation.GetTargetDirectory(importJob.assetDirectory);

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
	}
}
