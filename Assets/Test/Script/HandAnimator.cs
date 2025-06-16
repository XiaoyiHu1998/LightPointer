using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;
using System;
using System.Collections.Generic;

public sealed class HandAnimator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ImageSource _source = null;
    [Space]
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] bool _useAsyncReadback = true;
    [Space]
    [SerializeField] Mesh _jointMesh = null;
    [SerializeField] Mesh _boneMesh = null;
    [Space]
    [SerializeField] Material _jointMaterial = null;
    [SerializeField] Material _boneMaterial = null;
    [Space]
    [SerializeField] RawImage _monitorUI = null;
    [Space]
    [SerializeField] public RawImage _colorRawImage = null;
    [SerializeField] public RawImage _depthRawImage = null;
    [SerializeField] public RawImage _monitorRawImage = null;
    [Space]
    [SerializeField] public HandDepth handDepth = null;

    #endregion

    #region Private members

    HandPipeline _pipeline;

    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 17), (2, 5), (5, 9), (9, 13), (13, 17)  // Palm
    };

    Matrix4x4 CalculateJointXform(Vector3 pos)
      => Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.022f);

    Matrix4x4 CalculateBoneXform(Vector3 p1, Vector3 p2)
    {
        var length = Vector3.Distance(p1, p2) / 2;
        var radius = 0.03f;

        var center = (p1 + p2) / 2;
        var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
        var scale = new Vector3(radius, length, radius) / 1.5f;

        return Matrix4x4.TRS(center, rotation, scale);
    }

    #endregion

    #region MonoBehaviour implementation

    //void Start()
    //  => _pipeline = new HandPipeline(_resources);

    private void Start()
    {
        _pipeline = new HandPipeline(_resources);
        _pipeline.handDepth = this.handDepth;
        jointPositions = new Vector3[HandPipeline.KeyPointCount];
        boneMatrices = new Dictionary<(int, int), Matrix4x4>();
    }

    void OnDestroy()
      => _pipeline.Dispose();


    private Vector3[] jointPositions;
    private Dictionary<(int, int), Matrix4x4> boneMatrices;
    void LateUpdate()
    {

        // Feed the input image to the Hand pose pipeline.
        _pipeline.UseAsyncReadback = _useAsyncReadback;
        _pipeline.ProcessImage(_colorRawImage.texture);

        var layer = gameObject.layer;

        if(_pipeline.LandmarkScore > 0.8)
        {
            
            // Joint balls
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                var xform = CalculateJointXform(_pipeline.GetKeyPoint(i));
                jointPositions[i] = _pipeline.GetKeyPoint(i);
                //Graphics.DrawMesh(_jointMesh, xform, _jointMaterial, layer);
            }

            handDepth.JointPositions = jointPositions;
            handDepth.WristLocation = _pipeline.GetKeyPoint(0);

            // Bones
            //foreach (var pair in BonePairs)
            //{
            //    var p1 = _pipeline.GetKeyPoint(pair.Item1);
            //    var p2 = _pipeline.GetKeyPoint(pair.Item2);
            //    var xform = CalculateBoneXform(p1, p2);
            //    boneMatrices[pair] = xform;
            //    //Graphics.DrawMesh(_boneMesh, xform, _boneMaterial, layer);
            //}
        }

        // UI update
         _monitorUI.texture = _source.Texture;
        _monitorRawImage.texture = _colorRawImage.texture;
    }

    #endregion
}
