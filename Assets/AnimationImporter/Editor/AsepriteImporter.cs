using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor;
using System.IO;

namespace AnimationImporter
{
	public class AsepriteImporter
	{
		// ================================================================================
		//  const
		// --------------------------------------------------------------------------------

		const string ASEPRITE_STANDARD_PATH_WINDOWS = @"C:\Program Files (x86)\Aseprite\Aseprite.exe";
		const string ASEPRITE_STANDARD_PATH_MACOSX = @"/Applications/Aseprite.app/Contents/MacOS/aseprite";

		public static string standardApplicationPath
		{
			get
			{
				if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					return ASEPRITE_STANDARD_PATH_WINDOWS;
				}
				else
				{
					return ASEPRITE_STANDARD_PATH_MACOSX;
				}
			}
		}

		// ================================================================================
		//  calling Aseprite for creating PNG and JSON files
		// --------------------------------------------------------------------------------

		/// <summary>
		/// calls the Aseprite application which then should output a png with all sprites and a corresponding JSON
		/// </summary>
		/// <returns></returns>
		public static bool CreateSpriteAtlasAndMetaFile(string asepritePath, string path, string name, bool saveSpritesToSubfolder = true)
		{
			string parameters = "'" + name + ".ase' --data '" + name + ".json' --sheet '" + name + ".png' --sheet-pack --list-tags --format json-array";
			bool success = CallAsepriteCLI(asepritePath, path, parameters) == 0;

			// move png and json file to subfolder
			if (success && saveSpritesToSubfolder)
			{
				// create subdirectory
				if (!Directory.Exists(path + "/Sprites"))
					Directory.CreateDirectory(path + "/Sprites");

				string target = path + "/Sprites/" + name + ".json";
				if (File.Exists(target))
					File.Delete(target);
				File.Move(path + "/" + name + ".json", target);

				target = path + "/Sprites/" + name + ".png";
				if (File.Exists(target))
					File.Delete(target);
				File.Move(path + "/" + name + ".png", target);
			}

			return success;
		}

		private static int CallAsepriteCLI(string asepritePath, string path, string buildOptions)
		{
			string workingDirectory = Application.dataPath.Replace("Assets", "") + path;

			System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
			start.Arguments = "-b " + buildOptions;
			start.FileName = asepritePath;
			start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			start.CreateNoWindow = true;
			start.UseShellExecute = false;
			start.WorkingDirectory = workingDirectory;

			// Run the external process & wait for it to finish
			using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start(start))
			{
				proc.WaitForExit();
				// Retrieve the app's exit code
				return proc.ExitCode;
			}
		}

		// ================================================================================
		//  JSON IMPORT
		// --------------------------------------------------------------------------------

		public static ImportedAnimationInfo GetAnimationInfo(JSONObject root)
		{
			if (root == null)
			{
				Debug.LogWarning("Error importing JSON animation info: JSONObject is NULL");
				return null;
			}

			ImportedAnimationInfo importedInfos = new ImportedAnimationInfo();

			// import all informations from JSON

			if (!root.ContainsKey("meta"))
			{
				Debug.LogWarning("Error importing JSON animation info: no 'meta' object");
				return null;
			}
			var meta = root["meta"].Obj;
			GetMetaInfosFromJSON(importedInfos, meta);
			GetAnimationsFromJSON(importedInfos, meta);

			if (!root.ContainsKey("frames"))
			{
				Debug.LogWarning("Error importing JSON animation info: no 'frames' object");
				return null;
			}
			GetSpritesFromJSON(root, importedInfos);

			importedInfos.CalculateTimings();

			return importedInfos;
		}

		private static void GetMetaInfosFromJSON(ImportedAnimationInfo importedInfos, JSONObject meta)
		{
			var size = meta["size"].Obj;
			importedInfos.width = (int)size["w"].Number;
			importedInfos.height = (int)size["h"].Number;
		}

		private static void GetAnimationsFromJSON(ImportedAnimationInfo importedInfos, JSONObject meta)
		{
			if (!meta.ContainsKey("frameTags"))
			{
				Debug.LogWarning("No 'frameTags' found in JSON created by Aseprite. Please use official Aseprite 1.1.1 or newer.");
				return;
			}

			var frameTags = meta["frameTags"].Array;
			foreach (var item in frameTags)
			{
				JSONObject frameTag = item.Obj;
				ImportedSingleAnimationInfo anim = new ImportedSingleAnimationInfo();
				anim.name = frameTag["name"].Str;
				anim.firstSpriteIndex = (int)(frameTag["from"].Number);
				anim.lastSpriteIndex = (int)(frameTag["to"].Number);

				importedInfos.animations.Add(anim);
			}
		}

		private static void GetSpritesFromJSON(JSONObject root, ImportedAnimationInfo importedInfos)
		{
			var list = root["frames"].Array;
			foreach (var item in list)
			{
				ImportedSpriteInfo frame = new ImportedSpriteInfo();
				frame.name = item.Obj["filename"].Str.Replace(".ase","");

				var frameValues = item.Obj["frame"].Obj;
				frame.width = (int)frameValues["w"].Number;
				frame.height = (int)frameValues["h"].Number;
				frame.x = (int)frameValues["x"].Number;
				frame.y = importedInfos.height - (int)frameValues["y"].Number - frame.height; // unity has a different coord system

				frame.duration = (int)item.Obj["duration"].Number;

				importedInfos.frames.Add(frame);
			}
		}
	}
}