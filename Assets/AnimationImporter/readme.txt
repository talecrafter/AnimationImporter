Pixelart Animation Importer for Unity
====

This tool is an Aseprite and PyxelEdit Animation Importer for Unity.
It's already used in several projects and should work for most use cases. There is no guaranteed support though, so test and use this at your own will.

Tested with: Unity 5.5, Aseprite 1.1.13, PyxelEdit 0.4.3


Setup
-----

Open the tool with "Window" -> "Animation Importer".
If using Aseprite: Edit the path to the Aseprite Application on your system under "Config".


ASEPRITE:

Generate a file with animations (tags) in Aseprite.
Save that file in your Unity project.
Open the tool "Animation Importer" from the Menu "Window".
Drag and drop the Aseprite asset on one of the fields according to your needs.

When you update the animations, drop the asset again on the same tool.
It should use the existing AnimatorController or AnimatorOverrideController, so if you have used them in the scene or prefabs, the reference is not lost.

Steps this tool goes through:

- call the Aseprite application and let it generate a png with all sprites and a json file with meta info
- change the png import settings to something more appropriate to pixel art
- import the info from the json file and delete it afterwards
- create Unity animations
- optional AnimatorController:
	- if does not exist: create AnimatorController and place all animations as states
	- if exists: replace animations on the first layer on all states that have the same name as one of the animation
- optional AnimatorOverrideController
	- if does not exist: create AnimatorOverrideController and replace all animations that have the same name
	- if exists: replace all animations that have the same name


PYXELEDIT:

Tiles and Layer Blend Modes are not supported.

Generate a file with animations in PyxelEdit.
Save that file in your Unity project.
Open the tool "Animation Importer" from the Menu "Window".
Drag and drop the PyxelEdit asset on one of the fields according to your needs.

When you update the animations, drop the asset again on the same tool.
It should use the existing AnimatorController or AnimatorOverrideController, so if you have used them in the scene or prefabs, the reference is not lost.

Steps this tool goes through:

- open the .pyxel file, which is a zip, get json data from it
- get png layers from the .pyxel file and recreate as one png
- change the png import settings to something more appropriate to pixel art
- create Unity animations
- optional AnimatorController:
	- if does not exist: create AnimatorController and place all animations as states
	- if exists: replace animations on the first layer on all states that have the same name as one of the animation
- optional AnimatorOverrideController
	- if does not exist: create AnimatorOverrideController and replace all animations that have the same name
	- if exists: replace all animations that have the same name


AUTOMATIC IMPORT

This option reimports Animation files when Unity notifies them as changed. It looks for an AnimatorController or AnimatorOverrideController with the same name and in the same directory. Current import settings are used, not the ones from first import.

PIVOT POINTS

Pivot point settings get reused on further reimports. If you want to apply the pivot point settings from the Animation Importer on Animations that got imported already, delete the sprites and then import again.


Feedback
-----

Send your comments and questions to talecrafter@deathtrash.com or http://twitter.com/talecrafter.
You can use the GitHub Features (Issues, Pull Requests) for posting bugs, feature wishes and additional code.


Credits
-----

Contributors:

- Stephan Hövelbrinks (http://twitter.com/talecrafter)
- Ya-ma (http://twitter.com/PixelYam)
- Edward Rowe (http://twitter.com/edwardlrowe)
- Alberto Fernandez

Contains JSONObject from Boomlagoon (www.boomlagoon.com)


License
-----

You can freely use/distribute this project in any way possible with the exception of selling it on it's own.