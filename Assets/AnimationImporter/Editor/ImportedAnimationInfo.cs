using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor;

namespace AnimationImporter
{
	public class ImportedAnimationInfo
	{
		public string basePath { get; set; }
		public string name { get; set; }
		public List<string> nonLoopingAnimations { get; set; }

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

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		public AnimationClip GetClip(string clipName)
		{
			if (_animationDatabase == null)
				BuildIndex();

			if (_animationDatabase.ContainsKey(clipName))
				return _animationDatabase[clipName].animationClip;

			return null;
		}

		public void CreateAnimation(ImportedSingleAnimationInfo anim, List<Sprite> sprites, string basePath, string masterName)
		{
			AnimationClip clip;
            string fileName = basePath + "/" + masterName + "_" + anim.name + ".anim";

			// check if animation file already exists
			clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
			if (clip == null)
			{
				clip = new AnimationClip();
				AssetDatabase.CreateAsset(clip, fileName);
			}

			// change loop settings
			if (ShouldLoop(anim.name))
			{
				clip.wrapMode = WrapMode.Loop;
				clip.SetLoop(true);
			}
			else
			{
				clip.wrapMode = WrapMode.Clamp;
				clip.SetLoop(false);
			}

			EditorCurveBinding curveBinding = new EditorCurveBinding
			{
				path = "", // assume SpriteRenderer is at same GameObject as AnimationController
				type = typeof(SpriteRenderer),
				propertyName = "m_Sprite"
			};

			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[anim.Count + 1]; // one more than sprites because we repeat the last sprite

			for (int i = 0; i < anim.Count; i++)
			{
				ObjectReferenceKeyframe keyFrame = new ObjectReferenceKeyframe { time = anim.GetTimePoint(i) };

				Sprite sprite = sprites[anim.from + i];
				keyFrame.value = sprite;
				keyFrames[i] = keyFrame;
			}

			// repeating the last frame at a point "just before the end" so the animation gets its correct length

			ObjectReferenceKeyframe lastKeyFrame = new ObjectReferenceKeyframe { time = anim.GetLastTimePoint(clip.frameRate) };

			Sprite lastSprite = sprites[anim.from + anim.Count - 1];
			lastKeyFrame.value = lastSprite;
			keyFrames[anim.Count] = lastKeyFrame;

			// save animation clip values
			AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);
			EditorUtility.SetDirty(clip);
			anim.animationClip = clip;
		}

		private bool ShouldLoop(string name)
		{
			if (nonLoopingAnimations.Contains(name))
			{
				return false;
			}
			
			return true;
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

				spriteMetaData.name = spriteInfo.filename.Replace(".ase","");
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

				anim.timings = new List<float>();
				float timeCount = 0;
				anim.timings.Add(timeCount);

				for (int k = 0; k < anim.Count; k++)
				{
					timeCount += frames[k + anim.from].duration / 1000f;
					anim.timings.Add(timeCount);
				}
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private void BuildIndex()
		{
			_animationDatabase = new Dictionary<string, ImportedSingleAnimationInfo>();

			for (int i = 0; i < animations.Count; i++)
			{
				ImportedSingleAnimationInfo anim = animations[i];
				_animationDatabase[animations[i].name] = anim;
			}
		}
	}

	public class ImportedSpriteInfo
	{
		public string filename { get; set; }

		public int x;
		public int y;
		public int width;
		public int height;

		public int duration;
	}

	public class ImportedSingleAnimationInfo
	{
		public string name;
		public int from;
		public int to;

		public List<float> timings;

		public AnimationClip animationClip;

		public int Count
		{
			get
			{
				return to - from + 1;
			}
		}

		public override string ToString()
		{
			return name + " (" + from.ToString() + "-" + to.ToString() + ")";
		}

		public float GetTimePoint(int i)
		{
			return timings[i];
		}

		public float GetLastTimePoint(float frameRate)
		{
			float timePoint = GetTimePoint(Count);
			timePoint -= (1f / frameRate);

			return timePoint;
		}
	}
}