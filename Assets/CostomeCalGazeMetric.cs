using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEditor;
using ViveSR.anipal.Eye;
using System.IO;
using System.Runtime.InteropServices;


namespace GazeMetrics
{
    public class CostomeCalGazeMetric : MonoBehaviour
    {

        [Header("Scene References")]
        public new Camera camera;
        public Transform marker;

        [Header("Settings")]
        public GazeMetricsSettings settings;
        public GazeMetricsTargets targets;

        public bool showPreview;

        public bool IsCalibrating { get { return calibration.IsCalibrating; } }

        public string userid = "001";
        public string Session = "A";

        [SerializeField] private LineRenderer GazeRayRenderer;
        private static EyeData_v2 _gazeData = new EyeData_v2();
        private bool eye_callback_registered = false;


        //events
        public event Action OnCalibrationStarted;
        public event Action OnCalibrationRoutineDone;
        public event Action OnCalibrationFailed;
        public event Action OnCalibrationSucceeded;
        public event Action<TargetMetrics> OnMetricsCalculated;

        //members
        GazeMetricsBase calibration = new GazeMetricsBase();

        int targetIdx;
        int targetSampleCount;
        Vector3 currLocalTargetPos;

        float tLastSample = 0;
        float tLastTarget = 0;
        List<GameObject> previewMarkers = new List<GameObject>();

        bool markersInitialized = false;
        bool _isSampleExcluded;


        private string Name { get { return GetType().Name; } }


        private MainController mainControl;
        private string dataOutputDir;
        private string experimentID;

        void Awake()
        {
            Debug.Log($"{this.Name}: Awake()");



            mainControl = FindObjectOfType<MainController>();
            if (mainControl == null)
            {
                Debug.LogError("Can not get a valid object of MainController!");
            }


            dataOutputDir = mainControl.dataSaveDir;
            experimentID = string.Format("{0}_{1}_{2}", DateTime.Now.ToString("yyyyMMdd-HHmm"), mainControl.userid, mainControl.Session);

            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
            }
        }


        void OnEnable()
        {
            Debug.Log($"{this.Name}: OnEnable()");



            calibration.OnCalibrationSucceeded += CalibrationSucceeded;
            calibration.OnCalibrationFailed += CalibrationFailed;
            calibration.OnMetricsCalculated += MetricsCalcuated;

            if (marker == null || camera == null || settings == null || targets == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }

            Time.fixedDeltaTime = (float)1 / settings.samplingRate;  // set the frame rate of FixedUpdate()
            // 
            InitPreviewMarker();
            //if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            //{
            //    StartFramework();
            //}
            var sranipal = CostomeCalGazeMetric.FindObjectOfType<SRanipal_Eye_Framework>();
            sranipal.StartFramework();
        }


        void Start()
        {
            Debug.Log($"{this.Name}: Start()");
        }


        void OnDisable()
        {
            Debug.Log($"{this.Name}: OnDisable()");


            calibration.OnCalibrationSucceeded -= CalibrationSucceeded;
            calibration.OnCalibrationFailed -= CalibrationFailed;

            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
        }


        void Update()
        {
            SetPreviewMarkers(showPreview);

            //if (calibration.IsCalibrating)
            //{
            //    UpdateCalibration();
            //}

            UnityEngine.InputSystem.Keyboard.current.onTextInput +=
          inputText =>
          {
              if (inputText.ToString() == "a")
              {
                  ToggleCalibration();
              }
              else if (inputText.ToString() == "b")
              {
                  showPreview = !showPreview;
                  SetPreviewMarkers(showPreview);
              }

          };
        }



        int counter = 0;
        void FixedUpdate()
        {
            if (calibration.IsCalibrating)
            {
                UpdateCalibration();

                if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT)
                {
                    Debug.LogWarning(" Eye tracking framework not working or not supported");
                    return;
                }

                if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
                {
                    SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                    eye_callback_registered = true;

                }
                else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
                {
                    SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                    eye_callback_registered = false;
                }

                Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;
                if (eye_callback_registered)
                {
                    if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, _gazeData)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, _gazeData)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, _gazeData)) { }
                    else return;
                }
                else
                {
                    if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
                    else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
                    else return;
                }

            }
        }

        public void ToggleCalibration()
        {
            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
            else
            {
                StartCalibration();
            }
        }

        public void StartCalibration()
        {
            if (!enabled)
            {
                Debug.LogWarning("Component not enabled!");
                return;
            }


            Debug.Log($"{this.Name}: StartCalibration()");
            //Debug.Log((_gazeProvider.GetType().ToString()));

            // not show all markers
            showPreview = false;
            SetPreviewMarkers(showPreview);

            // set marker to show
            targetIdx = 0;
            targetSampleCount = 0;

            UpdatePosition();

            marker.gameObject.SetActive(true);

            calibration.StartCalibration(settings);
            Debug.Log($"Sample Rate: {settings.samplingRate}");

            if (OnCalibrationStarted != null)
            {
                OnCalibrationStarted();
            }
        }

        public void StopCalibration()
        {
            if (!calibration.IsCalibrating)
            {
                Debug.Log("Nothing to stop.");
                return;
            }

            calibration.StopCalibration(dataOutputDir, experimentID);

            marker.gameObject.SetActive(false);
            SetPreviewMarkers(false);

            if (OnCalibrationRoutineDone != null)
            {
                OnCalibrationRoutineDone();
            }

            //subsCtrl.OnDisconnecting -= StopCalibration;
        }

        void OnApplicationQuit()
        {
            //calibration.Destroy();
        }

        private void UpdateCalibration()
        {
            UpdateMarker();

            float tNow = Time.time;
            // if (tNow - tLastSample >= 1f / settings.SampleRate - Time.deltaTime / 2f)
            // {
            _isSampleExcluded = false;
            if (tNow - tLastTarget < settings.ignoreInitialSeconds - Time.deltaTime / 2f)
            {
                _isSampleExcluded = true;
            }

            tLastSample = tNow;

            //Adding the calibration reference data to the list that will be passed on, once the required sample amount is met.

            AddSample();

            targetSampleCount++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

            if (tNow - tLastTarget >= settings.secondsPerTarget)
            {
                calibration.SendCalibrationReferenceData();

                if (targetIdx < targets.GetTargetCount())
                {
                    targetSampleCount = 0;

                    UpdatePosition();
                }
                else
                {
                    StopCalibration();
                }
            }
            // }
        }

        private void CalibrationSucceeded()
        {
            if (OnCalibrationSucceeded != null)
            {
                OnCalibrationSucceeded();
            }
        }

        private void CalibrationFailed()
        {
            if (OnCalibrationFailed != null)
            {
                OnCalibrationFailed();
            }
        }

        private void MetricsCalcuated()
        {
            if (OnMetricsCalculated != null)
            {
                OnMetricsCalculated(new TargetMetrics());
            }
        }


        private Vector3 _previousGazeDirection;
        private void AddSample()
        {
            SampleData pointData = new SampleData();

            pointData.timeStamp = _gazeData.timestamp;
            //isValid?"Valid":"Invalid" (_gazeData.verbose_data.combined.eye_data.eye_data_validata_bit_mask<3)? false:true;
            pointData.isValid = (_gazeData.verbose_data.combined.eye_data.eye_data_validata_bit_mask == 3);
            pointData.exclude = _isSampleExcluded;
            pointData.targetId = targetIdx;
            pointData.localMarkerPosition = currLocalTargetPos;
            pointData.worldMarkerPosition = marker.position;
            pointData.cameraPosition = camera.transform.position; //Camera.main.cameraToWorldMatrix 
            pointData.localGazeOrigin = _gazeData.verbose_data.combined.eye_data.gaze_origin_mm;
            pointData.localGazeDirection = _gazeData.verbose_data.combined.eye_data.gaze_direction_normalized;
            //pointData.worldGazeOrigin = camera.transform.localToWorldMatrix.MultiplyPoint(Vector3.Scale(_gazeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f, new Vector3(-1, 1, -1)));
            //pointData.worldGazeDirection = camera.transform.localToWorldMatrix.MultiplyVector(Vector3.Scale(_gazeData.verbose_data.combined.eye_data.gaze_direction_normalized, new Vector3(-1, 1, -1)));
            pointData.worldGazeOrigin = camera.transform.localToWorldMatrix.MultiplyPoint(_gazeData.verbose_data.combined.eye_data.gaze_origin_mm);
            pointData.worldGazeDirection = camera.transform.localToWorldMatrix.MultiplyVector(_gazeData.verbose_data.combined.eye_data.gaze_direction_normalized);
            pointData.worldGazeDistance = 1;//_gazeData.verbose_data.combined.convergence_distance_mm;
            //_localEyeGazeData.Distance = _eyeData.verbose_data.combined.convergence_distance_mm/1000;
            //camera.transform.localToWorldMatrix.MultiplyPoint
            //camera.transform.TransformPoint

            //Calculate sample metrics
            MetricsCalculator.CalculateSampleMetrics(ref pointData, _previousGazeDirection);

            _previousGazeDirection = pointData.worldGazeDirection;

            calibration.AddCalibrationPointReferencePosition(pointData);
        }

        private void UpdatePosition()
        {
            currLocalTargetPos = targets.GetLocalTargetPosAt(targetIdx);

            targetIdx++;
            tLastTarget = Time.time;
        }

        private void UpdateMarker()
        {
            marker.position = camera.transform.localToWorldMatrix.MultiplyPoint(currLocalTargetPos);
            marker.LookAt(camera.transform.position);
        }

        void InitPreviewMarker()
        {
            if (markersInitialized) return;

            var previewMarkerParent = new GameObject("Calibration Targets Preview");
            previewMarkerParent.transform.SetParent(camera.transform);//
            previewMarkerParent.transform.localPosition = Vector3.zero;
            previewMarkerParent.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < targets.GetTargetCount(); ++i)
            {
                var target = targets.GetLocalTargetPosAt(i);
                var previewMarker = Instantiate<GameObject>(marker.gameObject);
                previewMarker.transform.parent = previewMarkerParent.transform;
                previewMarker.transform.localPosition = target;
                previewMarker.transform.LookAt(camera.transform.position);
                // modified by zyk, 2022-12-29
                //previewMarker.SetActive(true);
                //previewMarker.SetActive(this.gameObject.activeInHierarchy);
                previewMarker.SetActive(false);

                previewMarkers.Add(previewMarker);
            }

            markersInitialized = true;
        }

        void SetPreviewMarkers(bool value)
        {
            foreach (var marker in previewMarkers)
            {
                marker.SetActive(value);
            }
        }

        private static void EyeCallback(ref EyeData_v2 eye_data)
        {
            _gazeData = eye_data;
        }

        public void OnDestroy()
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));

        }

        private void Release()
        {
            if (eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }



    }
}
