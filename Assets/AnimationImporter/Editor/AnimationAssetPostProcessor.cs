using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;

namespace AnimationImporter
{
	public class AnimationAssetPostprocessor : AssetPostprocessor
	{
		private static List<string> _assetsMarkedForImport = new List<string>();
		private static EditorApplication.CallbackFunction _importDelegate;

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
		{
			AnimationImporter importer = AnimationImporter.Instance;

			if (importer == null)
			{
				return;
			}

			// Do not create shared config during AssetPostprocess, or else it will recreate an empty config
			importer.LoadUserConfig();

			// If no config exists, they can't have set up automatic importing so just return out.
			if (importer.sharedData == null)
			{
				return;
			}

			if (importer.sharedData.automaticImporting)
			{
				List<string> markedAssets = new List<string>();

				foreach (string asset in importedAssets)
				{
					if (AnimationImporter.IsValidAsset(asset))
					{
						MarkAssetForImport(asset, markedAssets);
					}
				}

				if (markedAssets.Count > 0)
				{
					_assetsMarkedForImport.Clear();
					_assetsMarkedForImport.AddRange(markedAssets);

					if (_importDelegate == null)
					{
						_importDelegate = new EditorApplication.CallbackFunction(ImportAssets);
					}

					// Subscribe to callback
					EditorApplication.update = Delegate.Combine(EditorApplication.update, _importDelegate) as EditorApplication.CallbackFunction;
				}
			}
		}

		// ================================================================================
		//  private methods
		// --------------------------------------------------------------------------------

		private static void MarkAssetForImport(string asset, List<string> markedAssets)
		{
			AnimationImporter importer = AnimationImporter.Instance;

			if (!importer.canImportAnimations)
			{
				return;
			}

			if ((AnimationImporter.HasCustomReImport != null && AnimationImporter.HasCustomReImport(asset))
				|| importer.HasExistingAnimatorController(asset)
				|| importer.HasExistingAnimatorOverrideController(asset))
			{
				markedAssets.Add(asset);
			}
		}

		private static void ImportAssets()
		{
			// Unsubscribe from callback
			EditorApplication.update = Delegate.Remove(EditorApplication.update, _importDelegate as EditorApplication.CallbackFunction) as EditorApplication.CallbackFunction;

			AssetDatabase.Refresh();
			AnimationImporter importer = AnimationImporter.Instance;

			importer.AutomaticReImport(_assetsMarkedForImport.ToArray());

			_assetsMarkedForImport.Clear();
		}
	}
}