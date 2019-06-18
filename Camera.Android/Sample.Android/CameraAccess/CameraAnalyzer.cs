using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using Sample.Android.Helpers;
using SkiaSharp;
using TailwindTraders.Mobile.Features.Scanning;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        private readonly CameraController _cameraController;
        private readonly CameraEventsListener _cameraEventListener;
        private Task _processingTask;
        private DateTime _lastPreviewAnalysis = DateTime.UtcNow;

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
            _cameraEventListener = new CameraEventsListener();
            _cameraController = new CameraController(surfaceView, _cameraEventListener);
            Torch = new Torch(_cameraController, surfaceView.Context);
        }

        public Torch Torch { get; }

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
            _cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
            _cameraController.ShutdownCamera();
        }

        public void SetupCamera()
        {
            _cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
            _cameraController.SetupCamera();
        }

        public void AutoFocus()
        {
            _cameraController.AutoFocus();
        }

        public void AutoFocus(int x, int y)
        {
            _cameraController.AutoFocus(x, y);
        }

        public void RefreshCamera()
        {
            _cameraController.RefreshCamera();
        }

        private bool CanAnalyzeFrame
        {
            get
            {
				if (!IsAnalyzing)
					return false;
				
                //Check and see if we're still processing a previous frame
                // todo: check if we can run as many as possible or mby run two analyzers at once (Vision + ZXing)
                if (_processingTask != null && !_processingTask.IsCompleted)
                    return false;

				return true;
            }
        }

        private void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
        {
            if (!CanAnalyzeFrame)
                return;

            _lastPreviewAnalysis = DateTime.UtcNow;

			_processingTask = Task.Run(() =>
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
                    Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "DecodeFrame exception occurs");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private unsafe void DecodeFrame(FastJavaByteArray fastArray)
        {
            if (rgba == null)
            {
                var cameraParameters = _cameraController.Camera.GetParameters();
                width = cameraParameters.PreviewSize.Width;
                height = cameraParameters.PreviewSize.Height;

                cDegrees = _cameraController.LastCameraDisplayOrientationDegree;

                rgbaCount = width * height;
                rgba = new int[rgbaCount];

                var rgbGCHandle = GCHandle.Alloc(rgba, GCHandleType.Pinned);
                output = rgbGCHandle.AddrOfPinnedObject();

                var inputInfo = new SKImageInfo(width, height, SkiaSharp.SKColorType.Rgba8888);
                skiaRGB = new SKBitmap(inputInfo);

                var outputInfo = new SKImageInfo(TensorflowLiteService.ModelInputSize, TensorflowLiteService.ModelInputSize, SKColorType.Rgba8888);
                skiaScaledRGB = new SKBitmap(outputInfo);
                skiaRotatedRGB = new SKBitmap(outputInfo);
            }

            var start = PerformanceCounter.Start();

            var pY = fastArray.Raw;
            var pUV = pY + rgbaCount;
            
            YuvHelper.ConvertYUV420SPToARGB8888(pY, pUV, (int*)output, width, height);

            skiaRGB.InstallPixels(skiaRGB.Info, output);

            skiaRGB.ScalePixels(skiaScaledRGB, SKFilterQuality.None);

            RotateBitmap(skiaScaledRGB, cDegrees);

            var colors = skiaRotatedRGB.GetPixels();
            var colorCount = TensorflowLiteService.ModelInputSize * TensorflowLiteService.ModelInputSize;

            ZxingActivity.tfService.Recognize((int*)colors, colorCount);

            PerformanceCounter.Stop(start, "{0} ms");
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