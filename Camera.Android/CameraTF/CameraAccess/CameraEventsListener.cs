using System;
using Android.Hardware;
using ApxLabs.FastAndroidCamera;

namespace CameraTF.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback, Camera.IAutoFocusCallback
    {
        public event EventHandler<FastJavaByteArray> OnPreviewFrameReady; 

        public void OnPreviewFrame(IntPtr data, Camera camera)
        {
            using (var fastArray = new FastJavaByteArray(data))
            {
                OnPreviewFrameReady?.Invoke(this, fastArray);

                camera.AddCallbackBuffer(fastArray);
            }
        }

        public void OnAutoFocus(bool success, Camera camera)
        {
            System.Diagnostics.Debug.WriteLine("AutoFocus {0}", success ? "Succeeded" : "Failed");
        }
    }
}