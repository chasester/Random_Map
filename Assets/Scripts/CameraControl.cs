using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {
    [Range(0.01f, 20f)]
    public float Speed = 2f;
    [Range(0.01f, 20f)]
    public float ZoomSpeed = 1f;
    [Range(1.01f, 100f)]
    public float BoostSpeed = 2f;
    private Transform trans;
    private Camera camera;
    // Use this for initialization
    void Start()
    {
        camera = GetComponent<Camera>();
        trans = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 nv = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
        float z = 0f;

        if (Input.GetKey(KeyCode.E))  z = ZoomSpeed;
        if (Input.GetKey(KeyCode.Q)) z = -ZoomSpeed;

        z *= Input.GetKey(KeyCode.LeftShift) ? BoostSpeed : 1.0f;
        nv *= Input.GetKey(KeyCode.LeftShift) ? BoostSpeed : 1.0f;
        trans.Translate(new Vector3(nv.x * Speed, nv.y * Speed, 0));
        camera.orthographicSize = Mathf.Clamp(z + camera.orthographicSize, 15, 2000);

    }
}
