using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace AnimationImporter
{
	public static class AnimationClipUtility
	{
		class AnimationClipSettings
		{
			SerializedProperty m_property;

			private SerializedProperty Get(string property) { return m_property.FindPropertyRelative(property); }

			public AnimationClipSettings(SerializedProperty prop) { m_property = prop; }

			public float startTime { get { return Get("m_StartTime").floatValue; } set { Get("m_StartTime").floatValue = value; } }
			public float stopTime { get { return Get("m_StopTime").floatValue; } set { Get("m_StopTime").floatValue = value; } }
			public float orientationOffsetY { get { return Get("m_OrientationOffsetY").floatValue; } set { Get("m_OrientationOffsetY").floatValue = value; } }
			public float level { get { return Get("m_Level").floatValue; } set { Get("m_Level").floatValue = value; } }
			public float cycleOffset { get { return Get("m_CycleOffset").floatValue; } set { Get("m_CycleOffset").floatValue = value; } }

			public bool loopTime { get { return Get("m_LoopTime").boolValue; } set { Get("m_LoopTime").boolValue = value; } }
			public bool loopBlend { get { return Get("m_LoopBlend").boolValue; } set { Get("m_LoopBlend").boolValue = value; } }
			public bool loopBlendOrientation { get { return Get("m_LoopBlendOrientation").boolValue; } set { Get("m_LoopBlendOrientation").boolValue = value; } }
			public bool loopBlendPositionY { get { return Get("m_LoopBlendPositionY").boolValue; } set { Get("m_LoopBlendPositionY").boolValue = value; } }
			public bool loopBlendPositionXZ { get { return Get("m_LoopBlendPositionXZ").boolValue; } set { Get("m_LoopBlendPositionXZ").boolValue = value; } }
			public bool keepOriginalOrientation { get { return Get("m_KeepOriginalOrientation").boolValue; } set { Get("m_KeepOriginalOrientation").boolValue = value; } }
			public bool keepOriginalPositionY { get { return Get("m_KeepOriginalPositionY").boolValue; } set { Get("m_KeepOriginalPositionY").boolValue = value; } }
			public bool keepOriginalPositionXZ { get { return Get("m_KeepOriginalPositionXZ").boolValue; } set { Get("m_KeepOriginalPositionXZ").boolValue = value; } }
			public bool heightFromFeet { get { return Get("m_HeightFromFeet").boolValue; } set { Get("m_HeightFromFeet").boolValue = value; } }
			public bool mirror { get { return Get("m_Mirror").boolValue; } set { Get("m_Mirror").boolValue = value; } }
		}

		public static void SetLoop(this AnimationClip clip, bool value)
		{
			SerializedObject serializedClip = new SerializedObject(clip);
			AnimationClipSettings clipSettings = new AnimationClipSettings(serializedClip.FindProperty("m_AnimationClipSettings"));

			clipSettings.loopTime = value;
			clipSettings.loopBlend = false;

			serializedClip.ApplyModifiedProperties();
		}

		// ================================================================================
		//  curve bindings
		// --------------------------------------------------------------------------------

		public static EditorCurveBinding spriteRendererCurveBinding
		{
			get
			{
				return new EditorCurveBinding
				{
					path = "", // assume SpriteRenderer is at same GameObject as AnimationController
					type = typeof(SpriteRenderer),
					propertyName = "m_Sprite"
				};
			}
		}

		// get spriteRendererCurveBinding with an optional parameter of setting a custom path
		public static EditorCurveBinding GetSpriteRendererCurveBinding(string transformPathToSpriteRenderer = "")
		{
			EditorCurveBinding curveBinding = spriteRendererCurveBinding;
			curveBinding.path = transformPathToSpriteRenderer;

			return curveBinding;
		}

		public static EditorCurveBinding imageCurveBinding
		{
			get
			{
				return new EditorCurveBinding
				{
					path = "", // assume Image is at same GameObject as AnimationController
					type = typeof(Image),
					propertyName = "m_Sprite"
				};
			}
		}

		// get imageCurveBinding with an optional parameter of setting a custom path
		public static EditorCurveBinding GetImageCurveBinding(string transformPathToImage = "")
		{
			EditorCurveBinding curveBinding = imageCurveBinding;
			curveBinding.path = transformPathToImage;

			return curveBinding;
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

		public static void GetComponentPathsFromExistingClip(AnimationClip clip, AnimationTargetObjectType targetType, out string spriteRendererComponentPath, out string imageComponentPath)
		{
			var curveBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

			spriteRendererComponentPath = string.Empty;
			imageComponentPath = string.Empty;

			for (int i = 0; i < curveBindings.Length; i++)
			{
				if (targetType != AnimationTargetObjectType.Image && curveBindings[i].type == typeof(SpriteRenderer))
				{
					spriteRendererComponentPath = curveBindings[i].path;
				}
				else if (targetType != AnimationTargetObjectType.SpriteRenderer && curveBindings[i].type == typeof(UnityEngine.UI.Image))
				{
					imageComponentPath = curveBindings[i].path;
				}
			}

			return;
		}
	}
}