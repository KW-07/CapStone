using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public AudioSource soundManager;

    public AudioClip preSound;

    [SerializeField]private AudioClip backgroundSound;

    public GameObject soundSlider;
    public float soundValue;

    private void Start()
    {
        preSound = backgroundSound;

        
    }

    private void Update()
    {
        soundValue = soundSlider.GetComponent<Slider>().value;

        soundManager.volume = soundValue;
    }
}
