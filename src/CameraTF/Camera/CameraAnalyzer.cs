using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using CameraTF.Helpers;
using SkiaSharp;

namespace CameraTF.CameraAccess
{
    public class CameraAnalyzer
    {
        private const string Model = "hardhat_detect.tflite";

        private readonly CameraController cameraController;
        private readonly CameraEventsListener cameraEventListener;
        private Task processingTask;

        private int width;
        private int height;
        private int cDegrees;

        private SKBitmap input;
        private IntPtr colors;
        private int colorCount;
        private SKBitmap inputScaled;
        private SKBitmap inputScaledRotated;
        private int[] imageData;
        private GCHandle imageGCHandle;
        private IntPtr imageIntPtr;

        private TensorflowLiteService tfService;

        private readonly FPSCounter cameraFPSCounter;
        private readonly FPSCounter processingFPSCounter;

        private readonly Stopwatch stopwatch;

        public CameraAnalyzer(SurfaceView surfaceView)
        {
            cameraEventListener = new CameraEventsListener();
            cameraController = new CameraController(surfaceView, cameraEventListener);

            InitTensorflowLineService();

            var outputInfo = new SKImageInfo(
                TensorflowLiteService.ModelInputSize, 
                TensorflowLiteService.ModelInputSize, 
                SKColorType.Rgba8888);
            inputScaled = new SKBitmap(outputInfo);
            inputScaledRotated = new SKBitmap(outputInfo);

            colors = inputScaledRotated.GetPixels();
            colorCount = TensorflowLiteService.ModelInputSize * TensorflowLiteService.ModelInputSize;

            stopwatch = new Stopwatch();

            cameraFPSCounter = new FPSCounter((x) => {
                Stats.CameraFps = x.fps;
                Stats.CameraMs = x.ms;
            });

            processingFPSCounter = new FPSCounter((x) => {
                Stats.ProcessingFps = x.fps;
                Stats.ProcessingMs = x.ms;
            });
        }

        public void SetupCamera()
        {
            cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            cameraController.SetupCamera();
        }

        public void RefreshCamera()
        {
            cameraController.RefreshCamera();
        }

        public void ShutdownCamera()
        {
            cameraController.ShutdownCamera();
        }

        private bool CanAnalyzeFrame
        {
            get
            {				
                if (processingTask != null && !processingTask.IsCompleted)
                    return false;

				return true;
            }
        }

        private void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
        {
            cameraFPSCounter.Report();

            if (!CanAnalyzeFrame)
                return;

            processingFPSCounter.Report();

            processingTask = Task.Run(() =>
			{
                try
				{
					DecodeFrame(fastArray);
				} catch (Exception ex) {
					Console.WriteLine(ex);
				}
			}).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Debug.WriteLine("DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private unsafe void DecodeFrame(FastJavaByteArray fastArray)
        {
            if (input == null)
            {
                width = cameraController.LastCameraDisplayWidth;
                height = cameraController.LastCameraDisplayHeight;
                cDegrees = cameraController.LastCameraDisplayOrientationDegree;

                imageData = new int[width * height];
                imageGCHandle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
                imageIntPtr = imageGCHandle.AddrOfPinnedObject();

                input = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888));
                input.InstallPixels(input.Info, imageIntPtr);
            }

            var pY = fastArray.Raw;
            var pUV = pY + width * height;

            stopwatch.Restart();
            YuvHelper.ConvertYUV420SPToARGB8888(pY, pUV, (int*)imageIntPtr, width, height);
            stopwatch.Stop();
            Stats.YUV2RGBElapsedMs = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            input.ScalePixels(inputScaled, SKFilterQuality.None);
            RotateBitmap(inputScaled, cDegrees);
            stopwatch.Stop();
            Stats.ResizeAndRotateElapsedMs = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            tfService.Recognize(colors, colorCount);
            stopwatch.Stop();
            Stats.InterpreterElapsedMs = stopwatch.ElapsedMilliseconds;

            MainActivity.ReloadCanvas();
        }

        private void RotateBitmap(SKBitmap bitmap, int degrees)
        {
            using (var surface = new SKCanvas(inputScaledRotated))
            {
                surface.Translate(inputScaledRotated.Width / 2, inputScaledRotated.Height / 2);
                surface.RotateDegrees(degrees);
                surface.Translate(-inputScaledRotated.Width / 2, -inputScaledRotated.Height / 2);
                surface.DrawBitmap(bitmap, 0, 0);
            }
        }

        private void InitTensorflowLineService()
        {
            using (var modelData = Application.Context.Assets.Open(Model))
            {
                tfService = new TensorflowLiteService();
                tfService.Initialize(modelData, useNumThreads: true);
            }
        }
    }
}