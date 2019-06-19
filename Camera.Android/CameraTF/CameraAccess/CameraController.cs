using System;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Camera = Android.Hardware.Camera;

namespace CameraTF.CameraAccess
{
    public class CameraController
    {
        private readonly Context context;
        private readonly ISurfaceHolder holder;
        private readonly SurfaceView surfaceView;
        private readonly CameraEventsListener cameraEventListener;

        private int cameraId;
        private Camera camera;

        public int LastCameraDisplayOrientationDegree { get; private set; }

        public int LastCameraDisplayWidth { get; private set; }

        public int LastCameraDisplayHeight { get; private set; }

        public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener)
        {
            this.context = surfaceView.Context;
            this.holder = surfaceView.Holder;
            this.surfaceView = surfaceView;
            this.cameraEventListener = cameraEventListener;
        }

        public void RefreshCamera()
        {
            if (holder == null) return;

            ApplyCameraSettings();

            try
            {
                camera.SetPreviewDisplay(holder);
                camera.StartPreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public void ShutdownCamera()
        {
            if (camera == null) return;

            try
            {
                try
                {
                    camera.StopPreview();
                    camera.SetNonMarshalingPreviewCallback(null);

                    camera.SetPreviewDisplay(null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }

                camera.Release();
                camera = null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

        }

        public void SetupCamera()
        {
            if (camera != null) return;

            PermissionsHandler.CheckCameraPermissions(context);

            OpenCamera();

            if (camera == null) return;

            ApplyCameraSettings();

            try
            {
                camera.SetPreviewDisplay(holder);
                
                var previewParameters = camera.GetParameters();
                var previewSize = previewParameters.PreviewSize;
                var bitsPerPixel = ImageFormat.GetBitsPerPixel(previewParameters.PreviewFormat);

                int bufferSize = (previewSize.Width * previewSize.Height * bitsPerPixel) / 8;
				const int NUM_PREVIEW_BUFFERS = 5;
				for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
				{
					using (var buffer = new FastJavaByteArray(bufferSize))
						camera.AddCallbackBuffer(buffer);
				}

				camera.StartPreview();

                camera.SetNonMarshalingPreviewCallback(cameraEventListener);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return;
            }
            finally
            {
            }
        }

        private void OpenCamera()
        {
            try
            {
                var version = Build.VERSION.SdkInt;

                if (version >= BuildVersionCodes.Gingerbread)
                {
                    System.Diagnostics.Debug.WriteLine("Checking Number of cameras...");

                    var numCameras = Camera.NumberOfCameras;
                    var camInfo = new Camera.CameraInfo();
                    var found = false;
                    System.Diagnostics.Debug.WriteLine("Found " + numCameras + " cameras...");

                    var whichCamera = CameraFacing.Back;

					//if (_scannerHost.ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
     //                   _scannerHost.ScanningOptions.UseFrontCameraIfAvailable.Value)
     //                   whichCamera = CameraFacing.Front;

                    //TODO

                    for (var i = 0; i < numCameras; i++)
                    {
                        Camera.GetCameraInfo(i, camInfo);
                        if (camInfo.Facing == whichCamera)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "Found " + whichCamera + " Camera, opening...");
                            camera = Camera.Open(i);
                            cameraId = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Finding " + whichCamera + " camera failed, opening camera 0...");
                        camera = Camera.Open(0);
                        cameraId = 0;
                    }
                }
                else
                {
                    camera = Camera.Open();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Setup Error: {ex}");
            }
        }

        private void ApplyCameraSettings()
        {
            if (camera == null)
            {
                OpenCamera();
            }

            // do nothing if something wrong with camera
            if (camera == null) return;

            var parameters = camera.GetParameters();
            parameters.PreviewFormat = ImageFormatType.Nv21;

            var supportedFocusModes = parameters.SupportedFocusModes;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich &&
                supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
                parameters.FocusMode = Camera.Parameters.FocusModeAuto;
            else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
                parameters.FocusMode = Camera.Parameters.FocusModeFixed;

            var selectedFps = parameters.SupportedPreviewFpsRange.FirstOrDefault();
            if (selectedFps != null)
            {
                // This will make sure we select a range with the lowest minimum FPS
                // and maximum FPS which still has the lowest minimum
                // This should help maximize performance / support for hardware
                foreach (var fpsRange in parameters.SupportedPreviewFpsRange)
                {
                    if (fpsRange[0] <= selectedFps[0] && fpsRange[1] > selectedFps[1])
                        selectedFps = fpsRange;
                }
                parameters.SetPreviewFpsRange(selectedFps[0], selectedFps[1]);
            }

            CameraResolution resolution = null;
            var supportedPreviewSizes = parameters.SupportedPreviewSizes;
            if (supportedPreviewSizes != null)
            {
                var availableResolutions = supportedPreviewSizes.Select(sps => new CameraResolution
                {
                    Width = sps.Width,
                    Height = sps.Height
                });

                // If the user did not specify a resolution, let's try and find a suitable one
                if (resolution == null)
                {
                    foreach (var sps in supportedPreviewSizes)
                    {
                        if (sps.Width >= 640 && sps.Width <= 1000 && sps.Height >= 360 && sps.Height <= 1000)
                        {
                            resolution = new CameraResolution
                            {
                                Width = sps.Width,
                                Height = sps.Height
                            };
                            break;
                        }
                    }
                }
            }

            // Hopefully a resolution was selected at some point
            if (resolution != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Selected Resolution: " + resolution.Width + "x" + resolution.Height);
                parameters.SetPreviewSize(resolution.Width, resolution.Height);

                LastCameraDisplayWidth = parameters.PreviewSize.Width;
                LastCameraDisplayHeight = parameters.PreviewSize.Height;
            }

            camera.SetParameters(parameters);

            SetCameraDisplayOrientation();
        }

        private void SetCameraDisplayOrientation()
        {
            var degrees = GetCameraDisplayOrientation();
            LastCameraDisplayOrientationDegree = degrees;

            System.Diagnostics.Debug.WriteLine("Changing Camera Orientation to: " + degrees);

            try
            {
                camera.SetDisplayOrientation(degrees);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private int GetCameraDisplayOrientation()
        {
            int degrees;
            var windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var display = windowManager.DefaultDisplay;
            var rotation = display.Rotation;

            switch (rotation)
            {
                case SurfaceOrientation.Rotation0:
                    degrees = 0;
                    break;
                case SurfaceOrientation.Rotation90:
                    degrees = 90;
                    break;
                case SurfaceOrientation.Rotation180:
                    degrees = 180;
                    break;
                case SurfaceOrientation.Rotation270:
                    degrees = 270;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var info = new Camera.CameraInfo();
            Camera.GetCameraInfo(cameraId, info);

            int correctedDegrees;
            if (info.Facing == CameraFacing.Front)
            {
                correctedDegrees = (info.Orientation + degrees)%360;
                correctedDegrees = (360 - correctedDegrees)%360; // compensate the mirror
            }
            else
            {
                // back-facing
                correctedDegrees = (info.Orientation - degrees + 360)%360;
            }

            return correctedDegrees;
        }
    }
}