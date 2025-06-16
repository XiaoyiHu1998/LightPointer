using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;

public class CylinderPointer : MonoBehaviour
{
    public Vector3 StartPosition;
    public Vector3 LineVector;
    public float Length;

    public HandControls HandControls;

    private bool keepGrowing;

    // Start is called before the first frame update
    void Start()
    {
        keepGrowing = true;
        //UpdateCylinder();

        if (!HandControls.DebugMode)
        {
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (keepGrowing)
        {
            Vector3 currentScale = gameObject.transform.localScale;
            Vector3 newScale = new Vector3(currentScale.x + 0.005f, currentScale.y, currentScale.z + 0.005f);
            transform.localScale = newScale;
        }
    }

    private void UpdateCylinder()
    {
        LineVector.Normalize();
        this.transform.position = StartPosition;
        this.transform.position += Length * LineVector;
        this.transform.up = LineVector;
        this.transform.localScale = new Vector3(1, Length, 1);
    }

    public void UpdateCylinder(Vector3 Position, Vector3 PointingDirection, HandControls HandControls, float Length = 100)
    {
        this.StartPosition = Position;
        this.LineVector = PointingDirection;
        this.Length = Length;
        this.HandControls = HandControls;
        UpdateCylinder();
    }

    private void OnCollisionEnter(Collision collision)
    {
        keepGrowing = false;

        if(collision.body.tag == "PointingCylinder")
            HandControls.LightPosition = collision.contacts[0].point;

        if (HandControls.DebugMode)
        {
            //transform.localScale = new Vector3(0.05f, Length, 0.05f);
            Destroy(gameObject.GetComponent<Rigidbody>());
        }
        else
        {
            Destroy(gameObject);
        }
    }


}
