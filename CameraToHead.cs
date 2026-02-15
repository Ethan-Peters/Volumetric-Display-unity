using Unity.VisualScripting;
using UnityEngine;

public class CameraToHead : MonoBehaviour
{
    [Header("Screen Corner References")]
    [Tooltip("Bottom-left corner of the screen in world space")]
    public Transform bottomLeft;

    [Tooltip("Bottom-right corner of the screen in world space")]
    public Transform bottomRight;

    [Tooltip("Top-left corner of the screen in world space")]
    public Transform topLeft;

    [Tooltip("Offset how close camera gets to screen edge")]
    public float offset = .1f;
    

    [Header("Head Tracking")]
    [Tooltip("Transform representing the viewer's eye/head position")]
    public Transform headTransform;

    private void LateUpdate()
    {

        transform.position = headTransform.position;

    }
}
