using Google.Protobuf.WellKnownTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightState
{
    public bool on;
    public int sat;
    public int bri;
    public int hue;
    public string alert;

    public LightState(bool on, float hue, float saturation, float value, bool alert = false)
    {
        this.on = on;
        this.sat = (int)(saturation * 255);
        this.bri = (int)(value * 255);
        this.hue = (int)(hue * 65536);
        this.alert = alert ? "select" : "none";
    }
}


public class HueLight : MonoBehaviour
{
    private float _Hue = 0.5f;
    private float _Saturation = 0.5f;
    private float _Value = 0.5f;
    private bool _On = true;
    private Color _ColorRGB;

    public Material LightMaterial;
    public HueAPI HueAPI;
    public int LightID;

    public float Hue { get { return _Hue; } set { _Hue = value; OnValueUpdate(); } }
    public float Saturation { get { return _Saturation; } set { _Saturation = value;  OnValueUpdate(); } }
    public float Value { get { return _Value; } set { _Value = value;  OnValueUpdate(); } }
    public bool On { get { return _On; } set { _On = value;  OnValueUpdate(); } }

    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<Renderer>().material = new Material(LightMaterial);
        UpdateLightMaterial();
    }

    private void UpdateLightMaterial()
    {
        _ColorRGB = _On ? Color.HSVToRGB(_Hue, _Saturation, _Value) : Color.black;
        gameObject.GetComponent<Renderer>().material.color = _ColorRGB;
        gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", _ColorRGB);
    }

    private void CallHueAPI()
    {
        HueAPI.SetLightColor(LightID, _On, _Hue, _Saturation, _Value);
    }

    private void OnValueUpdate()
    {
        UpdateLightMaterial();
        CallHueAPI();
    }

    public void SelectionBlink()
    {
        HueAPI.Blink(LightID, _On, _Hue, _Saturation, _Value);
    }
}
