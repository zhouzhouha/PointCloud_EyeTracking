using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CustomeCalibration : MonoBehaviour
{

    float distance = 1;
    Vector3 v3Viewport;
    Vector3 v3BottomLeft;
    Vector3 v3TopRight;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Now print the message");

        v3Viewport.Set(0, 0, distance);
        v3BottomLeft = Camera.main.ViewportToWorldPoint(v3Viewport);
        v3Viewport.Set(1, 1, distance);
        v3TopRight = Camera.main.ViewportToWorldPoint(v3Viewport);
        Debug.Log("v3BottomLeft is " + v3BottomLeft);
        Debug.Log("v3TopRight is  " + v3TopRight);
        var height = 2.0 * Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad) * distance;
        var width = height * Camera.main.aspect;
        Debug.Log("height is " + height);
        Debug.Log("width is  " + width);
        Debug.Log("width by computation is  " + (v3TopRight.x - v3BottomLeft.x));
        Debug.Log("height by computation is  " + (v3TopRight.y - v3BottomLeft.y));
    }

    //public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
    //{
    //    var centerBaseViewportPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
    //    var canvasRect = canvas.GetComponent<RectTransform>();
    //    var scale = canvasRect.sizeDelta;
    //    return Vector3.Scale(centerBaseViewportPosition,scale);
    //}jstatsStream.Close();

}



