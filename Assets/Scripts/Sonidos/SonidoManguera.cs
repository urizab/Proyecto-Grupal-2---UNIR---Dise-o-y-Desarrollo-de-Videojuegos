using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class SonidoManguera : MonoBehaviour
{
    public AudioClip mangueraSonido;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.clip = mangueraSonido;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        audioSource.Stop();
    }

    void Update()
    {
   
        if (Mouse.current.leftButton.isPressed)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}