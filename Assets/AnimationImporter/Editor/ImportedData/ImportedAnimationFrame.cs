using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AnimationImporter
{
	public class ImportedAnimationFrame
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
}