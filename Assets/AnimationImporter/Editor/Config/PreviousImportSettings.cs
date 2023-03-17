using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.U2D.Sprites;
#endif

namespace AnimationImporter
{
	public class PreviousImportSettings
	{
#if UNITY_2021_2_OR_NEWER
		private SpriteRect _previousFirstSprite = null;
#else
		private SpriteMetaData? _previousFirstSprite = null;
#endif

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

#if UNITY_2021_2_OR_NEWER
				var factory = new SpriteDataProviderFactories();
				factory.Init();
				var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
				dataProvider.InitSpriteEditorDataProvider();

				var spriteRects = dataProvider.GetSpriteRects();

				if (spriteRects.Length > 0)
				{
					_previousFirstSprite = spriteRects[0];
				}
#else
				if (importer.spritesheet != null && importer.spritesheet.Length > 0)
				{
					_previousFirstSprite = importer.spritesheet[0];
				}
#endif
			}
		}

#if UNITY_2021_2_OR_NEWER
		public void ApplyPreviousTextureImportSettings(ISpriteEditorDataProvider dataProvider)
		{
			if (!_hasPreviousTextureImportSettings || dataProvider == null)
			{
				return;
			}

			// apply old pivot point settings
			// we assume every sprite should have the same pivot point
			if (_previousFirstSprite != null)
			{
				var spriteSheet = dataProvider.GetSpriteRects();

				for (int i = 0; i < spriteSheet.Length; i++)
				{
					var sprite = spriteSheet[i];

					sprite.alignment = _previousFirstSprite.alignment;
					sprite.pivot = _previousFirstSprite.pivot;
				}

				dataProvider.SetSpriteRects(spriteSheet);
			}
		}
#else
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
#endif
	}
}