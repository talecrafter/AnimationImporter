using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor;

namespace AnimationImporter
{
	public class AsepriteAnimationInfo
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

		public List<AsepriteFrame> frames = new List<AsepriteFrame>();
		public List<AsepriteAnimation> animations = new List<AsepriteAnimation>();

		public bool hasAnimations
		{
			get
			{
				return animations != null && animations.Count > 0;
			}
		}

		private Dictionary<string, AsepriteAnimation> _animationDatabase = null;

		// ================================================================================
		//  JSON IMPORT
		// --------------------------------------------------------------------------------

		public static AsepriteAnimationInfo GetAnimationInfo(JSONObject root)
		{
			if (root == null)
			{
				Debug.LogWarning("Error importing JSON animation info: JSONObject is NULL");
				return null;
			}

			AsepriteAnimationInfo importedInfos = new AsepriteAnimationInfo();

			// import all informations from JSON

			if (!root.ContainsKey("meta"))
			{
				Debug.LogWarning("Error importing JSON animation info: no 'meta' object");
				return null;
			}
			var meta = root["meta"].Obj;
			GetMetaInfosFromJSON(importedInfos, meta);
			GetAnimationsFromJSON(importedInfos, meta);

			if (!root.ContainsKey("frames"))
			{
				Debug.LogWarning("Error importing JSON animation info: no 'frames' object");
				return null;
			}
			GetSpritesFromJSON(root, importedInfos);

			importedInfos.CalculateTimings();

			return importedInfos;
		}

		private static void GetMetaInfosFromJSON(AsepriteAnimationInfo importedInfos, JSONObject meta)
		{
			var size = meta["size"].Obj;
			importedInfos.width = (int)size["w"].Number;
			importedInfos.height = (int)size["h"].Number;
		}

		private static void GetAnimationsFromJSON(AsepriteAnimationInfo importedInfos, JSONObject meta)
		{
			if (!meta.ContainsKey("frameTags"))
			{
				Debug.LogWarning("No 'frameTags' found in JSON created by Aseprite. Please use official Aseprite 1.1.1 or newer.");
				return;
			}

			var frameTags = meta["frameTags"].Array;
			foreach (var item in frameTags)
			{
				JSONObject frameTag = item.Obj;
				AsepriteAnimation anim = new AsepriteAnimation();
				anim.name = frameTag["name"].Str;
				anim.from = (int)(frameTag["from"].Number);
				anim.to = (int)(frameTag["to"].Number);

				importedInfos.animations.Add(anim);
			}
		}

		private static void GetSpritesFromJSON(JSONObject root, AsepriteAnimationInfo importedInfos)
		{
			var list = root["frames"].Array;
			foreach (var item in list)
			{
				AsepriteFrame frame = new AsepriteFrame();
				frame.filename = item.Obj["filename"].Str;

				var frameValues = item.Obj["frame"].Obj;
				frame.width = (int)frameValues["w"].Number;
				frame.height = (int)frameValues["h"].Number;
				frame.x = (int)frameValues["x"].Number;
				frame.y = importedInfos.height - (int)frameValues["y"].Number - frame.height; // unity has a different coord system

				frame.duration = (int)item.Obj["duration"].Number;

				importedInfos.frames.Add(frame);
			}
		}

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

		public void CreateAnimation(AsepriteAnimation anim, List<Sprite> sprites, string basePath, string masterName)
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
				type = typeof(SpriteRenderer),
				propertyName = "m_Sprite"
			};

			ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[anim.Count + 1]; // one more than sprites because we repeat the last sprite
			float timePoint;

			for (int i = 0; i < anim.Count; i++)
			{
				timePoint = anim.GetTimePoint(i);
				ObjectReferenceKeyframe keyFrame = new ObjectReferenceKeyframe { time = timePoint };

				Sprite sprite = sprites[i + anim.from];
				keyFrame.value = sprite;
				keyFrames[i] = keyFrame;
			}

			// repeating the last frame at a point "just before the end" so the animation gets its correct length

			timePoint = anim.GetTimePoint(anim.Count);
			ObjectReferenceKeyframe lastKeyFrame = new ObjectReferenceKeyframe { time = timePoint - (1f / clip.frameRate) };
			Sprite lastSprite = sprites[anim.Count - 1 + anim.from];
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
				AsepriteFrame frame = frames[i];
				SpriteMetaData sprite = new SpriteMetaData();

				// sprite alignment
				sprite.alignment = (int)spriteAlignment;
				if (spriteAlignment == SpriteAlignment.Custom)
				{
					sprite.pivot.x = customX;
					sprite.pivot.y = customY;
				}

				sprite.name = frame.filename.Replace(".ase","");
				sprite.rect = new Rect(frame.x, frame.y, frame.width, frame.height);

				metaData[i] = sprite;
			}

			return metaData;
		}

		public class AsepriteFrame
		{
			public string filename { get; set; }

			public int x;
			public int y;
			public int width;
			public int height;

			public int duration;
		}

		public class AsepriteAnimation
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
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private void BuildIndex()
		{
			_animationDatabase = new Dictionary<string, AsepriteAnimation>();

			for (int i = 0; i < animations.Count; i++)
			{
				AsepriteAnimation anim = animations[i];
				_animationDatabase[animations[i].name] = anim;
			}
		}

		private void CalculateTimings()
		{
			for (int i = 0; i < animations.Count; i++)
			{
				AsepriteAnimation anim = animations[i];

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
	}
}