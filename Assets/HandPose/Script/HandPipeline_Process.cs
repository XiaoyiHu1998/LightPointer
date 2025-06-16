using System;
using UnityEngine;

namespace MediaPipe.HandPose {

//
// Image processing part of the hand pipeline class
//

    partial class HandPipeline
    {
        public HandDepth handDepth;
        void RunPipeline(Texture input)
        {
            var cs = _resources.compute;

            // Letterboxing scale factor
            var scale = new Vector2
              (Mathf.Max((float)input.height / input.width, 1),
               Mathf.Max(1, (float)input.width / input.height));

            // Image scaling and padding
            cs.SetInt("_spad_width", InputWidth);
            cs.SetVector("_spad_scale", scale);
            cs.SetTexture(0, "_spad_input", input);
            cs.SetBuffer(0, "_spad_output", _detector.palm.InputBuffer);
            cs.Dispatch(0, InputWidth / 8, InputWidth / 8, 1);

            // Palm detection
            _detector.palm.ProcessInput();

            // Hand region bounding box update
            cs.SetFloat("_bbox_dt", Time.deltaTime);
            cs.SetBuffer(1, "_bbox_count", _detector.palm.CountBuffer);
            cs.SetBuffer(1, "_bbox_palm", _detector.palm.DetectionBuffer);
            cs.SetBuffer(1, "_bbox_region", _buffer.region);
            cs.Dispatch(1, 1, 1, 1);

            if (handDepth != null)
            {
                float[] array = new float[24];
                _buffer.region.GetData(array);
                handDepth.HandRegionCenter.x = array[0];
                handDepth.HandRegionCenter.y = array[1];
                handDepth.RegionSize = array[2];
                handDepth.RegionAngle = array[3];
            }

            // Hand region cropping
            cs.SetTexture(2, "_crop_input", input);
            cs.SetBuffer(2, "_crop_region", _buffer.region);
            cs.SetBuffer(2, "_crop_output", _detector.landmark.InputBuffer);
            cs.Dispatch(2, CropSize / 8, CropSize / 8, 1);

            // Hand landmark detection
            _detector.landmark.ProcessInput();
            handDepth.HandScore = _detector.landmark.Score;
            handDepth.Handedness = _detector.landmark.Handedness;
            //handDepth.LandmarkBuffer = _detector.landmark.OutputBuffer;
            //handDepth.WristLocation = _detector.landmark.GetKeyPoint(HandLandmark.HandLandmarkDetector.KeyPoint.Wrist);

            // Key point postprocess
            cs.SetFloat("_post_dt", Time.deltaTime);
            cs.SetFloat("_post_scale", scale.y);
            cs.SetBuffer(3, "_post_input", _detector.landmark.OutputBuffer);
            cs.SetBuffer(3, "_post_region", _buffer.region);
            cs.SetBuffer(3, "_post_output", _buffer.filter);
            cs.Dispatch(3, 1, 1, 1);

            // Read cache invalidation
            InvalidateReadCache();
        }
    }

} // namespace MediaPipe.HandPose
