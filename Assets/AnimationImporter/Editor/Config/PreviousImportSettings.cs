using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;

namespace AnimationImporter
{
	public class PreviousImportSettings
	{
		private SpriteMetaData? _previousFirstSprite = null;

		private bool _hasPreviousTextureImportSettings = false;
		public bool hasPreviousTextureImportSettings
		{
			get
			{
				return _hasPreviousTextureImportSettings;
			}
		}

		// ================================================================================
		//  public methods
		// --------------------------------------------------------------------------------

		public void GetTextureImportSettings(string filename)
		{
			TextureImporter importer = AssetImporter.GetAtPath(filename) as TextureImporter;

			if (importer != null)
			{
				_hasPreviousTextureImportSettings = true;

				if (importer.spritesheet != null && importer.spritesheet.Length > 0)
				{
					_previousFirstSprite = importer.spritesheet[0];
				}
			}
		}

		public void ApplyPreviousTextureImportSettings(TextureImporter importer)
		{
			if (!_hasPreviousTextureImportSettings|| importer == null)
			{
				return;
			}

			// apply old pivot point settings
			// we assume every sprite should have the same pivot point
			if (_previousFirstSprite.HasValue)
			{
				var spritesheet = importer.spritesheet; // read values

				for (int i = 0; i < spritesheet.Length; i++)
				{
					spritesheet[i].alignment = _previousFirstSprite.Value.alignment;
					spritesheet[i].pivot = _previousFirstSprite.Value.pivot;
				}

				importer.spritesheet = spritesheet; // write values
			}
		}

		// ================================================================================
		//  analyzing animations
		// --------------------------------------------------------------------------------

		public static AnimationTargetObjectType GetAnimationTargetFromExistingClip(AnimationClip clip)
		{
			var curveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

			bool targetingSpriteRenderer = false;
			bool targetingImage = false;

			for (int i = 0; i < curveBindings.Length; i++)
			{
				if (curveBindings[i].type == typeof(SpriteRenderer))
				{
					targetingSpriteRenderer = true;
				}
				else if (curveBindings[i].type == typeof(UnityEngine.UI.Image))
				{
					targetingImage = true;
				}
			}

			if (targetingSpriteRenderer && targetingImage)
			{
				return AnimationTargetObjectType.SpriteRendererAndImage;
			}
			else if (targetingImage)
			{
				return AnimationTargetObjectType.Image;
			}
			else
			{
				return AnimationTargetObjectType.SpriteRenderer;
			}
		}
	}
}