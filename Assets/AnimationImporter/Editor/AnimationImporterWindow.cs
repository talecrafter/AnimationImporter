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
		private string _nonLoopingAnimationEnterValue = "";
		private Vector2 _scrollPos = Vector2.zero;

		// ================================================================================
		//  menu entry
		// --------------------------------------------------------------------------------

		[MenuItem("Window/Animation Importer")]
		public static void ImportAnimationsMenu()
		{
			EditorWindow.GetWindow(typeof(AnimationImporterWindow), false, "Anim Importer");
        }

		// ================================================================================
		//  unity methods
		// --------------------------------------------------------------------------------

		public void OnEnable()
		{
			importer.LoadUserConfig();
		}

		public void OnGUI()
		{
			if (_dropBoxStyle == null)
				GetBoxStyle();

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

		// ================================================================================
		//  GUI methods
		// --------------------------------------------------------------------------------

		private void GetBoxStyle()
		{
			_dropBoxStyle = new GUIStyle(EditorStyles.helpBox);
			_dropBoxStyle.alignment = TextAnchor.MiddleCenter;
		}

		private void ShowUserConfig()
		{
			ShowHeadline("Config");

			/*
				Aseprite Application
			*/

			GUILayout.BeginHorizontal();
			GUILayout.Label("Aseprite Application Path");

			string newPath = importer.asepritePath;

			if (GUILayout.Button("Select"))
			{
				var path = EditorUtility.OpenFilePanel(
					"Select Aseprite Application",
					"",
					"exe");
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

			GUILayout.Space(5f);

			/*
				sprite values
			*/

			importer.targetObjectType = (AnimationTargetObjectType)EditorGUILayout.EnumPopup("Target Object", importer.targetObjectType);

			importer.spriteAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Alignment", importer.spriteAlignment);

			if (importer.spriteAlignment == SpriteAlignment.Custom)
			{
				importer.spriteAlignmentCustomX = EditorGUILayout.Slider("x", importer.spriteAlignmentCustomX, 0, 1f);
				importer.spriteAlignmentCustomY = EditorGUILayout.Slider("y", importer.spriteAlignmentCustomY, 0, 1f);
			}

			importer.spritePixelsPerUnit = EditorGUILayout.FloatField("Sprite Pixels per Unit", importer.spritePixelsPerUnit);

			EditorGUILayout.BeginHorizontal();
			importer.saveSpritesToSubfolder = EditorGUILayout.Toggle("Sprites to Subfolder", importer.saveSpritesToSubfolder);

			importer.saveAnimationsToSubfolder = EditorGUILayout.Toggle("Animations to Subfolder", importer.saveAnimationsToSubfolder);
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(25f);

			ShowHeadline("Automatic Import");
			EditorGUILayout.BeginHorizontal();
			importer.automaticImporting = EditorGUILayout.Toggle("Automatic Import", importer.automaticImporting);
			EditorGUILayout.LabelField("Use at your own risk!", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("Looks for existing Controller with same name. Uses current import setting.");

			/*
				animations that do not loop
			*/

			GUILayout.Space(25f);
			ShowHeadline("Non-looping Animations");

			for (int i = 0; i < importer.animationNamesThatDoNotLoop.Count; i++)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(importer.animationNamesThatDoNotLoop[i]);
				bool doDelete = GUILayout.Button("Delete");			
				GUILayout.EndHorizontal();
				if (doDelete)
				{
					importer.RemoveAnimationThatDoesNotLoop(i);
					break;
				}
			}

			EditorGUILayout.Space();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Add ");
			_nonLoopingAnimationEnterValue = EditorGUILayout.TextField(_nonLoopingAnimationEnterValue);
			if (GUILayout.Button("Enter"))
			{
				if (importer.AddAnimationThatDoesNotLoop(_nonLoopingAnimationEnterValue))
				{
					_nonLoopingAnimationEnterValue = "";
				}
			}
			GUILayout.EndHorizontal();
		}

		private void ShowAnimationsGUI()
		{
			ShowHeadline("Animations");

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(importer.canImportAnimations);
			if (droppedAsset != null)
			{
				importer.CreateAnimationsForAssetFile(droppedAsset);
				AssetDatabase.Refresh();
			}
		}

		private void ShowAnimatorControllerGUI()
		{
			ShowHeadline("Animator Controller + Animations");

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(importer.canImportAnimations);
			if (droppedAsset != null)
			{
				var animationInfo = importer.CreateAnimationsForAssetFile(droppedAsset);

				if (animationInfo != null)
				{
					importer.CreateAnimatorController(animationInfo);
				}

				AssetDatabase.Refresh();
			}
		}

		private void ShowAnimatorOverrideControllerGUI()
		{
			ShowHeadline("Animator Override Controller + Animations");

			importer.baseController = EditorGUILayout.ObjectField("Based on Controller:", importer.baseController, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;

			DefaultAsset droppedAsset = ShowDropButton<DefaultAsset>(importer.canImportAnimationsForOverrideController);
			if (droppedAsset != null)
			{
				var animationInfo = importer.CreateAnimationsForAssetFile(droppedAsset);

				if (animationInfo != null)
				{
					importer.CreateAnimatorOverrideController(animationInfo);
				}

				AssetDatabase.Refresh();
			}
		}

		private void ShowHeadline(string headline)
		{
			EditorGUILayout.LabelField(headline, EditorStyles.boldLabel, GUILayout.Height(20f));
		}

		// ================================================================================
		//  OnGUI helper
		// --------------------------------------------------------------------------------

		private T ShowDropButton<T>(bool isEnabled) where T : UnityEngine.Object
		{
			T returnValue = null;

			Rect drop_area = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));

			GUI.enabled = isEnabled;
			GUI.Box(drop_area, "Drop Aseprite file here", _dropBoxStyle);
			GUI.enabled = true;

			if (!isEnabled)
				return null;

			Event evt = Event.current;
			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:

					if (!drop_area.Contains(evt.mousePosition)
						|| !DraggedObjectsContainType<T>())
						return null;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
						{
							if (dragged_object is T)
							{
								returnValue = dragged_object as T;
							}
						}
					}

					evt.Use();

					break;
			}

			return returnValue;
		}

		private bool DraggedObjectsContainType<T>() where T : UnityEngine.Object
		{
			foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
			{
				if (dragged_object is T)
				{
					return true;
				}
			}

			return false;
		}
	}
}
