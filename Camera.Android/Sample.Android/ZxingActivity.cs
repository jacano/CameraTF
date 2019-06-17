using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Sample.Android;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile
{
    [Activity (Label = "Scanner", MainLauncher = true, Theme = "@android:style/Theme.Holo.Light", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
    public class ZxingActivity : FragmentActivity
    {
        public static readonly string[] RequiredPermissions = new[] {
            Android.Manifest.Permission.Camera,
            Android.Manifest.Permission.Flashlight
        };

        ZXingScannerFragment scannerFragment;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            this.RequestWindowFeature (WindowFeatures.NoTitle);

            this.Window.AddFlags (WindowManagerFlags.Fullscreen); //to show
            this.Window.AddFlags (WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

            //if (ScanningOptions.AutoRotate.HasValue && !ScanningOptions.AutoRotate.Value)
            //    RequestedOrientation = ScreenOrientation.Nosensor;

            //TODO

            SetContentView (Resource.Layout.zxingscanneractivitylayout);

            scannerFragment = new ZXingScannerFragment ();

            SupportFragmentManager.BeginTransaction ()
				.Replace (Resource.Id.contentFrame, scannerFragment, "ZXINGFRAGMENT")
				.Commit ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();

            if (ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(this))
                ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(this);
            else
                scannerFragment.StartScanning();
        }

        public override void OnRequestPermissionsResult (int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        { 
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged (newConfig);

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "Configuration Changed");
        }

        public void SetTorch (bool on)
        {
            scannerFragment.Torch (on);
        }

        public void AutoFocus ()
        {
            scannerFragment.AutoFocus ();
        }

        public void CancelScan ()
        {
            Finish ();
        }

        public override bool OnKeyDown (Keycode keyCode, KeyEvent e)
        {
            switch (keyCode) {
            case Keycode.Back:
                CancelScan ();
                break;
            case Keycode.Focus:
                return true;
            }

            return base.OnKeyDown (keyCode, e);
        }
    }

}