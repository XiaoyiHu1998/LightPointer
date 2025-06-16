using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ControlState
{
    LightControl,
    Placement,
    //Hue,
    //Saturation,
    //Value
}

public enum ControlParameter
{
    Hue,
    Saturation,
    Value
}

[Serializable]
public struct Line : IEquatable<Line>, INullable
{
    public Vector3 Position;
    public Vector3 Direction;

    public Line(Vector3 position, Vector3 direction)
    {
        Position = position;
        Direction = direction;
    }

    public bool Equals(Line otherLine)
    {
        return Position == otherLine.Position && Direction == otherLine.Direction;
    }

    public bool IsNull {  get { return Position == null || Direction == null; } }
}

public class HandControls : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField]
    public HandDepth HandDepth;
    public HandStateAnalyzer HandStateAnalyzer;

    [Header("Objects")]
    [SerializeField]
    public UnityEngine.UI.Text ModeText;
    public UnityEngine.UI.Text ParameterText;
    public UnityEngine.UI.Text ActiveLightText;

    [Header("Resources")]
    [SerializeField]
    public UnityEngine.Object LightPrefab;
    public UnityEngine.Object PointingCylinder;


    [Header("Control State")]
    [SerializeField]
    public ControlState ControlState = ControlState.LightControl;
    public ControlParameter ControlParameter = ControlParameter.Value;
    public Vector2 ControlBoundsMultipliers = new Vector2(1, 1);

    [Space]
    public Vector3 PointingPosition;
    public Vector3 PointingDirection;
    public Vector2 CurrentLocation;
    public Vector2 ControlEnterLocation;
    public float ControlTop;
    public float ControlBottom;

    [Space]
    public HueLight ActiveLight;
    public float Hue = 0.5f;
    public float Saturation = 0.5f;
    public float Value = 0.5f;
    public List<GameObject> Lights;
    public List<Line> PlacementLines;
    public Vector3 LightPosition
    {
        set { InstantiateHueLight(value); }
    }

    [Space]
    [Header("Debug")]
    [SerializeField]
    public bool DebugMode = false;
    public  bool NewHandState = false;
    public int PlacementLineCount = 0;

    private HandState[] HandStateHistory;
    private HandState[] PreviousHandStateHistory;
    private float previousPinchTime;
    // Start is called before the first frame update
    void Start()
    {
        PlacementLines = new List<Line>();
        PlacementLines.Capacity = 2;
        HandStateHistory = new HandState[5];
        PreviousHandStateHistory = new HandState[5];
        Lights = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        CurrentLocation = HandDepth.GetWristLocationUV();
        UpdatePointingValues();
        StateUpdate();
        ModeText.text = ControlState.ToString();
        ParameterText.text = ControlParameter.ToString();
        ActiveLightText.text = ActiveLight != null ? ActiveLight.gameObject.GetInstanceID().ToString() : "No Light Selected";
    }

    private void UpdatePointingValues()
    {
        PointingPosition.x = HandDepth.JointPositions[HandStateAnalyzer.IndexFingerJoints[3]].x;
        PointingPosition.y = HandDepth.JointPositions[HandStateAnalyzer.IndexFingerJoints[3]].y;
        PointingPosition.z = HandDepth.JointPositions[HandStateAnalyzer.IndexFingerJoints[3]].z;

        PointingDirection = HandDepth.JointPositions[HandStateAnalyzer.IndexFingerJoints[3]] - HandDepth.JointPositions[HandStateAnalyzer.IndexFingerJoints[0]];
    }    

    public void StateUpdate()
    {
        Array.Copy(HandStateAnalyzer.HandStateHistory, HandStateHistory, HandStateHistory.Length);
        NewHandState = !HandStateHistorySame(HandStateHistory);

        switch (ControlState)
        {
            case ControlState.LightControl:
                DefaultStateUpdate(HandStateHistory);
                break;
            case ControlState.Placement:
                PlacementStateUpdate(HandStateHistory);
                break;
            //case ControlState.Hue:
            //    HueStateUpdate(HandStateHistory);
            //    break;
            //case ControlState.Saturation:
            //    SaturationStateUpdate(HandStateHistory);
            //    break;
            //case ControlState.Value:
            //    ValueStateUpdate(HandStateHistory);
            //    break;
            default:
                break;
        }

        Array.Copy(HandStateHistory, PreviousHandStateHistory, PreviousHandStateHistory.Length);
    }

    private void UpdateHSV()
    {
        if(ActiveLight != null)
        {
            Hue = ActiveLight.Hue;
            Saturation = ActiveLight.Saturation;
            Value = ActiveLight.Value;
        }
        else
        {
            Hue = Saturation = Value = -1;
        }
    }

    public bool HandStateHistorySame(HandState[] handStateHistory)
    {
        bool noChange = true;

        for (int i = 0; i < handStateHistory.Length; i++)
        {
            noChange &= handStateHistory[i] == PreviousHandStateHistory[i];
        }
        
        return noChange;
    }

    public void DefaultStateUpdate(HandState[] handStateHistory)
    {
        //state transitions
        HandState currentHandState = HandStateAnalyzer.HandState;
        ControlState oldControlState = ControlState;

        ControlState = currentHandState == HandState.DoublePointing ? ControlState.Placement : ControlState;
        //ControlState = currentHandState == HandState.Pinching ? ControlState.Value : ControlState;
        //ControlState = currentHandState == HandState.Grabbing ? ControlState.Hue : ControlState;
        //ControlState = currentHandState == HandState.WideHandling ? ControlState.Saturation : ControlState;

        if(oldControlState != ControlState)
        {
            SetControlsConfig();
            return;
        }

        //Light pointing
        if (currentHandState == HandState.Pointing)
            ActiveLight = GetActiveHueLight();

        if (ExitControlState(handStateHistory))
            ActiveLight = null;

        //Light Control
        ControlParameter = currentHandState == HandState.Fisting ? ControlParameter.Value : ControlParameter;
        ControlParameter = currentHandState == HandState.Grabbing ? ControlParameter.Hue : ControlParameter;
        ControlParameter = currentHandState == HandState.WideHandling ? ControlParameter.Saturation : ControlParameter;

        if (NewHandState && HandStateHistory[HandStateHistory.Length - 2] != HandState.Pinching && HandStateHistory[HandStateHistory.Length - 1] == HandState.Pinching)
        {
            SetControlsConfig();
            float currentTime = Time.time;
            if (currentTime <= previousPinchTime + 1.20f && currentTime > previousPinchTime + 0.250f)
            {
                ActiveLight.On = !ActiveLight.On;
                Debug.Log(currentTime);
            }

            previousPinchTime = currentTime;
        }

        if (HandStateAnalyzer.HandState == HandState.Pinching)
        {
            switch(ControlParameter)
            {
                case ControlParameter.Hue:
                    ActiveLight.Hue = GetControlValue();
                    Hue = ActiveLight.Hue;
                    break;
                case ControlParameter.Saturation:
                    ActiveLight.Saturation = GetControlValue();
                    Saturation = ActiveLight.Saturation;
                    break;
                case ControlParameter.Value:
                    ActiveLight.Value = GetControlValue();
                    Value = ActiveLight.Value;
                    break;
            }
        }
    }

    public HueLight GetActiveHueLight()
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(PointingPosition, PointingDirection, out raycastHit) && raycastHit.collider.tag == "LightSphere")
        {
            Debug.DrawRay(PointingPosition, PointingDirection, Color.green);
            HueLight SelectedLight = raycastHit.collider.gameObject.GetComponent<HueLight>();

            if(SelectedLight != ActiveLight)
                SelectedLight.SelectionBlink();

            return SelectedLight;
        }

        return ActiveLight;
    }

    public void InstantiateHueLight(Vector3 position)
    {
        GameObject newLight = Instantiate(LightPrefab, position, Quaternion.identity) as GameObject;
        Lights.Add(newLight);

        HueLight newHueLight = newLight.GetComponentInChildren<HueLight>();
        newHueLight.HueAPI = gameObject.GetComponent<HueAPI>();
        newHueLight.LightID = Lights.Count;
    }

    public void SetControlsConfig()
    {
        UpdateHSV();
        float activeValue = 0;
        activeValue = ControlParameter == ControlParameter.Hue ? 1 - Hue : activeValue;
        activeValue = ControlParameter == ControlParameter.Saturation ? 1 - Saturation : activeValue;
        activeValue = ControlParameter == ControlParameter.Value ? 1 - Value : activeValue;

        float controlLength = ControlBoundsMultipliers.y * HandDepth.HandSize.y;
        ControlEnterLocation = CurrentLocation;
        ControlTop = ControlEnterLocation.y - activeValue * controlLength;
        ControlBottom = ControlEnterLocation.y + (1.0f - activeValue) * controlLength;
    }

    public void PlacementStateUpdate(HandState[] handStateHistory)
    {
        int lastIndex = handStateHistory.Length - 1;
        if (NewHandState && handStateHistory[lastIndex] == HandState.DoublePointing && handStateHistory[lastIndex - 1] == HandState.GunPointing)
        {
            PlacementLines.Add(new Line(PointingPosition, PointingDirection));
            PlacementLineCount = PlacementLines.Count;

            if (PlacementLines.Count >= 2)
            {
                for (int i = 0; i < PlacementLines.Count; i++)
                {
                    Vector3 position = PlacementLines[i].Position;
                    Vector3 direction = PlacementLines[i].Direction;
                    GameObject PointingCylinder = Instantiate(this.PointingCylinder) as GameObject;
                    PointingCylinder.GetComponent<CylinderPointer>().UpdateCylinder(position, direction, this);
                    PointingCylinder.transform.localScale = new Vector3(0.05f, PointingCylinder.transform.localScale.y, 0.05f);
                    
                    if(i != 0)
                    {
                        PointingCylinder.tag = "Untagged";
                    }
                }

                PlacementLines.Clear();
                PlacementLineCount = 0;
            }
        }

        if (ExitControlState(handStateHistory))
            ControlState = ControlState.LightControl;
    }

    //public void HueStateUpdate(HandState[] handStateHistory)
    //{
    //    if (NewHandState && HandStateHistory[HandStateHistory.Length - 2] != HandState.Pinching && HandStateHistory[HandStateHistory.Length - 1] == HandState.Pinching)
    //        SetControlsConfig();

    //    if (HandStateAnalyzer.HandState == HandState.Pinching)
    //        ActiveLight.Hue = Hue = GetControlValue();

    //    if (ExitControlState(handStateHistory))
    //        ControlState = ControlState.Default;
    //}

    //public void SaturationStateUpdate(HandState[] handStateHistory)
    //{
    //    if (NewHandState && HandStateHistory[HandStateHistory.Length - 2] != HandState.Pinching && HandStateHistory[HandStateHistory.Length - 1] == HandState.Pinching)
    //        SetControlsConfig();

    //    if (HandStateAnalyzer.HandState == HandState.Pinching)
    //        ActiveLight.Saturation = Saturation = GetControlValue();

    //    if (ExitControlState(handStateHistory))
    //        ControlState = ControlState.Default;
    //}

    //public void ValueStateUpdate(HandState[] handStateHistory)
    //{
    //    if (NewHandState && HandStateHistory[HandStateHistory.Length - 2] != HandState.Pinching && HandStateHistory[HandStateHistory.Length - 1] == HandState.Pinching)
    //        SetControlsConfig();

    //    if (HandStateAnalyzer.HandState == HandState.Pinching)
    //        ActiveLight.Value = Value = GetControlValue();

    //    if (ExitControlState(handStateHistory))
    //        ControlState = ControlState.Default;
    //}

    public float GetControlValue()
    {
        float clampedHeight = Math.Clamp(CurrentLocation.y, ControlTop, ControlBottom);
        return Math.Abs(clampedHeight - ControlBottom) / Math.Abs(ControlTop - ControlBottom);
    }

    private bool ExitControlState(HandState[] handStateHistory)
    {
        int lastIndex = handStateHistory.Length - 1;
        return handStateHistory[lastIndex] == HandState.Palming
               && handStateHistory[lastIndex - 1] == HandState.Fisting 
               && handStateHistory[lastIndex - 2] == HandState.Palming
               && NewHandState;
    }

    //private bool ToggleLightGesture(HandState[] handStateHistory)
    //{
    //    int lastIndex = handStateHistory.Length - 1;
    //    return handStateHistory[lastIndex] == HandState.Palming
    //           && handStateHistory[lastIndex - 1] == HandState.Fisting
    //           && handStateHistory[lastIndex - 2] == HandState.Palming
    //           && handStateHistory[lastIndex - 3] == HandState.Palming
    //           && handStateHistory[lastIndex - 4] == HandState.Palming
    //           && NewHandState;
    //}
}
