namespace ZXing.Mobile
{
    public interface IScannerView
    {        
        void StartScanning ();
        void StopScanning ();

        void PauseAnalysis();
        void ResumeAnalysis();

        void AutoFocus();
        void AutoFocus(int x, int y);
        bool IsAnalyzing { get; }
    }
}

