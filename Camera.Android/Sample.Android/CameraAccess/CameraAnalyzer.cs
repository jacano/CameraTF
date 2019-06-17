using System;
using System.IO;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using ApxLabs.FastAndroidCamera;
using TailwindTraders.Mobile.Droid.ThirdParties.Camera;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile.CameraAccess
{
    public class CameraAnalyzer
    {
        private readonly CameraController _cameraController;
        private readonly CameraEventsListener _cameraEventListener;
        private Task _processingTask;
        private DateTime _lastPreviewAnalysis = DateTime.UtcNow;

        //private int[] colorArray;

        public CameraAnalyzer(SurfaceView surfaceView)
        {
            _cameraEventListener = new CameraEventsListener();
            _cameraController = new CameraController(surfaceView, _cameraEventListener);
            Torch = new Torch(_cameraController, surfaceView.Context);

            //colorArray = new int[300 * 300];
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

        private void DecodeFrame(FastJavaByteArray fastArray)
        {
            var cameraParameters = _cameraController.Camera.GetParameters();
            var width = cameraParameters.PreviewSize.Width;
            var height = cameraParameters.PreviewSize.Height;

            // use last value for performance gain
            var cDegrees = _cameraController.LastCameraDisplayOrientationDegree;

            var start = PerformanceCounter.Start();


            //YuvImage yuv = new YuvImage(data, parameters.getPreviewFormat(), width, height, null);

            //ByteArrayOutputStream out = new ByteArrayOutputStream();
            //yuv.compressToJpeg(new Rect(0, 0, width, height), 50, out);

            //byte[] bytes = out.toByteArray();
            //final Bitmap bitmap = BitmapFactory.decodeByteArray(bytes, 0, bytes.length);


            //var byteBuffer = Java.Nio.ByteBuffer.Wrap(fastArray.Handle);

            //var rawImg = new byte[fastArray.Count];

            //fastArray.CopyTo(rawImg, fastArray.Count);

            //var decodeOpt = new BitmapFactory.Options()
            //{
            //    InSampleSize = CalculateInSampleSize(width, height, 300, 300)
            //};

            //using (var resizedBitmap = BitmapFactory.DecodeByteArray(rawImg, 0, rawImg.Length, decodeOpt))
            //{
            //    using (var rotatedImage = RotateImage(resizedBitmap, cDegrees))
            //    {
            //        CopyColors(rotatedImage);

            //        //ZxingActivity.tfService.Recognize(colorArray);
            //    }
            //}

            PerformanceCounter.Stop(start,
                    "Decode Time: {0} ms (width: " + width + ", height: " + height + ", degrees: " + cDegrees + ")");
        }

        //private void SaveImg(Bitmap img)
        //{
        //    var path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
        //    var filePath = System.IO.Path.Combine(path, "test.png");

        //    using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        //    {
        //        img.Compress(Bitmap.CompressFormat.Png, 100, stream);
        //    }
        //}

        //private int CalculateInSampleSize(int currentWidth, int currentHeight, int reqWidth, int reqHeight)
        //{
        //    // Raw height and width of image
        //    float height = currentHeight;
        //    float width = currentWidth;
        //    double inSampleSize = 1D;

        //    if (height > reqHeight || width > reqWidth)
        //    {
        //        int halfHeight = (int)(height / 2);
        //        int halfWidth = (int)(width / 2);

        //        // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
        //        while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
        //        {
        //            inSampleSize *= 2;
        //        }
        //    }

        //    return (int)inSampleSize;
        //}

        //private Bitmap RotateImage(Bitmap image, int orientation)
        //{
        //    var matrix = new Matrix();
        //    matrix.PostRotate(orientation);

        //    var rotatedImage = Bitmap.CreateBitmap(image, 0, 0, image.Width, image.Height, matrix, true);
        //    return rotatedImage;
        //}

        //private void CopyColors(Bitmap bmp)
        //{
        //    bmp.GetPixels(colorArray, 0, bmp.Width, 0, 0, bmp.Width, bmp.Height);

        //    for (int i = 0; i < colorArray.Length; i++)
        //    {
        //        var color = new ColorUnion((uint)colorArray[i]);
        //        var swappedColor = new ColorUnion(color.B, color.G, color.R, color.A);

        //        colorArray[i] = (int)swappedColor.Value;
        //    }
        //}
    }
}