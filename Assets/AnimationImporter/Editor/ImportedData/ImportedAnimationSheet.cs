using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Linq;

namespace AnimationImporter
{
	public class ImportedAnimationSheet
	{
		public string name { get; set; }
		public string assetDirectory { get; set; }

		public int width { get; set; }
		public int height { get; set; }
		public int maxTextureSize
		{
			get
			{
				return Mathf.Max(width, height);
			}
		}

		public List<ImportedAnimationFrame> frames = new List<ImportedAnimationFrame>();
		public List<ImportedAnimation> animations = new List<ImportedAnimation>();

		public bool hasAnimations
		{
			get
			{
				return animations != null && animations.Count > 0;
			}
		}

		private Dictionary<string, ImportedAnimation> _animationDatabase = null;

		private PreviousImportSettings _previousImportSettings = null;
		public PreviousImportSettings previousImportSettings
		{
			get
			{
				return _previousImportSettings;
			}
			set
			{
				_previousImportSettings = value;
			}
		}
		public bool hasPreviousTextureImportSettings
		{
			get
			{
				return _previousImportSettings != null && _previousImportSettings.hasPreviousTextureImportSettings;
			}
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		// get animation by name; used when updating an existing AnimatorController 
		public AnimationClip GetClip(string clipName)
		{
			if (_animationDatabase == null)
				BuildIndex();

			if (_animationDatabase.ContainsKey(clipName))
				return _animationDatabase[clipName].animationClip;

			return null;
		}

		/* 
			get animation by name; used when creating an AnimatorOverrideController
			we look for similar names so the OverrideController is still functional in cases where more specific or alternative animations are not present
			idle <- idle
			idleAlt <- idle
		*/
		public AnimationClip GetClipOrSimilar(string clipName)
		{
			AnimationClip clip = GetClip(clipName);

			if (clip != null)
				return clip;

			List<ImportedAnimation> similarAnimations = new List<ImportedAnimation>();
			foreach (var item in animations)
			{
				if (clipName.Contains(item.name))
					similarAnimations.Add(item);
			}

			if (similarAnimations.Count > 0)
			{
				ImportedAnimation similar = similarAnimations.OrderBy(x => x.name.Length).Reverse().First();
				return similar.animationClip;
			}

			return null;
		}

		public void CreateAnimation(ImportedAnimation anim, string basePath, string masterName, AnimationTargetObjectType targetType, string spriteRendererComponentPath, string imageComponentPath)
		{
			AnimationClip clip;
			string fileName = basePath + "/" + masterName + "_" + anim.name + ".anim";
			bool isLooping = anim.isLooping;

			// check if animation file already exists
			clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
			if (clip != null)
			{
				// get previous animation settings
				targetType = AnimationClipUtility.GetAnimationTargetFromExistingClip(clip);

				// get path(s) to SpriteRenderer and Image Components from previous clip
				AnimationClipUtility.GetComponentPathsFromExistingClip(clip, targetType, out spriteRendererComponentPath, out imageComponentPath);
			}
			else
			{
				clip = new AnimationClip();
				AssetDatabase.CreateAsset(clip, fileName);
			}

			// change loop settings
			if (isLooping)
			{
				clip.wrapMode = WrapMode.Loop;
				clip.SetLoop(true);
			}
			else
			{
				clip.wrapMode = WrapMode.Clamp;
				clip.SetLoop(false);
			}

			// convert keyframes
			ImportedAnimationFrame[] srcKeyframes = anim.ListFramesAccountingForPlaybackDirection().ToArray();
			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[srcKeyframes.Length + 1];
			float timeOffset = 0f;

			for (int i = 0; i < srcKeyframes.Length; i++)
			{
				// first sprite will be set at the beginning (t=0) of the animation
				keyFrames[i] = new ObjectReferenceKeyframe
				{
					time = timeOffset,
					value = srcKeyframes[i].sprite
				};

				// add duration of frame in seconds
				timeOffset += srcKeyframes[i].duration / 1000f;
			}

			// repeating the last frame at a point "just before the end" so the animation gets its correct length
			keyFrames[srcKeyframes.Length] = new ObjectReferenceKeyframe
			{
				time = timeOffset - (1f / clip.frameRate), // substract the duration of one frame
				value = srcKeyframes.Last().sprite
			};

			// save curve into clip, either for SpriteRenderer, Image, or both
			if (targetType == AnimationTargetObjectType.SpriteRenderer)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetSpriteRendererCurveBinding(spriteRendererComponentPath), keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetImageCurveBinding(imageComponentPath), null);
			}
			else if (targetType == AnimationTargetObjectType.Image)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetSpriteRendererCurveBinding(spriteRendererComponentPath), null);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetImageCurveBinding(imageComponentPath), keyFrames);
			}
			else if (targetType == AnimationTargetObjectType.SpriteRendererAndImage)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetSpriteRendererCurveBinding(spriteRendererComponentPath), keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.GetImageCurveBinding(imageComponentPath), keyFrames);
			}

			EditorUtility.SetDirty(clip);
			anim.animationClip = clip;
		}

		public void ApplyGlobalFramesToAnimationFrames()
		{
			for (int i = 0; i < animations.Count; i++)
			{
				ImportedAnimation anim = animations[i];

				anim.frames = frames.GetRange(anim.firstSpriteIndex, anim.Count).ToArray();
			}
		}

		// ================================================================================
		//  determine looping state of animations
		// --------------------------------------------------------------------------------

		public void SetNonLoopingAnimations(List<string> nonLoopingAnimationNames)
		{
			Regex nonLoopingAnimationsRegex = GetRegexFromNonLoopingAnimationNames(nonLoopingAnimationNames);

			foreach (var item in animations)
			{
				item.isLooping = ShouldLoop(nonLoopingAnimationsRegex, item.name);
			}
		}

		private bool ShouldLoop(Regex nonLoopingAnimationsRegex, string name)
		{
			if (!string.IsNullOrEmpty(nonLoopingAnimationsRegex.ToString()))
			{
				if (nonLoopingAnimationsRegex.IsMatch(name))
				{
					return false;
				}
			}

			return true;
		}

		private Regex GetRegexFromNonLoopingAnimationNames(List<string> value)
		{
			string regexString = string.Empty;
			if (value.Count > 0)
			{
				// Add word boundaries to treat non-regular expressions as exact names
				regexString = string.Concat("\\b", value[0], "\\b");
			}

			for (int i = 1; i < value.Count; i++)
			{
				string anim = value[i];
				// Add or to speed up the test rather than building N regular expressions
				regexString = string.Concat(regexString, "|", "\\b", anim, "\\b");
			}

			return new System.Text.RegularExpressions.Regex(regexString);
		}

		// ================================================================================
		//  Sprite Data
		// --------------------------------------------------------------------------------

		public SpriteMetaData[] GetSpriteSheet(
			SpriteAlignment spriteAlignment,
			PivotAlignmentType pivotAlignmentType,
			float customX,
			float customY
		)
		{
			SpriteMetaData[] metaData = new SpriteMetaData[frames.Count];

			for (int i = 0; i < frames.Count; i++)
			{
				ImportedAnimationFrame spriteInfo = frames[i];

				SpriteMetaData spriteMetaData = new SpriteMetaData();

				spriteMetaData.name = spriteInfo.name;
				spriteMetaData.rect = new Rect(spriteInfo.x, spriteInfo.y, spriteInfo.width, spriteInfo.height);

				// sprite alignment
				spriteMetaData.alignment = (int)spriteAlignment;
				spriteMetaData.pivot = GetSpritePivot(spriteAlignment, pivotAlignmentType, customX, customY, spriteInfo.width, spriteInfo.height);

				metaData[i] = spriteMetaData;
			}

			return metaData;
		}

#if UNITY_2021_2_OR_NEWER
		public SpriteRect[] GetSpriteSheet(
			SpriteRect[] previousSpriteRects,
			SpriteAlignment spriteAlignment,
			PivotAlignmentType pivotAlignmentType,
			float customX,
			float customY)
		{
			SpriteRect[] spriteRects = new SpriteRect[frames.Count];

			for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
			{
				ImportedAnimationFrame spriteInfo = frames[frameIndex];

				SpriteRect spriteRect = new SpriteRect();

				spriteRect.name = spriteInfo.name;
				spriteRect.rect = new Rect(spriteInfo.x, spriteInfo.y, spriteInfo.width, spriteInfo.height);

				// sprite alignment
				spriteRect.alignment = spriteAlignment;
				spriteRect.pivot = GetSpritePivot(spriteAlignment, pivotAlignmentType, customX, customY, spriteInfo.width, spriteInfo.height);

				spriteRects[frameIndex] = spriteRect;
			}

			// applying the spriteIDs from the previous sprites;
			// this is a very simple implementation, reassigning IDs on a first come first serve basis;
			// an improved implementation might take the sprite names into account to find the correct pairs
			// if a Sprite naming style was used that takes animation names into account
			for (int spriteIndex = 0; spriteIndex < spriteRects.Length; spriteIndex++)
			{
				if (spriteIndex < previousSpriteRects.Length
					&& IsValidGUID(previousSpriteRects[spriteIndex].spriteID))
				{
					spriteRects[spriteIndex].spriteID = previousSpriteRects[spriteIndex].spriteID;
				}

				// if a new spriteId is needed, it got assigned already through the SpriteRect constructor
			}

			return spriteRects;
		}
#endif

		private static GUID[] INVALID_GUIDS = new GUID[]
			{
				new GUID("00000000000000000800000000000000")
			};

		public bool IsValidGUID(GUID guid)
		{
			if (guid.Empty())
			{
				return false;
			}

			foreach (var invalidGUID in INVALID_GUIDS)
			{
				if (guid == invalidGUID)
				{
					return false;
				}
			}

			return true;
		}

		public void ApplySpriteNamingScheme(SpriteNamingScheme namingScheme)
		{
			const string NAME_DELIMITER = "_";

			if (namingScheme == SpriteNamingScheme.Classic)
			{
				for (int i = 0; i < frames.Count; i++)
				{
					frames[i].name = name + " " + i.ToString();
				}
			}
			else
			{
				foreach (var anim in animations)
				{
					for (int i = 0; i < anim.frames.Length; i++)
					{
						var animFrame = anim.frames[i];

						switch (namingScheme)
						{
							case SpriteNamingScheme.FileAnimationZero:
								animFrame.name = name + NAME_DELIMITER + anim.name + NAME_DELIMITER + i.ToString();
								break;
							case SpriteNamingScheme.FileAnimationOne:
								animFrame.name = name + NAME_DELIMITER + anim.name + NAME_DELIMITER + (i + 1).ToString();
								break;
							case SpriteNamingScheme.AnimationZero:
								animFrame.name = anim.name + NAME_DELIMITER + i.ToString();
								break;
							case SpriteNamingScheme.AnimationOne:
								animFrame.name = anim.name + NAME_DELIMITER + (i + 1).ToString();
								break;
						}
					}
				}
			}

			// remove unused frames from the list so they don't get created for the sprite sheet
			for (int i = frames.Count - 1; i >= 0; i--)
			{
				if (string.IsNullOrEmpty(frames[i].name))
				{
					frames.RemoveAt(i);
				}
			}
		}

		public void ApplyCreatedSprites(Sprite[] sprites)
		{
			if (sprites == null)
			{
				return;
			}

			// add final Sprites to frames by comparing names
			// as we can't be sure about the right order of the sprites
			for (int i = 0; i < sprites.Length; i++)
			{
				Sprite sprite = sprites[i];

				for (int k = 0; k < frames.Count; k++)
				{
					if (frames[k].name == sprite.name)
					{
						frames[k].sprite = sprite;
						break;
					}
				}
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private static Vector2 GetSpritePivot(SpriteAlignment spriteAlignment, PivotAlignmentType pivotAlignmentType, float customX, float customY, int spriteWidth, int spriteHeight)
		{
			if (spriteAlignment == SpriteAlignment.Custom)
			{
				if (pivotAlignmentType == PivotAlignmentType.Pixels)
				{
					return new Vector2(customX / spriteWidth, customY / spriteHeight);
				}
				else
				{
					return new Vector2(customX, customY);
				}
			}
			else
			{
				// we're setting a correct pivot value even if not custom;
				// technically this is not needed but Unity might set it to this value anyway at some point,
				// by setting it ourselves we prevent meta file changes
				return GetSpritePivotForStandardAlignment(spriteAlignment);
			}
		}

		private static Vector2 GetSpritePivotForStandardAlignment(SpriteAlignment alignment)
		{
			switch (alignment)
			{
				case SpriteAlignment.Center:
					return new Vector2(0.5f, 0.5f);
				case SpriteAlignment.TopLeft:
					return new Vector2(0f, 1f);
				case SpriteAlignment.TopCenter:
					return new Vector2(0.5f, 1f);
				case SpriteAlignment.TopRight:
					return new Vector2(1f, 1f);
				case SpriteAlignment.LeftCenter:
					return new Vector2(0f, 0.5f);
				case SpriteAlignment.RightCenter:
					return new Vector2(1f, 0.5f);
				case SpriteAlignment.BottomLeft:
					return new Vector2(0f, 0f);
				case SpriteAlignment.BottomCenter:
					return new Vector2(0.5f, 0f);
				case SpriteAlignment.BottomRight:
					return new Vector2(1f, 0f);
			}

			return default;
		}

		private void BuildIndex()
		{
			_animationDatabase = new Dictionary<string, ImportedAnimation>();

			for (int i = 0; i < animations.Count; i++)
			{
				ImportedAnimation anim = animations[i];
				_animationDatabase[anim.name] = anim;
			}
		}
	}
}
