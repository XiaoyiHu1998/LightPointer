using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

public enum HandState
{
    None,
    Palming,
    Pointing,
    DoublePointing,
    GunPointing,
    Pinching,
    Grabbing,
    WideHandling,
    Fisting
}

public enum Finger
{
    Thumb,
    IndexFinger,
    MiddleFinger,
    RingFinger,
    Pinky
}

public enum FingerState
{
    None,
    Pointing,
    Open,
    HalfClosed,
    Clenched
}

public class HandStateAnalyzer : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField]
    public HandDepth HandDepth;

    [Header("Objects")]
    [SerializeField]
    public UnityEngine.UI.Text HandStateText;

    [Header("Finger Joints")]
    public int[] ThumbJoints;
    public int[] IndexFingerJoints;
    public int[] MiddleFingerJoints;
    public int[] RingFingerJoints;
    public int[] PinkyJoints;

    [Header("Hand State")]
    [SerializeField]
    public HandState HandState;
    [SerializeField]
    public HandState[] HandStateHistory;

    [Header("Finger State")]
    [SerializeField]
    public FingerState ThumbState;
    [SerializeField]
    public FingerState IndexFingerState;
    [SerializeField]
    public FingerState MiddleFingerState;
    [SerializeField]
    public FingerState RingFingerState;
    [SerializeField]
    public FingerState PinkyState;

    [Space]
    [SerializeField]
    public float[] JointAngles;

    private HashSet<int> zeroAngleJoints = new HashSet<int>
    {
        0, 4, 8, 12, 16, 20
    };
    private HashSet<int> firstJointsInFingers = new HashSet<int>
    {
        1, 5, 9, 13, 17
    };

    public Dictionary<Finger, int[]> FingerJointDictionary;
    private Tuple<HandState, float> NextHandState;

    // Start is called before the first frame update
    void Start()
    {
        HandState = HandState.None;
        NextHandState = new Tuple<HandState, float>(HandState.None, 0);
        ThumbState = IndexFingerState = MiddleFingerState = RingFingerState = PinkyState = FingerState.None;
        JointAngles = new float[21];

        FingerJointDictionary = new Dictionary<Finger, int[]>()
        {
            {Finger.Thumb, ThumbJoints },
            {Finger.IndexFinger, IndexFingerJoints },
            {Finger.MiddleFinger, MiddleFingerJoints },
            {Finger.RingFinger, RingFingerJoints },
            {Finger.Pinky, PinkyJoints },
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (HandDepth.HandScore < 0.8)
            return;

        UpdateJointAngles();
        UpdateFingerStates();
        UpdateHandState();
        HandStateText.text = HandState.ToString();
    }

    private void UpdateJointAngles()
    {
        for (int i = 0; i < JointAngles.Length; i++)
        {
            JointAngles[i] = CalculateJointAngle(i);
        }
    }

    private float CalculateJointAngle(int jointNumber)
    {
        if (zeroAngleJoints.Contains(jointNumber))
            return 0;

        int nextJointNumber = jointNumber + 1;
        int prevJointNumber = jointNumber - 1;

        if (firstJointsInFingers.Contains(jointNumber))
            prevJointNumber = 0;

        Vector3 jointPosition = HandDepth.JointPositions[jointNumber];
        Vector3 nextJointPosition = HandDepth.JointPositions[nextJointNumber];
        Vector3 prevJointPosition = HandDepth.JointPositions[prevJointNumber];

        float nextJointSide = (float)(nextJointPosition - jointPosition).magnitude;
        float prevJointSide = (float)(prevJointPosition - jointPosition).magnitude;
        float nextJointSideSquared = (float)Math.Pow(nextJointSide, 2);
        float prevJointSideSquared = (float)Math.Pow(prevJointSide, 2);
        float hypotheneuseSquared = (float)Math.Pow((nextJointPosition - prevJointPosition).magnitude, 2);

        float jointAngle = (float)Math.Acos((double)(nextJointSideSquared + prevJointSideSquared - hypotheneuseSquared) / (double)(2 * nextJointSide * prevJointSide));
        jointAngle = jointAngle / (float)Math.PI * 180f;

        return jointAngle;
    }

    private void UpdateHandState()
    {
        HandState newHandState = HandState.None;

        newHandState = IsPalming() ? HandState.Palming : newHandState;
        newHandState = IsPointing() ? HandState.Pointing : newHandState;
        newHandState = IsDoublePointing() ? HandState.DoublePointing : newHandState;
        newHandState = IsGunPointing() ? HandState.GunPointing : newHandState;
        newHandState = IsWideHandling() ? HandState.WideHandling : newHandState;
        newHandState = IsFisting() ? HandState.Fisting : newHandState;
        newHandState = IsPinching() ? HandState.Pinching : newHandState;
        newHandState = IsGrabbing() ? HandState.Grabbing : newHandState;

        if (newHandState != NextHandState.Item1)
            NextHandState = new Tuple<HandState, float>(newHandState, Time.time);

        float timeDelayMilliseconds = HandState == HandState.None ? 0.50f : 0.150f;
        if (Time.time > NextHandState.Item2 + timeDelayMilliseconds && NextHandState.Item1 != HandState)
        {
            HandStateHistory[HandStateHistory.Length - 1] = HandState;
            for (int i = 0; i < HandStateHistory.Length - 1; i++) 
            {
                HandStateHistory[i] = HandStateHistory[i + 1];
            }

            HandState = NextHandState.Item1;
            HandStateHistory[HandStateHistory.Length - 1] = HandState;
        }
    }

    private void UpdateFingerStates()
    {
        ThumbState = GetFingerState(Finger.Thumb);
        IndexFingerState = GetFingerState(Finger.IndexFinger);
        MiddleFingerState = GetFingerState(Finger.MiddleFinger);
        RingFingerState = GetFingerState(Finger.RingFinger);
        PinkyState = GetFingerState(Finger.Pinky);
    }

    private bool IsPalming()
    {
        bool isPalming = true;

        isPalming &= ThumbState == FingerState.Pointing;
        isPalming &= IndexFingerState == FingerState.Pointing;
        isPalming &= MiddleFingerState == FingerState.Pointing;
        isPalming &= IndexFingerState == FingerState.Pointing;
        isPalming &= PinkyState == FingerState.Pointing;

        return isPalming;
    }

    private bool IsPointing()
    {
        bool isPointing = true;

        //isPointing &= ThumbState != FingerState.Pointing;
        isPointing &= IndexFingerState == FingerState.Pointing;
        isPointing &= MiddleFingerState != FingerState.Pointing;
        isPointing &= RingFingerState != FingerState.Pointing;
        isPointing &= PinkyState != FingerState.Pointing;

        return isPointing;
    }

    private bool IsDoublePointing()
    {
        bool isDoublePointing = true;

        isDoublePointing &= ThumbState != FingerState.Pointing;
        isDoublePointing &= IndexFingerState == FingerState.Pointing;
        isDoublePointing &= MiddleFingerState == FingerState.Pointing;
        isDoublePointing &= RingFingerState != FingerState.Pointing;
        isDoublePointing &= PinkyState != FingerState.Pointing;

        return isDoublePointing;
    }

    private bool IsGunPointing()
    {
        bool isGunPointing = true;

        isGunPointing &= ThumbState == FingerState.Pointing;
        isGunPointing &= IndexFingerState == FingerState.Pointing;
        isGunPointing &= MiddleFingerState == FingerState.Pointing;
        isGunPointing &= RingFingerState != FingerState.Pointing;
        isGunPointing &= PinkyState != FingerState.Pointing;

        return isGunPointing;
    }

    private bool IsPinching()
    {
        bool isPinching = true;

        isPinching &= FingerTipsTouching(Finger.Thumb, Finger.IndexFinger, 1.15f);
        isPinching &= !FingerTipsTouching(Finger.Thumb, Finger.MiddleFinger);

        return isPinching;
    }

    private bool IsGrabbing()
    {
        bool isGrabbing = true;

        float allowanceFactor = 1.35f;
        isGrabbing &= FingerTipsTouching(Finger.Thumb, Finger.IndexFinger, allowanceFactor);
        isGrabbing &= FingerTipsTouching(Finger.Thumb, Finger.MiddleFinger, allowanceFactor);
        //isGrabbing &= FingerTipsTouching(Finger.IndexFinger, Finger.MiddleFinger, allowanceFactor);
        isGrabbing &= ThumbState == FingerState.Pointing;

        return isGrabbing;
    }

    private bool IsWideHandling()
    {
        bool isWideHandling = true;

        isWideHandling &= ThumbState == FingerState.Pointing;
        isWideHandling &= IndexFingerState != FingerState.Pointing;
        isWideHandling &= MiddleFingerState != FingerState.Pointing;
        isWideHandling &= RingFingerState != FingerState.Pointing;
        isWideHandling &= PinkyState == FingerState.Pointing;
        isWideHandling &= !FingerTipsTouching(Finger.Thumb, Finger.IndexFinger);

        return isWideHandling;
    }

    private bool IsFisting()
    {
        bool isFisting = true;

        isFisting &= IndexFingerState == FingerState.Clenched;
        isFisting &= MiddleFingerState == FingerState.Clenched;
        isFisting &= RingFingerState == FingerState.Clenched;
        isFisting &= PinkyState == FingerState.Clenched || PinkyState == FingerState.HalfClosed;

        return isFisting;
    }

    private FingerState GetFingerState(Finger finger)
    {
        if(JointAngles.Length == 0)
            return FingerState.None;

        int[] fingerJoints = FingerJointDictionary[finger];

        if (FingerPointing(fingerJoints))
            return FingerState.Pointing;

        if (FingerOpen(fingerJoints))
            return FingerState.Open;

        if (FingerHalfClosed(fingerJoints))
            return FingerState.HalfClosed;

        if (FingerClenched(fingerJoints))
            return FingerState.Clenched;

        return FingerState.None;
    }

    private bool FingerPointing(int[] fingerJoints)
    {
        bool isPointing = true;
        isPointing &= JointAngles[fingerJoints[0]] > 120;
        isPointing &= JointAngles[fingerJoints[1]] > 148;
        isPointing &= JointAngles[fingerJoints[2]] > 148;

        //float angleSum = JointAngles[fingerJoints[0]] + JointAngles[fingerJoints[1]] + JointAngles[fingerJoints[2]];
        //isPointing &= angleSum > 170 * 2 + 120;

        return isPointing;
    }

    private bool FingerOpen(int[] fingerJoints)
    {
        bool isPointing = true;
        isPointing &= JointAngles[fingerJoints[0]] > 90;
        isPointing &= JointAngles[fingerJoints[1]] > 120;
        isPointing &= JointAngles[fingerJoints[2]] > 120;

        return isPointing;
    }

    private bool FingerHalfClosed(int[] fingerJoints)
    {
        bool isPointing = true;
        isPointing &= JointAngles[fingerJoints[0]] > 90;
        isPointing &= JointAngles[fingerJoints[1]] > 95;
        isPointing &= JointAngles[fingerJoints[2]] > 90;

        return isPointing;
    }

    private bool FingerClenched(int[] fingerJoints)
    {
        bool isPointing = true;
        //isPointing &= JointAngles[fingerJoints[0]] < 170;
        isPointing &= JointAngles[fingerJoints[1]] < 98;
        isPointing &= JointAngles[fingerJoints[2]] < 165;

        return isPointing;
    }

    private bool FingerTipsTouching(Finger finger1, Finger finger2, float allowanceFactor = 1.25f)
    {
        int finger1Joint = FingerJointDictionary[finger1][2];
        int finger1Tip = FingerJointDictionary[finger1][3];
        int finger2Tip = FingerJointDictionary[finger2][3];

        Vector3 finger1JointPosition = HandDepth.JointPositions[finger1Joint];
        Vector3 finger1TipPosition = HandDepth.JointPositions[finger1Tip];
        Vector3 finger2TipPosition = HandDepth.JointPositions[finger2Tip];

        float FingertipsDistance = (finger1TipPosition - finger2TipPosition).magnitude;
        float Finger1TipToJointDistance = (finger1TipPosition - finger1JointPosition).magnitude;

        return FingertipsDistance <= Finger1TipToJointDistance * allowanceFactor;
    }
}
