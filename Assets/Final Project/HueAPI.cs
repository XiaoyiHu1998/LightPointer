using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class HueAPI : MonoBehaviour
{
    private static string DeveloperID;
    private static string bridgeIP;
    private static float lastRequestTime = 0;

    public void Start()
    {
        StreamReader streamReader = new StreamReader("C:\\Users\\Xiaoyi\\Documents\\FinalProjectDemoConfig.txt");
        DeveloperID = streamReader.ReadLine();
        bridgeIP = streamReader.ReadLine();
        streamReader.Close();
    }

    public void SetLightColor(int lightID, bool on, float hue, float saturation, float value)
    {
        StartCoroutine(PutLightStateAPI(lightID, new LightState(on, hue, saturation, value)));
    }

    public void Blink(int lightID, bool on, float hue, float saturation, float value)
    {
        StartCoroutine(PutLightStateAPI(lightID, new LightState(on, hue, saturation, value, true)));
    }

    private IEnumerator PutLightStateAPI(int lightID, LightState lightState)
    {
        if (Time.time < lastRequestTime + 0.200f)
            yield return null;

        string url = $"http://{bridgeIP}/api/{DeveloperID}/lights/{lightID}/state";
        byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(lightState).ToString());
        Debug.Log($"{url} | {JsonUtility.ToJson(lightState).ToString()}");

        using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
        {
            yield return request.SendWebRequest();

            if(request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("UnityWebRequest error");
                Debug.LogWarning($"{request.responseCode} | {request.result}");
            }
        }

        lastRequestTime = Time.time;
    }
}
