using GazeMetrics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;


public class MainController : MonoBehaviour
{
    //XRController RightController;
    //public ActionBasedController rightController;

    private int flag = 0;  // 0: render, 1: rating, 2: calibration  init state only the render is working
    private RenderController renderController;
    private RatingController ratingController;
    //private CalibrationController calibController;
    private CostomeCalGazeMetric cusGazeMetricController;


    [Header("Switch status code")]
    public string Rating1;
    public string Calibration2;
    public string Eyetracking3;

    [Header("Experiment setting")]
    public string userid = "001";
    public string Session = "A";
    public string dataSaveDir = @"D:\";


    private void Awake()
    {
        // judge dataSaveDir
        if (string.IsNullOrWhiteSpace(dataSaveDir))
        {
            Debug.LogError("dataSaveDir is empty!");
        }

        if (!System.IO.Directory.Exists(dataSaveDir))
        {
            try
            {
                Directory.CreateDirectory(dataSaveDir);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Create dataSaveDir error! [{dataSaveDir}]");
                throw;
            }
        }
    }




    // Start is called before the first frame update
    void Start()
    {

        renderController = FindObjectOfType<RenderController>();
        ratingController = FindObjectOfType<RatingController>();
        //calibController = FindObjectOfType<CalibrationController>();
        cusGazeMetricController = FindObjectOfType<CostomeCalGazeMetric>();

        //// TODO
        if (renderController == null)
        {
            Debug.LogError("Need to load the point cloud!");
            UnityEditor.EditorApplication.isPlaying = false;
        }
        if (ratingController.isActiveAndEnabled)
        {
            //Debug.Log("Right Controller is active?" + ratingController.isActiveAndEnabled);
            ratingController.gameObject.SetActive(false);
        }
        //if (calibController.isActiveAndEnabled)
        //{
        //    calibController.gameObject.SetActive(false);
        //}

        if (cusGazeMetricController.isActiveAndEnabled)
        {
            cusGazeMetricController.gameObject.SetActive(false);
            Debug.Log("Name is " + cusGazeMetricController.name + "\n" + "Type is" + cusGazeMetricController.GetType());
        }

    }

    // Update is called once per frame
    void Update()
    {



        //if (rightController.activateAction.action.triggered)
        //{
        //    Debug.Log("Pressing Trigger button.");

        //}
        //else
        //{
        //    Debug.Log("Need to test the trigger."); //seems succeseful!
        //}


        //if (rightController.activateAction.action.triggered)
        //{

        //    Debug.Log("Pressing Trigger button.");
        //}

        //var aa = rightController.positionAction.action.ReadValue<Vector3>();
        //Debug.Log(string.Format("right controller value: {0}", aa));



        UnityEngine.InputSystem.Keyboard.current.onTextInput +=
           inputText =>
           {
               // now: render switch to rating
               if (inputText.ToString() == Rating1 && flag == 0)
               {
                   renderController.SetRenderActive(false);
                   //calibController.gameObject.SetActive(false);
                   cusGazeMetricController.gameObject.SetActive(false);
                   ratingController.gameObject.SetActive(true);
                   Debug.Log("Now flag is 0 and will disable playing the Point cloud!");
                   flag = 1;
               }
               // now: rating switch to calib
               else if (flag == 1 && inputText.ToString() == Calibration2)  // 
               {
                   renderController.SetRenderActive(false);
                   ratingController.gameObject.SetActive(false);
                   //calibController.gameObject.SetActive(true);
                   cusGazeMetricController.gameObject.SetActive(true);
                   Debug.Log("Now flag is 1 and doing the Rating!");
                   flag = 2;

               }
               // now: calib switch to next render
               else if (flag == 2 && inputText.ToString() == Eyetracking3)  // switch to next render
               {
                   // added by zyk, 2022-12-29, before show next pct, clear the last pct

                   renderController.RenderNext();

                   //calibController.gameObject.SetActive(false);
                   cusGazeMetricController.gameObject.SetActive(false);
                   ratingController.gameObject.SetActive(false);
                   renderController.SetRenderActive(true); // Todo: remove the frist 2 seconds data


                   Debug.Log("Now flag is 2 and doing the Calibration!");
                   flag = 0;

               }

           };


    }

}
