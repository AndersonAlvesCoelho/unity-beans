using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {

    }

    public void PlaySound()
    {
        audioSource.Play();
    }
}
