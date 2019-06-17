﻿using System;

namespace ZXing.Mobile
{
    public interface IScannerView
    {        
        void StartScanning ();
        void StopScanning ();

        void PauseAnalysis();
        void ResumeAnalysis();

        void Torch(bool on);
        void AutoFocus();
        void AutoFocus(int x, int y);
        void ToggleTorch();
        bool IsTorchOn { get; }
        bool IsAnalyzing { get; }

        bool HasTorch { get; }
    }
}
