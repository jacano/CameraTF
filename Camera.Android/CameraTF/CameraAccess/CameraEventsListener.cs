using System;
using Android.Hardware;
using ApxLabs.FastAndroidCamera;

namespace CameraTF.CameraAccess
{
    public class CameraEventsListener : Java.Lang.Object, INonMarshalingPreviewCallback
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
    }
}