using System;
using System.Diagnostics;

namespace CameraTF.Helpers
{
    public class FPSCounter
    {
        private Action<(float fps, float ms)> callback;
        private Stopwatch fpsTimer;
        private int fpsCounter;

        public FPSCounter(Action<(float fps, float ms)> callback = null)
        {
            this.callback = callback;

            fpsTimer = Stopwatch.StartNew();
        }

        public void Report()
        {
            this.fpsCounter++;
            if (this.fpsTimer.ElapsedMilliseconds > 1000)
            {
                var fps = 1000.0f * this.fpsCounter / this.fpsTimer.ElapsedMilliseconds;
                var ms = (float)this.fpsTimer.ElapsedMilliseconds / this.fpsCounter;

                callback?.Invoke((fps, ms));

                this.fpsTimer.Restart();
                this.fpsCounter = 0;
            }
        }
    }
}