using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
namespace Sample.Android.Helpers
{
    public static unsafe class YuvHelper
    {
        [DllImport("libyuv")]
        public static extern void ConvertYUV420SPToARGB8888(byte* pY, byte* pUV, int* output, int width, int height);
    }
}