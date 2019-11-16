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
    private bool changeVolume;
    private Rect micOptionsRect;
    private int changingKeys;
    private GUIStyle overlayStyle;
    private GUIStyle micStyle;
    private GUIStyle areaStyle;
    private GUIStyle buttonStyle;

    public void Start()
    {
        if (PlayerPrefs.HasKey("voiceKey"))
        {
            guiKey = (KeyCode)PlayerPrefs.GetInt("voiceKey");
        }

        changingKeys = -1;
        changeVolume = false;
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
        buttonBack.SetPixel(0, 0, new Color(0f, 0.2314f, 0.4588f));
        buttonBack.Apply();
        buttonStyle = new GUIStyle();
        buttonStyle.normal.background = buttonBack; // Normal color
        Texture2D buttonAct = new Texture2D(1, 1);
        buttonAct.SetPixel(0, 0, new Color(0f, 0.1843f, 0.3686f));
        buttonAct.Apply();
        buttonStyle.active.background = buttonAct; // Press Color
        buttonStyle.active.textColor = new Color(0.149f, 0.149f, 0.149f); // active text color
        Texture2D buttonHov = new Texture2D(1, 1);
        buttonHov.SetPixel(0, 0, new Color(0f, 0.2902f, 0.5725f));
        buttonHov.Apply();
        buttonStyle.hover.background = buttonHov; // Hover color
        buttonStyle.hover.textColor = new Color(0.149f, 0.149f, 0.149f); // hover text color
        buttonStyle.normal.textColor = Color.white; // normal text Color
        buttonStyle.alignment = TextAnchor.MiddleCenter; // Aligns text to center
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
        }
        else if (GUILayout.Button("Options", buttonStyle))
        {
            selection = 1;
        }
        else if (GUILayout.Button("Credits", buttonStyle))
        {
            selection = 2;
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
                    changeVolume = !changeVolume;
                }

                GUILayout.EndHorizontal();
                if (changeVolume)
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

            GUILayout.Label("Controls:");


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

            // Auto Connect
            GUILayout.BeginHorizontal();
            GUILayout.Label("Auto Conneect:");
            bool autoConnect = MicEF.autoConnect;
            MicEF.autoConnect = GUILayout.Toggle(autoConnect, "On");
            if (autoConnect != MicEF.autoConnect)
            {
                PlayerPrefs.SetString("voiceAutoConnect", MicEF.disconnected + "");
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

        GUILayout.EndArea();
        GUILayout.EndVertical();
        if ((!Input.GetKey(KeyCode.Mouse0) || !Input.GetKey(KeyCode.Mouse1)) && !Input.GetKey(KeyCode.C) && (IN_GAME_MAIN_CAMERA.cameraMode == CAMERA_TYPE.WOW || IN_GAME_MAIN_CAMERA.cameraMode == CAMERA_TYPE.ORIGINAL))
        {
            GUI.DragWindow();
        }
    }

    // Just to turn on the GUI, didn't work properly in OnGUI
    public void Update()
    {
        if (Input.GetKeyDown(guiKey) && PhotonNetwork.room != null)
        {
            guiOn = !guiOn;
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
                micRect = GUI.Window(1732, micRect, this.mainGUI, "", micStyle);
            }
        }
    }

    // Fixes the GUI sizes
    private void adjustRect()
    {
        overlayRect = new Rect(0, Screen.height / 2 - 100, 200, 200);
        float desiredWidth = Math.Min(Screen.width, 1920); // Theoretically makes 4k screens still look okay(may need to change some other things though)
        float desiredHeight = Math.Min(Screen.height, 1080);
        float width = Convert.ToSingle(desiredWidth / 2.4);
        float height = Convert.ToSingle(desiredHeight/ 2.4);
        if (micRect != null)
        {
            micRect.height = height;
            micRect.width = width;
        }
        else
        {
            micRect = new Rect(Screen.width - width, Screen.height - height, width, height);
        }
        micAreaRect = new Rect(10, height / 8, width - 20, height / 8 * 7 - 10);
        micOptionsRect = new Rect(10, 10, width - 20, height / 8);
        labelLength = micAreaRect.width / 8 * 6;
    }
}