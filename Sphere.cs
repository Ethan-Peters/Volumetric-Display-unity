using UnityEngine;

public class Sphere : MonoBehaviour
{
    public float radius = 1f;
    public int segments = 64; // Points per ring
    public int latitudeLines = 8; // Horizontal rings
    public int longitudeLines = 8; // Vertical rings
    public float lineWidth = 0.02f;

    private LineRenderer[] lineRenderers;

    void Awake()
    {
        CreateRings();
    }

    void Update()
    {
        UpdateSphere();
    }

    void CreateRings()
    {
        int totalRings = latitudeLines + longitudeLines;
        lineRenderers = new LineRenderer[totalRings];

        for (int i = 0; i < totalRings; i++)
        {
            GameObject ringObj = new GameObject("Ring_" + i);
            ringObj.transform.parent = transform;
            ringObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.positionCount = segments + 1;
            lr.loop = true;
            lr.useWorldSpace = false;

            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white;
            lr.material = mat;

            lineRenderers[i] = lr;
        }
    }

    void UpdateSphere()
    {
        // Update latitude rings (horizontal circles)
        for (int lat = 0; lat < latitudeLines; lat++)
        {
            LineRenderer lr = lineRenderers[lat];
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            // Calculate height and radius for this latitude
            float t = (lat + 1) / (float)(latitudeLines + 1);
            float angle = Mathf.PI * t; // 0 to PI
            float y = Mathf.Cos(angle) * radius;
            float ringRadius = Mathf.Sin(angle) * radius;

            for (int i = 0; i <= segments; i++)
            {
                float theta = i * Mathf.PI * 2f / segments;
                Vector3 pos = new Vector3(
                    Mathf.Cos(theta) * ringRadius,
                    y,
                    Mathf.Sin(theta) * ringRadius
                );
                lr.SetPosition(i, pos);
            }
        }

        // Update longitude rings (vertical circles)
        for (int lon = 0; lon < longitudeLines; lon++)
        {
            LineRenderer lr = lineRenderers[latitudeLines + lon];
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            float rotationAngle = lon * Mathf.PI * 2f / longitudeLines;

            for (int i = 0; i <= segments; i++)
            {
                float phi = i * Mathf.PI * 2f / segments;

                // Create a vertical circle in XY plane, then rotate around Y axis
                float x = Mathf.Sin(phi) * radius;
                float y = Mathf.Cos(phi) * radius;
                float z = 0;

                // Rotate around Y axis
                Vector3 pos = new Vector3(
                    x * Mathf.Cos(rotationAngle) - z * Mathf.Sin(rotationAngle),
                    y,
                    x * Mathf.Sin(rotationAngle) + z * Mathf.Cos(rotationAngle)
                );

                lr.SetPosition(i, pos);
            }
        }
    }

    // Call this when you change latitudeLines or longitudeLines at runtime
    public void Rebuild()
    {
        // Clean up old rings
        if (lineRenderers != null)
        {
            foreach (var lr in lineRenderers)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
        }

        CreateRings();
        UpdateSphere();
    }
}