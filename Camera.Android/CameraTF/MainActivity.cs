using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Android.Support.V7.App;

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

        public static TensorflowLiteService tfService;

        private CameraSurfaceView scanner;

        private FrameLayout frameLayout1;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate(bundle);

            this.RequestWindowFeature(WindowFeatures.NoTitle);

            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            this.Window.AddFlags(WindowManagerFlags.KeepScreenOn); 

            SetContentView(Resource.Layout.activitylayout);

            frameLayout1 = this.FindViewById<FrameLayout>(Resource.Id.frameLayout1);

            InitTensorflowLineService();

            scanner = new CameraSurfaceView(this);
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

            if (PermissionsHandler.NeedsPermissionRequest(this))
                PermissionsHandler.RequestPermissionsAsync(this);
        }

        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        { 
            PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

}