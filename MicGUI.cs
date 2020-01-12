// Created By: Elite Future, Discord: Elite Future#1043 for questions, suggestions, or optimizations

using System;
using System.Collections.Generic;
using UnityEngine;

public class MicGUI : UnityEngine.MonoBehaviour
{
    private Rect micRect;
    private Rect overlayRect;
    private Vector2 vSliderValue;
    private Vector2 controlSlider;
    private float appHeight;
    private int selection;
    private bool guiOn;
    private KeyCode guiKey = KeyCode.Backslash;
    private Rect micAreaRect;
    private float labelLength;
    private Rect micOptionsRect;
    private int changingKeys;
    private GUIStyle overlayStyle;
    private GUIStyle micStyle;
    private GUIStyle areaStyle;
    private GUIStyle buttonStyle;
    private Color buttonGUIColor = new Color(0f, 0.2314f, 0.4588f);
    private bool dropDown;
    private Vector2 clickPos;
    private Rect deviceRect;

    public void Start()
    {
        dropDown = false;
        if (PlayerPrefs.HasKey("voiceKey"))
        {
            guiKey = (KeyCode)PlayerPrefs.GetInt("voiceKey");
        }

        changingKeys = -1;
        selection = 0;
        guiOn = false;
        appHeight = Screen.height;
        adjustRect();
        overlayStyle = new GUIStyle();

        Texture2D defaultBackground = new Texture2D(1, 1);
        defaultBackground.SetPixel(0, 0, new Color(0.1569f, 0.1569f, 0.1569f));
        defaultBackground.Apply();
        micStyle = new GUIStyle();
        micStyle.normal.background = defaultBackground; // mic GUI color

        Texture2D areaBack = new Texture2D(1, 1);
        areaBack.SetPixel(0, 0, new Color(0.1961f, 0.1961f, 0.1961f));
        areaBack.Apply();
        areaStyle = new GUIStyle();
        areaStyle.normal.background = areaBack; // inner area GUI color
        
        Texture2D buttonBack = new Texture2D(1, 1);
        buttonBack.SetPixel(0, 0, buttonGUIColor);
        buttonBack.Apply();
        buttonStyle = new GUIStyle();
        buttonStyle.normal.background = buttonBack; // Normal button color
        Texture2D buttonAct = new Texture2D(1, 1);
        buttonAct.SetPixel(0, 0, adjustColor(buttonGUIColor, 0.75f)); // 25% darker
        buttonAct.Apply();
        buttonStyle.active.background = buttonAct; // Press button Color
        buttonStyle.active.textColor = new Color(0.149f, 0.149f, 0.149f); // active text color
        Texture2D buttonHov = new Texture2D(1, 1);
        buttonHov.SetPixel(0, 0, adjustColor(buttonGUIColor, 1.25f)); // 25% brighter
        buttonHov.Apply();
        buttonStyle.hover.background = buttonHov; // Hover button color
        buttonStyle.hover.textColor = new Color(0.149f, 0.149f, 0.149f); // hover text color
        buttonStyle.normal.textColor = Color.white; // normal text Color
        buttonStyle.alignment = TextAnchor.MiddleCenter; // Aligns text to center
        buttonStyle.wordWrap = true;
    }

    public static Color adjustColor(Color col, float adjustment)
    {
        float red = col.r * adjustment;
        float green = col.g * adjustment;
        float blue = col.b * adjustment;
        return new Color(red, green, blue);
    }

    // Transparent overlay GUI to show who is talking
    public void overlayGUI(int ID)
    {
        GUILayout.BeginVertical();
        if (MicEF.threadID != -1) // This sees if your mic is on
        {
            GUILayout.Label("<b>(" + PhotonNetwork.player.ID + ") </b>" + PhotonNetwork.player.customProperties["name"].ToString().hexColor());
        }
        foreach (KeyValuePair<int, MicPlayer> entry in MicEF.users)
        {
            MicPlayer player = entry.Value;
            if (player.clipProcess)
            {
                GUILayout.Label("<b>(" + entry.Key  + ") </b>" + entry.Value.name);
            }
        }
        GUILayout.EndVertical();
    }

    public void mainGUI(int ID)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginArea(micOptionsRect);
        GUILayout.BeginHorizontal();

        // Button Options
        if (GUILayout.Button("User List", buttonStyle))
        {
            selection = 0;
            dropDown = false;
        }
        else if (GUILayout.Button("Options", buttonStyle))
        {
            selection = 1;
            dropDown = false;
        }
        else if (GUILayout.Button("Credits", buttonStyle))
        {
            selection = 2;
            dropDown = false;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUILayout.BeginArea(micAreaRect, areaStyle);
        if (selection == 0) // Main User List
        {
            vSliderValue = GUILayout.BeginScrollView(vSliderValue);
            foreach (KeyValuePair<int, MicPlayer> entry in MicEF.users)
            {
                MicPlayer player = entry.Value;

                GUILayout.BeginHorizontal();

                GUILayout.Label(player.name, GUILayout.Width(labelLength));
                Color oldCol = buttonStyle.normal.textColor;

                if (player.mutedYou)
                {
                    buttonStyle.normal.textColor = Color.yellow;
                }
                else if (!player.isMuted)
                {
                    buttonStyle.normal.textColor = Color.green;
                }
                else
                {
                    buttonStyle.normal.textColor = Color.red;
                }

                if (GUILayout.Button("M", buttonStyle)) // Speaker Icon
                {
                    player.mute(!player.isMuted);
                }

                buttonStyle.normal.textColor = oldCol;

                if (GUILayout.Button("V", buttonStyle)) // Volume
                {
                    player.changingVolume = !player.changingVolume;
                }

                GUILayout.EndHorizontal();
                if (player.changingVolume)
                {
                    player.volume = GUILayout.HorizontalSlider(player.volume, 0f, 4f, new GUILayoutOption[0]);
                    if (!player.isMuted && player.volume == 0f)
                    {
                        player.mute(true);
                    }
                    else if (player.isMuted)
                    {
                        player.mute(false);
                    }
                }
            }
            GUILayout.EndScrollView();
        }
        else if (selection == 1)
        {
            controlSlider = GUILayout.BeginScrollView(controlSlider);
            GUILayout.BeginVertical();

            // Voice Assignment
            GUILayout.BeginHorizontal();
            GUILayout.Label("Push To talk:");
            string buttonText = MicEF.pushToTalk.ToString();
            if (changingKeys == 0)
            {
                buttonText = "Waiting...";
                for (int i = 1; i <= 429; i++)
                {
                    KeyCode code = (KeyCode)(i);
                    if (Input.GetKeyDown(code))
                    {
                        MicEF.pushToTalk = code;
                        changingKeys = -1;
                        PlayerPrefs.SetInt("pushToTalk", (int)code);
                    }
                }
            }
            if (GUILayout.Button(buttonText, buttonStyle))
            {
                if (changingKeys == -1)
                {
                    changingKeys = 0;
                }
            }
            GUILayout.EndHorizontal();


            // GUI Assignment
            GUILayout.BeginHorizontal();
            GUILayout.Label("Voice GUI Key:");
            buttonText = guiKey.ToString();
            if (changingKeys == 1)
            {
                buttonText = "Waiting...";
                for (int i = 1; i <= 429; i++)
                {
                    KeyCode code = (KeyCode)(i);
                    if (Input.GetKeyDown(code))
                    {
                        guiKey = code;
                        changingKeys = -1;
                        PlayerPrefs.SetInt("voiceKey", (int)code);
                    }
                }
            }
            if (GUILayout.Button(buttonText, buttonStyle))
            {
                if (changingKeys == -1)
                {
                    changingKeys = 1;
                }
            }
            GUILayout.EndHorizontal();


            // Volume
            GUILayout.Label("Volume Multiplier: " + MicEF.volumeMultiplier);
            float oldVol = MicEF.volumeMultiplier;
            MicEF.volumeMultiplier = GUILayout.HorizontalSlider(MicEF.volumeMultiplier, 0f, 3f, new GUILayoutOption[0]);
            if (oldVol != MicEF.volumeMultiplier)
            {
                PlayerPrefs.SetFloat("volumeMultiplier", MicEF.volumeMultiplier);
            }


            // Device Name
            GUILayout.BeginHorizontal();

            GUILayout.Label("Microphone: ");

            string micButtonText = "Default";
            if (MicEF.deviceName.Length > 0)
            {
                micButtonText = MicEF.deviceName;
                if (micButtonText.StartsWith("Microphone ("))
                {
                    micButtonText = micButtonText.Remove(0, 12);
                    micButtonText = micButtonText.Substring(0, micButtonText.Length - 1);
                }
            }
            
            if (GUILayout.Button(micButtonText, buttonStyle))
            {
                clickPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                deviceRect = new Rect(clickPos.x - micAreaRect.width / 5f, clickPos.y + 5, micAreaRect.width / 2.5f, micAreaRect.height);
                dropDown = !dropDown;
            }
            
            GUILayout.EndHorizontal();


            //Auto Mute People On Join
            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Mute People On Join:");
            bool autoMute = MicEF.autoMute;
            MicEF.autoMute = GUILayout.Toggle(autoMute, "On");
            if (autoMute != MicEF.autoMute)
            {
                PlayerPrefs.SetString("voiceAutoMute", MicEF.autoMute + "");
            }
            GUILayout.EndHorizontal();


            // Auto Connect
            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Connect:");
            bool autoConnect = MicEF.autoConnect;
            MicEF.autoConnect = GUILayout.Toggle(autoConnect, "On");
            if (autoConnect != MicEF.autoConnect)
            {
                PlayerPrefs.SetString("voiceAutoConnect", MicEF.autoConnect + "");
            }
            GUILayout.EndHorizontal();


            // Toggle Mic
            GUILayout.BeginHorizontal();
            GUILayout.Label("Toggle Mic:");
            bool toggleMic = MicEF.toggleMic;
            MicEF.toggleMic = GUILayout.Toggle(toggleMic, "On");
            if (toggleMic != MicEF.toggleMic)
            {
                PlayerPrefs.SetString("voiceToggleMic", MicEF.toggleMic + "");
            }
            GUILayout.EndHorizontal();


            // Disconnect Button
            buttonText = "Disconnect From Voice";
            if (MicEF.disconnected)
            {
                buttonText = "Connect To Voice";
            }

            Color oldCol = buttonStyle.normal.textColor;

            if (MicEF.disconnected)
            {
                buttonStyle.normal.textColor = Color.green;
            }
            else
            {
                buttonStyle.normal.textColor = Color.red;
            }

            if (GUILayout.Button(buttonText, buttonStyle))
            {
                if (!MicEF.disconnected)
                {
                    RaiseEventOptions raised = new RaiseEventOptions();
                    raised.Receivers = ReceiverGroup.Others;
                    PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[] { (byte)253 }, true, raised);
                    MicEF.disconnected = true;
                }
                else
                {
                    RaiseEventOptions raised = new RaiseEventOptions();
                    raised.Receivers = ReceiverGroup.Others;
                    PhotonNetwork.networkingPeer.OpRaiseEvent((byte)173, new byte[0], true, raised);
                    MicEF.disconnected = false;
                }
            }

            buttonStyle.normal.textColor = oldCol;

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
        else if (selection == 2) // Credits, Please keep this in as I worked hard on this
        {
            GUILayout.Label("Main Developer: Elite Future(Kevin) - Discord:Elite Future#1043");
            GUILayout.Label("Data Compression: Sadico");
        }

        GUILayout.EndArea();
        GUILayout.EndVertical();
        if ((!Input.GetKey(KeyCode.Mouse0) || !Input.GetKey(KeyCode.Mouse1)) && !Input.GetKey(KeyCode.C) && (IN_GAME_MAIN_CAMERA.cameraMode == CAMERA_TYPE.WOW || IN_GAME_MAIN_CAMERA.cameraMode == CAMERA_TYPE.ORIGINAL))
        {
            GUI.DragWindow();
        }
    }

    // GUI to allow the user to change microphones
    public void deviceList(int ID)
    {// Maybe add a scroll view later
        float butHeight = micAreaRect.height / 10;
        GUILayout.BeginVertical();

        foreach (string str in Microphone.devices)
        {
            string butText = str.Remove(0, 12);
            butText = butText.Substring(0, butText.Length - 1);
            float height = micAreaRect.height / 12;
            if (GUILayout.Button(butText, buttonStyle))
            {
                MicEF.deviceName = str;
                dropDown = false;
                PlayerPrefs.SetString("micDevice", str);
            }
        }
        GUILayout.EndVertical();
    }

    // Just to turn on the GUI, didn't work properly in OnGUI
    public void Update()
    {
        if (Input.GetKeyDown(guiKey) && PhotonNetwork.room != null)
        {
            guiOn = !guiOn;
            dropDown = false;
        }
    }
    
    // Calls all GUIs
    public void OnGUI()
    {
        if (PhotonNetwork.room != null)
        {
            if (Screen.height != appHeight)
            {
                appHeight = Screen.height;
                adjustRect();
            }
            if (MicEF.users.Count > 0 || MicEF.threadID != -1)
            {
                overlayRect = GUI.Window(1731, overlayRect, this.overlayGUI, "", overlayStyle);
            }
            if (guiOn)
            {
                if (dropDown)
                {
                    deviceRect = GUI.Window(1733, deviceRect, this.deviceList, "", overlayStyle);
                }
                micRect = GUI.Window(1732, micRect, this.mainGUI, "", micStyle);
            }
        }
    }

    // Fixes the GUI sizes
    private void adjustRect()
    {
        float desiredWidth = 1920; // Theoretically makes 4k screens still look okay(may need to change some other things though)
        float desiredHeight = 1080;

        if (Screen.width > 1920)
        {
            desiredWidth = Screen.width;
        }
        if (Screen.height > 1080)
        {
            desiredHeight = Screen.height;
        }
        float width = Convert.ToSingle(desiredWidth / 4.2);
        float height = Convert.ToSingle(desiredHeight / 4.2);

        micRect = new Rect(Screen.width - width, Screen.height - height, width, height);
        micAreaRect = new Rect(10, height / 8, width - 20, height / 8 * 7 - 10);
        micOptionsRect = new Rect(10, 10, width - 20, height / 8);
        labelLength = micAreaRect.width / 8 * 6;
    }
}