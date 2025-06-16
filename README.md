Unity project for a Robotics course that allows the user to control Phillips Hue lights using hand gestures. 

The goal was to allow the user to accurately place and select lights in 3D space by using LiDAR for accurate depth measurements of hand positions. 
Hand-pose recognition is implemented through classifying different relative positions of hand landmarks recognized through an Unity implementation of Google MediaPipe hand-pose recognition and depth measurements using an intel RealSense L515 LiDAR using intels RealSense Unity SDK.

The project currently only supports LiDAR based hand depth detection though depth estimation through precalibrated hand size and camera focal lengths could be added in the future to support full functionality using only a monocular RGB camera.

Demo Video:

[![Image-link showing a youtube video of the LightPointer project in action](https://img.youtube.com/vi/15Gn8WxPhrE/0.jpg)](https://www.youtube.com/watch?v=15Gn8WxPhrE)

Requirements:
- Intel Realsense L515 LiDAR.
- Unity 2021.3.18f1.
- Windows 10.
