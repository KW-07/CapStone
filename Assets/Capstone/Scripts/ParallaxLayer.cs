using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public float parallaxFactor = 0.5f; // 작을수록 더 느리게 이동 (원거리)
    private Transform cam;
    private Vector3 previousCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        previousCamPos = cam.position;
    }

    void Update()
    {
        Vector3 delta = cam.position - previousCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0);
        previousCamPos = cam.position;
    }
}