using System;
using UnityEngine;

namespace AnimationImporter
{
	public enum SpriteNamingScheme : int
	{
		Classic,			// hero 0
		FileAnimationZero,	// hero_idle_0, ...
		FileAnimationOne,   // hero_idle_1, ...
		AnimationZero,      // idle_0, ...
		AnimationOne        // idle_1, ...
	}

	public static class SpriteNaming
	{
		private static int[] _namingSchemesValues = null;
		public static int[] namingSchemesValues
		{
			get
			{
				if (_namingSchemesValues == null)
				{
					InitNamingLists();
				}

				return _namingSchemesValues;
			}
		}

		private static string[] _namingSchemesDisplayValues = null;
		public static string[] namingSchemesDisplayValues
		{
			get
			{
				if (_namingSchemesDisplayValues == null)
				{
					InitNamingLists();
				}

				return _namingSchemesDisplayValues;
			}
		}

		private static void InitNamingLists()
		{
			var allNamingSchemes = Enum.GetValues(typeof(SpriteNamingScheme));

			_namingSchemesValues = new int[allNamingSchemes.Length];
			_namingSchemesDisplayValues = new string[allNamingSchemes.Length];

			for (int i = 0; i < allNamingSchemes.Length; i++)
			{
				SpriteNamingScheme namingScheme = (SpriteNamingScheme)allNamingSchemes.GetValue(i);
				_namingSchemesValues[i] = (int)namingScheme;
				_namingSchemesDisplayValues[i] = namingScheme.ToDisplayString();
			}
		}

		private static string ToDisplayString(this SpriteNamingScheme namingScheme)
		{
			switch (namingScheme)
			{
				case SpriteNamingScheme.Classic:
					return "hero 0, hero 1, ... (Default)";
				case SpriteNamingScheme.FileAnimationZero:
					return "hero_idle_0, hero_idle_1, ...";
				case SpriteNamingScheme.FileAnimationOne:
					return "hero_idle_1, hero_idle_2, ...";
				case SpriteNamingScheme.AnimationZero:
					return "idle_0, idle_1, ...";
				case SpriteNamingScheme.AnimationOne:
					return "idle_1, idle_2, ...";
			}

			return "";
		}
	}
}