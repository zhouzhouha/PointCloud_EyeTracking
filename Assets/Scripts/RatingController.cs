using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required when Using UI elements.
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;
using System.Text;
using Unity.VisualScripting;

public class RatingController : MonoBehaviour
{
    //public Slider mainSlider;
    //Reference to new "RectTransform"(Child of FillArea).
    //public RectTransform newFillRect;
    public ActionBasedController rightHandController;

    private RenderController renderControl;


    public TextMeshPro textForSliderValue;

    //private Text textonSliderValue;
    //private Text Excellent;
    //private Text Good;
    //private Text Fair;
    //private Text Poor;
    //private Text Bad;

    private MainController mainControl;
    private RenderController renderController;
    private string dataOutputDir;
    private string experimentID;
    private string savePath;
    private string pc_id;
    //public GameObject OkButton; 
    public GameObject ButtonBad;
    public GameObject ButtonPoor;
    public GameObject ButtonFair;
    public GameObject ButtonGood;
    public GameObject ButtonExcellent;




    private bool isFinised = false;
    public bool FinishedRating
    {
        get => isFinised;
        private set => isFinised = value;
    }


    void Awake()
    {
        mainControl = FindObjectOfType<MainController>();
        if (mainControl == null)
        {
            Debug.LogError("Can not get a valid object of MainController!");
        }

        renderControl = FindObjectOfType<RenderController>();
        if (renderControl == null)
        {
            Debug.LogError("Can not get a valid object of RenderController!");
        }
        ButtonBad = GameObject.Find("ButtonBad");
        ButtonPoor = GameObject.Find("ButtonPoor");
        ButtonFair = GameObject.Find("ButtonFair");
        ButtonGood = GameObject.Find("ButtonGood");
        ButtonExcellent = GameObject.Find("ButtonExcellent");


    }



    private void OnEnable()
    {
        FinishedRating = false;
        ButtonBad.SetActive(true);
        ButtonPoor.SetActive(true);
        ButtonFair.SetActive(true);
        ButtonGood.SetActive(true);
        ButtonExcellent.SetActive(true);
        //ResetSlider();
    }


    //private void ResetSlider()
    //{
    //    mainSlider.fillRect.gameObject.SetActive(false);
    //    //mainSlider.fillRect = newFillRect;
    //    mainSlider.direction = Slider.Direction.LeftToRight;
    //    mainSlider.minValue = 1.0f;
    //    mainSlider.maxValue = 5.0f;
    //    mainSlider.wholeNumbers = true; // set the slider's value to accept not only the int value.
    //    mainSlider.value = 0;
    //}


    //Deactivates the old FillRect and assigns a new one.
    void Start()
    {
        
        dataOutputDir = mainControl.dataSaveDir;
        experimentID = string.Format("{0}_{1}{2}", mainControl.userid, mainControl.Session, ".txt");
        savePath = Path.Combine(dataOutputDir, experimentID);

        string pcdpath = renderControl.GetCurrentPcdPath();
        OnCurrDirPathUpdated(pcdpath);
        renderControl.OnCurrDirPathUpdated += this.OnCurrDirPathUpdated;

    }

    private void OnCurrDirPathUpdated(string dirpath)
    {
        // get "H5_C1_R5" from "E:\DUMP\H5_C1_R5"
        string pcdName = Path.GetFileName(dirpath);
        pc_id = pcdName;
    }


    //Update is called once per frame
    void Update()
    {
        //ShowSliderValue(mainSlider, textonSliderValue);

    }


    //public void ShowSliderValue()
    //{
    //    string sliderMessage = "Your quality score is:" + mainSlider.value.ToString("0.00");
    //    textForSliderValue.text = sliderMessage;
    //}



    public void FinishedRatingFun(int ButtonScore)
    {
        Debug.Log("On Click()");

        if (!this.FinishedRating)
        {
            // save rating data pc_id
            //int rating_score = mainSlider.value;
            int rating_score = ButtonScore;
            string allInfo = "pc_id: " + pc_id + " " + "MOS: " + rating_score.ToString() + "\n";
            // RecordRatingScore(allInfo, savePath);
            SaveRatingScoreButton(allInfo, savePath);
            FinishedRating = true;
            //OkButton.SetActive(false);
        }

    }




    //public void RecordRatingScore(string strs, string path)
    //{
    //    Debug.Log("Here is the rating score of the User" + mainSlider.value.ToString());

    //    if (!File.Exists(path))
    //    {
    //        FileStream fs = File.Create(path);
    //        fs.Dispose();
    //    }

    //    using (StreamWriter stream = new StreamWriter(path, true))
    //    {
    //        stream.WriteLine(strs);
    //    }

    //    //here is where the user should go to the calibration scene, remember to disable the rating
    //    //create seperate function to call (do everything by funcation then call them!) save the socre

    //}

    public void SaveRatingScoreButton(string strs, string path)
    {
        //Debug.Log("Here is the rating score of the User" + mainSlider.value.ToString());

        if (!File.Exists(path))
        {
            FileStream fs = File.Create(path);
            fs.Dispose();
        }

        using (StreamWriter stream = new StreamWriter(path, true))
        {
            stream.WriteLine(strs);
        }

        //here is where the user should go to the calibration scene, remember to disable the rating
        //create seperate function to call (do everything by funcation then call them!) save the socre

    }



}


