using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using ZXing.Mobile;

namespace Sample.Android
{
	[Activity (Label = "Camera test", MainLauncher = true, Theme="@android:style/Theme.Holo.Light", ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

            var scanIntent = new Intent(this, typeof(ZxingActivity));
            scanIntent.AddFlags(ActivityFlags.NewTask);

            this.StartActivity(scanIntent);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(this))
                ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}


