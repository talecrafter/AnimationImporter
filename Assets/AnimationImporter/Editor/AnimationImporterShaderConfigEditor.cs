using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AnimationImporter
{
	[CustomEditor(typeof(AnimationImporterSharedConfig))]
	public class AnimationImporterShaderConfigEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			GUI.enabled = false;
			base.OnInspectorGUI ();
			GUI.enabled = true;
		}
	}
}