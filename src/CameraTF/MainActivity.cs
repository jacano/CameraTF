using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Support.V7.App;
using SkiaSharp.Views.Android;
using CameraTF.Helpers;
using System.IO;
using System;
using System.Linq;

namespace CameraTF
{
    [Activity (
        MainLauncher = true, 
        Theme = "@style/AppTheme.NoActionBar", 
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    public class MainActivity : AppCompatActivity
    {
        private const float MinScore = 0.6f;
        private const int LabelOffset = 1;
        private const string LabelsFilename = "hardhat_labels_list.txt";

        private string[] labels;

        private static SKCanvasView canvasView;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.activitylayout);

            LoadModelLabels();

            var cameraSurface = new CameraSurfaceView(this);
            canvasView = new SKCanvasView(this);
            canvasView.PaintSurface += Canvas_PaintSurface;

            var mainView = this.FindViewById<FrameLayout>(Resource.Id.frameLayout1);
            mainView.AddView(cameraSurface);
            mainView.AddView(canvasView);
        }

        public static void ReloadCanvas()
        {
            canvasView.postInvalidate();
        }

        private void LoadModelLabels()
        {
            using (var labelData = Application.Context.Assets.Open(LabelsFilename))
            {
                using (var reader = new StreamReader(labelData))
                {
                    var text = reader.ReadToEnd();
                    labels = text
                        .Split(new[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToArray();
                }
            }
        }

        private void Canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            var canvasWidth = e.Info.Width;
            var canvasHeight = e.Info.Height;

            canvas.Clear();

            var leftMargin = 5;
            var bottomMargin = 5;

            var recHeight = 250;
            DrawingHelper.DrawBackgroundRectangle(
                canvas,
                canvasWidth,
                recHeight,
                0,
                canvasHeight - recHeight);

            DrawingHelper.DrawText(
                canvas,
                leftMargin,
                canvasHeight - 0 - bottomMargin,
                $"Camera: {Stats.CameraFps} fps ({Stats.CameraMs} ms)");

            DrawingHelper.DrawText(
                canvas,
                leftMargin,
                canvasHeight - 50 - bottomMargin,
                $"Processing: {Stats.ProcessingFps} fps ({Stats.ProcessingMs} ms)");

            DrawingHelper.DrawText(
                canvas,
                leftMargin,
                canvasHeight - 100 - bottomMargin,
                $"YUV2RGB: {Stats.YUV2RGBElapsedMs} ms");

            DrawingHelper.DrawText(
                canvas,
                leftMargin,
                canvasHeight - 150 - bottomMargin,
                $"ResizeAndRotate: {Stats.ResizeAndRotateElapsedMs} ms");

            DrawingHelper.DrawText(
                canvas,
                leftMargin,
                canvasHeight - 200 - bottomMargin,
                $"TFRecognize: {Stats.InterpreterElapsedMs} ms");

            for (var i = 0; i < Stats.NumDetections; i++)
            {
                var score = Stats.Scores[i];
                var labelIndex = (int)Stats.Labels[i];
                var xmin = Stats.BoundingBoxes[i * 4 + 0];
                var ymin = Stats.BoundingBoxes[i * 4 + 1];
                var xmax = Stats.BoundingBoxes[i * 4 + 2];
                var ymax = Stats.BoundingBoxes[i * 4 + 3];

                if (!labelIndex.Between(0, labels.Length - 1)) continue;
                if (score < MinScore) continue;

                var left = ymin * canvasWidth;
                var top = xmin * canvasHeight;
                var right = ymax * canvasWidth;
                var bottom = xmax * canvasHeight;

                DrawingHelper.DrawBoundingBox(
                    canvas,
                    left,
                    top,
                    right,
                    bottom);

                var label = labels[labelIndex + LabelOffset];
                DrawingHelper.DrawText(canvas, left, bottom, $"{label} - {score}");
            }
        }

        protected async override void OnResume ()
        {
            base.OnResume ();

            if (PermissionsHandler.NeedsPermissionRequest(this))
                await PermissionsHandler.RequestPermissionsAsync(this);
        }

        public override void OnRequestPermissionsResult (
            int requestCode, 
            string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        { 
            PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}