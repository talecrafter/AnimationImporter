using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimationImporter
{
	/// <summary>
	/// Utility functions for ScriptableObjects.
	/// </summary>
	public static class ScriptableObjectUtility
	{
		/// <summary>
		/// Loads the save data from a Unity relative path. Returns null if the data doesn't exist.
		/// </summary>
		/// <returns>The saved data as a ScriptableObject, or null if not found.</returns>
		/// <param name="unityPathToFile">Unity path to file (e.g. "Assets/Resources/MyFile.asset")</param>
		/// <typeparam name="T">The ScriptableObject type</typeparam>
		public static T LoadSaveData<T> (string unityPathToFile) where T : ScriptableObject
		{
			// Path must contain Resources folder
			var resourcesFolder = string.Concat(
				AssetDatabaseUtility.UnityDirectorySeparator,
				AssetDatabaseUtility.ResourcesFolderName,
				AssetDatabaseUtility.UnityDirectorySeparator);
			if (!unityPathToFile.Contains(resourcesFolder))
			{
				var exceptionMessage = string.Format(
					"Failed to Load ScriptableObject of type, {0}, from path: {1}. " +
					"Path must begin with Assets and include a directory within the Resources folder.",
					typeof(T).ToString(),
					unityPathToFile);
				throw new UnityException(exceptionMessage);
			}

			// Get Resource relative path - Resource path should only include folders underneath Resources and no file extension
			var resourceRelativePath = GetResourceRelativePath(unityPathToFile);

			// Remove file extension
			var fileExtension = System.IO.Path.GetExtension(unityPathToFile);
			resourceRelativePath = resourceRelativePath.Replace(fileExtension, string.Empty);

			return Resources.Load<T>(resourceRelativePath);
		}

		/// <summary>
		/// Loads the saved data, stored as a ScriptableObject at the specified path. If the file or folders don't exist,
		/// it creates them.
		/// </summary>
		/// <returns>The saved data as a ScriptableObject.</returns>
		/// <param name="unityPathToFile">Unity path to file (e.g. "Assets/Resources/MyFile.asset")</param>
		/// <typeparam name="T">The ScriptableObject type</typeparam>
		public static T LoadOrCreateSaveData<T>(string unityPathToFile) where T : ScriptableObject
		{
			var loadedSettings = LoadSaveData<T>(unityPathToFile);
			if (loadedSettings == null)
			{
				loadedSettings = ScriptableObject.CreateInstance<T>();
				AssetDatabaseUtility.CreateAssetAndDirectories(loadedSettings, unityPathToFile);
			}

			return loadedSettings;
		}

		private static string GetResourceRelativePath(string unityPath)
		{
			var resourcesFolder = AssetDatabaseUtility.ResourcesFolderName + AssetDatabaseUtility.UnityDirectorySeparator;
			var pathToResources = unityPath.Substring(0, unityPath.IndexOf(resourcesFolder));

			// Remove all folders leading up to the Resources folder
			pathToResources = unityPath.Replace(pathToResources, string.Empty);

			// Remove the Resources folder
			pathToResources = pathToResources.Replace(resourcesFolder, string.Empty);

			return pathToResources;
		}
	}
}