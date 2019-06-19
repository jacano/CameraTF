using CameraTF.AR;
using Emgu.TF.Lite;
using PubSub.Extension;
using System;
using System.Diagnostics;
using System.IO;

namespace CameraTF
{
    public unsafe class TensorflowLiteService
    {
        public const int ModelInputSize = 300;

        private byte[] quantizedColors;
        private FlatBufferModel model;

        private Interpreter interpreter;

        private Tensor inputTensor;
        private Tensor[] outputTensors;

        public bool Initialize(Stream modelData, bool useNumThreads)
        {
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

            var op = new BuildinOpResolver();
            interpreter = new Interpreter(model, op);

            if (useNumThreads)
            {
                interpreter.SetNumThreads(Environment.ProcessorCount);
            }

            var allocateTensorStatus = interpreter.AllocateTensors();
            if (allocateTensorStatus == Status.Error)
            {
                return false;
            }

            var input = interpreter.GetInput();
            inputTensor = interpreter.GetTensor(input[0]);

            var output = interpreter.GetOutput();
            var outputIndex = output[0];

            outputTensors = new Tensor[output.Length];
            for (var i = 0; i < output.Length; i++)
            {
                outputTensors[i] = interpreter.GetTensor(outputIndex + i);
            }

            return true;
        }

        public bool Recognize(int* colors, int colorsCount)
        {
            CopyColorsToTensor(inputTensor.DataPointer, colors, colorsCount);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            interpreter.Invoke();
            stopwatch.Stop();

            var detection_boxes_out = (float[])outputTensors[0].GetData();
            var detection_classes_out = (float[])outputTensors[1].GetData();
            var detection_scores_out = (float[])outputTensors[2].GetData();
            var num_detections_out = (float[])outputTensors[3].GetData();

            var numDetections = (int)num_detections_out[0];

            var detectionMessage = new DetectionMessage()
            {
                InterpreterElapsedMs = stopwatch.ElapsedMilliseconds,
                NumDetections = numDetections,
                Labels = detection_classes_out,
                Scores = detection_scores_out,
                BoundingBoxes = detection_boxes_out,
            };

            this.Publish(detectionMessage);

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
    }
}
