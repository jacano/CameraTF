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
    [Activity (MainLauncher = true, Theme = "@style/AppTheme.NoActionBar", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    public class MainActivity : AppCompatActivity
    {
        public static readonly string[] RequiredPermissions = new[] {
            Android.Manifest.Permission.Camera,
            Android.Manifest.Permission.Flashlight,
            Android.Manifest.Permission.WriteExternalStorage
        };

        private const float MinScore = 0.6f;
        private const int LabelOffset = 1;

        private string[] labels;

        private DetectionMessage lastDetectionMessage;
        private StatsMessage lastStatsMessage;
        private SKCanvasView canvasView;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(Resource.Layout.activitylayout);

            LoadModelLabels();

            this.Subscribe<StatsMessage>((d) =>
            {
                lastStatsMessage = d;
            });

            this.Subscribe<DetectionMessage>((d) =>
            {
                lastDetectionMessage = d;

                this.canvasView.Invalidate();
            });

            this.canvasView = new SKCanvasView(this);
            var cameraSurface = new CameraSurfaceView(this);

            canvasView.PaintSurface += Canvas_PaintSurface;

            var mainView = this.FindViewById<FrameLayout>(Resource.Id.frameLayout1);
            mainView.AddView(cameraSurface);
            mainView.AddView(canvasView);
        }

        private void LoadModelLabels()
        {
            var labelsFilename = "hardhat_labels_list.txt";
            using (var labelData = Application.Context.Assets.Open(labelsFilename))
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

            DrawingHelper.DrawText(
               canvas,
               5,
               canvasHeight - 50,
               $"TF Model eval: {detection.InferenceElapsedMs} ms");

            var stats = lastStatsMessage;
            if (stats != null)
            {
                DrawingHelper.DrawText(
                    canvas,
                    5,
                    canvasHeight - 5,
                    $"Processing FPS: {stats.Fps} fps ({stats.Ms} ms)");
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
                    ymax,
                    score,
                    labels[labelIndex + LabelOffset]);
            }

            lastDetectionMessage = null;
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            if (PermissionsHandler.NeedsPermissionRequest(this))
                PermissionsHandler.RequestPermissionsAsync(this);
        }

        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        { 
            PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}