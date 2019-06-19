using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using CameraTF.AR;
using CameraTF.Helpers;
using PubSub.Extension;
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
        private SKBitmap inputScaled;
        private SKBitmap inputScaledRotated;
        private int[] imageData;
        private GCHandle imageGCHandle;
        private IntPtr imageIntPtr;

        private TensorflowLiteService tfService;

        private FPSCounter cameraFPSCounter;
        private FPSCounter processingFPSCounter;

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

            cameraFPSCounter = new FPSCounter((x) => this.Publish(new CameraStatsMessage()
            {
                Fps = x.fps,
                Ms = x.ms,
            }));

            processingFPSCounter = new FPSCounter((x) => this.Publish(new ProcessingStatsMessage()
            {
                Fps = x.fps,
                Ms = x.ms,
            }));
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

			processingTask = Task.Run(() =>
			{
                processingFPSCounter.Report();

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
            }

            var pY = fastArray.Raw;
            var pUV = pY + width * height;
            YuvHelper.ConvertYUV420SPToARGB8888(pY, pUV, (int*)imageIntPtr, width, height);

            input.InstallPixels(input.Info, imageIntPtr);

            input.ScalePixels(inputScaled, SKFilterQuality.None);

            RotateBitmap(inputScaled, cDegrees);

            var colors = inputScaledRotated.GetPixels();
            var colorCount = TensorflowLiteService.ModelInputSize * TensorflowLiteService.ModelInputSize;

            tfService.Recognize((int*)colors, colorCount);
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

        private void SaveSkiaImg(SKBitmap img)
        {
            var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            var filePath = Path.Combine(path, "test_skia.png");

            using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var d = SKImage.FromBitmap(img).Encode(SKEncodedImageFormat.Png, 100);
                d.SaveTo(stream);
            }
        }
    }
}