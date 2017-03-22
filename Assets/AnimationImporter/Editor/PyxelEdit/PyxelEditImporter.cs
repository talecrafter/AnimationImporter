using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnimationImporter.Boomlagoon.JSON;

namespace AnimationImporter.PyxelEdit
{
	[InitializeOnLoad]
	public class PyxelEditImporter : IAnimationImporterPlugin
	{
		private static PyxelEditData _latestData = null;

		// ================================================================================
		//  static constructor, registering plugin
		// --------------------------------------------------------------------------------

		static PyxelEditImporter()
		{
			PyxelEditImporter importer = new PyxelEditImporter();
			AnimationImporter.RegisterImporter(importer, "pyxel");
		}

		public ImportedAnimationSheet Import(AnimationImportJob job, AnimationImporterSharedConfig config)
		{
			if (ImportImageAndMetaInfo(job))
			{
				AssetDatabase.Refresh();
				return GetAnimationInfo(_latestData);
			}

			return null;
		}

		public bool IsValid()
		{
			return IonicZipDllIsPresent();
		}

		private static bool ImportImageAndMetaInfo(AnimationImportJob job)
		{
			_latestData = null;

			var zipFilePath = GetFileSystemPath(job.assetDirectory + "/" + job.fileName);

			var files = GetContentsFromZipFile(zipFilePath);

			if (files.ContainsKey("docData.json"))
			{
				string jsonData = System.Text.Encoding.UTF8.GetString(files["docData.json"]);

				PyxelEditData pyxelEditData = ReadJson(jsonData);

				List<Layer> allLayers = new List<Layer>();

				foreach (var item in pyxelEditData.canvas.layers)
				{
					Layer layer = item.Value;
					string layerName = "layer" + item.Key.ToString() + ".png";
					layer.texture = LoadTexture(files[layerName]);
					allLayers.Add(layer);
				}

				Texture2D image = CreateBlankTexture(new Color(0f, 0f, 0f, 0), pyxelEditData.canvas.width, pyxelEditData.canvas.height);
				for (int i = allLayers.Count - 1; i >= 0; i--)
				{
					Layer layer = allLayers[i];

					if (!layer.hidden)
					{
						float maxAlpha = layer.alpha / 255f;
						image = CombineTextures(image, layer.texture, maxAlpha);
					}
				}

				if (!Directory.Exists(job.directoryPathForSprites))
				{
					Directory.CreateDirectory(job.directoryPathForSprites);
				}

				SaveTextureToAssetPath(image, job.imageAssetFilename);

				_latestData = pyxelEditData;

				return true;
			}
			else
			{
				return false;
			}
		}

		private static ImportedAnimationSheet GetAnimationInfo(PyxelEditData data)
		{
			if (data == null)
			{
				return null;
			}

			int tileWidth = data.tileset.tileWidth;
			int tileHeight = data.tileset.tileHeight;

			int maxTileIndex = 0;

			ImportedAnimationSheet animationSheet = new ImportedAnimationSheet();
			animationSheet.width = data.canvas.width;
			animationSheet.height = data.canvas.height;

			// animations
			animationSheet.animations = new List<ImportedAnimation>();
			for (int i = 0; i < data.animations.Count; i++)
			{
				var animationData = data.animations[i];

				ImportedAnimation importAnimation = new ImportedAnimation();

				importAnimation.name = animationData.name;

				importAnimation.firstSpriteIndex = animationData.baseTile;
				importAnimation.lastSpriteIndex = animationData.baseTile + animationData.length - 1;

				maxTileIndex = Mathf.Max(maxTileIndex, importAnimation.lastSpriteIndex);

				ImportedAnimationFrame[] frames = new ImportedAnimationFrame[animationData.length];
				for (int frameIndex = 0; frameIndex < animationData.length; frameIndex++)
				{
					ImportedAnimationFrame frame = new ImportedAnimationFrame();

					frame.duration = animationData.frameDuration;
					if (animationData.frameDurationMultipliers[i] != 100)
					{
						frame.duration *= (int)(animationData.frameDurationMultipliers[i] / 100f);
					}

					int tileIndex = animationData.baseTile + frameIndex;

					int columnCount = data.canvas.width / tileWidth;

					int column = tileIndex % columnCount;
					int row = tileIndex / columnCount;

					frame.y = row * tileHeight;
					frame.x = column * tileWidth;
					frame.width = tileWidth;
					frame.height = tileHeight;

					frames[frameIndex] = frame;
				}

				importAnimation.SetFrames(frames);

				animationSheet.animations.Add(importAnimation);
			}

			// gather all frames used by animations for the sprite sheet
			animationSheet.frames = new List<ImportedAnimationFrame>();
			foreach (var anim in animationSheet.animations)
			{
				foreach (var frame in anim.frames)
				{
					animationSheet.frames.Add(frame);
				}
			}

			return animationSheet;
		}

		private static PyxelEditData ReadJson(string jsonData)
		{
			PyxelEditData data = new PyxelEditData();

			JSONObject obj = JSONObject.Parse(jsonData);

			if (obj.ContainsKey("name"))
			{
				data.name = obj["name"].Str;
			}
			if (obj.ContainsKey("tileset"))
			{
				data.tileset.tileWidth = (int)obj["tileset"].Obj["tileWidth"].Number;
				data.tileset.tileHeight = (int)obj["tileset"].Obj["tileHeight"].Number;
				data.tileset.tilesWide = (int)obj["tileset"].Obj["tilesWide"].Number;
				data.tileset.fixedWidth = obj["tileset"].Obj["fixedWidth"].Boolean;
				data.tileset.numTiles = (int)obj["tileset"].Obj["numTiles"].Number;
			}
			if (obj.ContainsKey("animations"))
			{
				foreach (var item in obj["animations"].Obj)
				{
					data.animations[int.Parse(item.Key)] = new Animation(item.Value.Obj);
				}
			}
			if (obj.ContainsKey("canvas"))
			{
				data.canvas.width = (int)obj["canvas"].Obj["width"].Number;
				data.canvas.height = (int)obj["canvas"].Obj["height"].Number;
				data.canvas.tileWidth = (int)obj["canvas"].Obj["tileWidth"].Number;
				data.canvas.tileHeight = (int)obj["canvas"].Obj["tileHeight"].Number;
				data.canvas.numLayers = (int)obj["canvas"].Obj["numLayers"].Number;
				foreach (var item in obj["canvas"].Obj["layers"].Obj)
				{
					data.canvas.layers[int.Parse(item.Key)] = new Layer(item.Value.Obj);
				}
			}

			return data;
		}

		public static string GetFileSystemPath(string path)
		{
			string basePath = Application.dataPath;

			// if the path already begins with the Assets folder, remove that one from the base
			if (path.StartsWith("Assets") || path.StartsWith("/Assets"))
			{
				basePath = basePath.Replace("/Assets", "");
			}

			return Path.Combine(basePath, path);
		}

		public static void SaveTextureToAssetPath(Texture2D texture, string assetPath)
		{
			string path = Application.dataPath + "/../" + assetPath;
			File.WriteAllBytes(path, texture.EncodeToPNG());
		}

		public static Texture2D CreateBlankTexture(
			Color color, int width = 2, int height = -1, TextureFormat format = TextureFormat.RGBA32,
			bool mipmap = false, bool linear = false)
		{
			if (height < 0)
			{
				height = width;
			}

			// create empty texture
			Texture2D texture = new Texture2D(width, height, format, mipmap, linear);

			// get all pixels as an array
			var cols = texture.GetPixels();
			for (int i = 0; i < cols.Length; i++)
			{
				cols[i] = color;
			}

			// important steps to save changed pixel values
			texture.SetPixels(cols);
			texture.Apply();

			texture.hideFlags = HideFlags.HideAndDontSave;

			return texture;
		}

		static Texture2D LoadTexture(byte[] imageData)
		{
			var w = ReadInt32FromImageData(imageData, 3 + 15);
			var h = ReadInt32FromImageData(imageData, 3 + 15 + 2 + 2);
			var texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
			texture.hideFlags = HideFlags.HideAndDontSave;
			texture.filterMode = FilterMode.Point;
			texture.LoadImage(imageData);
			return texture;
		}

		static int ReadInt32FromImageData(byte[] imageData, int offset)
		{
			return (imageData[offset] << 8) | imageData[offset + 1];
		}

		public static Texture2D CombineTextures(Texture2D aBaseTexture, Texture2D aToCopyTexture, float maxAlpha)
		{
			int aWidth = aBaseTexture.width;
			int aHeight = aBaseTexture.height;
			Texture2D aReturnTexture = new Texture2D(aWidth, aHeight, TextureFormat.RGBA32, false);

			Color[] aBaseTexturePixels = aBaseTexture.GetPixels();
			Color[] aCopyTexturePixels = aToCopyTexture.GetPixels();
			Color[] aColorList = new Color[aBaseTexturePixels.Length];
			int aPixelLength = aBaseTexturePixels.Length;

			for (int p = 0; p < aPixelLength; p++)
			{
				float minA = aBaseTexturePixels[p].a;
				float alpha = aCopyTexturePixels[p].a * maxAlpha;
				aColorList[p] = Color.Lerp(aBaseTexturePixels[p], aCopyTexturePixels[p], alpha);
				aColorList[p].a = Mathf.Lerp(minA, 1f, alpha);
			}

			aReturnTexture.SetPixels(aColorList);
			aReturnTexture.Apply(false);

			return aReturnTexture;
		}

		// ================================================================================
		//  extracting from zip file
		// --------------------------------------------------------------------------------

		private static Type zipFileClass = null;
		private static System.Reflection.MethodInfo readZipFileMethod = null;
		private static System.Reflection.MethodInfo extractMethod = null;

		public static Dictionary<string, byte[]> GetContentsFromZipFile(string fileName)
		{
			Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

			if (zipFileClass == null)
			{
				InitZipMethods();
			}

			if (zipFileClass != null)
			{
				using (var zipFile = readZipFileMethod.Invoke(null, new object[] { fileName }) as IDisposable)
				{
					var zipFileAsEnumeration = zipFile as IEnumerable;
					foreach (var entry in zipFileAsEnumeration)
					{
						MemoryStream stream = new MemoryStream();
						extractMethod.Invoke(entry, new object[] { stream });

						files.Add(entry.ToString().Replace("ZipEntry::", ""), stream.ToArray());
					}
				}
			}

			return files;
		}

		private static void InitZipMethods()
		{
			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in allAssemblies)
			{
				zipFileClass = assembly.GetType("Ionic.Zip.ZipFile");

				if (zipFileClass != null)
				{
					readZipFileMethod = zipFileClass.GetMethod("Read", new Type[] { typeof(string) });

					Type zipEntryClass = assembly.GetType("Ionic.Zip.ZipEntry");

					extractMethod = zipEntryClass.GetMethod("Extract", new Type[] { typeof(MemoryStream) });

					return;
				}
			}
		}

		private static bool IonicZipDllIsPresent()
		{
			if (zipFileClass != null)
			{
				return true;
			}

			var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in allAssemblies)
			{
				var zipClass = assembly.GetType("Ionic.Zip.ZipFile");

				if (zipClass != null)
				{
					return true;
				}
			}

			return false;
		}
	}
}