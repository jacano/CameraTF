using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Support.V7.App;
using SkiaSharp.Views.Android;
using CameraTF.AR;
using PubSub.Extension;
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
        public static readonly string[] RequiredPermissions = new[] {
            Android.Manifest.Permission.Camera,
            Android.Manifest.Permission.Flashlight,
            Android.Manifest.Permission.WriteExternalStorage
        };

        private const float MinScore = 0.6f;
        private const int LabelOffset = 1;
        private const string LabelsFilename = "hardhat_labels_list.txt";

        private string[] labels;

        private DetectionMessage lastDetectionMessage;
        private StatsMessage lastCameraStatsMessage;
        private ProcessingStatsMessage lastProcessingStatsMessage;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.activitylayout);

            LoadModelLabels();

            var cameraSurface = new CameraSurfaceView(this);
            var canvasView = new SKCanvasView(this);
            canvasView.PaintSurface += Canvas_PaintSurface;

            this.Subscribe<CameraStatsMessage>((d) => lastCameraStatsMessage = d);
            this.Subscribe<ProcessingStatsMessage>((d) => lastProcessingStatsMessage = d);
            this.Subscribe<DetectionMessage>((d) =>
            {
                lastDetectionMessage = d;

                canvasView.Invalidate();
            });

            var mainView = this.FindViewById<FrameLayout>(Resource.Id.frameLayout1);
            mainView.AddView(cameraSurface);
            mainView.AddView(canvasView);
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

            var detection = lastDetectionMessage;
            if (detection == null) return;

            canvas.Clear();

            var leftMargin = 5;

            var recHeight = 150;
            DrawingHelper.DrawBackgroundRectangle(
                canvas,
                canvasWidth,
                recHeight,
                0,
                canvasHeight - recHeight);

            DrawingHelper.DrawText(
               canvas,
               leftMargin,
               canvasHeight - 105,
               $"TF interpreter invoke: {detection.InterpreterElapsedMs} ms");

            var cameraStats = lastCameraStatsMessage;
            var processingStats = lastProcessingStatsMessage;
            if (cameraStats != null && processingStats != null)
            {
                DrawingHelper.DrawText(
                    canvas,
                    leftMargin,
                    canvasHeight - 55,
                    $"Processing FPS: {processingStats.Fps} fps ({processingStats.Ms} ms)");

                DrawingHelper.DrawText(
                    canvas,
                    leftMargin,
                    canvasHeight - 5,
                    $"Camera FPS: {cameraStats.Fps} fps ({cameraStats.Ms} ms)");
            }

            for (var i = 0; i < detection.NumDetections; i++)
            {
                var score = detection.Scores[i];
                var labelIndex = (int)detection.Labels[i];
                var xmin = detection.BoundingBoxes[i * 4 + 0];
                var ymin = detection.BoundingBoxes[i * 4 + 1];
                var xmax = detection.BoundingBoxes[i * 4 + 2];
                var ymax = detection.BoundingBoxes[i * 4 + 3];

                if (!labelIndex.Between(0, labels.Length - 1)) continue;
                if (score < MinScore) continue;

                DrawingHelper.DrawBoundingBox(
                    canvas,
                    canvasWidth,
                    canvasHeight,
                    xmin,
                    ymin,
                    xmax,
                    ymax);

                var left = ymin * canvasWidth;
                var bottom = xmax * canvasHeight;
                var label = labels[labelIndex + LabelOffset];
                DrawingHelper.DrawText(canvas, left, bottom, $"{label} - {score}");
            }

            lastDetectionMessage = null;
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