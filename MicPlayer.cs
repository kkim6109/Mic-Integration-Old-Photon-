﻿// Created By: Elite Future, Discord: Elite Future#1043 for questions, suggestions, or optimizations

using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MicPlayer
{
    private float micModifier = 1f; // Used to adjust audio wait times(Look in the IEnum)
    public bool clipProcess = false; // To know if the IEnum is running
    private Queue<AudioClip> clipQueue = new Queue<AudioClip>(); // Queue of clips itself
    private int playerID = -1;
    private float micVolume = 1.5f;
    public string name;
    private bool muted;
    public bool mutedYou;
    public bool changingVolume = false;

    // Maybe an Icon as well if wanted

    // Add a way to remove yourself from the list of receivers, like sending a float[] of { 0.173 } or something

    // Sets up the mic player, not much else
    public MicPlayer(int pID)
    {
        if (Camera.main.gameObject.GetComponent<AudioSource>() == null)
        {
            Camera.main.gameObject.AddComponent<AudioSource>();
        }
        playerID = pID;
        PhotonPlayer player = PhotonPlayer.Find(playerID);
        if (player.customProperties.ContainsKey("name"))
        {
            name = player.customProperties["name"].ToString().hexColor();
        }
        mutedYou = false;
    }

    public bool isMuted
    {
        get
        {
            return muted;
        }
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
            FengGameManagerMKII.instance.StartCoroutine(waitTimeTillNext());
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
            AudioClip clip = clipQueue.Dequeue();
            Camera.main.gameObject.GetComponent<AudioSource>().PlayOneShot(clip, micVolume * MicEF.volumeMultiplier);

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
            FengGameManagerMKII.instance.StartCoroutine(waitTimeTillNext());
        }
        else
        {
            clipProcess = false;
        }
    }

    // Refreshes so the audio will work correctly
    public void refreshInformation()
    {
        clipProcess = false;
        clipQueue = new Queue<AudioClip>();
    }

    // Mutes player
    public void mute(bool enabled)
    {
        if (!mutedYou)
        {
            muted = enabled;
            if (enabled)
            {
                RaiseEventOptions raised = new RaiseEventOptions();
                raised.TargetActors = new int[] { playerID };
                PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[] { (byte)254 }, true, raised);
                MicEF.muteList.Add(playerID);
                if (MicEF.adjustableList.Contains(playerID))
                {
                    MicEF.adjustableList.Remove(playerID);
                    MicEF.recompileSendList();
                }
            }
            else if (MicEF.muteList.Contains(playerID))
            {
                MicEF.muteList.Remove(playerID);
                RaiseEventOptions raised = new RaiseEventOptions();
                raised.TargetActors = new int[] { playerID };
                PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[] { (byte)255 }, true, raised);
                if (!MicEF.adjustableList.Contains(playerID))
                {
                    MicEF.adjustableList.Add(playerID);
                    MicEF.recompileSendList();
                }
            }
        }
    }
}
