# OpenCV_Tracking_Unity_Template

Template to start projects with that will require the OpenCV for Unity Asset
This specifically has my code to make it easy to control game objects using color tracking which can be found in: Assets/Scripts/Target Control/

Objective: I want to make it easier for users and developers to calibrate their computer vision object tracking on the fly. Across different application uses, the object being tracked does not change, but the lighting and background often does, making the object harder to track. I broke this problem down into two steps that, when used together, provide effective and mostly automated calibration: Suggest an object color, or set of colors, that will track best based on the users’ current camera background, and Calibrate the application to the color of the specific object that the user already has.

Description:
In the first step, the application takes every pixel’s hue value in the frame, and draws the least common at the bottom of the screen to indicate to the user what colors would stand out most against their current background.
Step two calibrates the color values for the tracked object, there is a target area in the frame to fill with the desired object. This looks at the hue, saturation, and brightness value of every pixel in that area, and calculates the minimum and maximum for each, thus establishing a color threshold that informs how the image is processed to track the object.
