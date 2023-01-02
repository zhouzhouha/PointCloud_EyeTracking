using GazeMetrics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class MainController : MonoBehaviour
{
    private int flag = 0;  // 0: render, 1: rating, 2: calibration  init state only the render is working
    private RenderController renderController;
    private RatingController ratingController;
    private CostomeCalGazeMetric cusGazeMetricController;


    [Header("Switch status code")]
    public string Rating1;
    public string Calibration2;
    public string Eyetracking3;

    [Header("Experiment setting")]
    public string userid = "001";
    public string Session = "A";
    public string dataSaveDir = @"D:\";
    // TODO
    public string pc_folder_name;

    [Header("RightHand Controller")]
    //public ActionBasedController leftHandController;
    public ActionBasedController rightHandController;
    [Tooltip("The Input System Action that will go to the next stage")]
    [SerializeField] InputActionProperty m_nextStageAction;
    public InputActionProperty nextStageAction { get => m_nextStageAction;  }


    private void Awake()
    {
        // judge dataSaveDir
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
        cusGazeMetricController = FindObjectOfType<CostomeCalGazeMetric>();

        //// TODO
        if (renderController == null || ratingController == null || cusGazeMetricController == null)
        {
            Debug.LogError("renderController == null || ratingController == null || cusGazeMetricController == null !!!");
            UnityEditor.EditorApplication.isPlaying = false;
        }

        ratingController.gameObject.SetActive(false);
        cusGazeMetricController.gameObject.SetActive(false);
        renderController.gameObject.SetActive(true);
        pc_folder_name = renderController.pcdReader.dirName;
        //pc_folder_name = renderController.pc_folder_name; // get the current point cloud name

    }

    // Update is called once per frame
    void Update()
    {



        //var aa = rightController.positionAction.action.ReadValue<Vector3>();
        //Debug.Log(string.Format("right controller value: {0}", aa));

        //UnityEngine.InputSystem.Keyboard.current.onTextInput +=
        //   inputText =>
        //   {
        // now: render switch to rating
        //if (inputText.ToString() == Rating1 && flag == 0)
#if oldcoderemovedbyJacktotestnewercode
        bool nextWasTriggered = rightHandController.selectAction.action.triggered;
#endif
        bool nextWasTriggered = m_nextStageAction.action.triggered;

        if (nextWasTriggered && flag == 0)
        {
            renderController.SetRenderActive(false);
            //calibController.gameObject.SetActive(false);
            cusGazeMetricController.gameObject.SetActive(false);
            ratingController.gameObject.SetActive(true);
            Debug.Log("Now flag is 0 and will disable playing the Point cloud!");
            flag = 1;
        }
        // now: rating switch to calib
        //else if (flag == 1 && inputText.ToString() == Calibration2)  // 
        else if (nextWasTriggered && flag == 1)
        {
            if (ratingController.Finished)
            {
                renderController.SetRenderActive(false);
                ratingController.gameObject.SetActive(false);
                //calibController.gameObject.SetActive(true);
                cusGazeMetricController.gameObject.SetActive(true);
                Debug.Log("Now flag is 1 and doing the Rating!");
                flag = 2;
            }

        }
        // now: calib switch to next render
        //else if (flag == 2 && inputText.ToString() == Eyetracking3)  // switch to next render
        else if (nextWasTriggered && flag == 2)
        {
            if (cusGazeMetricController.Finished_calibration)
            {
                // added by zyk, 2022-12-29, before show next pct, clear the last pct
                renderController.RenderNext();
                cusGazeMetricController.gameObject.SetActive(false);
                ratingController.gameObject.SetActive(false);
                renderController.SetRenderActive(true); // Todo: remove the frist 2 seconds data
                Debug.Log("Now flag is 2 and doing the Calibration!");
                flag = 0;
            }
                
        }

    }

}
