// Created By: Elite Future, Discord: Elite Future#1043 for questions, suggestions, or optimizations
// Compression/Decompression By: Sadico

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;

public class MicEF : MonoBehaviour
{
    private int lastPos;
    private AudioClip c;
    public static float FREQUENCY = 10000f;
    public static int DECREASE = 125;
    public static int threadID;
    public static Dictionary<int, MicPlayer> users;
    public static KeyCode pushToTalk = KeyCode.Z;
    public static List<int> muteList;
    private static int[] sendList;
    public static List<int> adjustableList;
    public static float volumeMultiplier = 1f;
    public static bool disconnected;
    public static bool autoConnect = true;
    public static string deviceName = "";

    public void Start()
    {
        if (PlayerPrefs.HasKey("pushToTalk"))
        {
            pushToTalk = (KeyCode)PlayerPrefs.GetInt("pushToTalk");
        }
        if (PlayerPrefs.HasKey("voiceAutoConnect"))
        {
            string str = PlayerPrefs.GetString("voiceAutoConnect");
            if (str.ToLower().StartsWith("f"))
            {
                autoConnect = false;
            }
        }
        if (PlayerPrefs.HasKey("volumeMultiplier"))
        {
            volumeMultiplier = PlayerPrefs.GetFloat("volumeMultiplier");
        }
        if (PlayerPrefs.HasKey("micDevice"))
        {
            deviceName = PlayerPrefs.GetString("micDevice");
        }
        disconnected = !autoConnect;
        sendList = new int[0];
        adjustableList = new List<int>();
        muteList = new List<int>();
        users = new Dictionary<int, MicPlayer>(); // int for ID
        DECREASE = (int)(FREQUENCY / 80f); // This is to slightly slow down the audio, not noticeable sound-wise, but removes some gaps
        threadID = -1;
        base.gameObject.AddComponent<MicGUI>();
    }

    // Resets when joining a room
    public void OnJoinedRoom()
    {
        threadID = -1;
        disconnected = !autoConnect;
        sendList = new int[0];
        adjustableList = new List<int>();
        muteList = new List<int>();
        users = new Dictionary<int, MicPlayer>();
        if (base.gameObject.GetComponent<MicGUI>() == null)
        {
            base.gameObject.AddComponent<MicGUI>();
        }
    }
    
    // Resets when leaving room
    public void OnLeftRoom()
    {
        threadID = -1;
        disconnected = true;
        sendList = new int[0];
        adjustableList = new List<int>();
        muteList = new List<int>();
        users = new Dictionary<int, MicPlayer>();
        if (base.gameObject.GetComponent<MicGUI>() == null)
        {
            base.gameObject.AddComponent<MicGUI>();
        }
    }

    // Adds a person to the voice sending list
    public static void addPerson(int addID)
    {
        if (!users.ContainsKey(addID))
        {
            users.Add(addID, new MicPlayer(addID));
            if (!adjustableList.Contains(addID))
            {
                adjustableList.Add(addID);
                recompileSendList();
            }
            RaiseEventOptions raised = new RaiseEventOptions();
            raised.TargetActors = new int[] { addID };
            PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[0], true, raised);
        }
    }

    // Recompiles list to an array so I don't need to do it whever it sends voice
    public static void recompileSendList()
    {
        sendList = new int[adjustableList.Count];
        adjustableList.CopyTo(sendList);
    }

    public void Update()
    {
        if (!disconnected)
        {
            if (Input.GetKeyUp(pushToTalk))
            {
                threadID = -1;
            }
            else if (Input.GetKey(pushToTalk) && threadID == -1)
            {
                // Too lazy to actually put this onto onjoin, so ez pz send that you have mic to everyone every time you use your mic
                RaiseEventOptions raised = new RaiseEventOptions();
                raised.Receivers = ReceiverGroup.Others;
                PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[0], true, raised);

                c = Microphone.Start(deviceName, true, 100, (int)FREQUENCY);

                threadID = UnityEngine.Random.Range(0, Int32.MaxValue);
                new Thread(() =>
                {
                    try
                    {
                        int ID = threadID;
                        Thread.CurrentThread.IsBackground = true;
                        while (ID == threadID && !disconnected) // Just in case 2 instances of the thread is up, it'll stop the old one
                        {
                            // Delay to make larger packets and less gaps(so less choppiness)
                            Thread.Sleep(300); // You can adjust it as you like to see what the best combo is for low latency + low choppiness

                            // Send after delay
                            sendMicData();
                            if (ID != threadID)
                            {
                                lastPos = 0;
                                Microphone.End(deviceName);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //RPCList.add(e.Message); // Debugger
                    }
                }).Start();
            }
        }
    }

    // Removes player so that it doesn't send stuff unnecessarily
    public void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        if (users.ContainsKey(player.ID))
        {
            users.Remove(player.ID);
            if (adjustableList.Contains(player.ID))
            {
                adjustableList.Remove(player.ID);
                recompileSendList();
            }
        }
    }

    // Used to show their name while they talk
    public void OnPhotonPlayerPropertiesChanged(PhotonPlayer player, ExitGames.Client.Photon.Hashtable hash)
    {
        if (hash.ContainsKey("name") && users.ContainsKey(player.ID))
        {
            users[player.ID].name = hash["name"].ToString().hexColor(); // converts [color] to <color=#color> using rc's because it's the most used, there are more efficient ones though
        }
    }

    // Sends Mic data
    private void sendMicData()
    {
        if (adjustableList.Count > 0)
        {
            int pos = Microphone.GetPosition(deviceName);
            if (pos < lastPos) // If the microphone loops, the last sample needs to loop too
            {
                lastPos = 0;
            }
            int diff = pos - lastPos;
            if (diff > 0) // So it doesn't send an empty float[]
            {
                float[] samples = new float[diff];
                c.GetData(samples, lastPos);
                RaiseEventOptions raised = new RaiseEventOptions();
                raised.TargetActors = sendList;
                byte[] bytes = GzipData(samples);
                if (bytes.Length < 12000)
                {
                    PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, bytes, false, raised);
                }
                else
                {
                    //RPCList.add("Packet too large: " + bytes.Length);
                }
            }
            lastPos = pos;
        }
    }

    // Sadico's Gzip Compression (but my comments)
    public static byte[] GzipData(float[] data)
    {
        if (data == null)
            return null;

        using (Stream memOutput = new MemoryStream())
        {
            using (GZipOutputStream zipOut = new GZipOutputStream(memOutput))
            {
                using (BinaryWriter writer = new BinaryWriter(zipOut))
                {
                    byte[] floatSerialization = new byte[data.Length * 4]; // Float uses 4 bytes (it's 32 bit)
                    Buffer.BlockCopy(data, 0, floatSerialization, 0, floatSerialization.Length); // Serializes the float[] to bytes

                    writer.Write(floatSerialization); // Writes the bytes to the stream

                    writer.Flush(); // Clears the buffer for memory cleanup
                    zipOut.Finish(); // Finishes the stream

                    // Stores the data into a field to return
                    byte[] bytes = new byte[memOutput.Length];
                    memOutput.Seek(0, SeekOrigin.Begin);
                    memOutput.Read(bytes, 0, bytes.Length);

                    return bytes;
                }
            }
        }
    }

    // Sadico's Gzip Decompression (but my comments)
    public static float[] unGzipData(byte[] bytes)
    {
        if (bytes == null)
            return null;

        using (Stream memInput = new MemoryStream(bytes))
        using (GZipInputStream zipInput = new GZipInputStream(memInput))
        using (MemoryStream reader = new MemoryStream())
        {
            byte[] buffer = new byte[256];
            int size;
            
            // Smh, I converted Sadico's while(true) to a do while
            do
            {
                size = zipInput.Read(buffer, 0, buffer.Length); // Reads the uncompressed data into the buffer
                reader.Write(buffer, 0, size); // Writes the bytes to the stream
            } while (size > 0);

            zipInput.Close();

            byte[] decompressed = reader.ToArray(); // Converts the stream to byte array
            float[] data = new float[decompressed.Length / 4]; // float uses 4 bytes (32 bits)
            Buffer.BlockCopy(decompressed, 0, data, 0, decompressed.Length); // Converts the decompressed bytes into the float[]
            return data;
        }
    }
}

/*
 * Add to OnEvent in networking peer between some other events to make it simple to add
case 173:
try // In case there isn't a float[], quick and ez for lazy people to do
{
    if (MicEF.disconnected)
    {
        return;
    }
    byte[] bytes = (byte[])photonEvent[0xf5];

    if (bytes.Length >= 12000) // Too large for a message
    {
        return;
    }
    else if (bytes.Length < 4) // 1 float requires at least 4 bytes
    {
        if (!MicEF.users.ContainsKey(sender.ID))
        {
            MicEF.addPerson(sender.ID);
        }

        if (bytes.Length == 1) // Commands
        {
            if (bytes[0] == (byte)254) // This person muted you
            {
                if (MicEF.adjustableList.Contains(sender.ID))
                {
                    MicEF.adjustableList.Remove(sender.ID);
                    MicEF.recompileSendList();
                    MicEF.users[sender.ID].mutedYou = true;
                }
            }
            else if (bytes[0] == (byte)255) // This person unmuted you
            {
                if (!MicEF.adjustableList.Contains(sender.ID))
                {
                    MicEF.adjustableList.Add(sender.ID);
                    MicEF.recompileSendList();
                    MicEF.users[sender.ID].mutedYou = false;
                }
            }
            else if (bytes[0] == (byte) 253) // This person disconnected from the voice
            {
                if (MicEF.users.ContainsKey(sender.ID))
                {
                    MicEF.users.Remove(sender.ID);
                    MicEF.adjustableList.Remove(sender.ID);
                    MicEF.recompileSendList();
                }
            }
        }
    }
    else
    {
        if (MicEF.muteList.Count > 0 && MicEF.muteList.Contains(sender.ID)) // in case they didn't remove you for some reason
        {
            RaiseEventOptions raised = new RaiseEventOptions();
            raised.TargetActors = new int[] { sender.ID };
            PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[] { (byte) 254 }, true, raised);
            return;
        }

        float[] f = MicEF.unGzipData(bytes);

        // Identifier so they can add them to the list on join
        if (!MicEF.users.ContainsKey(sender.ID))
        {// I know that this will make the person who joined send 0 twice(one on entry one in return) but that doesn't really matter
            MicEF.addPerson(sender.ID);
        }
        else if (f.Length > 0)
        {
            AudioClip clip = AudioClip.Create(UnityEngine.Random.Range(float.MinValue, float.MaxValue) + "", f.Length, 1, (int)MicEF.FREQUENCY - MicEF.DECREASE, true, false);
            clip.SetData(f, 0);
            if (clip.length > 0.9f) // Message is 3x larger than normal
            {
                return;
            }
            MicEF.users[sender.ID].add(clip);
        }
    }
}
catch (Exception e)
{
    //RPCList.add(e.Message); // Debugger
}
return;
*/

// base.gameObject.AddComponent<MicEF>(); // in Fenggame start or wherever you want
// Also don't forget to add an exception for event 173 from your antis!!!