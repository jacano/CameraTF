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

        public static void DrawBoundingBox(
            SKCanvas canvas,
            float width,
            float height,
            float xmin,
            float ymin,
            float xmax,
            float ymax,
            float score,
            string label)
        {
            var top = xmin * height;
            var left = ymin * width;
            var bottom = xmax * height;
            var right = ymax * width;

            var rect = new SKRect(left, top, right, bottom);

            canvas.DrawRoundRect(rect, boundingBoxCornerRadius, boundingBoxPaint);

            canvas.DrawText(
                $"{label} - {score}",
                left,
                bottom,
                statsPaint);
        }

        public static void DrawText(
           SKCanvas canvas,
           float x,
           float y,
           string text)
        {
            canvas.DrawText(
                text,
                x,
                y,
                statsPaint);
        }
    }
}