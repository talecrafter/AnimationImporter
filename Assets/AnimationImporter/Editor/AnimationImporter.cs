using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;
using System.IO;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor.Animations;
using System.Linq;

namespace AnimationImporter
{
	public class AnimationImporter
	{
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
		//  const
		// --------------------------------------------------------------------------------

		private const string PREFS_PREFIX = "ANIMATION_IMPORTER_";

		private static string[] allowedExtensions = { "ase" };

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
					CheckIfApplicationIsValid();
				}
			}
		}

		// sprite import values
		private float _spritePixelsPerUnit = 100f;
		public float spritePixelsPerUnit
		{
			get
			{
				return _spritePixelsPerUnit;
			}
			set
			{
				if (_spritePixelsPerUnit != value)
				{
					_spritePixelsPerUnit = value;
					SaveUserConfig();
				}
			}
		}

		private AnimationTargetObjectType _targetObjectType = AnimationTargetObjectType.SpriteRenderer;
		public AnimationTargetObjectType targetObjectType
		{
			get
			{
				return _targetObjectType;
			}
			set
			{
				if (_targetObjectType != value)
				{
					_targetObjectType = value;
					SaveUserConfig();
				}
			}
		}

		private SpriteAlignment _spriteAlignment = SpriteAlignment.BottomCenter;
		public SpriteAlignment spriteAlignment
		{
			get
			{
				return _spriteAlignment;
			}
			set
			{
				if (_spriteAlignment != value)
				{
					_spriteAlignment = value;
					SaveUserConfig();
				}
			}
		}

		private float _spriteAlignmentCustomX = 0;
		public float spriteAlignmentCustomX
		{
			get
			{
				return _spriteAlignmentCustomX;
			}
			set
			{
				if (_spriteAlignmentCustomX != value)
				{
					_spriteAlignmentCustomX = value;
					SaveUserConfig();
				}
			}
		}

		private float _spriteAlignmentCustomY = 0;
		public float spriteAlignmentCustomY
		{
			get
			{
				return _spriteAlignmentCustomY;
			}
			set
			{
				if (_spriteAlignmentCustomY != value)
				{
					_spriteAlignmentCustomY = value;
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

		private int _saveSpritesToSubfolder = 1;
		public bool saveSpritesToSubfolder
		{
			get
			{
				return Convert.ToBoolean(_saveSpritesToSubfolder);
			}
			set
			{
				if (_saveSpritesToSubfolder != Convert.ToInt16(value))
				{
					_saveSpritesToSubfolder = Convert.ToInt16(value);
					SaveUserConfig();
				}
			}
		}
		private int _saveAnimationsToSubfolder = 1;
		public bool saveAnimationsToSubfolder
		{
			get
			{
				return Convert.ToBoolean(_saveAnimationsToSubfolder);
			}
			set
			{
				if (_saveAnimationsToSubfolder != Convert.ToInt16(value))
				{
					_saveAnimationsToSubfolder = Convert.ToInt16(value);
					SaveUserConfig();
				}
			}
		}

		private int _automaticImporting = 0;
		public bool automaticImporting
		{
			get
			{
				return Convert.ToBoolean(_automaticImporting);
			}
			set
			{
				if (_automaticImporting != Convert.ToInt16(value))
				{
					_automaticImporting = Convert.ToInt16(value);
					SaveUserConfig();
				}
			}
		}

		private List<string> _animationNamesThatDoNotLoop = new List<string>() { "death" };
		public List<string> animationNamesThatDoNotLoop { get { return _animationNamesThatDoNotLoop; } }

		// ================================================================================
		//  private
		// --------------------------------------------------------------------------------

		private bool _hasApplication = false;
		public bool canImportAnimations
		{
			get
			{
				return _hasApplication;
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

		public void LoadUserConfig()
		{
			if (EditorPrefs.HasKey(PREFS_PREFIX + "asepritePath"))
			{
				_asepritePath = EditorPrefs.GetString(PREFS_PREFIX + "asepritePath");
			}
			else
			{
				_asepritePath = AsepriteImporter.standardApplicationPath;

				if (!File.Exists(_asepritePath))
					_asepritePath = "";
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "spritePixelsPerUnit"))
			{
				_spritePixelsPerUnit = PlayerPrefs.GetFloat(PREFS_PREFIX + "spritePixelsPerUnit");
			}
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "spriteTargetObjectType"))
			{
				_targetObjectType = (AnimationTargetObjectType)PlayerPrefs.GetInt(PREFS_PREFIX + "spriteTargetObjectType");
			}
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "spriteAlignment"))
			{
				_spriteAlignment = (SpriteAlignment)PlayerPrefs.GetInt(PREFS_PREFIX + "spriteAlignment");
			}
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomX"))
			{
				_spriteAlignmentCustomX = PlayerPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomX");
			}
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomY"))
			{
				_spriteAlignmentCustomY = PlayerPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomY");
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "saveSpritesToSubfolder"))
			{
				_saveSpritesToSubfolder = PlayerPrefs.GetInt(PREFS_PREFIX + "saveSpritesToSubfolder");
			}
			if (PlayerPrefs.HasKey(PREFS_PREFIX + "saveAnimationsToSubfolder"))
			{
				_saveAnimationsToSubfolder = PlayerPrefs.GetInt(PREFS_PREFIX + "saveAnimationsToSubfolder");
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "automaticImporting"))
			{
				_automaticImporting = PlayerPrefs.GetInt(PREFS_PREFIX + "automaticImporting");
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "baseControllerPath"))
			{
				string baseControllerPath = PlayerPrefs.GetString(PREFS_PREFIX + "baseControllerPath");
				if (!string.IsNullOrEmpty(baseControllerPath))
				{
					_baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
				}
			}

			if (PlayerPrefs.HasKey(PREFS_PREFIX + "nonLoopCount"))
			{
				_animationNamesThatDoNotLoop = new List<string>();
				int count = PlayerPrefs.GetInt(PREFS_PREFIX + "nonLoopCount");

				for (int i = 0; i < count; i++)
				{
					if (PlayerPrefs.HasKey(PREFS_PREFIX + "nonLoopCount" + i.ToString()))
					{
						_animationNamesThatDoNotLoop.Add(PlayerPrefs.GetString(PREFS_PREFIX + "nonLoopCount" + i.ToString()));
					}
				}
			}

			CheckIfApplicationIsValid();
		}

		private void SaveUserConfig()
		{
			EditorPrefs.SetString(PREFS_PREFIX + "asepritePath", _asepritePath);

			PlayerPrefs.SetFloat(PREFS_PREFIX + "spritePixelsPerUnit", _spritePixelsPerUnit);
			PlayerPrefs.SetInt(PREFS_PREFIX + "spriteTargetObjectType", (int)_targetObjectType);
			PlayerPrefs.SetInt(PREFS_PREFIX + "spriteAlignment", (int)_spriteAlignment);
			PlayerPrefs.SetFloat(PREFS_PREFIX + "spriteAlignmentCustomX", _spriteAlignmentCustomX);
			PlayerPrefs.SetFloat(PREFS_PREFIX + "spriteAlignmentCustomY", _spriteAlignmentCustomY);

			PlayerPrefs.SetInt(PREFS_PREFIX + "saveSpritesToSubfolder", _saveSpritesToSubfolder);
			PlayerPrefs.SetInt(PREFS_PREFIX + "saveAnimationsToSubfolder", _saveAnimationsToSubfolder);

			PlayerPrefs.SetInt(PREFS_PREFIX + "automaticImporting", _automaticImporting);

			if (_baseController != null)
			{
				PlayerPrefs.SetString(PREFS_PREFIX + "baseControllerPath", AssetDatabase.GetAssetPath(_baseController));
			}
			else
			{
				PlayerPrefs.SetString(PREFS_PREFIX + "baseControllerPath", "");
			}

			PlayerPrefs.SetInt(PREFS_PREFIX + "nonLoopCount", _animationNamesThatDoNotLoop.Count);
			for (int i = 0; i < _animationNamesThatDoNotLoop.Count; i++)
			{
				PlayerPrefs.SetString(PREFS_PREFIX + "nonLoopCount" + i.ToString(), _animationNamesThatDoNotLoop[i]);
			}
		}

		public void RemoveAnimationThatDoesNotLoop(int index)
		{
			_animationNamesThatDoNotLoop.RemoveAt(index);
			SaveUserConfig();
		}

		public bool AddAnimationThatDoesNotLoop(string animationName)
		{
			if (string.IsNullOrEmpty(animationName) || _animationNamesThatDoNotLoop.Contains(animationName))
				return false;

			_animationNamesThatDoNotLoop.Add(animationName);
			SaveUserConfig();

			return true;
		}

		// ================================================================================
		//  import methods
		// --------------------------------------------------------------------------------

		// check if this is a valid file; we are only looking at the file extension here
		public static bool IsValidAsset(string path)
		{
			string name = Path.GetFileNameWithoutExtension(path);

			for (int i = 0; i < allowedExtensions.Length; i++)
			{
				string lastPart = "/" + name + "." + allowedExtensions[i];

				if (path.Contains(lastPart))
				{
					return true;
				}
			}

			return false;
		}

		public bool HasExistingAnimatorController(string assetPath)
		{
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);

			string pathForController = basePath + "/" + name + ".controller";
			AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForController);

			return controller != null;
		}

		public bool HasExistingAnimatorOverrideController(string assetPath)
		{
			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);

			string pathForController = basePath + "/" + name + ".overrideController";
			AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(pathForController);

			return controller != null;
		}

		public ImportedAnimationInfo CreateAnimationsForAssetFile(DefaultAsset droppedAsset)
		{
			return CreateAnimationsForAssetFile(AssetDatabase.GetAssetPath(droppedAsset));
		}

		public ImportedAnimationInfo CreateAnimationsForAssetFile(string assetPath)
		{
			if (!IsValidAsset(assetPath))
			{
				return null;
			}

			string name = Path.GetFileNameWithoutExtension(assetPath);
			string basePath = GetBasePath(assetPath);

			// we analyze import settings on existing files
			PreviousImportSettings previousAnimationInfo = CollectPreviousImportSettings(basePath, name);

			if (AsepriteImporter.CreateSpriteAtlasAndMetaFile(_asepritePath, basePath, name, saveSpritesToSubfolder))
			{
				AssetDatabase.Refresh();
				return ImportJSONAndCreateAnimations(basePath, name, previousAnimationInfo);
			}

			return null;
		}

		public void CreateAnimatorController(ImportedAnimationInfo animations)
		{
			AnimatorController controller;

			// check if controller already exists; use this to not loose any references to this in other assets
			string pathForAnimatorController = animations.basePath + "/" + animations.name + ".controller";
			controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(pathForAnimatorController);

			if (controller == null)
			{
				// create a new controller and place every animation as a state on the first layer
				controller = AnimatorController.CreateAnimatorControllerAtPath(animations.basePath + "/" + animations.name + ".controller");
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

		public void CreateAnimatorOverrideController(ImportedAnimationInfo animations, bool useExistingBaseController = false)
		{
			AnimatorOverrideController overrideController;

			// check if override controller already exists; use this to not loose any references to this in other assets
			string pathForOverrideController = animations.basePath + "/" + animations.name + ".overrideController";
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

		private ImportedAnimationInfo ImportJSONAndCreateAnimations(string basePath, string name, PreviousImportSettings previousImportSettings)
		{
			string imageAssetFilename = GetImageAssetFilename(basePath, name);
			string textAssetFilename = GetJSONAssetFilename(basePath, name);
			TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textAssetFilename);

			if (textAsset != null)
			{
				// parse the JSON file
				JSONObject jsonObject = JSONObject.Parse(textAsset.ToString());
				ImportedAnimationInfo animationInfo = AsepriteImporter.GetAnimationInfo(jsonObject);

				if (animationInfo == null)
					return null;

				animationInfo.previousImportSettings = previousImportSettings;

				animationInfo.basePath = basePath;
				animationInfo.name = name;
				animationInfo.nonLoopingAnimations = _animationNamesThatDoNotLoop;

				// delete JSON file afterwards
				AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(textAsset));

				CreateSprites(animationInfo, imageAssetFilename);

				CreateAnimations(animationInfo, imageAssetFilename);

				return animationInfo;
			}
			else
			{
				Debug.LogWarning("Problem with JSON file: " + textAssetFilename);
			}

			return null;
		}

		private void CreateAnimations(ImportedAnimationInfo animationInfo, string imageAssetFilename)
		{
			if (animationInfo.hasAnimations)
			{
				if (saveAnimationsToSubfolder)
				{
					string path = animationInfo.basePath + "/Animations";
					if (!Directory.Exists(path))
					{
						Directory.CreateDirectory(path);
					}

					CreateAnimationAssets(animationInfo, imageAssetFilename, path);
				}
				else
				{
					CreateAnimationAssets(animationInfo, imageAssetFilename, animationInfo.basePath);
				}
			}
		}

		private void CreateAnimationAssets(ImportedAnimationInfo animationInfo, string imageAssetFilename, string pathForAnimations)
		{
			string masterName = Path.GetFileNameWithoutExtension(imageAssetFilename);

			var assets = AssetDatabase.LoadAllAssetsAtPath(imageAssetFilename);
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
			sprites = sprites.OrderBy(x => int.Parse(x.name.Substring(x.name.LastIndexOf(' ')).TrimStart()))
							 .ToList();

			foreach (var animation in animationInfo.animations)
			{
				animationInfo.CreateAnimation(animation, sprites, pathForAnimations, masterName, targetObjectType);
			}
		}

		private void CreateSprites(ImportedAnimationInfo animations, string imageFile)
		{
			TextureImporter importer = AssetImporter.GetAtPath(imageFile) as TextureImporter;

			// apply texture import settings if there are no previous ones
			if (!animations.hasPreviousTextureImportSettings)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spritePixelsPerUnit = _spritePixelsPerUnit;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Point;
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			}

			// create sub sprites for this file according to the AsepriteAnimationInfo
			importer.spritesheet = animations.GetSpriteSheet(_spriteAlignment, _spriteAlignmentCustomX, _spriteAlignmentCustomY);

			// reapply old import settings (pivot settings for sprites)
			if (animations.hasPreviousTextureImportSettings)
			{
				animations.previousImportSettings.ApplyPreviousTextureImportSettings(importer);
			}

			// these values will be set in any case, not influenced by previous import settings
			importer.spriteImportMode = SpriteImportMode.Multiple;
			importer.maxTextureSize = animations.maxTextureSize;

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
		}

		private PreviousImportSettings CollectPreviousImportSettings(string basePath, string name)
		{
			PreviousImportSettings previousImportSettings = new PreviousImportSettings();

			previousImportSettings.GetTextureImportSettings(GetImageAssetFilename(basePath, name));

			return previousImportSettings;
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private void CheckIfApplicationIsValid()
		{
			_hasApplication = File.Exists(_asepritePath);
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
			if (saveSpritesToSubfolder)
				basePath += "/Sprites";

			return basePath + "/" + name + ".png";
		}

		private string GetJSONAssetFilename(string basePath, string name)
		{
			if (saveSpritesToSubfolder)
				basePath += "/Sprites";

			return basePath + "/" + name + ".json";
		}
	}
}
