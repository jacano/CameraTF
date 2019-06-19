using System;
using System.Diagnostics;

namespace CameraTF.Helpers
{
    public class FPSCounter
    {
        private Action<string> callback;
        private Stopwatch fpsTimer;
        private int fpsCounter;

        public FPSCounter(Action<string> callback = null)
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

                var fpsString = $"{fps} fps ({ms} ms)";

                callback?.Invoke(fpsString);

                this.fpsTimer.Restart();
                this.fpsCounter = 0;
            }
        }
    }
}