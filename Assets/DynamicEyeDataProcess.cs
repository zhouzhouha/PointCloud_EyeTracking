using Cwipc;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class DynamicEyeDataProcess : MonoBehaviour
{

    //file reader
    [Header("Files")]
    public StreamReader sr;
    [Space(10)]


    public StreamWriter sw;
    public StreamReader srTransformData;

    private List<Vector3> currentPointCloud = new List<Vector3>();
    private List<float> currentPointGazeImportance = new List<float>();
    private int currentIndex;
    private List<Vector3> pointWithinRange = new List<Vector3>();
    private List<int> pointIndices = new List<int>();

    [Header("RegisterPoints")]
    //processing param
    public float globalAngleThreshold = 0.5f;
    public float acceptingDepthRange;
    public int angleSegments = 10;
    public int slices = 16;
    public float kappa = 1.5f;
    public float theta = 1.5f;

    public bool obtainOnlyResult = false;
    private Vector3 Valid_gaze_orgin = new Vector3();
    private Vector3 Valid_gaze_direction = new Vector3();

    public PrerecordedPointCloudReader pctReader;
    public PointCloudRenderer pcdRenderer;

    public int LengthOfRay = 25;
    [SerializeField] private LineRenderer GazeRayRenderer;
    public Gradient gradient = new Gradient();


    bool isProcessing = false;

    Vector3 Valid_gaze_origin_world;
    Vector3 Valid_gaze_direction_world;
    // H4C1R5
    string ply_Folder_path = @"E:\PLY\H4_C1_R5\";
    string dump_Folder_path = @"E:\DUMP\H4_C1_R5\";
    string GazeDataDir = @"D:\xuemei\RawData\user_001\20230104-2236_001_A.json";
    int curIdx = 0;


    // test for gaze result
    public GameObject sphere;


    void Start()
    {
        
        Debug.Log("Test start!");
        GazeRayRenderer = GetComponent<LineRenderer>();
        GazeRayRenderer.material = new Material(Shader.Find("Sprites/Default"));
        // a simple 2 color gradient with a fixed alpha of 1.0f.
        float alpha = 1.0f;
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
            );
    }


    void Update()
    {
        ShowGaze(Valid_gaze_origin_world, Valid_gaze_direction_world);
        if (Keyboard.current.spaceKey.isPressed)
        {
            // Spacebar was pressed 
            Debug.Log("Spacebar was pressed !");
            ProcessAsync();
        } 

    }


    void ShowGaze(Vector3 Valid_gaze_orgin, Vector3 Valid_gaze_direction)
    {
        GazeRayRenderer.SetPosition(0, Valid_gaze_orgin); 
        GazeRayRenderer.SetPosition(1, Valid_gaze_orgin + Valid_gaze_direction * LengthOfRay);
        GazeRayRenderer.startColor = Color.red;
        GazeRayRenderer.colorGradient = gradient;
        GazeRayRenderer.endColor = Color.green;
    }



    void ProcessAsync()
    {
        if (isProcessing)
            return;
        isProcessing = true;

        Thread th = new Thread(ProcessFunc);
        th.IsBackground = true;
        th.Start();
    }





    void ProcessFunc()
    {
        ReadEyeData(GazeDataDir);
        isProcessing = false;
    }





    void LoadPointCloud(String PC_path)
    {
        if (!obtainOnlyResult)
        {

            currentPointCloud.Clear();
            currentPointGazeImportance.Clear();

            if (File.Exists(PC_path))
            {
                var stream = File.Open(PC_path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = ReadDataHeader(new StreamReader(stream));
                var body = ReadDataBody(header, new BinaryReader(stream));

                for (int i = 0; i < body.vertices.Count; i++)
                {
                    Vector3 v = body.vertices[i];
                    currentPointCloud.Add(v);
                    currentPointGazeImportance.Add(0f);
                }
            }
            else
            {

                Debug.LogWarning("currentObject Not Found");
            }
        }
    }

    enum DataProperty
    {
        Invalid,
        X, Y, Z,
        R, G, B, A,
        Data8, Data16, Data32
    }

    static int GetPropertySize(DataProperty p)
    {
        Debug.Log(p);
        switch (p)
        {

            case DataProperty.X: return 4;
            case DataProperty.Y: return 4;
            case DataProperty.Z: return 4;
        }
        return 0;
    }

    class DataHeader
    {
        public List<DataProperty> properties = new List<DataProperty>();
        public int vertexCount = -1;
    }

    class DataBody
    {
        public List<Vector3> vertices;

        public DataBody(int vertexCount)
        {
            vertices = new List<Vector3>(vertexCount);
        }

        public void AddPoint(
            float x, float y, float z
        )
        {
            vertices.Add(new Vector3(x, y, z));
        }
    }

    DataHeader ReadDataHeader(StreamReader reader)
    {
        var data = new DataHeader();
        var readCount = 0;

        // Magic number line ("ply")
        var line = reader.ReadLine();
        readCount += line.Length + 1;
        if (line != "ply")
            throw new ArgumentException("Magic number ('ply') mismatch.");

        // Data format: check if it's binary/little endian.
        line = reader.ReadLine();
        readCount += line.Length + 1;
        if (line != "format binary_little_endian 1.0")
            throw new ArgumentException(
                "Invalid data format ('" + line + "'). " +
                "Should be binary/little endian.");

        // Read header contents.
        for (var skip = false; ;)
        {
            // Read a line and split it with white space.
            line = reader.ReadLine();
            readCount += line.Length + 1;
            if (line == "end_header") break;
            var col = line.Split();

            // Element declaration (unskippable)
            if (col[0] == "element")
            {
                if (col[1] == "vertex")
                {
                    data.vertexCount = Convert.ToInt32(col[2]);
                    skip = false;
                }
                else
                {
                    // Don't read elements other than vertices.
                    skip = true;
                }
            }

            if (skip) continue;

            // Property declaration line
            if (col[0] == "property")
            {
                var prop = DataProperty.Invalid;

                // Parse the property name entry.
                switch (col[2])
                {
                    case "x": prop = DataProperty.X; break;
                    case "y": prop = DataProperty.Y; break;
                    case "z": prop = DataProperty.Z; break;
                }

                if (col[1] == "char" || col[1] == "uchar")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data8;
                    else if (GetPropertySize(prop) != 1)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else if (col[1] == "short" || col[1] == "ushort")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data16;
                    else if (GetPropertySize(prop) != 2)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else if (col[1] == "int" || col[1] == "uint" || col[1] == "float")
                {
                    if (prop == DataProperty.Invalid)
                        prop = DataProperty.Data32;
                    else if (GetPropertySize(prop) != 4)
                        throw new ArgumentException("Invalid property type ('" + line + "').");
                }
                else
                {
                    throw new ArgumentException("Unsupported property type ('" + line + "').");
                }


                data.properties.Add(prop);
            }
        }

        // Rewind the stream back to the exact position of the reader.
        reader.BaseStream.Position = readCount;

        return data;
    }

    DataBody ReadDataBody(DataHeader header, BinaryReader reader)
    {
        var data = new DataBody(header.vertexCount);

        float x = 0, y = 0, z = 0;

        for (var i = 0; i < header.vertexCount; i++)
        {
            foreach (var prop in header.properties)
            {
                switch (prop)
                {
                    case DataProperty.X: x = reader.ReadSingle(); break;
                    case DataProperty.Y: y = reader.ReadSingle(); break;
                    case DataProperty.Z: z = reader.ReadSingle(); break;
                    case DataProperty.Data8: reader.ReadByte(); break;
                    case DataProperty.Data16: reader.BaseStream.Position += 2; break;
                    case DataProperty.Data32: reader.BaseStream.Position += 4; break;
                }
            }
            data.AddPoint(x, y, z);

        }

        return data;
    }

    /// <param name="EyeDatapath"></param>
    void ReadEyeData(string EyeDatapath)
    {
        string dataJsonString = File.ReadAllText(EyeDatapath);
        List<FullData_cwi> fullDataList = JsonConvert.DeserializeObject<List<FullData_cwi>>(dataJsonString);
        HashSet<string> uniqueFilenames = new HashSet<string>();
        for (int i = 0; i < fullDataList.Count; i++) //
        {
            if (fullDataList[i].eye_data_cwi.verbose_data.combined.eye_data.eye_data_validata_bit_mask == 3) // valid
            {
                string dump_filename = fullDataList[i].pcname;
                if (dump_filename == "" || dump_filename.Contains("H2_C1_R4") || dump_filename.Contains("H1_C1_R5"))
                { continue; }
                uniqueFilenames.Add(dump_filename);
                string ply_filename = Path.GetFileName(Path.ChangeExtension(dump_filename, "ply")); // from dump to ply
                string ply_full_filename = Path.Combine(ply_Folder_path, ply_filename); // load the point cloud frame by file name ply format

            }
                
        }
        print("There are " + uniqueFilenames.Count + " unique filenames");
        foreach (var fname in uniqueFilenames)
        {
            string pattern = "DUMP";
            string replace = "PLY";
            string only_folder_filename = Path.GetFileName(Path.GetDirectoryName(Path.ChangeExtension(fname, "txt")));
            string only_ply_filename = Path.GetFileName(Path.ChangeExtension(fname, "txt"));
            string HeatMapSaveName = Path.Combine(only_folder_filename, only_ply_filename);
            string ply_filename = Path.ChangeExtension(fname, "ply"); // from dump to ply
            string ply_full_filename = Regex.Replace(ply_filename, pattern, replace); // load the point cloud frame by file name ply format
            Debug.Log("This is ply filename: " + ply_full_filename);
            // load corresponding point cloud data
            LoadPointCloud(ply_full_filename); //right hand coordinate system
            Debug.Log("Load Sucessful!");
            for (int i = 0; i < fullDataList.Count; i++) //
            {
                print(" the" + (i + 1) + "gazedata");
                string dump_filename = fullDataList[i].pcname;
                if (dump_filename == "")
                { continue; }                
                if (dump_filename == fname)
                {    
                    if (fullDataList[i].eye_data_cwi.verbose_data.combined.eye_data.eye_data_validata_bit_mask == 3) // valid
                    {
                        Vector3 origin_world = fullDataList[i].gaze_origin_global_combined;
                        Vector3 direction_world = fullDataList[i].gaze_direction_global_combined;
                        Valid_gaze_origin_world = origin_world;
                        Valid_gaze_direction_world = direction_world;
                        int TimeStamp = fullDataList[i].eye_data_cwi.timestamp;
                        RegisterPoints(direction_world, origin_world, currentPointCloud, 1.0f, 0.05f);
                    }
                }
            }
            WritePointCloud(currentPointCloud, currentPointGazeImportance, only_folder_filename, HeatMapSaveName);
            Debug.Log("has already wirte the point cloud!");
        }
        
    }
    void RegisterPoints(Vector3 gazeRay, Vector3 camPos, List<Vector3> currentPointCloud, float currentAngleThreshold, float acceptingDepthRange)
    {
        if (!obtainOnlyResult)
        {
            pointWithinRange.Clear();
            pointIndices.Clear();
            float angleThreshold;
            angleThreshold = Mathf.Max(globalAngleThreshold, currentAngleThreshold);
            angleThreshold += 0.5f;
            float minDistance = float.MaxValue;
            Vector3 normalVector = new Vector3(1f, 1f, -(gazeRay.x + gazeRay.y) / gazeRay.z);
            List<int>[] segments = new List<int>[slices * angleSegments];
            Vector3[] closestPoints = new Vector3[slices * angleSegments];
            float[] minDistances = new float[slices * angleSegments];

            for (int i = 0; i < slices * angleSegments; i++)
            {
                segments[i] = new List<int>();
                minDistances[i] = float.MaxValue;
            }

            for (int i = 0; i < currentPointCloud.Count; i++)
            {
                Vector3 point = currentPointCloud[i];
                point.z *= -1; // to unity left hand 
                Vector3 dir = point - camPos;
                float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));


                if (angleInDegree < angleThreshold)
                {
                    pointWithinRange.Add(point);
                    pointIndices.Add(i);
                    float distance = Mathf.Abs(Vector3.Dot(dir, gazeRay) / gazeRay.magnitude);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }

                    float perAngle = angleThreshold / angleSegments;
                    for (int p = 0; p < angleSegments; p++)
                    {
                        if (angleInDegree <= (p + 1) * perAngle && angleInDegree > p * perAngle)
                        {
                            float lamda = gazeRay.x * point.x + gazeRay.y * point.y + gazeRay.z * point.z;
                            float k = (lamda - gazeRay.x * camPos.x - gazeRay.y * camPos.y - gazeRay.z * camPos.z) / (gazeRay.x * gazeRay.x + gazeRay.y * gazeRay.y + gazeRay.z * gazeRay.z);
                            Vector3 intersect = camPos + k * gazeRay;
                            Vector3 distanceVector = point - intersect;
                            float angle = Vector3.SignedAngle(normalVector, distanceVector, gazeRay) + 180f;
                            float perSlice = 360f / slices;
                            for (int q = 0; q < slices; q++)
                            {
                                if (angle <= (q + 1) * perSlice && angle > q * perSlice)
                                {
                                    segments[p * slices + q].Add(i);
                                    if (distance < minDistances[p * slices + q])
                                    {
                                        minDistances[p * slices + q] = distance;
                                        closestPoints[p * slices + q] = point;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 dirClose = closestPoints[i] - camPos;
                float mDist = Vector3.Dot(gazeRay, dirClose) / gazeRay.magnitude;
                float radius = (mDist + acceptingDepthRange) * Mathf.Tan(angleThreshold * Mathf.PI / 180);
                foreach (int j in segments[i])
                {
                    Vector3 point = currentPointCloud[j];
                    point.z *= -1; // To do change currentPointCloud mirror z
                    Vector3 diffvec = point - closestPoints[i];
                    float depth = Vector3.Dot(gazeRay, diffvec) / gazeRay.magnitude;

                    if (depth < acceptingDepthRange && depth > 0f)
                    {
                        Vector3 dir = point - camPos;
                        float angleInDegree = Mathf.Abs(Vector3.Angle(gazeRay, dir));
                        float pDist = Vector3.Dot(gazeRay, dir) / gazeRay.magnitude;
                        float pRadius = pDist * Mathf.Tan(angleInDegree * Mathf.PI / 180);
                        float var = radius * radius / 3f / 3f;
                        currentPointGazeImportance[j] += Mathf.Exp(-Mathf.Pow(pRadius, 2f) / (2f * var)) / Mathf.Sqrt(2f * Mathf.PI * var);
                    }

                }
            }

        }
    }


    void WritePointCloud(List<Vector3> currentPointCloud, List<float> currentPointGazeImportance, string savefoldername, string savefilename)
    {
        if (!obtainOnlyResult)
        {
            string HeatMapDir = @"D:\xuemei\HeatMap\";
            string SaveFolderName = Path.Combine(HeatMapDir, savefoldername);
            string SaveHeatmapDir = Path.Combine(HeatMapDir, savefilename);
            string path = SaveHeatmapDir;
            if (!System.IO.Directory.Exists(SaveFolderName))
                Directory.CreateDirectory(SaveFolderName);
            File.WriteAllText(path, string.Empty);
            sw = new StreamWriter(path, true);
            sw.WriteLine("PosX PosY PosZ GazeCount");
            sw.Flush();

            for (int i = 0; i < currentPointCloud.Count; i++)
            {
                sw.WriteLine(currentPointCloud[i].x + " " + currentPointCloud[i].y + " " + currentPointCloud[i].z * (-1) + " " + currentPointGazeImportance[i]);
                sw.Flush();
            }
            sw.Dispose();
        }
    }

    

    [Serializable]
    public class FullData_cwi
    {
        public CameraMatrix camera_matrix { get; set; }
        public int pointcloudTs { get; set; }
        public EyeDataCwi eye_data_cwi { get; set; }
        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public string pcname { get; set; }
    }

    public class ValidGazeforHeatmap
    {
        
        public Vector3 gaze_origin_global_combined { get; set; }
        public Vector3 gaze_direction_global_combined { get; set; }
        public int timestamp { get; set; }
    }
    public class CameraMatrix
    {
        public float e00 { get; set; }
        public float e01 { get; set; }
        public float e02 { get; set; }
        public float e03 { get; set; }
        public float e10 { get; set; }
        public float e11 { get; set; }
        public float e12 { get; set; }
        public float e13 { get; set; }
        public float e20 { get; set; }
        public float e21 { get; set; }
        public float e22 { get; set; }
        public float e23 { get; set; }
        public float e30 { get; set; }
        public float e31 { get; set; }
        public float e32 { get; set; }
        public float e33 { get; set; }
    }

    public class Combined
    {
        public EyeData eye_data { get; set; }
        public bool convergence_distance_validity { get; set; }
        public double convergence_distance_mm { get; set; }
    }

    public class ExpressionData
    {
        public Left left { get; set; }
        public Right right { get; set; }
    }

    public class EyeData
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public double pupil_diameter_mm { get; set; }
        public double eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
    }

    public class EyeDataCwi
    {
        public bool no_user { get; set; }
        public int frame_sequence { get; set; }
        public int timestamp { get; set; }
        public VerboseData verbose_data { get; set; }
        public ExpressionData expression_data { get; set; }
    }

    public class GazeDirectionNormalized
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class GazeOriginMm
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class Left
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public double pupil_diameter_mm { get; set; }
        public double eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
        public float eye_wide { get; set; }
        public float eye_squeeze { get; set; }
        public float eye_frown { get; set; }
    }

    public class PupilPositionInSensorArea
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    public class Right
    {
        public int eye_data_validata_bit_mask { get; set; }
        public GazeOriginMm gaze_origin_mm { get; set; }
        public GazeDirectionNormalized gaze_direction_normalized { get; set; }
        public float pupil_diameter_mm { get; set; }
        public float eye_openness { get; set; }
        public PupilPositionInSensorArea pupil_position_in_sensor_area { get; set; }
        public float eye_wide { get; set; }
        public float eye_squeeze { get; set; }
        public float eye_frown { get; set; }
    }


    public class VerboseData
    {
        public Left left { get; set; }
        public Right right { get; set; }
        public Combined combined { get; set; }
    }
}

