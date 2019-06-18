using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Support.V7.App;
using SkiaSharp.Views.Android;
using SkiaSharp;
using CameraTF.AR;
using PubSub.Extension;
using CameraTF.Helpers;

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

        private DetectionMessage lastDetectionMessage;
        private SKCanvasView canvasView;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn); 

            SetContentView(Resource.Layout.activitylayout);

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

        private void Canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            var canvasWidth = e.Info.Width;
            var canvasHeight = e.Info.Height;

            var d = lastDetectionMessage;

            if (d == null) return;

            canvas.Clear();

            DrawingHelper.DrawBoundingBox(
                canvas,
                canvasWidth,
                canvasHeight,
                d.Xmin,
                d.Ymin,
                d.Xmax,
                d.Ymax);

            DrawingHelper.DrawStats(
                canvas,
                canvasWidth,
                canvasHeight,
                d.InferenceElapsedMs,
                d.Score,
                d.Label);

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