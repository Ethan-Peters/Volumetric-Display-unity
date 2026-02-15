using UnityEngine;

public class DistanceCalculator : MonoBehaviour
{
    [Header("Smoothing Factor")]
    //public float xyScalingFactor = 1f;
    public float smoothingFactor = 0.1f;

    [Header("Scale Calibration")]
    public float xScalingFactor = 6.83f;
    public float yScalingFactor = 1f;
    public float zScalingFactor = 1f;
    public bool restrictX = false;
    public bool restrictY = false;
    public bool restrictZ = false;

    [Header("Calibration")]
    public float referenceEyeDistPixels = 38f; // Your measurement at 1 meter
    public float referenceDistanceMeters = 1.0f; // 1 meter
    public float realEyeDistanceMeters = 0.063f; // ~6.3cm average

    [Header("Screen Properties")]
    public float screenWidthMeters = 0.3429f; // Measure your actual screen width in meters
    public float screenHeightMeters = 0.1929f; // Measure your actual screen height in meters

    [Header("Output")]
    public Vector3 eyePosition;

    private eye_tracking_receiver receiver;
    private Vector3 smoothedPosition;

    void Start()
    {
        smoothedPosition = transform.position;
        receiver = GetComponent<eye_tracking_receiver>();
        eyePosition = Vector3.zero;
    }

    void Update()
    {
        float eyeX = receiver.eyeX; // Normalized screen coords (-0.5 to 0.5)
        float eyeY = receiver.eyeY;
        float eyeDist = receiver.eyeDist; // Pixels

        // Calculate depth (distance from screen)
        //float depthZ = (realEyeDistanceMeters * referenceDistanceMeters * referenceEyeDistPixels) / eyeDist;
        float depthZ = (referenceDistanceMeters * referenceEyeDistPixels) / eyeDist;

        // Convert normalized screen coords to real-world positions
        // At distance Z, a position of eyeX=-0.5 means you're at the left edge of the screen
        float visibleWidthAtDepth = screenWidthMeters * (depthZ / referenceDistanceMeters);
        float visibleHeightAtDepth = screenHeightMeters * (depthZ / referenceDistanceMeters);

        eyePosition.x = eyeX * visibleWidthAtDepth * xScalingFactor;
        eyePosition.y = -eyeY * visibleHeightAtDepth * yScalingFactor; // Negative for Unity's coordinate system
        eyePosition.z = depthZ * zScalingFactor;
        eyePosition *= 100;

        if (restrictX) eyePosition.x = 0;
        if (restrictY) eyePosition.y = 0;
        if (restrictZ) eyePosition.z = 50;


        // Validate data
        if (!float.IsNaN(eyePosition.x) && !float.IsNaN(eyePosition.y) && eyeDist > 10f)
        {
            smoothedPosition = eyePosition * smoothingFactor + (1 - smoothingFactor) * smoothedPosition;
            transform.position = smoothedPosition;
        }
    }

}