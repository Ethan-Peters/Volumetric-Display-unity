using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FOVOffAxisProjection : MonoBehaviour
{
    [Header("Screen Corners (World Space)")]
    public Transform lowerLeft;
    public Transform lowerRight;
    public Transform upperLeft;

    [Header("Head")]
    public Transform headTransform;

    public float nearClip = 0.01f;
    public float farClip = 100f;

    public bool drawGizmos = true;

    private Camera cam;
    private float screenHeightSqr;
    void Start()
    {
        cam = GetComponent<Camera>();
        screenHeightSqr = (upperLeft.position - lowerLeft.position).sqrMagnitude;
    }

    void LateUpdate()
    {
        if (headTransform == null) return;

        Vector3 E = headTransform.position;

        Vector3 A = upperLeft.position;
        Vector3 B = lowerLeft.position;
        Vector3 C = lowerRight.position;

        float alphaSqr = (E - A).sqrMagnitude;
        float betaSqr = (E - B).sqrMagnitude;

        float cosine = (alphaSqr + betaSqr - screenHeightSqr) / (2 * Mathf.Sqrt(alphaSqr * betaSqr));
        float theta = Mathf.Acos(cosine) * Mathf.Rad2Deg;

        Vector3 midPoint = (A + C) / 2f;

        transform.position = E;
        transform.LookAt(midPoint);
        cam.fieldOfView = theta;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(upperLeft.position, lowerLeft.position);
        Gizmos.DrawLine(lowerLeft.position, lowerRight.position);
        Gizmos.DrawLine(upperLeft.position, lowerRight.position);
        Gizmos.DrawSphere(upperLeft.position, .01f);
        Gizmos.DrawSphere(lowerLeft.position, .01f);
        Gizmos.DrawSphere(lowerRight.position, .01f);
    }
}
