using MediaPipe.BlazePalm;
using MediaPipe.HandLandmark;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum Landmarks
{
    Wrist,
    Thumb1, Thumb2, Thumb3, Thumb4,
    Index1, Index2, Index3, Index4,
    Middle1, Middle2, Middle3, Middle4,
    Ring1, Ring2, Ring3, Ring4,
    Pinky1, Pinky2, Pinky3, Pinky4
}


public class HandDepth : MonoBehaviour
{
    [Header("Other Objects")]
    [SerializeField]
    public Camera Camera;
    [SerializeField]
    public Mesh JointMesh = null;
    [SerializeField]
    public Material JointMaterial = null;
    [SerializeField]
    public Mesh BoneMesh = null;
    [SerializeField]
    public Material BoneMaterial = null;

    [Header("Hand Data")]
    [SerializeField]
    public float HandScore;
    [SerializeField]
    public float Handedness;
    [SerializeField]
    public Vector2 HandSize;
    [SerializeField]
    public Vector3 WristLocation;
    [SerializeField]
    public Vector2 HandRegionCenter;
    [SerializeField]
    public float RegionSize;
    [SerializeField]
    public float RegionAngle;
    [SerializeField]
    public Int16 DepthValue;
    public float DepthDistance;
    public int DepthSearchDistance = 25;

    [Space]
    [Header("Color Image Data")]
    [SerializeField]
    public RawImage ColorRawImage;
    [SerializeField]
    public Vector2Int ColorResolution;

    [Space]
    [Header("Depth Image Data")]
    [SerializeField]
    public RawImage DepthRawImage;
    [SerializeField]
    public Vector2Int DepthResolution;
    [SerializeField]
    public float DepthScale;

    [Space]
    [Header("Depth Overlay Image")]
    [SerializeField]
    public RawImage OverlayRawImage;
    [SerializeField]
    public Vector2Int OverlayResolution;
    [SerializeField]
    public Color OverlayColor;
    public Color OverlayColorNoDepth;

    public Texture2D OverlayTexture;
    private Color[] OverlayColorArray;

    [Space]
    [Header("Hand Data")]
    public Vector3[] JointPositions;

    private Vector2Int SampleCoordinate;
    private List<Tuple<Vector2Int, float>> minDepths;
    private Tuple<Vector2Int, float> minDepthFound;
    public GraphicsBuffer LandmarkBuffer;

    private bool HandDetected;

    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 17), (2, 5), (5, 9), (9, 13), (13, 17)  // Palm
    };

    private void Start()
    {
        WristLocation = new Vector2();
        HandRegionCenter = new Vector2();
        OverlayTexture = new Texture2D(DepthResolution.x, DepthResolution.y, TextureFormat.RGBA32, false);
        OverlayColorArray = new Color[DepthResolution.x * DepthResolution.y];
        minDepthFound = new Tuple<Vector2Int, float>(new Vector2Int(-1, -1), float.MaxValue);
        minDepths = new List<Tuple<Vector2Int, float>>();

        for(int i = 0; i < DepthResolution.y; i++)
        {
            minDepths.Add(new Tuple<Vector2Int, float>(new Vector2Int(0, i), -1f));
        }
    }

    void Update()
    {
        HandDetected = HandScore > 0.8;
        SampleCoordinate = WristLocationToDepthImageCoordinate();
        GetHandDepth(SampleCoordinate);
        UpdateOverlayTexture(SampleCoordinate);
        DrawHand();
        UpdateHandSize();
    }

    private void UpdateHandSize()
    {
        if (JointPositions.Length == 0)
        {
            HandSize.x = 0;
            HandSize.y = 0;
            return;
        }

        HandSize.x = (JointPositions[17] - JointPositions[5]).magnitude;
        HandSize.y = (JointPositions[17] - JointPositions[5]).magnitude * 2;
    }

    private void DrawLandmarks()
    {
        Vector3 cameraToPalmVector = JointPositions[0] - Camera.transform.position;
        cameraToPalmVector.Normalize();
        cameraToPalmVector *= DepthDistance;

        //Vector3 cameraToPalmVector = Vector3.zero;
        for (int i = 0; i < JointPositions.Length ; i++)
        {
            JointPositions[i] += cameraToPalmVector;
            Matrix4x4 matrix = Matrix4x4.TRS(JointPositions[i], Quaternion.identity, Vector3.one * 0.022f);
            Graphics.DrawMesh(JointMesh, matrix, JointMaterial, gameObject.layer);
        }
    }

    private Vector2Int UVToDepthImageCoordinate(Vector2 UV)
    {
        return new Vector2Int((int)(UV.x * DepthResolution.x), (int)(UV.y * DepthResolution.y * (1.20 * UV.y + 0.35)));
    }

    private Vector2Int WristLocationToDepthImageCoordinate()
    {
        //Vector2 UVW = new Vector2(WristLocation.x * 0.8f + 0.5f, WristLocation.y + 0.5f); // Align to Depth
        
        // Align to Color
        float u = WristLocation.x * 0.5f + 0.5f;
        float v = WristLocation.y + 0.5f;
        return new Vector2Int((int)(u * DepthResolution.x), (int)(v * DepthResolution.y));
    }

    public Vector2 GetWristLocationUV()
    {
        Vector2 wristLocationXY = (Vector2)WristLocationToDepthImageCoordinate();
        wristLocationXY.x /= DepthResolution.x;
        wristLocationXY.y /= DepthResolution.y;

        return wristLocationXY;
    }

    private Color GetHandDepth(Vector2Int sampleCoordinate)
    {
        Color depthColor;
        if (!HandDetected)
        {
            depthColor = Color.black;
            DepthValue = -1;
            DepthDistance = -1;
            return depthColor;
        }

        Texture2D texture = (Texture2D)DepthRawImage.texture;
        Color[] textureArray = texture.GetPixels();
        Parallel.For(0, minDepths.Count, (currentY) =>
        {
            if (Math.Abs(sampleCoordinate.y - currentY) > DepthSearchDistance)
                minDepths[currentY] = new Tuple<Vector2Int, float>(new Vector2Int(0, currentY), float.MaxValue);

            int startX = Math.Max(sampleCoordinate.x - DepthSearchDistance, 0);
            int endX = Math.Min(sampleCoordinate.x + DepthSearchDistance, DepthResolution.x - 1);
            Tuple<Vector2Int, float> minDepthValue = new Tuple<Vector2Int, float>(new Vector2Int(-1, -1), float.MaxValue);

            for (int x = startX; x < endX; x++)
            {
                float depthValue = textureArray[x + currentY * DepthResolution.x].r * Int16.MaxValue;
                if (depthValue > 0 && depthValue < minDepthValue.Item2)
                {
                    minDepthValue = new Tuple<Vector2Int, float>(new Vector2Int(x, currentY), depthValue);
                }
            }

            minDepths[currentY] = minDepthValue;
        });

        int startY = Math.Max(sampleCoordinate.y - DepthSearchDistance, 0);
        int endY = Math.Min(sampleCoordinate.y + DepthSearchDistance, DepthResolution.y - 1);
        minDepthFound = new Tuple<Vector2Int, float>(new Vector2Int(-1, -1), float.MaxValue);
        for (int currentY = startY; currentY < endY + 1; currentY++)
        {
            Tuple<Vector2Int, float> minDepthPixel = minDepths[currentY];
            if (0 < minDepthPixel.Item2 && minDepthPixel.Item2 < minDepthFound.Item2)
                minDepthFound = minDepthPixel;
        }

        // R16 depth data
        depthColor = texture.GetPixel(minDepthFound.Item1.x, minDepthFound.Item1.y);
        DepthValue = (Int16)(minDepthFound.Item2);
        DepthDistance = DepthValue * DepthScale;

        return depthColor;
    }

    private void UpdateOverlayTexture(Vector2Int targetLocation)
    {
        Parallel.For(0, OverlayColorArray.Length, (i) =>
        {
            int currentX = i % DepthResolution.x;
            int currentY = i / DepthResolution.x;

            int targetX = targetLocation.x;
            int targetY = targetLocation.y;

            bool visible = Math.Abs(targetX - currentX) <= 1 && Math.Abs(targetY - currentY) <= 1;
            visible |= Math.Abs(minDepthFound.Item1.x - currentX) <= 1 && Math.Abs(minDepthFound.Item1.y - currentY) <= 1;
            visible |= Math.Abs(targetX - currentX) == DepthSearchDistance && Math.Abs(targetY - currentY) <= DepthSearchDistance || Math.Abs(targetY - currentY) == DepthSearchDistance && Math.Abs(targetX - currentX) <= DepthSearchDistance;

            Color drawColor = DepthDistance > 0 ? OverlayColor : OverlayColorNoDepth;
            OverlayColorArray[i] = (visible && HandDetected) ? drawColor : Color.clear;
        });

        OverlayTexture.SetPixels(OverlayColorArray);
        OverlayTexture.Apply();
        OverlayRawImage.texture = OverlayTexture;
    }

    private void DrawBones()
    {
        Func<Vector3, Vector3, Matrix4x4> CalculateBoneMatrix = (Vector3 p1, Vector3 p2) =>
        {
            var length = Vector3.Distance(p1, p2) / 2;
            var radius = length / 2;

            var center = (p1 + p2) / 2;
            var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
            var scale = new Vector3(radius, length, radius) / 1.5f;

            return Matrix4x4.TRS(center, rotation, scale);
        };

        for (int i = 0; i < BonePairs.Length; i++)
        {
            Matrix4x4 matrix = CalculateBoneMatrix(JointPositions[BonePairs[i].Item1], JointPositions[BonePairs[i].Item2]);
            Graphics.DrawMesh(BoneMesh, matrix, BoneMaterial, gameObject.layer);
        }
    }

    private void DrawHand()
    {
        if (!HandDetected)
            return;

        DrawLandmarks();
        DrawBones();
    }
}
