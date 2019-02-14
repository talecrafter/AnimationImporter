using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AnimationImporter
{
	public enum PlaybackDirection
	{
		Forward, // default
		Reverse, // reversed frames
		PingPong // forward, then reverse
	}

	public class ImportedAnimation
	{
		public string name;

		public ImportedAnimationFrame[] frames = null;

		public bool isLooping = true;

		// final animation clip; saved here for usage when building the AnimatorController
		public AnimationClip animationClip;

		// ================================================================================
		//  temporary data, only used for first import
		// --------------------------------------------------------------------------------

		// assuming all sprites are in some array/list and an animation is defined as a continous list of indices
		public int firstSpriteIndex;
		public int lastSpriteIndex;

		// unity animations only play forward, so this will affect the way frames are added to the final animation clip
		public PlaybackDirection direction;

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

		/// <summary>
		/// Lists frames so that the final anim seems to play in the desired direction.
		/// *Attention:* Can return more than <see cref="Count"/> frames. 
		/// </summary>
		public IEnumerable<ImportedAnimationFrame> ListFramesAccountingForPlaybackDirection()
		{
			switch (direction)
			{
				default:
				case PlaybackDirection.Forward: // ex: 1, 2, 3, 4
					return frames;

				case PlaybackDirection.Reverse: // ex: 4, 3, 2, 1
					return frames.Reverse();

				case PlaybackDirection.PingPong: // ex: 1, 2, 3, 4, 3, 2
					return frames.Concat(frames.Skip(1).Take(frames.Length - 2).Reverse());
			}
		}
	}
}