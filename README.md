## What Is It?

This script allows you to bring head tracking into Unity using [AITrack](https://github.com/AIRLegend/aitrack), which allows for the tracking of your head using a variety of tools, including webcams and phones. This script receives information from AITrack and outputs basic position and rotation information that can be applied directly to a transform (such as a head). You can see a demo here:

[![AITrackReceiver Demo](https://img.youtube.com/vi/586IDJlAw2M/0.jpg)](https://www.youtube.com/watch?v=586IDJlAw2M)

### Requirements

To use this script, you will need AITrack and a compatible device to capture your face. AITrack works with standard webcams, [Droid Cam](https://www.dev47apps.com/), and more. It is also remarkably effective in low light and with low resolution and low frame rates. Check out their page for more information.

It does not use any special packages or tools within Unity, and should work with any edition of the game engine. I have only tested it in 2022.3.21f, however.

### Installing

1. Download [AITrack](https://github.com/AIRLegend/aitrack) and get everything up and running.
2. In AITrack's settings, ensure "Use remote OpenTrack client" is checked and make anote of the port number.
3. Drag AITrackReceiver.cs into your project and attach it to a GameObject in your scene.
4. Ensure the "port" variable in the inspector matches the port number AITrack from above.

### Usage

Wherever you need the position and rotation values of your head, get a reference to the AITrackerReceiver component and simply set your position and rotation with `AITrackReceiver.Position` (Vector3) and `AITrackReceiver.Rotation` (Quaternion). There are also some public variables for grabbing the raw position data if you want to play with them.

Once you have an object following the movement/rotation of the head tracker, there are some settings in the AITrackerReciever Inspector that you can tweak to get the movement to your liking.

### Other Information

#### Relative Position
`AITrackReceiver.Position` contains position information based around 0, meaning that if you set the tracker up perfectly and placed your head dead-centre in front of the camera, `AITrackReceiver.Position` would return 0, 0, 0. Bear this in mind when setting your position. You made need to add an offset and/or set `localPosition` rather than `position`.

#### Clamping
The script does not currently include any "protections" against over-rotating or moving too far. You can tweak the variables in the inspector to your liking, but if you want to restrict movement to a specific range, you will need to implement that yourself.

#### AITrack Instability
AITrack seems to have some issues with certain extremes (looking down and to the right in my case), which can cause the position/rotation information to jump rapidly around.

#### Y Position/Rotation
There seems to be some confusion between up/down movement and up/down rotation, in that if you look up, the head position goes up. Similarly, if you move up, the head rotation tilts up. I didn't find an easy way to fix this and Y position is not something I needed so I left it.

## Credits

Though it has been almost entirely rewritten, this script started out as a modification of [unityFaceTracking](https://github.com/marcteys/unityFaceTracking) by [https://github.com/marcteys/unityFaceTracking/commits?author=marcteys](marcteys).