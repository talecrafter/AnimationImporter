Aseprite Animation Importer for Unity
====

This tool is a work in progress Aseprite Importer for Unity.
It may work for you, it may not. And it may overwrite files in unexpected ways. So be careful and always use Version Control, so you can go back to a previous state if something goes wrong.

Tested with: Unity 5.2.2f1, Aseprite 1.1.1


Setup
-----

Open the tool with "Window" -> "Animation Importer".
Edit the path to the Aseprite Application on your system. If it works, the drag and drop fields are no longer disabled.


HOW IT WORKS:

Generate a file with animations (tags) in Aseprite.
Save that file in your Unity project.
Open the tool "Animation Importer" from the Menu "Window".
Drag and drop the Aseprite asset on one of the fields according to your needs.

When you update the animations, drop the asset again on the same tool.
It should use the existing AnimatorController or AnimatorOverrideController, so if you have used them in the scene or prefabs, the reference is not lost.


THE STEPS THIS TOOL GOES THROUGH:

- call the Aseprite application and let it generate a png with all sprites and a json file with meta info
- change the png import settings to something more appropriate to pixel art
- import the info from the json file and delete it afterwards
- create animations in the same directory
- optional AnimatorController:
	- if does not exist: create AnimatorController and place all animations as states
	- if exists: replace animations on the first layer on all states that have the same name as one of the animation
- optional AnimatorOverrideController
	- if does not exist: create AnimatorOverrideController and replace all animations that have the same name
	- if exists: replace all animations that have the same name


Feedback
-----

Send your comments, feedback and bugs to stephan.hoevelbrinks@craftinglegends.com or http://twitter.com/talecrafter.
I cannot promise I have time to fix things or improve this tool, but there is a good chance I will have a look at it nonetheless.


Credits
-----

Mostly written by Stephan Hövelbrinks (http://twitter.com/talecrafter)

Contains code from Ya-ma (http://twitter.com/PixelYam)
Contains JSONObject from Boomlagoon (www.boomlagoon.com)


License
-----

You can freely use/distribute this project in any way possible with the exception of selling it on it's own.