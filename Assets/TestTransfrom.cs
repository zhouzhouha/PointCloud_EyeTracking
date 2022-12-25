using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTransfrom : MonoBehaviour
{
    Vector3 Origin_local = new Vector3(3, 4, 5);
    Vector3 direction_local = new Vector3(1, 1, 6);
    private static Matrix4x4 lastCameraMatrix;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
       
        cam = Camera.main;

    }

    // Update is called once per frame
    void Update()
    {
        lastCameraMatrix = Camera.main.cameraToWorldMatrix;
        Vector3 Unity_point = cam.transform.TransformPoint(Origin_local);
        Debug.Log("Unity Point :" + Unity_point);
        Vector3 Matrix_point = lastCameraMatrix.MultiplyPoint( Vector3.Scale(Origin_local,new Vector3(1,1,-1)));
        // This is because if I use the matrix and multiplyPoint I have to minus z 
        Debug.Log("My Point :" + Matrix_point);

    }
}
