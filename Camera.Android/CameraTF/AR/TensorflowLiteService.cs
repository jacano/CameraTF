using CameraTF.AR;
using Emgu.TF.Lite;
using PubSub.Extension;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CameraTF
{
    public unsafe class TensorflowLiteService
    {
        public const int ModelInputSize = 300;
        public const float MinScore = 0.6f;

        private const int LabelOffset = 1;

        private byte[] quantizedColors;
        private string[] labels = null;
        private FlatBufferModel model;

        private bool useNumThreads;

        public bool Initialize(Stream modelData, Stream labelData, bool useNumThreads)
        {
            this.useNumThreads = useNumThreads;

            using (var ms = new MemoryStream())
            {
                labelData.CopyTo(ms);

                var labelContent = Encoding.Default.GetString(ms.ToArray());

                labels = labelContent
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
            }

            using (var ms = new MemoryStream())
            {
                modelData.CopyTo(ms);

                model = new FlatBufferModel(ms.ToArray());
            }

            quantizedColors = new byte[ModelInputSize * ModelInputSize * 3];

            if (!model.CheckModelIdentifier())
            {
                return false;
            }

            return true;
        }

        public bool Recognize(int* colors, int colorsCount)
        {
            using (var op = new BuildinOpResolver())
            {
                using (var interpreter = new Interpreter(model, op))
                {
                    if (useNumThreads)
                    {
                        interpreter.SetNumThreads(Environment.ProcessorCount);
                    }

                    return InvokeInterpreter(interpreter, colors, colorsCount);
                }
            }
        }

        private bool InvokeInterpreter(Interpreter interpreter, int* colors, int colorsCount)
        {
            var stopwatch = new Stopwatch();

            var allocateTensorStatus = interpreter.AllocateTensors();
            if (allocateTensorStatus == Status.Error)
            {
                return false;
            }

            var input = interpreter.GetInput();
            using (var inputTensor = interpreter.GetTensor(input[0]))
            {
                CopyColorsToTensor(inputTensor.DataPointer, colors, colorsCount);

                stopwatch.Start();
                interpreter.Invoke();
                stopwatch.Stop();
            }

            var output = interpreter.GetOutput();
            var outputIndex = output[0];

            var outputTensors = new Tensor[output.Length];
            for (var i = 0; i < output.Length; i++)
            {
                outputTensors[i] = interpreter.GetTensor(outputIndex + i);
            }

            var detection_boxes_out = (float[])outputTensors[0].GetData();
            var detection_classes_out = (float[])outputTensors[1].GetData();
            var detection_scores_out = (float[])outputTensors[2].GetData();
            var num_detections_out = (float[])outputTensors[3].GetData();

            var numDetections = num_detections_out[0];

            LogDetectionResults(detection_classes_out, detection_scores_out, detection_boxes_out, (int)numDetections, stopwatch.ElapsedMilliseconds);

            return true;
        }

        private void CopyColorsToTensor(IntPtr dest, int* colors, int colorsCount)
        {
            for (var i = 0; i < colorsCount; ++i)
            {
                var val = colors[i];

                //// AA RR GG BB
                var r = (byte)((val >> 16) & 0xFF);
                var g = (byte)((val >> 8) & 0xFF);
                var b = (byte)(val & 0xFF);

                quantizedColors[(i * 3) + 0] = r;
                quantizedColors[(i * 3) + 1] = g;
                quantizedColors[(i * 3) + 2] = b;
            }

            System.Runtime.InteropServices.Marshal.Copy(quantizedColors, 0, dest, quantizedColors.Length);
        }

        private void LogDetectionResults(
            float[] detection_classes_out,
            float[] detection_scores_out,
            float[] detection_boxes_out,
            int numDetections,
            long elapsedMilliseconds)
        {
            for (int i = 0; i < numDetections; i++)
            {
                var score = detection_scores_out[i];
                var classId = (int)detection_classes_out[i];

                var labelIndex = classId + LabelOffset;
                if (labelIndex.Between(0, labels.Length - 1))
                {
                    var label = labels[labelIndex];
                    if (score >= MinScore)
                    {
                        var xmin = detection_boxes_out[0];
                        var ymin = detection_boxes_out[1];
                        var xmax = detection_boxes_out[2];
                        var ymax = detection_boxes_out[3];

                        var detectionMessage = new DetectionMessage()
                        {
                            InferenceElapsedMs = elapsedMilliseconds,
                            Label = label,
                            Score = score,
                            Xmin = xmin,
                            Ymin = ymin,
                            Xmax = xmax,
                            Ymax = ymax,
                        };

                        this.Publish(detectionMessage);
                    }
                }
            }
        }
    }
}
