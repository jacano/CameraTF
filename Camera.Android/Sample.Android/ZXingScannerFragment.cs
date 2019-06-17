using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Sample.Android;
using ZXing.Net.Mobile.Android;

namespace ZXing.Mobile
{
    public class ZXingScannerFragment : Fragment, IScannerView
	{
		FrameLayout frame;

	    public override View OnCreateView (LayoutInflater layoutInflater, ViewGroup viewGroup, Bundle bundle)
		{
			frame = (FrameLayout)layoutInflater.Inflate(Resource.Layout.zxingscannerfragmentlayout, viewGroup, false);

            var layoutParams = getChildLayoutParams();

            try
            {
                scanner = new ZXingSurfaceView (this.Activity);

                frame.AddView(scanner, layoutParams);

            }
            catch (Exception ex)
            {
                Console.WriteLine ("Create Surface View Failed: " + ex);
            }

            Android.Util.Log.Debug (MobileBarcodeScanner.TAG, "ZXingScannerFragment->OnResume exit");

			return frame;
        }

        public override void OnStart()
        {
            base.OnStart();
            // won't be 0 if OnCreateView has been called before.
            if (frame.ChildCount == 0)
            {
                var layoutParams = getChildLayoutParams();
                // reattach scanner and overlay views.
                frame.AddView(scanner, layoutParams);
            }
        }

        public override void OnStop()
        {
            if (scanner != null)
            {
                scanner.StopScanning();

                frame.RemoveView(scanner);
            }

            base.OnStop();
        }

        private LinearLayout.LayoutParams getChildLayoutParams()
        {
            var layoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            layoutParams.Weight = 1;
            return layoutParams;
        }

		ZXingSurfaceView scanner;

		public void Torch(bool on)
		{
			scanner?.Torch(on);
		}
		
        public void AutoFocus()
        {
            scanner?.AutoFocus();
        }

        public void AutoFocus(int x, int y)
		{
			scanner?.AutoFocus(x, y);
		}


        public void StartScanning ()
        {            
            if (scanner == null) {
                return;
            }

            scan ();
        }

        void scan ()
        {
            scanner?.StartScanning ();
        }

        public void StopScanning ()
        {
            scanner?.StopScanning ();
        }

        public void PauseAnalysis ()
        {
            scanner?.PauseAnalysis ();
        }

        public void ResumeAnalysis ()
        {
            scanner?.ResumeAnalysis ();
        }

        public void ToggleTorch ()
        {
            scanner?.ToggleTorch ();
        }

        public bool IsTorchOn {
            get {
                return scanner?.IsTorchOn ?? false;
            }
        }

        public bool IsAnalyzing {
            get {
                return scanner?.IsAnalyzing ?? false;
            }
        }

        public bool HasTorch {
            get {
                return scanner?.HasTorch ?? false;
            }
        }
	}
}

