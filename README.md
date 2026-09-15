# CameraTF

Realtime object detection on Android with Xamarin.Android and TensorFlow Lite.

This is the sample app from the article
[Realtime mobile object detector in Xamarin.Android](https://jacano.github.io/blog/realtime-mobile-object-detector-xamarin-android/).

## What it does

- It shows the rear camera preview with a Xamarin binding for `android.hardware.camera2`.
- It converts NV21 (YUV420sp) to RGB in native code.
- It scales and rotates the frame with SkiaSharp.
- It runs an SSD MobileNet model with [EmguTF](https://github.com/emgucv/emgutf), a C# binding for TensorFlow Lite.
- It draws the detected object and its confidence.

The sample detects one class (a white hardhat) to keep the code small. It ran at
about 7 fps on a Google Pixel XL.

## Build

Open `src/CameraTF.sln` in Visual Studio. Build the Android project and run it on
a device. The model `hardhat_detect.tflite` and `hardhat_labels_list.txt` are in
`src/Resources/hardhat`.

## Related

- The EmguTF libs and the AR demo were part of my pull requests in
  [microsoft/TailwindTraders-Mobile](https://github.com/microsoft/TailwindTraders-Mobile).
- Native YUV to RGB lives in `src/CameraTF/Libs/YUV`.
