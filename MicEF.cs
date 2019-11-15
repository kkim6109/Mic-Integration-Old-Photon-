// Created By: Elite Future, Discord: Elite Future#1043 for questions, suggestions, or optimizations

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MicEF : MonoBehaviour
{

    private int lastPos;
    private AudioClip c;
    public static float FREQUENCY = 10000f;
    public static int DECREASE = 125;
    private int threadID;
    public static Dictionary<int, MicPlayer> users;
    public static KeyCode pushToTalk = KeyCode.Z;

    void Start()
    {
        users = new Dictionary<int, MicPlayer>(); // int for ID
        DECREASE = (int)(FREQUENCY / 80f); // This is to slightly slow down the audio, not noticeable sound-wise, but removes some gaps
        threadID = -1;
    }

    void Update()
    {
        if (Input.GetKeyUp(pushToTalk))
        {
            sendMicData(); // sends remaining mic data
            threadID = -1;
            lastPos = 0;
            Microphone.End(null);
        }
        else if (Input.GetKey(pushToTalk) && threadID == -1)
        {
            // Too lazy to actually put this onto onjoin, so ez pz send that you have mic to everyone every time you use your mic
            RaiseEventOptions raised = new RaiseEventOptions();
            raised.Receivers = ReceiverGroup.Others;
            PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new float[0], false, raised);

            c = Microphone.Start(null, true, 100, (int)FREQUENCY);
            
            threadID = UnityEngine.Random.Range(0, Int32.MaxValue);
            new Thread(() =>
            {
                try
                {
                    int ID = threadID;
                    Thread.CurrentThread.IsBackground = true;
                    while (ID == threadID) // Just in case 2 instances of the thread is up, it'll stop the old one
                    {
                        sendMicData();
                        // Delay to make larger packets and less gaps(so less choppiness)
                        Thread.Sleep(300); // You can adjust it as you like to see what the best combo is for low latency + low choppiness
                    }
                }
                catch (Exception e)
                {
                    RPCList.add(e.Message); // Debugger
                }
            }).Start();
        }
    }

    // Removes player so that it doesn't send stuff unnecessarily
    public void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        if (users.ContainsKey(player.ID))
        {
            users.Remove(player.ID);
        }
    }

    // Sends Mic data
    private void sendMicData()
    {
        if (users.Count > 0)
        {
            int pos = Microphone.GetPosition(null);
            if (pos < lastPos) // If the microphone loops, the last sample needs to loop too
            {
                lastPos = 0;
            }
            int diff = pos - lastPos;
            if (diff > 4000) // Just so you don't send something too big, shouldn't happen unless you adjust the frequency too high
            {
                RPCList.add("Packet Too Big"); // Debugger
            }
            else if (diff > 0) // So it doesn't send an empty float[]
            {
                float[] samples = new float[diff];
                c.GetData(samples, lastPos);
                RaiseEventOptions raised = new RaiseEventOptions();
                int[] targets = new int[users.Count];
                users.Keys.CopyTo(targets, 0);
                PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, samples, false, raised);
            }
            lastPos = pos;
        }
    }
}

/*
 * Add to OnEvent in networking peer between some other events to make it simple to add
case 173:
try // In case there isn't a float[], quick and ez for lazy people to do
{
    float[] f = (float[])photonEvent[0xf5];

    // Identifier so they can add them to the list on join
    if (!MicEF.users.ContainsKey(sender.ID))
    {// I know that this will make the person who joined send 0 twice(one on entry one in return) but that doesn't really matter
        MicEF.users.Add(sender.ID, new MicPlayer(sender.ID));
        RaiseEventOptions raised = new RaiseEventOptions();
        raised.TargetActors = new int[] { sender.ID };
        PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new float[0], false, raised);
    }
    else if (f.Length > 0)
    {
        AudioClip clip = AudioClip.Create(UnityEngine.Random.Range(float.MinValue, float.MaxValue) + "", f.Length, 1, (int)MicEF.FREQUENCY - MicEF.DECREASE, true, false);
        clip.SetData(f, 0);
        MicEF.users[sender.ID].add(clip);
    }
}
catch (Exception e)
{
    RPCList.add(e.Message); // Debugger
}
return;
*/

// base.gameObject.AddComponent<MicEF>(); // in Fenggame start or wherever you want
// Also don't forget to add an exception for event 173 from your antis!!!