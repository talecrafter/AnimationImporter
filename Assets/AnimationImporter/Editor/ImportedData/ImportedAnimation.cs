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

	}
}