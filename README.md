# J3D-Model-Viewer
This project uses the JStudio library to load .bmd and .bdl models and apply bone and texture animations to them. It also emulates lighting and modification of the TEV Color and TEV kColor registers, useful in emulating effects in many of the games that support .bmd/.bdl.

![Link opening a chest](http://i.imgur.com/0BRD2hb.png)


# Dependencies (Included)
* [JStudio](https://github.com/LordNed/JStudio)
* [WCommon](https://github.com/LordNed/WCommon)
* [WArchiveTools](https://github.com/LordNed/WArchive-Tools)
* [OpenTK](https://github.com/opentk/opentk)
*   OpenTK-GLControl
* [GameFormatReader](https://github.com/lioncash/GameFormatReader)

# Loading and Viewing Files
`File > Open (Ctrl + O)`
`File > Open Additive`

These two options allow you to open a .bmd, or .bdl model. The additive option will replace all currently loaded models, while Open Additive will additively load the other model (for situations where there may be multiple models put together). The viewer does not currently support bone merging techniques so loading Link's sword will not put it in his hand.

`File > Load Animation`
This option allows you to load a bone animation (.bck) or a "Texture" (uv) animation (.btk). When bone animations are loaded, they will show up in the right hand column under "Bone Animations". There is currently an issue where the UI does not update until it is resized, but once you resize it, it should list the animation name, whether or not it is intended to loop in game, its length in seconds and frames, and its internal version number (such as "bck1"). Clicking on the different animations in the list will switch which one the model plays. There is currently no way to deselect all animations once an animation is loaded, so you will have to close and re-open the model!

There is currently no UI for selecting different material animations that have been loaded either.

`File > Close`
This simply closes the opened models and unloads any animations.

`File > Export...`
None of these work yet, it's probably not worth trying!

`File > Exit`
Closes the application!

`View > Show Pivot`
Shows where the origin of the world is with a small RGB line marker.

`View > Show Grid`
Shows a grid of a fixed size on the ground. Helpful in large models or when moving the camera.

`View > Show...`
Supports showing various data stored inside the model format which is useful for debugging or investigation but not all of the time. Has support for showing the bounding box/bounding sphere of both Mesh Batches and Bones. May not be entirely accurate, this is still under investigation.

# Controls
Hold RMB + WASD lets to move around, holding down Left Shift at the same time speeds the camera up. Holding RMB allows you to rotate the camera with the mouse. Holding RMB + Mouse Scroll (up/down) changes the speed at which the camera moves as well. Pressing tilda/backtick toggles an experimental Orbit viewing mode, RMB to orbit. Pressing it again returns you to normal flycam mode.

# TEV Color Overrides
This allows you to overwrite the 8 TEV registers available for GameCube applications to choose. These registers are broken into two groups, "Color" and "kColor". The "Color" registers are the initial value of the four color registers inside of TEV which get modified at the end of each TEV Stage. "kColor" registers on the other hand are constant and cannot get modified by a TEV Stage. Various models use various combinations of both Color and kColor to achive certain effects like choosing both the light and shadow color for a given model. Clicking the checkbox enables whether or not it overrides the default provided by the model's material or not, and clicking on the color boxes lets you change the color that you are overriding it with.

# High Resolution Screenshots
This allows you to take super high resolution images of your current rendered viewport. The sldier acts as a multiplier for your current viewport's resolution, and it will try to make the screenshot that big. Many GPUs have a maximum texture resolution size in the 16-32k range and surpassing that may cause your video drivers to crash. If that happens, simply choose a smaller multiplier next time! These are saved to the "Screenshots" folder next to the executable. 

# ToDos
`Rendering`
The loading of models/animations and rendering is relatively well supported. This viewer correctly emulates "Toon" style shading from The Legend of Zelda: The Wind Waker, which none of the other standalone model viewers are able to do. It struggles with many of the Super Mario Galaxy models as they use Post Tex matrices which are currently not supported so many will result in a broken shader. This broken shader will show up as a view of the normals instead, or flat blue if the mesh does not provide normals. These are enhancements that need to be added to the [JStudio](https://github.com/LordNed/JStudio) project.

`Lighting`
The J3D format supports up to 8 lights of various type (spot, point, etc.) and various parameters (attenuation, falloff, etc.), as well as positioning them. Ideally the translation/rotation/scale gizmo from the main editor will be ported over here to let you choose lighting to set up basic lighting conditions. Currently two lights are supported with one of them circuling around the origin which makes most Wind Waker models render correctly. There is no way (via the UI) to disable the light rotation or disable these lights yet!

`Animation`
Bone and Texture (uv) animations are currently supported. There is an issue with the cubic sampling applied to texture animations which makes the speed of the animations slow down to zero at the end of the loop point, so they will render correctly but their timing may seem off. Some .bck/.btk files still crash it when opening them and attempting playback. There is no support for other types of animation yet, though many of them are documented on the original Wind Editor wiki. Support for these formats need to be added to [JStudio](https://github.com/LordNed/JStudio), and then they can be hooked up to the UI in this project.

`UI`
There is a bug with the UI that draws Bone Animations and the list in the UI will not update until it is resized. There is currently no UI for choosing material animations, but it should be straightforward to implement based on the Bone Animations. Lots of the J3D stuff needs to be exposed to the UI, which involves finding a UX for drawing the UI that is not too cluttered even though J3D files can have a lot of data in them.
