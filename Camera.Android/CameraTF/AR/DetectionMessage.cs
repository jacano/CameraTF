namespace CameraTF.AR
{
    public class DetectionMessage
    {
        public string Label { get; set; }
        public long InferenceElapsedMs { get; set; }
        public float Score { get; set; }

        public float Xmin { get; set; }
        public float Ymin { get; set; }
        public float Xmax { get; set; }
        public float Ymax { get; set; }
    }
}