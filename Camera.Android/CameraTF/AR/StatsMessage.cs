namespace CameraTF.AR
{
    public class CameraStatsMessage : StatsMessage { }

    public class ProcessingStatsMessage : StatsMessage { }

    public class StatsMessage
    {
        public float Fps { get; set; }
        public float Ms { get; set; }
    }
}