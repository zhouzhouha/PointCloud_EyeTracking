using GazeMetrics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using ViveSR.anipal.Eye;
using Cwipc;
using System;

public class MainController : MonoBehaviour
{
    private int flag = 0;  // 0: render, 1: rating, 2: calibration  init state only the render is working
    private RenderController renderController;
    private RatingController ratingController;
    private CustomCalGazeMetric cusGazeMetricController;
    private PointCloudPlayback pointcloudPlayback;

    //[Tooltip("The reader for the pointclouds for which we get gaze data")]
    //public PrerecordedPointCloudReader pcdReader;
    ////public PointCloudRenderer pcdRender;



    [Header("Experiment setting")]
    public string userid = "001";
    public string Session = "A";
    public string dataSaveDir = @"C:\";
    // TODO
    //public string pc_folder_name;

    [Header("RightHand Controller")]
    public ActionBasedController rightHandController;
    [Tooltip("The Input System Action that will go to the next stage")]
    [SerializeField] InputActionProperty m_nextStageAction;
    private float ignoreNextUntil;

    public InputActionProperty nextStageAction { get => m_nextStageAction;  }


    private void Awake()
    {

        // test dataSaveDir
        if (string.IsNullOrWhiteSpace(dataSaveDir))
        {
            Debug.LogError("dataSaveDir is empty!");
        }
       

        dataSaveDir = Path.Combine(dataSaveDir, $"user_{userid}");
        if (!System.IO.Directory.Exists(dataSaveDir))
        {
            try
            {
                Directory.CreateDirectory(dataSaveDir);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Create dataSaveDir error! [{dataSaveDir}]  {ex.Message}");
                throw;
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        renderController = FindObjectOfType<RenderController>();
        ratingController = FindObjectOfType<RatingController>();
        cusGazeMetricController = FindObjectOfType<CustomCalGazeMetric>();
        pointcloudPlayback = FindObjectOfType<PointCloudPlayback>();

        //// TODO
        if (renderController == null || ratingController == null || cusGazeMetricController == null || pointcloudPlayback == null)
        {
            Debug.LogError("renderController == null || ratingController == null || cusGazeMetricController == null || pointcloudPlayback == null !!!");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        renderController.gameObject.SetActive(true);
        pointcloudPlayback.gameObject.SetActive(true);
        ratingController.gameObject.SetActive(false);
        cusGazeMetricController.gameObject.SetActive(false);
        
        //Debug.Log("The first path is :" + renderController.pcdReader.dirName); //already checked
        //Debug.Log("pointcloudPlayback is " + pointcloudPlayback.gameObject.activeSelf); //already checked true!
        //Debug.Log("Now is playback:" + pointcloudPlayback.dirName);    //already checked
        //pointcloudPlayback.RendererStarted();

    }

    // Update is called once per frame
    void Update()
    {

#if oldcoderemovedbyJacktotestnewercode
        bool nextWasTriggered = rightHandController.selectAction.action.triggered;
#endif
        bool nextWasTriggered = m_nextStageAction.action.triggered;
        
        if ( pointcloudPlayback.isRenderFinished && flag == 0)  // the number of loops has been played 
        {
            //    ratingController.gameObject.SetActive(true);
            //    cusGazeMetricController.gameObject.SetActive(false);
            //    renderController.SetRenderActive(false);  // OnDestroy call Stop then reader = null ;
            //pointcloudPlayback.Stop();
            Debug.Log("Now flag is 0 and will disable playing the Point cloud!");
            flag = 1;
            pointcloudPlayback.isRenderFinished = false;
            //Debug.Log("After disable the point")
            

        }

        else if (ratingController.FinishedRating && flag == 1)
        {
            
            renderController.SetRenderActive(false);
            pointcloudPlayback.gameObject.SetActive(false);
            ratingController.gameObject.SetActive(false);
            cusGazeMetricController.gameObject.SetActive(true);
                Debug.Log("Now flag is 1 and from Rating to Error Profiling!");
                flag = 2;

        }
        // now: calib switch to next render
        // before switch to next render, need do the re-calibration
        else if (nextWasTriggered && flag == 2)
        {
            if (cusGazeMetricController.Finished_calibration)
            {
                // added by xuemei.zykk, 2022-1-5, need to do the calibration again
                bool calibrationsucssful = SRanipal_Eye_v2.LaunchEyeCalibration();
                while (!calibrationsucssful)
                {
                    Debug.LogError("LaunchEyeCalibration failed!");
                    calibrationsucssful = SRanipal_Eye_v2.LaunchEyeCalibration();
                }

                Debug.Log("LaunchEyeCalibration Successuful!");

                //bool calibrationsucssful = SRanipal_Eye_v2.LaunchEyeCalibration();
                //if (!calibrationsucssful)
                //{
                //    Debug.LogError ("Clibration States:" + calibrationsucssful);
                //    SRanipal_Eye_v2.LaunchEyeCalibration();
                //}
                // if the calibration is failed, re do the calibration again?? how to get the state of the calibration??

                // added by xuemei.zyk, 2022-12-29, before show next pct, clear the last pct
                
                renderController.RenderNext();
                cusGazeMetricController.gameObject.SetActive(false);
                ratingController.gameObject.SetActive(false);
                renderController.SetRenderActive(true); // Todo: remove the frist 80 miliseconds data
                pointcloudPlayback.gameObject.SetActive(true);
                pointcloudPlayback.Play(pointcloudPlayback.dirName);
                Debug.Log("Now flag is 2 and doing the Calibration!");
                flag = 0;
            }
                
        }
        

    }

    internal void NewSequenceHasStarted()
    {
        ignoreNextUntil = Time.realtimeSinceStartup + 10;
    }
}
