using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    private static Rect unit = new Rect(0, 0, 1, 1);

    new public Camera camera;
    [SerializeField] private Transform focusTransform;
    [SerializeField] private Transform scaleTransform;
    [SerializeField] private Transform pivotTransform;

    public Vector2 focusTarget;
    private Vector2 focusVelocity;

    public float scaleTarget;
    private float scaleVelocity;

    public float rotationTarget;
    private float rotationVelocity;

    public float pivotTarget;
    private float pivotVelocity;

    public Rect pixelRect
    {
        get
        {
            return camera.pixelRect;
        }
    }

    public bool focussed
    {
        get
        {
            Vector3 normal = camera.ScreenToViewportPoint(Input.mousePosition);

            return unit.Contains(normal);
        }
    }

    public Rect viewport
    {
        set
        {
            camera.rect = value;
        }

        get
        {
            return camera.rect;
        }
    }

    public Vector2 up
    {
        get
        {
            return focusTransform.up;
        }
    }

    public Vector2 right
    {
        get
        {
            return focusTransform.right;
        }
    }

    public Vector2 focus
    {
        set
        {
            focusTransform.localPosition = new Vector3(value.x, value.y, 0);
        }

        get
        {
            Vector3 pos = focusTransform.localPosition;

            return new Vector2(pos.x, pos.y);
        }
    }

    private float _scale;

    public float scale
    {
        set
        {
            camera.orthographicSize = camera.pixelHeight / (2 * value);

            float h = camera.pixelHeight * 0.5f;
            float s = value;
            float t = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);

            scaleTransform.localPosition = Vector3.back * h / (s * t);
        }

        get
        {
            return camera.pixelHeight / (2 * camera.orthographicSize);
        }
    }

    public float rotation
    {
        set
        {
            focusTransform.localEulerAngles = Vector3.forward * value;
        }

        get
        {
            return focusTransform.localEulerAngles.z;
        }
    }

    public float pivot
    {
        set
        {
            pivotTransform.localEulerAngles = Vector3.left * value;
        }

        get
        {
            return (360 - pivotTransform.localEulerAngles.x) % 360;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        float h = camera.pixelHeight * 0.5f;
        float t = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);

        Gizmos.DrawLine(transform.position, transform.position + Vector3.back * h / (scale * t));
    }

    private void Awake()
    {
        Halt();
    }

    private void Update()
    {
        focus = Vector2.SmoothDamp(focus, focusTarget, ref focusVelocity, .1f, 1000, Time.deltaTime);
        scale = Mathf.SmoothDamp(scale, scaleTarget, ref scaleVelocity, .1f);
        rotation = Mathf.SmoothDampAngle(rotation, rotationTarget, ref rotationVelocity, .1f);
        pivot = Mathf.SmoothDampAngle(pivot, pivotTarget, ref pivotVelocity, .1f);
    }

    public void Halt()
    {
        focusTarget = focus;
        scaleTarget = scale;
        rotationTarget = rotation;
        pivotTarget = pivot;

        focusVelocity = Vector2.zero;
        scaleVelocity = 0f;
        rotationVelocity = 0f;
        pivotVelocity = 0f;
    }

    public Vector3 ScreenToWorld(Vector2 screen)
    {
        var plane = new Plane(Vector3.back, transform.position);
        var ray = camera.ScreenPointToRay(screen);
        float t;

        plane.Raycast(ray, out t);

        return ray.GetPoint(t);
    }
}
