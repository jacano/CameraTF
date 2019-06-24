namespace CameraTF.Helpers
{
    public static class Stats
    {
        public static float CameraFps { get; set; }
        public static float CameraMs { get; set; }

        public static float ProcessingFps { get; set; }
        public static float ProcessingMs { get; set; }

        public static long YUV2RGBElapsedMs { get; set; }

        public static long ResizeAndRotateElapsedMs { get; set; }

        public static long InterpreterElapsedMs { get; set; }
        public static int NumDetections { get; set; }

        public static float[] Labels { get; set; }
        public static float[] Scores { get; set; }
        public static float[] BoundingBoxes { get; set; }
    }
}