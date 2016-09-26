using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AnimationImporter
{
	public class ImportedAnimation
	{
		public string name;

		public ImportedAnimationFrame[] frames = null;

		public bool isLooping = true;

		// duration of each frame
		private List<float> timings = null;

		// final animation clip; saved here for usage when building the AnimatorController
		public AnimationClip animationClip;

		// ================================================================================
		//  temporary data, only used for first import
		// --------------------------------------------------------------------------------

		// assuming all sprites are in some array/list and an animation is defined as a continous list of indices
		public int firstSpriteIndex;
		public int lastSpriteIndex;

		// used with the indices because we to not have the Frame array yet
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

		public void SetFrames(ImportedAnimationFrame[] frames)
		{
			this.frames = frames;

			CalculateKeyFrameTimings();
		}

		// ================================================================================
		//  Key Frames
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

		private void CalculateKeyFrameTimings()
		{
			float timeCount;
			timings = new List<float>();

			// first sprite will be set at the beginning of the animation
			timeCount = 0;
			timings.Add(timeCount);

			for (int k = 0; k < frames.Length; k++)
			{
				// add duration of frame in seconds
				timeCount += frames[k].duration / 1000f;
				timings.Add(timeCount);
			}
		}
	}
}