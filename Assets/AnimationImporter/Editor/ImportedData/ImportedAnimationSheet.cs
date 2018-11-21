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

		public void CreateAnimation(ImportedAnimation anim, string basePath, string masterName, AnimationTargetObjectType targetType)
		{
			AnimationClip clip;
            string fileName;
            if (string.IsNullOrEmpty(masterName))
                fileName = $"{basePath}/{anim.name}.anim";
            else
                fileName = $"{basePath}/{masterName}_{anim.name}.anim";

            bool isLooping = anim.isLooping;

			// check if animation file already exists
			clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
			if (clip != null)
			{
				// get previous animation settings
				targetType = PreviousImportSettings.GetAnimationTargetFromExistingClip(clip);
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

			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[anim.Count + 1]; // one more than sprites because we repeat the last sprite

			for (int i = 0; i < anim.Count; i++)
			{
				ObjectReferenceKeyframe keyFrame = new ObjectReferenceKeyframe { time = anim.GetKeyFrameTime(i) };

				Sprite sprite = anim.frames[i].sprite;
				keyFrame.value = sprite;
				keyFrames[i] = keyFrame;
			}

			// repeating the last frame at a point "just before the end" so the animation gets its correct length

			ObjectReferenceKeyframe lastKeyFrame = new ObjectReferenceKeyframe { time = anim.GetLastKeyFrameTime(clip.frameRate) };

			Sprite lastSprite = anim.frames[anim.Count - 1].sprite;
			lastKeyFrame.value = lastSprite;
			keyFrames[anim.Count] = lastKeyFrame;

			// save curve into clip, either for SpriteRenderer, Image, or both
			if (targetType == AnimationTargetObjectType.SpriteRenderer)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.spriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.imageCurveBinding, null);
			}
			else if (targetType == AnimationTargetObjectType.Image)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.spriteRendererCurveBinding, null);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.imageCurveBinding, keyFrames);
			}
			else if (targetType == AnimationTargetObjectType.SpriteRendererAndImage)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.spriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipUtility.imageCurveBinding, keyFrames);
			}

			EditorUtility.SetDirty(clip);
			anim.animationClip = clip;
		}

		public void ApplyGlobalFramesToAnimationFrames()
		{
			for (int i = 0; i < animations.Count; i++)
			{
				ImportedAnimation anim = animations[i];

				anim.SetFrames(frames.GetRange(anim.firstSpriteIndex, anim.Count).ToArray());
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

        static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 customOffset)
        {
            switch (alignment)
            {
                case SpriteAlignment.TopLeft:
                    return new Vector2(0f, 1f);
                case SpriteAlignment.TopCenter:
                    return new Vector2(0.5f, 1f);
                case SpriteAlignment.TopRight:
                    return new Vector2(1f, 1f);

                case SpriteAlignment.LeftCenter:
                    return new Vector2(0f, 0.5f);
                case SpriteAlignment.Center:
                    return new Vector2(0.5f, 0.5f);
                case SpriteAlignment.RightCenter:
                    return new Vector2(1f, 0.5f);

                case SpriteAlignment.BottomLeft:
                    return new Vector2(0f, 0f);
                case SpriteAlignment.BottomCenter:
                    return new Vector2(0.5f, 0f);
                case SpriteAlignment.BottomRight:
                    return new Vector2(1f, 0f);
            }

            return customOffset;
        }

        public SpriteMetaData[] GetSpriteSheet(SpriteAlignment spriteAlignment, Vector2 custom)
		{
            SpriteMetaData[] metaData = new SpriteMetaData[frames.Count];

			for (int i = 0; i < frames.Count; i++)
			{
				ImportedAnimationFrame spriteInfo = frames[i];
                SpriteMetaData spriteMetaData = new SpriteMetaData();

                // sprite alignment
                if (spriteInfo.trimmed)
                {
                    var pivotBeforeTrim = GetPivotValue(spriteAlignment, custom);
                    var originAfterTrim = spriteInfo.spriteSourceRect.position / spriteInfo.sourceSize;
                    var ratio = new Vector2(spriteInfo.sourceSize.x / spriteInfo.rect.size.x, spriteInfo.sourceSize.y / spriteInfo.rect.size.y);

                    spriteMetaData.alignment = (int)SpriteAlignment.Custom;
                    spriteMetaData.pivot = ratio * (pivotBeforeTrim - originAfterTrim);
                }
                else
                {
                    spriteMetaData.alignment = (int)spriteAlignment;
                    if (spriteAlignment == SpriteAlignment.Custom)
                    {
                        spriteMetaData.pivot.x = custom.x;
                        spriteMetaData.pivot.y = custom.y;
                    }
                }

				spriteMetaData.name = spriteInfo.name;
                spriteMetaData.rect = new Rect(spriteInfo.rect.position, spriteInfo.rect.size);

                metaData[i] = spriteMetaData;
			}

			return metaData;
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