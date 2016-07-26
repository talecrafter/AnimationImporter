using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;
using UnityEditor;
using System.Linq;

namespace AnimationImporter
{
	public class ImportedAnimationInfo
	{
		private Regex nonLoopingAnimationsRegex;
		private List<string> _nonLoopingAnimations;

		public string basePath { get; set; }
		public string name { get; set; }
		public List<string> nonLoopingAnimations
		{
			get
			{
				return _nonLoopingAnimations;
			}
			set
			{
				// Build a regex from the supplied values
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

				nonLoopingAnimationsRegex = new System.Text.RegularExpressions.Regex(regexString);

				_nonLoopingAnimations = value;
			}
		}

		public int width { get; set; }
		public int height { get; set; }
		public int maxTextureSize
		{
			get
			{
				return Mathf.Max(width, height);
			}			
		}

		public List<ImportedSpriteInfo> frames = new List<ImportedSpriteInfo>();
		public List<ImportedSingleAnimationInfo> animations = new List<ImportedSingleAnimationInfo>();

		public bool hasAnimations
		{
			get
			{
				return animations != null && animations.Count > 0;
			}
		}

		private Dictionary<string, ImportedSingleAnimationInfo> _animationDatabase = null;

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

			List<ImportedSingleAnimationInfo> similarAnimations = new List<ImportedSingleAnimationInfo>();
			foreach (var item in animations)
			{
				if (clipName.Contains(item.name))
					similarAnimations.Add(item);
			}

			if (similarAnimations.Count > 0)
			{
				ImportedSingleAnimationInfo similar = similarAnimations.OrderBy(x => x.name.Length).Reverse().First();
				return similar.animationClip;
			}

			return null;
		}

		public void CreateAnimation(ImportedSingleAnimationInfo anim, List<Sprite> sprites, string basePath, string masterName, AnimationTargetObjectType targetType)
		{
			AnimationClip clip;
            string fileName = basePath + "/" + masterName + "_" + anim.name + ".anim";
			bool isLooping = ShouldLoop(anim.name);

			// check if animation file already exists
			clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
			if (clip != null)
			{
				// get previous animation settings
				targetType = PreviousImportSettings.GetAnimationTargetFromExistingClip(clip);
				isLooping = clip.isLooping;
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

				Sprite sprite = sprites[anim.firstSpriteIndex + i];
				keyFrame.value = sprite;
				keyFrames[i] = keyFrame;
			}

			// repeating the last frame at a point "just before the end" so the animation gets its correct length

			ObjectReferenceKeyframe lastKeyFrame = new ObjectReferenceKeyframe { time = anim.GetLastKeyFrameTime(clip.frameRate) };

			Sprite lastSprite = sprites[anim.firstSpriteIndex + anim.Count - 1];
			lastKeyFrame.value = lastSprite;
			keyFrames[anim.Count] = lastKeyFrame;

			// save curve into clip, either for SpriteRenderer, Image, or both
			if (targetType == AnimationTargetObjectType.SpriteRenderer)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.spriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.imageCurveBinding, null);
			}
			else if (targetType == AnimationTargetObjectType.Image)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.spriteRendererCurveBinding, null);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.imageCurveBinding, keyFrames);
			}
			else if (targetType == AnimationTargetObjectType.SpriteRendererAndImage)
			{
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.spriteRendererCurveBinding, keyFrames);
				AnimationUtility.SetObjectReferenceCurve(clip, AnimationClipHelper.imageCurveBinding, keyFrames);
			}

			EditorUtility.SetDirty(clip);
			anim.animationClip = clip;
		}

		public SpriteMetaData[] GetSpriteSheet(SpriteAlignment spriteAlignment, float customX, float customY)
		{
			SpriteMetaData[] metaData = new SpriteMetaData[frames.Count];

			for (int i = 0; i < frames.Count; i++)
			{
				ImportedSpriteInfo spriteInfo = frames[i];
				SpriteMetaData spriteMetaData = new SpriteMetaData();

				// sprite alignment
				spriteMetaData.alignment = (int)spriteAlignment;
				if (spriteAlignment == SpriteAlignment.Custom)
				{
					spriteMetaData.pivot.x = customX;
					spriteMetaData.pivot.y = customY;
				}

				spriteMetaData.name = spriteInfo.name;
				spriteMetaData.rect = new Rect(spriteInfo.x, spriteInfo.y, spriteInfo.width, spriteInfo.height);

				metaData[i] = spriteMetaData;
			}

			return metaData;
		}

		public void CalculateTimings()
		{
			for (int i = 0; i < animations.Count; i++)
			{
				ImportedSingleAnimationInfo anim = animations[i];

				anim.SetFrames(frames.GetRange(anim.firstSpriteIndex, anim.Count));
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private bool ShouldLoop(string name)
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

		private void BuildIndex()
		{
			_animationDatabase = new Dictionary<string, ImportedSingleAnimationInfo>();

			for (int i = 0; i < animations.Count; i++)
			{
				ImportedSingleAnimationInfo anim = animations[i];
				_animationDatabase[anim.name] = anim;
			}
		}
	}

	public class ImportedSpriteInfo
	{
		// ================================================================================
		//  naming
		// --------------------------------------------------------------------------------

		private string _name;
		public string name
		{
			get { return _name; }
			set { _name = value; }
		}

		// ================================================================================
		//  properties
		// --------------------------------------------------------------------------------

		public int x;
		public int y;
		public int width;
		public int height;

		public int duration; // in milliseconds as part of an animation
	}

	public class ImportedSingleAnimationInfo
	{
		public string name;

		// assuming all sprites are in some array/list and an animation is defined as a continous list of indices
		public int firstSpriteIndex;
		public int lastSpriteIndex;
		
		// final animation clip; saved here for usage when building the AnimatorController
		public AnimationClip animationClip;

		// duration of each frame
		private List<float> timings = null;

		public int Count
		{
			get
			{
				return lastSpriteIndex - firstSpriteIndex + 1;
			}
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		public float GetKeyFrameTime(int i)
		{
			return timings[i];
		}

		public float GetLastKeyFrameTime(float frameRate)
		{
			float timePoint = GetKeyFrameTime(Count);
			timePoint -= (1f / frameRate);

			return timePoint;
		}

		public void SetFrames(List<ImportedSpriteInfo> frames)
		{
			float timeCount;
			timings = new List<float>();

			// first sprite will be set at the beginning of the animation
			timeCount = 0;
			timings.Add(timeCount);

			for (int k = 0; k < frames.Count; k++)
			{
				// add duration of frame in seconds
				timeCount += frames[k].duration / 1000f;
				timings.Add(timeCount);
			}
		}

		public override string ToString()
		{
			return name + " (" + firstSpriteIndex.ToString() + "-" + lastSpriteIndex.ToString() + ")";
		}
	}
}