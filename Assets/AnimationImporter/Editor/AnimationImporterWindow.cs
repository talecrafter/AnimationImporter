using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using UnityEditor;
using System.IO;
using AnimationImporter.Boomlagoon.JSON;
using UnityEditor.Animations;
using System.Linq;

namespace AnimationImporter
{
	public class AnimationImporterWindow : EditorWindow
	{
		// ================================================================================
		//  private
		// --------------------------------------------------------------------------------

		private AnimationImporter importer
		{
			get
			{
				return AnimationImporter.Instance;
			}
		}

		private GUIStyle _dropBoxStyle;
		private GUIStyle _infoTextStyle;

		private string _nonLoopingAnimationEnterValue = "";

		private Vector2 _scrollPos = Vector2.zero;

		// ================================================================================
		//  menu entry
		// --------------------------------------------------------------------------------

		[MenuItem("Window/Animation Importer")]
		public static void ImportAnimationsMenu()
		{
			GetWindow(typeof(AnimationImporterWindow), false, "Anim Importer");
		}

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		public void OnEnable()
		{
			importer.LoadOrCreateUserConfig();
		}

		public void OnGUI()
		{
			CheckGUIStyles();

			if (importer.canImportAnimations)
			{
				_scrollPos = GUILayout.BeginScrollView(_scrollPos);

				EditorGUILayout.Space();

				ShowAnimationsGUI();

				GUILayout.Space(25f);

				ShowAnimatorControllerGUI();

				GUILayout.Space(25f);

				ShowAnimatorOverrideControllerGUI();

				GUILayout.Space(25f);

				ShowUserConfig();

				GUILayout.EndScrollView();
			}
			else
			{
				EditorGUILayout.Space();

				ShowHeadline("Select Aseprite Application");

				EditorGUILayout.Space();

				ShowAsepriteApplicationSelection();

				EditorGUILayout.Space();

				GUILayout.Label("Aseprite has to be installed on this machine because the Importer calls Aseprite through the command line for creating images and getting animation data.", _infoTextStyle);
			}
		}

		// ================================================================================
		//  GUI methods
		// --------------------------------------------------------------------------------

		private void CheckGUIStyles()
		{
			if (_dropBoxStyle == null)
			{
				GetBoxStyle();
			}
			if (_infoTextStyle == null)
			{
				GetTextInfoStyle();
			}
		}

		private void GetBoxStyle()
		{
			_dropBoxStyle = new GUIStyle(EditorStyles.helpBox);
			_dropBoxStyle.alignment = TextAnchor.MiddleCenter;
		}

		private void GetTextInfoStyle()
		{
			_infoTextStyle = new GUIStyle(EditorStyles.label);
			_infoTextStyle.wordWrap = true;
		}

		private void ShowUserConfig()
		{
			if (importer == null || importer.sharedData == null)
			{
				return;
			}

			ShowHeadline("Config");

			/*
				Aseprite Application
			*/

			ShowAsepriteApplicationSelection();

			GUILayout.Space(5f);

			/*
				sprite values
			*/

			importer.sharedData.targetObjectType = (AnimationTargetObjectType)EditorGUILayout.EnumPopup("Target Object", importer.sharedData.targetObjectType);

			importer.sharedData.spriteAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Alignment", importer.sharedData.spriteAlignment);

			if (importer.sharedData.spriteAlignment == SpriteAlignment.Custom)
			{
				importer.sharedData.spriteAlignmentCustomX = EditorGUILayout.Slider("x", importer.sharedData.spriteAlignmentCustomX, 0, 1f);
				importer.sharedData.spriteAlignmentCustomY = EditorGUILayout.Slider("y", importer.sharedData.spriteAlignmentCustomY, 0, 1f);
			}

			importer.sharedData.spritePixelsPerUnit = EditorGUILayout.FloatField("Sprite Pixels per Unit", importer.sharedData.spritePixelsPerUnit);

			GUILayout.Space(5f);

			ShowTargetLocationOptions("Sprites", importer.sharedData.spritesTargetLocation);
			ShowTargetLocationOptions("Animations", importer.sharedData.animationsTargetLocation);
			ShowTargetLocationOptions("AnimationController", importer.sharedData.animationControllersTargetLocation);

			GUILayout.Space(5f);

			importer.sharedData.spriteNamingScheme = (SpriteNamingScheme)EditorGUILayout.IntPopup("Sprite Naming Scheme",
				(int)importer.sharedData.spriteNamingScheme,
				SpriteNaming.namingSchemesDisplayValues, SpriteNaming.namingSchemesValues);

			GUILayout.Space(25f);

			ShowHeadline("Automatic Import");
			EditorGUILayout.BeginHorizontal();
			importer.sharedData.automaticImporting = EditorGUILayout.Toggle("Automatic Import", importer.sharedData.automaticImporting);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("Looks for existing Animation Controller with same name.");

			/*
				animations that do not loop
			*/

			GUILayout.Space(25f);
			ShowHeadline("Non-looping Animations");

			for (int i = 0; i < importer.sharedData.animationNamesThatDoNotLoop.Count; i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(importer.sharedData.animationNamesThatDoNotLoop[i]);
				bool doDelete = GUILayout.Button("Delete");
				GUILayout.EndHorizontal();
				if (doDelete)
				{
					importer.sharedData.RemoveAnimationThatDoesNotLoop(i);
					break;
				}
			}

			EditorGUILayout.Space();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Add ");
			_nonLoopingAnimationEnterValue = EditorGUILayout.TextField(_nonLoopingAnimationEnterValue);
			if (GUILayout.Button("Enter"))
			{
				if (importer.sharedData.AddAnimationThatDoesNotLoop(_nonLoopingAnimationEnterValue))
				{
					_nonLoopingAnimationEnterValue = "";
				}
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.LabelField("Enter Part of the Animation Name or a Regex Expression.");

			if (GUI.changed)
			{
				EditorUtility.SetDirty(importer.sharedData);
			}
		}

		private void ShowTargetLocationOptions(string label, AssetTargetLocation targetLocation)
		{
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(label, GUILayout.Width(130f));

			targetLocation.locationType = (AssetTargetLocationType)EditorGUILayout.EnumPopup(targetLocation.locationType, GUILayout.Width(130f));

			bool prevEnabled = GUI.enabled;
			GUI.enabled = targetLocation.locationType == AssetTargetLocationType.GlobalDirectory;

			string globalDirectory = targetLocation.globalDirectory;

			if (GUILayout.Button("Select", GUILayout.Width(50f)))
			{
				var startDirectory = globalDirectory;
				if (!Directory.Exists(startDirectory))
				{
					startDirectory = Application.dataPath;
				}
				startDirectory = Application.dataPath;

				var path = EditorUtility.OpenFolderPanel("Select Target Location", globalDirectory, "");
				if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(AssetDatabaseUtility.GetAssetPath(path)))
				{
					targetLocation.globalDirectory = AssetDatabaseUtility.GetAssetPath(path);
				}
			}

			if (targetLocation.locationType == AssetTargetLocationType.GlobalDirectory)
			{
				string displayDirectory = "/" + globalDirectory;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(displayDirectory, GUILayout.MaxWidth(300f));
			}

			GUI.enabled = prevEnabled;

			EditorGUILayout.EndHorizontal();
		}

		private void ShowAsepriteApplicationSelection()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Aseprite Application Path");

			string newPath = importer.asepritePath;

			if (GUILayout.Button("Select"))
			{
				var path = EditorUtility.OpenFilePanel(
					"Select Aseprite Application",
					"",
					"exe,app");
				if (!string.IsNullOrEmpty(path))
				{
					newPath = path;

					if (Application.platform == RuntimePlatform.OSXEditor)
					{
						newPath += "/Contents/MacOS/aseprite";
					}
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			importer.asepritePath = GUILayout.TextField(newPath, GUILayout.MaxWidth(300f));

			GUILayout.EndHorizontal();

			if(!File.Exists(AnimationImporter.Instance.asepritePath))
			{
				var fileErrorMessage = string.Format(
					"Cannot find Aseprite at the specified path. Use the Select button to locate the application.");
				EditorGUILayout.HelpBox(fileErrorMessage, MessageType.Warning);
			}
		}

		private void ShowAnimationsGUI()
		{
			ShowHeadline("Animations");

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(importer.canImportAnimations, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0)
			{
				ImportAssetsOrError(droppedAssets);
			}
		}

		private void ShowAnimatorControllerGUI()
		{
			ShowHeadline("Animator Controller + Animations");

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(importer.canImportAnimations, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0)
			{
				ImportAssetsOrError(droppedAssets, ImportAnimatorController.AnimatorController);
			}
		}

		private void ShowAnimatorOverrideControllerGUI()
		{
			ShowHeadline("Animator Override Controller + Animations");

			importer.baseController = EditorGUILayout.ObjectField("Based on Controller:", importer.baseController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;

			DefaultAsset[] droppedAssets = ShowDropButton<DefaultAsset>(importer.canImportAnimationsForOverrideController, AnimationImporter.IsValidAsset);
			if (droppedAssets != null && droppedAssets.Length > 0)
			{
				ImportAssetsOrError(droppedAssets, ImportAnimatorController.AnimatorOverrideController);
			}
		}

		private void ImportAssetsOrError(DefaultAsset[] assets, ImportAnimatorController importAnimatorController = ImportAnimatorController.None)
		{
			if(AnimationImporter.IsConfiguredForAssets(assets))
			{
				importer.ImportAssets(assets, importAnimatorController);
			}
			else
			{
				ShowPopupForBadAsepritePath(assets[0].name);
			}
		}

		private void ShowPopupForBadAsepritePath(string assetName)
		{
			var message = string.Format(
				"Cannot import Aseprite file \"{0}\" because the application cannot be found at the configured path. Use the Select button in the Config section to locate Aseprite.",
				assetName);
			EditorUtility.DisplayDialog("Error", message, "Ok");
		}

		private void ShowHeadline(string headline)
		{
			EditorGUILayout.LabelField(headline, EditorStyles.boldLabel, GUILayout.Height(20f));
		}

		// ================================================================================
		//  OnGUI helper
		// --------------------------------------------------------------------------------

		public delegate bool IsValidAssetDelegate(string path);

		private T[] ShowDropButton<T>(bool isEnabled, IsValidAssetDelegate IsValidAsset) where T : UnityEngine.Object
		{
			T[] returnValue = null;

			Rect drop_area = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));

			GUI.enabled = isEnabled;
			GUI.Box(drop_area, "Drop Animation files here", _dropBoxStyle);
			GUI.enabled = true;

			if (!isEnabled)
				return null;

			Event evt = Event.current;
			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:

					if (!drop_area.Contains(evt.mousePosition)
						|| !DraggedObjectsContainValidObject<T>(IsValidAsset))
						return null;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						List<T> validObjects = new List<T>();

						foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
						{
							var assetPath = AssetDatabase.GetAssetPath(dragged_object);

							if (dragged_object is T && IsValidAsset(assetPath))
							{
								validObjects.Add(dragged_object as T);
							}
						}

						returnValue = validObjects.ToArray();
					}

					evt.Use();

					break;
			}

			return returnValue;
		}

		private bool DraggedObjectsContainValidObject<T>(IsValidAssetDelegate IsValidAsset) where T : UnityEngine.Object
		{
			foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
			{
				var assetPath = AssetDatabase.GetAssetPath(dragged_object);

				if (dragged_object is T && IsValidAsset(assetPath))
				{
					return true;
				}
			}

			return false;
		}
	}
}
