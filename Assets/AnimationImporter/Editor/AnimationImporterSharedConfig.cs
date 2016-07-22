using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
		private bool _saveSpritesToSubfolder = true;
		public bool saveSpritesToSubfolder
		{
			get
			{
				return _saveSpritesToSubfolder;
			}
			set
			{
				_saveSpritesToSubfolder = value;
			}
		}

		[SerializeField]
		private bool _saveAnimationsToSubfolder = true;
		public bool saveAnimationsToSubfolder
		{
			get
			{
				return _saveAnimationsToSubfolder;
			}
			set
			{
				_saveAnimationsToSubfolder = value;
			}
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
			return EditorPrefs.HasKey(PREFS_PREFIX + "spritePixelsPerUnit");
		}

		/// <summary>
		/// Copies shared data in from Preferences, used to upgrade versions of AnimationImporter
		/// </summary>
		public void CopyFromPreferences()
		{
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spritePixelsPerUnit"))
			{
				_spritePixelsPerUnit = EditorPrefs.GetFloat(PREFS_PREFIX + "spritePixelsPerUnit");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteTargetObjectType"))
			{
				_targetObjectType = (AnimationTargetObjectType)EditorPrefs.GetInt(PREFS_PREFIX + "spriteTargetObjectType");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignment"))
			{
				_spriteAlignment = (SpriteAlignment)EditorPrefs.GetInt(PREFS_PREFIX + "spriteAlignment");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomX"))
			{
				_spriteAlignmentCustomX = EditorPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomX");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "spriteAlignmentCustomY"))
			{
				_spriteAlignmentCustomY = EditorPrefs.GetFloat(PREFS_PREFIX + "spriteAlignmentCustomY");
			}

			if (EditorPrefs.HasKey(PREFS_PREFIX + "saveSpritesToSubfolder"))
			{
				_saveSpritesToSubfolder = EditorPrefs.GetBool(PREFS_PREFIX + "saveSpritesToSubfolder");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "saveAnimationsToSubfolder"))
			{
				_saveAnimationsToSubfolder = EditorPrefs.GetBool(PREFS_PREFIX + "saveAnimationsToSubfolder");
			}
			if (EditorPrefs.HasKey(PREFS_PREFIX + "automaticImporting"))
			{
				_automaticImporting = EditorPrefs.GetBool(PREFS_PREFIX + "automaticImporting");
			}

			// Find all nonLoopingClip Prefences, load them into the sharedData.
			int numOldClips = 0;
			string loopCountKey = PREFS_PREFIX + "nonLoopCount";
			if (EditorPrefs.HasKey(loopCountKey))
			{
				numOldClips = EditorPrefs.GetInt(loopCountKey);
			}

			for (int i = 0; i < numOldClips; ++i)
			{
				string clipKey = PREFS_PREFIX + "nonLoopCount" + i.ToString();

				// If the clip hasn't already been moved to the shared data, do it now.
				if (EditorPrefs.HasKey(clipKey))
				{
					var stringAtKey = EditorPrefs.GetString(clipKey);
					if (!_animationNamesThatDoNotLoop.Contains(stringAtKey))
					{
						_animationNamesThatDoNotLoop.Add(stringAtKey);
					}
				}
			}
		}
	}
}
