﻿using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AnimationImporter
{
	public class AnimationImporterSharedConfig : ScriptableObject
	{
		private const string PREFS_PREFIX = "ANIMATION_IMPORTER_";

		[SerializeField]
		private List<string> _animationNamesThatDoNotLoop = new List<string>() { "death" };
		public List<string> animationNamesThatDoNotLoop { get { return _animationNamesThatDoNotLoop; } }

		[SerializeField]
		private bool _automaticImporting = false;

		public bool automaticImporting
		{
			get
			{
				return _automaticImporting;
			}
			set
			{
				_automaticImporting = value;
			}
		}

		// sprite import values
		[SerializeField]
		private float _spritePixelsPerUnit = 100f;
		public float spritePixelsPerUnit
		{
			get
			{
				return _spritePixelsPerUnit;
			}
			set
			{
				_spritePixelsPerUnit = value;
			}
		}

		[SerializeField]
		private float _animationFrameRate = 60f;
		public float animationFrameRate
        {
            get
            {
				return _animationFrameRate;
            }
            set
            {
				_animationFrameRate = value;
            }
        }

		[SerializeField]
		private pivotAlignmentType _pivotAlignmentType = pivotAlignmentType.Normalized;
		public pivotAlignmentType pivotAlignmentType
		{
			get { return this._pivotAlignmentType; }
			set { this._pivotAlignmentType = value; }
		}


		[SerializeField]
		private AnimationTargetObjectType _targetObjectType = AnimationTargetObjectType.SpriteRenderer;
		public AnimationTargetObjectType targetObjectType
		{
			get
			{
				return _targetObjectType;
			}
			set
			{
				_targetObjectType = value;
			}
		}

		[SerializeField]
		private string _pathToSpriteRendererComponent = "";
		public string pathToSpriteRendererComponent
		{
			get
			{
				return _pathToSpriteRendererComponent;
			}
			set
			{
				_pathToSpriteRendererComponent = value;
			}
		}

		[SerializeField]
		private string _pathToImageComponent = "";
		public string pathToImageComponent
		{
			get
			{
				return _pathToImageComponent;
			}
			set
			{
				_pathToImageComponent = value;
			}
		}

		[SerializeField]
		private SpriteAlignment _spriteAlignment = SpriteAlignment.BottomCenter;
		public SpriteAlignment spriteAlignment
		{
			get
			{
				return _spriteAlignment;
			}
			set
			{
				_spriteAlignment = value;
			}
		}

		[SerializeField]
		private float _spriteAlignmentCustomX = 0;
		public float spriteAlignmentCustomX
		{
			get
			{
				return _spriteAlignmentCustomX;
			}
			set
			{
				_spriteAlignmentCustomX = value;
			}
		}

		[SerializeField]
		private float _spriteAlignmentCustomY = 0;
		public float spriteAlignmentCustomY
		{
			get
			{
				return _spriteAlignmentCustomY;
			}
			set
			{
				_spriteAlignmentCustomY = value;
			}
		}

		[SerializeField]
		private AssetTargetLocation _spritesTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SubDirectory, "Sprites");
		public AssetTargetLocation spritesTargetLocation
		{
			get { return _spritesTargetLocation; }
			set { _spritesTargetLocation = value; }
		}

		[SerializeField]
		private AssetTargetLocation _animationsTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SubDirectory, "Animations");
		public AssetTargetLocation animationsTargetLocation
		{
			get { return _animationsTargetLocation; }
			set { _animationsTargetLocation = value; }
		}

		[SerializeField]
		private AssetTargetLocation _animationControllersTargetLocation = new AssetTargetLocation(AssetTargetLocationType.SameDirectory, "Animations");
		public AssetTargetLocation animationControllersTargetLocation
		{
			get { return _animationControllersTargetLocation; }
			set { _animationControllersTargetLocation = value; }
		}

		[SerializeField]
		private SpriteNamingScheme _spriteNamingScheme = SpriteNamingScheme.Classic;
		public SpriteNamingScheme spriteNamingScheme
		{
			get { return _spriteNamingScheme; }
			set { _spriteNamingScheme = value; }
		}

		public void RemoveAnimationThatDoesNotLoop(int index)
		{
			animationNamesThatDoNotLoop.RemoveAt(index);
		}

		public bool AddAnimationThatDoesNotLoop(string animationName)
		{
			if (string.IsNullOrEmpty(animationName) || animationNamesThatDoNotLoop.Contains(animationName))
				return false;

			animationNamesThatDoNotLoop.Add(animationName);

			return true;
		}

		/// <summary>
		/// Specify if the Unity user has preferences for an older version of AnimationImporter
		/// </summary>
		/// <returns><c>true</c>, if the user has old preferences, <c>false</c> otherwise.</returns>
		public bool UserHasOldPreferences()
		{
			var pixelsPerUnityKey = PREFS_PREFIX + "spritePixelsPerUnit";
			return PlayerPrefs.HasKey(pixelsPerUnityKey) || EditorPrefs.HasKey(pixelsPerUnityKey);
		}

		private bool HasKeyInPreferences(string key)
		{
			return PlayerPrefs.HasKey(key) || EditorPrefs.HasKey(key);
		}

		private int GetIntFromPreferences(string intKey)
		{
			if (PlayerPrefs.HasKey(intKey))
			{
				return PlayerPrefs.GetInt(intKey);
			}
			else if (EditorPrefs.HasKey(intKey))
			{
				return EditorPrefs.GetInt(intKey);
			}
			else
			{
				return int.MinValue;
			}
		}

		private float GetFloatFromPreferences(string floatKey)
		{
			if (PlayerPrefs.HasKey(floatKey))
			{
				return PlayerPrefs.GetFloat(floatKey);
			}
			else if (EditorPrefs.HasKey(floatKey))
			{
				return EditorPrefs.GetFloat(floatKey);
			}
			else
			{
				return float.NaN;
			}
		}

		private bool GetBoolFromPreferences(string boolKey)
		{
			if (PlayerPrefs.HasKey(boolKey))
			{
				return System.Convert.ToBoolean(PlayerPrefs.GetInt(boolKey));
			}
			else if (EditorPrefs.HasKey(boolKey))
			{
				return EditorPrefs.GetBool(boolKey);
			}
			else
			{
				return false;
			}
		}

		private string GetStringFromPreferences(string stringKey)
		{
			if (PlayerPrefs.HasKey(stringKey))
			{
				return PlayerPrefs.GetString(stringKey);
			}
			else if (EditorPrefs.HasKey(stringKey))
			{
				return EditorPrefs.GetString(stringKey);
			}
			else
			{
				return string.Empty;
			}
		}
	}
}
