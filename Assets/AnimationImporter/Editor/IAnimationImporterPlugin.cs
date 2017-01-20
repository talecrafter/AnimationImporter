using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AnimationImporter
{
	public interface IAnimationImporterPlugin
	{
		ImportedAnimationSheet Import(AnimationImportJob job, AnimationImporterSharedConfig config);
		bool IsValid();
	}
}