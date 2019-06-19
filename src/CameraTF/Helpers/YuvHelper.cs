using System.Runtime.InteropServices;

namespace CameraTF
{
    public static unsafe class YuvHelper
    {
        [DllImport("libyuv")]
        public static extern void ConvertYUV420SPToARGB8888(byte* pY, byte* pUV, int* output, int width, int height);
    }
}