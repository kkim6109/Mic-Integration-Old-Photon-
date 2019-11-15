// Created By: Elite Future, Discord: Elite Future#1043 for questions, suggestions, or optimizations

using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MicPlayer
{
    private float micModifier = 1f; // Used to adjust audio wait times(Look in the IEnum)
    private bool clipProcess = false; // To know if the IEnum is running
    private Queue<AudioClip> clipQueue = new Queue<AudioClip>(); // Queue of clips itself
    private int playerID = -1;
    private float micVolume = 1.5f;

    // Maybe an Icon as well if wanted

    // Sets up the mic player, not much else
    public MicPlayer(int pID)
    {
        if (CacheGameObject.Find("MainCamera").GetComponent<AudioSource>() == null)
        {
            CacheGameObject.Find("MainCamera").AddComponent<AudioSource>();
        }
        playerID = pID;
    }

    // Potential use for future identification
    public int ID
    {
        get
        {
            return playerID;
        }
    }

    // Sets and gets the volume of the specific person [Make a GUI later]
    public float volume
    {
        get
        {
            return micVolume;
        }
        set
        {
            micVolume = value;
        }
    }

    // Adds an audioclip to the queue
    public void add(AudioClip clip)
    {
        clipQueue.Enqueue(clip);
        if (!clipProcess)
        {
            FengGameManagerMKII.fengGame.StartCoroutine(waitTimeTillNext());
        }
    }

    // Processes and plays the queue of clips for a smooth voice effect
    public IEnumerator waitTimeTillNext()
    {
        if (!clipProcess)
        {
            clipProcess = true;
        }
        if (clipQueue.Count > 0)
        {
            // Debug to see how the long the queue is
            RPCList.add(clipQueue.Count + "");

            AudioClip clip = clipQueue.Dequeue();
            CacheGameObject.Find("MainCamera").GetComponent<AudioSource>().PlayOneShot(clip, micVolume);

            // This makes it so that the queue doesn't get too long, the stiched audios also sounds a bit better at 0.98, but otherwise unnoticeable
            if (micModifier == 1f && clipQueue.Count >= 4)
            {
                micModifier = 0.98f;
            }
            else if (micModifier == 0.98f && clipQueue.Count <= 2)
            {
                micModifier = 1f;
            }

            // Waits for the audio to be finished
            yield return new WaitForSeconds(clip.length * micModifier);

            // Repeats the IEnum for the potential next audio clip
            FengGameManagerMKII.fengGame.StartCoroutine(waitTimeTillNext());
        }
        else
        {
            clipProcess = false;
        }
    }
}