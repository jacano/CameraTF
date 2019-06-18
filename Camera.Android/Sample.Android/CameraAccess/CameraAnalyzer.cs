using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Sample.Android.Helpers;
using SkiaSharp;
using TailwindTraders.Mobile.Features.Scanning;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        private readonly CameraController cameraController;
        private readonly CameraEventsListener cameraEventListener;
        private Task processingTask;

        private int[] rgba;
        private IntPtr output;
        private SKBitmap skiaRGB;
        private SKBitmap skiaScaledRGB;
        private SKBitmap skiaRotatedRGB;

        private int width;
        private int height;
        private int cDegrees;
        private int rgbaCount;

        public CameraAnalyzer(SurfaceView surfaceView)
        {
            cameraEventListener = new CameraEventsListener();
            cameraController = new CameraController(surfaceView, cameraEventListener);
        }

        public bool IsAnalyzing { get; private set; }

        public void PauseAnalysis()
        {
            IsAnalyzing = false;
        }

        public void ResumeAnalysis()
        {
            IsAnalyzing = true;
        }

        public void ShutdownCamera()
        {
            IsAnalyzing = false;
            cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            cameraController.SetupCamera();
        }

        public void AutoFocus()
        {
            cameraController.AutoFocus();
        }

        public void AutoFocus(int x, int y)
        {
            cameraController.AutoFocus(x, y);
        }

        public void RefreshCamera()
        {
            cameraController.RefreshCamera();
        }

        private bool CanAnalyzeFrame
        {
            get
            {
				if (!IsAnalyzing)
					return false;
				
                if (processingTask != null && !processingTask.IsCompleted)
                    return false;

				return true;
            }
        }

        private void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
        {
            if (!CanAnalyzeFrame)
                return;

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
            if (rgba == null)
            {
                var cameraParameters = cameraController.Camera.GetParameters();
                width = cameraParameters.PreviewSize.Width;
                height = cameraParameters.PreviewSize.Height;

                cDegrees = cameraController.LastCameraDisplayOrientationDegree;

                rgbaCount = width * height;
                rgba = new int[rgbaCount];

                var rgbGCHandle = GCHandle.Alloc(rgba, GCHandleType.Pinned);
                output = rgbGCHandle.AddrOfPinnedObject();

                var inputInfo = new SKImageInfo(width, height, SKColorType.Rgba8888);
                skiaRGB = new SKBitmap(inputInfo);

                var outputInfo = new SKImageInfo(TensorflowLiteService.ModelInputSize, TensorflowLiteService.ModelInputSize, SKColorType.Rgba8888);
                skiaScaledRGB = new SKBitmap(outputInfo);
                skiaRotatedRGB = new SKBitmap(outputInfo);
            }

            var stopwatch = Stopwatch.StartNew();

            var pY = fastArray.Raw;
            var pUV = pY + rgbaCount;
            
            YuvHelper.ConvertYUV420SPToARGB8888(pY, pUV, (int*)output, width, height);

            skiaRGB.InstallPixels(skiaRGB.Info, output);

            skiaRGB.ScalePixels(skiaScaledRGB, SKFilterQuality.None);

            RotateBitmap(skiaScaledRGB, cDegrees);

            var colors = skiaRotatedRGB.GetPixels();
            var colorCount = TensorflowLiteService.ModelInputSize * TensorflowLiteService.ModelInputSize;

            ZxingActivity.tfService.Recognize((int*)colors, colorCount);

            stopwatch.Stop();

            Debug.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");
        }

        private void RotateBitmap(SKBitmap bitmap, int degrees)
        {
            using (var surface = new SKCanvas(skiaRotatedRGB))
            {
                surface.Translate(skiaRotatedRGB.Width / 2, skiaRotatedRGB.Height / 2);
                surface.RotateDegrees(degrees);
                surface.Translate(-skiaRotatedRGB.Width / 2, -skiaRotatedRGB.Height / 2);
                surface.DrawBitmap(bitmap, 0, 0);
            }
        }

        private void SaveSkiaImg(SKBitmap img)
        {
            var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            var filePath = Path.Combine(path, "test-skia.png");

            using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var d = SKImage.FromBitmap(img).Encode(SKEncodedImageFormat.Png, 100);
                d.SaveTo(stream);
            }
        }
    }
}