using SkiaSharp;

namespace CameraTF.Helpers
{
    public static class DrawingHelper
    {
        private static readonly SKPaint boundingBoxPaint = new SKPaint
        {
            StrokeWidth = 5,
            Color = SKColors.Red,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        private static readonly SKSize boundingBoxCornerRadius = new SKSize(23, 23);

        private static readonly SKPaint statsPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextSize = 40,
        };

        private static readonly float statsMargin = 5;

        private static float statsHeight;

        static DrawingHelper()
        {
            var textBounds = new SKRect();
            statsPaint.MeasureText("123", ref textBounds);
            statsHeight = textBounds.Height;
        }

        public static void DrawBoundingBox(
            SKCanvas canvas,
            float width,
            float height,
            float xmin,
            float ymin,
            float xmax,
            float ymax)
        {
            var top = xmin * height;
            var left = ymin * width;
            var bottom = xmax * height;
            var right = ymax * width;

            var rect = new SKRect(left, top, right, bottom);

            canvas.DrawRoundRect(rect, boundingBoxCornerRadius, boundingBoxPaint);
        }

        public static void DrawStats(
            SKCanvas canvas,
            float width,
            float height,
            long elapsed,
            float score,
            string label)
        {
            canvas.DrawText(
                $"{elapsed} ms - {label} - {score}",
                statsMargin,
                height - (statsHeight / 2) - statsMargin,
                statsPaint);
        }
    }
}