using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using AnimationImporter.Boomlagoon.JSON;

namespace AnimationImporter.PyxelEdit
{
	public class PyxelEditData
	{
		public Tileset tileset = new Tileset();
		public Canvas canvas = new Canvas();
		public string name;
		public Animations animations = new Animations();
		public string version;
	}

	public class Tileset
	{
		public int tileWidth;
		public int tileHeight;
		public int tilesWide;
		public bool fixedWidth;
		public int numTiles;
	}

	public class Animations : Dictionary<int, Animation>
	{
	}

	public class Canvas
	{
		public int width;
		public int height;
		public int tileWidth;
		public int tileHeight;
		public int numLayers;
		public Layers layers = new Layers();
	}

	public class Layers : Dictionary<int, Layer>
	{
	}

	public class Layer
	{
		public string name;
		public int alpha;
		public bool hidden = false;
		public string blendMode = "normal";

		public TileRefs tileRefs = new TileRefs();

		public Texture2D texture = null;

		public Layer(JSONObject obj)
		{
			name = obj["name"].Str;
			alpha = (int)obj["alpha"].Number;
			hidden = obj["hidden"].Boolean;
			blendMode = obj["blendMode"].Str;

			foreach (var item in obj["tileRefs"].Obj)
			{
				tileRefs[int.Parse(item.Key)] = new TileRef(item.Value.Obj);
			}
		}
	}

	public class TileRefs : Dictionary<int, TileRef>
	{
	}

	public class TileRef
	{
		public int index;
		public int rot;
		public bool flipX;

		public TileRef(JSONObject obj)
		{
			index = (int)obj["index"].Number;
			rot = (int)obj["rot"].Number;
			flipX = obj["flipX"].Boolean;
		}
	}

	public class Animation
	{
		public string name;
		public int baseTile = 0;
		public int length = 7;
		public int[] frameDurationMultipliers;
		public int frameDuration = 200;

		public Animation(JSONObject value)
		{
			name = value["name"].Str;
			baseTile = (int)value["baseTile"].Number;
			length = (int)value["length"].Number;

			var list = value["frameDurationMultipliers"].Array;
			frameDurationMultipliers = new int[list.Length];
			for (int i = 0; i < list.Length; i++)
			{
				frameDurationMultipliers[i] = (int)list[i].Number;
			}

			frameDuration = (int)value["frameDuration"].Number;
		}
	}
}