using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using CameraTF.CameraAccess;

namespace CameraTF
{
    public class CameraSurfaceView : SurfaceView, ISurfaceHolderCallback
    {
        private bool addedHolderCallback = false;
        private bool surfaceCreated;
        private CameraAnalyzer cameraAnalyzer;

        public CameraSurfaceView(Context context)
            : base(context)
        {
            Init();
        }

        protected CameraSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Init();
        }

        private void Init()
        {
			if (cameraAnalyzer == null)
	            cameraAnalyzer = new CameraAnalyzer(this);

			if (!addedHolderCallback) {
				Holder.AddCallback(this);
				Holder.SetType(SurfaceType.PushBuffers);
				addedHolderCallback = true;
			}
        }

        public async void SurfaceCreated(ISurfaceHolder holder)
        {
            await PermissionsHandler.PermissionRequestTask;

            cameraAnalyzer.SetupCamera();

            surfaceCreated = true;
        }

        public async void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
        {
            await PermissionsHandler.PermissionRequestTask;

            cameraAnalyzer.RefreshCamera();
        }

        public async void SurfaceDestroyed(ISurfaceHolder holder)
        {
            await PermissionsHandler.PermissionRequestTask;

            try
            {
                if (addedHolderCallback)
                {
                    Holder.RemoveCallback(this);
                    addedHolderCallback = false;
                }
            }
            catch { }

            cameraAnalyzer.ShutdownCamera();
        }

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);
			if (visibility == ViewStates.Visible)
				Init();
		}

        public override async void OnWindowFocusChanged(bool hasWindowFocus)
        {
            base.OnWindowFocusChanged(hasWindowFocus);

            if (!hasWindowFocus) return;
            // SurfaceCreated/SurfaceChanged are not called on a resume
            await PermissionsHandler.PermissionRequestTask;

            //only refresh the camera if the surface has already been created. Fixed #569
            if (surfaceCreated)
            {
                cameraAnalyzer.RefreshCamera();
            }
        }
    }
}
