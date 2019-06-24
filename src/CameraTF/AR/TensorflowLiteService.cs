using CameraTF.Helpers;
using Emgu.TF.Lite;
using System;
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

        public void Recognize(int* colors, int colorsCount)
        {
            CopyColorsToTensor(inputTensor.DataPointer, colors, colorsCount);

            interpreter.Invoke();

            var detectionBoxes = (float[])outputTensors[0].GetData();
            var detectionClasses = (float[])outputTensors[1].GetData();
            var detectionScores = (float[])outputTensors[2].GetData();
            var detectionNumDetections = (float[])outputTensors[3].GetData();

            var numDetections = (int)detectionNumDetections[0];

            Stats.NumDetections = numDetections;
            Stats.Labels = detectionClasses;
            Stats.Scores = detectionScores;
            Stats.BoundingBoxes = detectionBoxes;
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
