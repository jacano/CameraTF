using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Sample.Android;
using TailwindTraders.Mobile.Features.Scanning;
using Android.Widget;

namespace ZXing.Mobile
{
    [Activity (Label = "Scanner", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    public class ZxingActivity : FragmentActivity
    {
        public static readonly string[] RequiredPermissions = new[] {
            Android.Manifest.Permission.Camera,
            Android.Manifest.Permission.Flashlight,
            Android.Manifest.Permission.WriteExternalStorage
        };

        public static TensorflowLiteService tfService;

        private ZXingSurfaceView scanner;

        private FrameLayout frameLayout1;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

            SetContentView(Resource.Layout.zxingscanneractivitylayout);

            frameLayout1 = this.FindViewById<FrameLayout>(Resource.Id.frameLayout1);

            InitTensorflowLineService();

            scanner = new ZXingSurfaceView(this);
            frameLayout1.AddView(scanner);
        }

        private void InitTensorflowLineService()
        {
            var model = "hardhat_detect.tflite";
            var labels = "hardhat_labels_list.txt";

            using (var modelData = Assets.Open(model))
            {
                using (var labelData = Assets.Open(labels))
                {
                    tfService = new TensorflowLiteService();
                    tfService.Initialize(modelData, labelData, useNumThreads: true);
                }
            }
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            if (ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(this))
                ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(this);
            else
                scanner.StartScanning();
        }

        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        { 
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override bool OnKeyDown (Keycode keyCode, KeyEvent e)
        {
            switch (keyCode) {
                case Keycode.Focus:
                    return true;
            }

            return base.OnKeyDown (keyCode, e);
        }
    }

}