using UnityEngine;

/// <summary>
/// Off-axis perspective projection for head-tracked displays.
/// Attach this to your Camera and assign the screen corners and head tracker.
/// </summary>
[RequireComponent(typeof(Camera))]
public class OffAxisProjection : MonoBehaviour
{
    [Header("Screen Corner References")]
    [Tooltip("Window Size [0f - 1f]")]
    [Range(0, 1)]
    public float windowSize = 1f;
    [Tooltip("Bottom-left corner of the screen in world space")]
    public Transform bottomLeft;
    public GameObject WallObject;

    [Tooltip("Bottom-right corner of the screen in world space")]
    public Transform bottomRight;

    [Tooltip("Top-left corner of the screen in world space")]
    public Transform topLeft;

    [Header("Head Tracking")]
    [Tooltip("Transform representing the viewer's eye/head position")]
    public Transform headTransform;

    [Header("Projection Settings")]
    [Tooltip("Near clipping plane distance")]
    public float nearClipPlane = 0.1f;

    [Tooltip("Far clipping plane distance")]
    public float farClipPlane = 100f;

    [Header("Debug")]
    [Tooltip("Draw gizmos showing the frustum")]
    public bool drawDebugFrustum = true;
    public float gizmoTunnelOffset = -.5f;
    public int gizmoTunnelSegments = 10;
    public float gizmoSubdivisions = 5f;
    public float lineWidth = 1f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Validate references
        if (bottomLeft == null || bottomRight == null || topLeft == null)
        {
            Debug.LogError("OffAxisProjection: Screen corner references not assigned!");
            enabled = false;
            return;
        }

        if (headTransform == null)
        {
            Debug.LogError("OffAxisProjection: Head transform not assigned!");
            enabled = false;
            return;
        }
        Vector3 A = topLeft.position;
        Vector3 B = bottomLeft.position;
        Vector3 C = bottomRight.position;
        Vector3 D = A + C - B;

        // Drawing Tunnel
        Vector3 pa = A * windowSize;
        Vector3 pb = B * windowSize;
        Vector3 pc = C * windowSize;
        Vector3 pd = D * windowSize; // Top-right corner (computed)
        Vector3 offset = new Vector3(0, 0, gizmoTunnelOffset);

        CreateLine(pa, pa + offset * (gizmoTunnelSegments - 1), lineWidth);
        CreateLine(pb, pb + offset * (gizmoTunnelSegments - 1), lineWidth);
        CreateLine(pc, pc + offset * (gizmoTunnelSegments - 1), lineWidth);
        CreateLine(pd, pd + offset * (gizmoTunnelSegments - 1), lineWidth);

        for (int i = 0; i < gizmoTunnelSegments; i++)
        {
            CreateLine(pb + offset * i, pc + offset * i, lineWidth);  // Bottom edge
            CreateLine(pc + offset * i, pd + offset * i, lineWidth);  // Right edge
            CreateLine(pd + offset * i, pa + offset * i, lineWidth);  // Top edge
            CreateLine(pa + offset * i, pb + offset * i, lineWidth);  // Left edge
        }

        for (int i = 0; i < gizmoSubdivisions; i++)
        {
            float o = i / gizmoSubdivisions;
            Vector3 a = Vector3.Lerp(pa, pd, o);
            Vector3 b = Vector3.Lerp(pb, pc, o);
            Vector3 c = Vector3.Lerp(pa + offset * (gizmoTunnelSegments - 1), pd + offset * (gizmoTunnelSegments - 1), o);
            Vector3 d = Vector3.Lerp(pb + offset * (gizmoTunnelSegments - 1), pc + offset * (gizmoTunnelSegments - 1), o);

            CreateLine(a, c, lineWidth);
            CreateLine(b, d, lineWidth);
            CreateLine(c, d, lineWidth);

            Vector3 e = Vector3.Lerp(pa, pb, o);
            Vector3 f = e + offset * (gizmoTunnelSegments - 1);
            Vector3 g = Vector3.Lerp(pd, pc, o);
            Vector3 h = g + offset * (gizmoTunnelSegments - 1);
            CreateLine(e, f, lineWidth);
            CreateLine(g, h, lineWidth);
            CreateLine(f, h, lineWidth);

        }

        Vector3[] vertices = new Vector3[]
        {
            A, D, pd, pa, //TOP
            A, B, pb, pa, //LEFT
            B, C, pc, pb, //BOTTOM
            D, C, pc, pd  //RIGHT
        };
        int[] triangles = new int[]
        {
            0,1,2, 0,2,3, //TOP
            6,5,4, 7,6,4, //LEFT
            10,9,8, 11,10,8, //Bottom
            12,13,14, 12,14,15 //RIGHT
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        WallObject.GetComponent<MeshFilter>().mesh = mesh;
        Material mat = WallObject.GetComponent<MeshRenderer>().material;
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);


    }

    void LateUpdate()
    {
        UpdateProjectionMatrix();
    }

    void UpdateProjectionMatrix()
    {
        // Get screen corner positions
        Vector3 pa = topLeft.position;      // Upper left
        Vector3 pb = bottomLeft.position;   // Lower left  
        Vector3 pc = bottomRight.position;  // Lower right

        // Eye position
        Vector3 pe = headTransform.position;

        // Compute screen basis vectors
        Vector3 vr = (pc - pb).normalized;  // Right vector
        Vector3 vu = (pa - pb).normalized;  // Up vector
        Vector3 vn = Vector3.Cross(vr, vu).normalized;  // Normal vector (pointing away from screen)

        // Vectors from eye to screen corners
        Vector3 va = pb - pe;  // Eye to lower-left
        Vector3 vb = pc - pe;  // Eye to lower-right
        Vector3 vc = pa - pe;  // Eye to upper-left

        // Distance from eye to screen plane
        float d = Vector3.Dot(va, vn);

        // Check if eye is behind the screen
        if (d < nearClipPlane)
        {
            Debug.LogWarning("Eye is too close to or behind the screen plane! nearClipPlane: " + nearClipPlane + "   d: " + d);
            return;
        }

        // Compute frustum boundaries at the near plane
        float scale = nearClipPlane / d;
        //float scale = 1;

        float l = Vector3.Dot(vr, va) * scale;  // Left
        float r = Vector3.Dot(vr, vb) * scale;  // Right
        float b = Vector3.Dot(vu, va) * scale;  // Bottom
        float t = Vector3.Dot(vu, vc) * scale;  // Top

        // Create the off-axis projection matrix
        Matrix4x4 projectionMatrix = PerspectiveOffCenter(l, r, b, t, nearClipPlane, farClipPlane);

        // Apply to camera
        cam.projectionMatrix = projectionMatrix;

        // Set camera transform to match screen orientation
        // Position camera at eye position, looking perpendicular to screen
        transform.position = pe;
        transform.rotation = Quaternion.LookRotation(vn, vu);
    }

    /// <summary>
    /// Creates an off-axis perspective projection matrix.
    /// </summary>
    Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0f * near / (right - left);
        float y = 2.0f * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0f * far * near) / (far - near);
        float e = -1.0f;

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x; m[0, 1] = 0; m[0, 2] = a; m[0, 3] = 0;
        m[1, 0] = 0; m[1, 1] = y; m[1, 2] = b; m[1, 3] = 0;
        m[2, 0] = 0; m[2, 1] = 0; m[2, 2] = c; m[2, 3] = d;
        m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = e; m[3, 3] = 0;

        return m;
    }

    void OnDrawGizmos()
    {
        if (!drawDebugFrustum || bottomLeft == null || bottomRight == null || topLeft == null || headTransform == null)
            return;

        // Draw screen rectangle
        Gizmos.color = Color.cyan;
        Vector3 pa = topLeft.position * windowSize;
        Vector3 pb = bottomLeft.position * windowSize;
        Vector3 pc = bottomRight.position * windowSize;
        Vector3 pd = pa + (pc - pb); // Top-right corner (computed)
        Vector3 offset = new Vector3(0, 0, gizmoTunnelOffset);

        Gizmos.DrawLine(pa, pa + offset * (gizmoTunnelSegments - 1));
        Gizmos.DrawLine(pb, pb + offset * (gizmoTunnelSegments - 1));
        Gizmos.DrawLine(pc, pc + offset * (gizmoTunnelSegments - 1));
        Gizmos.DrawLine(pd, pd + offset * (gizmoTunnelSegments - 1));

        for (int i = 0; i < gizmoTunnelSegments; i++)
        {
            Gizmos.DrawLine(pb + offset * i, pc + offset * i);  // Bottom edge
            Gizmos.DrawLine(pc + offset * i, pd + offset * i);  // Right edge
            Gizmos.DrawLine(pd + offset * i, pa + offset * i);  // Top edge
            Gizmos.DrawLine(pa + offset * i, pb + offset * i);  // Left edge
        }

        for (int i = 0; i < gizmoSubdivisions; i++)
        {
            float o = i / gizmoSubdivisions;
            Vector3 a = Vector3.Lerp(pa, pd, o);
            Vector3 b = Vector3.Lerp(pb, pc, o);
            Vector3 c = Vector3.Lerp(pa + offset * (gizmoTunnelSegments - 1), pd + offset * (gizmoTunnelSegments - 1), o);
            Vector3 d = Vector3.Lerp(pb + offset * (gizmoTunnelSegments - 1), pc + offset * (gizmoTunnelSegments - 1), o);

            Gizmos.DrawLine(a, c);
            Gizmos.DrawLine(b, d);
            Gizmos.DrawLine(c, d);

            Vector3 e = Vector3.Lerp(pa, pb, o);
            Vector3 f = e + offset * (gizmoTunnelSegments - 1);
            Vector3 g = Vector3.Lerp(pd, pc, o);
            Vector3 h = g + offset * (gizmoTunnelSegments - 1);
            Gizmos.DrawLine(e, f);
            Gizmos.DrawLine(g, h);
            Gizmos.DrawLine(f, h);

        }

        //Gizmos.DrawLine(pa + offset * (gizmoTunnelSegments - 1), pc + offset * (gizmoTunnelSegments - 1));
        //Gizmos.DrawLine(pb + offset * (gizmoTunnelSegments - 1), pd + offset * (gizmoTunnelSegments - 1));

        // Draw lines from eye to corners
        Gizmos.color = Color.yellow;
        Vector3 pe = headTransform.position;
        Gizmos.DrawLine(pe, pa);
        Gizmos.DrawLine(pe, pb);
        Gizmos.DrawLine(pe, pc);
        Gizmos.DrawLine(pe, pd);

        // Draw eye position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pe, 0.05f);

        // Draw screen normal
        Vector3 vr = (pc - pb).normalized;
        Vector3 vu = (pa - pb).normalized;
        Vector3 vn = Vector3.Cross(vr, vu).normalized;
        Vector3 screenCenter = (pa + pb + pc + pd) / 4f;

        Gizmos.color = Color.green;
        //Gizmos.DrawLine(screenCenter, screenCenter + vn * 0.5f);
    }


    LineRenderer CreateLine(Vector3 a, Vector3 b, float width)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        lr.startWidth = width;
        lr.endWidth = width;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.cyan;
        lr.endColor = Color.cyan;

        return lr;
    }

}