# Guide

The following is a guide for both a demonstratory executable and the source code, which can be imported to Unity and customised for variations of the current prototype.

## Executable

The executable is generated from a setup including two rigs. The "always assign alignments" boolean is set to true, so the alignment phase will be included every time the executable is run. The other inspector values are as follows:

- Proximity to switch: Set to 3.
- Rigged Model Follow speed: Set to 2.
- Rigged Model Rotation speed: Set to 5.
- Rigged Model Stopping distance: Set to 4.

Running the executable will first use the alignment phase for both talks. Then, the user enters the game scene, where they may navigate the environment. Red circles will appear at each talk location. Entering these thresholds will play a demonstratory volumetric video talk. Walking out of the threshold will convert the avatar back to its rigged form, where it will continue to follow the player.

Controls are as follows:

- WASD moves the player and the models in alignment mode
- Arrow keys rotate the volumetric video
- Enter to approve a set of alignments

It is important to also note that one visual element of the system which could be improved is no feedback for pressing enter in alignment mode on a rigged model. When enter is pressed, the rigged model can then be used to specify the next set of alignments, however nothing will have appeared to of happened if enter was pressed while the rigged model was still visible. This can easily lead to misalignment unless the user is aware that this has happened.

**Note: Due to file size limitations, the executable may be uploaded to github instead.**

## Code

Code is included inside of this zip file, and can also be found in Assets/Scripts directory in the Unity project on GitHub. The SwitchingCondition class is intended to be used for additional functionality, and all other necessary methods should be public.

## Setting Up the Project

Three additional features are needed for setting up the project in Unity:

- **DepthKit Volumetric video**: Generated using DepthKit's combined-per-pixel unity ready export.
- **Marionette Capture**: FBX capture of same performance as volumetric video. Should also be synced with the DepthKit video.
- **Rigged Model**: I used DepthKit and its "textured geometry sequence" export to generate this. I then rigged this model using Blender and Rigify.

Further instructions on how to obtain these components and advice for how to make them compatible with this system can be found in the dissertation. Volumetric video and rigged movements should also be synced using an external video editing tool.

The DepthKit clip should be imported in Unity using this tutorial: https://docs.depthkit.tv/docs/unity-expansion-depthkit-core. Relevant Unity plugins must be installed, namely the DepthKit expansion package. The rigged model and movement should be imported as humanoid rigs, and then placed in the scene. The Animator controller should be set up appropriately, instructions for this can be found inside the dissertation.

## Assigning fields

Each DepthKit clip, when set up, and marionette movement for each talk should be placed in the appropriate fields in the inspector. This can be seen as a list, where the '+' and '-' can be used for adding a new talk. No fields in the inspector should be left empty. The rigged model should be assigned in the rigged model field. The "Assign alignments every time" field can be used to assign alignments when they have already been saved. By default, when this option is disabled, in the alignment phase each additional talk will need alignments specified, however not any alignments which have already been saved during previous executions.

The Decal Material should already be set to the red circle material provided. The player should be assigned to the player container. Likewise, player camera should be assigned to the player camera container. Proximity to switch is how large the talk zones are, I use a value of 3 in most cases. The following scripts should be added to the following game objects:

- Switching Condition to the Volumetric switcher, as well as the volumetric switcher class.
- Rigged Guide to the Rigged Model, with the player assigned and a Nav mesh agent attached.
- The Move Models script to the move models game object.
- Player movement to the player container, and all relevant scripts/ fields inside.

Each object should be placed under the Volumetric switcher container, as shown in the paper associated with this project. A NavMesh Surface should be generated for the environment chosen.
