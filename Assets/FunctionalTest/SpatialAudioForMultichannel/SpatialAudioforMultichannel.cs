﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using agora_gaming_rtc;
using agora_utilities;
using System.Linq;

public class SpatialAudioforMultichannel : MonoBehaviour
{
    [SerializeField] private AppInfoObject appInfo;

    [SerializeField] private string CHANNEL_NAME_1 = "YOUR_CHANNEL_NAME_1";
    private IRtcEngine mRtcEngine = null;
    private const float Offset = 100;

    public Button joinButton, leaveButton;
    public bool joinedChannel = false;

    public double azimuth = 0f;
    public double elevation = 0f;
    public double distance = 1f;
    public int orientation = 0;
    public double attenuation = 0f;

    public bool spatialBlur = false;
    public bool spatialAirAbsorb = false;

    public Slider azimuthSlider, elevationSlider, distanceSlider,
    orientationSlider, attenuationSlider;

    public Toggle blurToggle, airAbsorbToggle, enableToggle;

    public Text azimuthText, elevationText,
    distanceText, orientationText, attenuationText;

    public InputField appIdText, tokenText, channelNameText;
    public Dropdown userDropdown;

    AgoraChannel spatialAudioChannel;
    ILocalSpatialAudioEngine spatialAudio;

    [SerializeField]
    private List<uint> remoteClientIDs;

    Dictionary<uint, EffectParams> UserParams = new Dictionary<uint, EffectParams>();

    private void Awake()
    {
        if (RootMenuControl.instance)
        {
            CHANNEL_NAME_1 = RootMenuControl.instance.channel;
        }
        UpdateDropDown();
    }

    // Use this for initialization
    void Start()
    {
        if (!CheckAppId())
        {
            return;
        }

        InitEngine();

        //channel setup.
        appIdText.text = appInfo.appID;
        tokenText.text = appInfo.token;
        channelNameText.text = CHANNEL_NAME_1;
        remoteClientIDs = new List<uint>();
        userDropdown.onValueChanged.AddListener(OnDropDownSelect);
    }

    public void updateAppID()
    {
        appInfo.appID = appIdText.text;
    }

    public void updateToken()
    {
        appInfo.token = tokenText.text;
    }

    public void updateChannelName()
    {
        CHANNEL_NAME_1 = channelNameText.text;
    }

    void Update()
    {
        PermissionHelper.RequestMicrophontPermission();

        if (joinedChannel)
        {

            joinButton.interactable = false;
            leaveButton.interactable = true;
            appIdText.interactable = false;
            tokenText.interactable = false;
            channelNameText.interactable = false;

            if (remoteClientIDs.Count > 0)
            {
                azimuthSlider.interactable = true;
                elevationSlider.interactable = true;
                distanceSlider.interactable = true;
                orientationSlider.interactable = true;
                attenuationSlider.interactable = true;
                blurToggle.interactable = true;
                airAbsorbToggle.interactable = true;
                userDropdown.interactable = true;
            }
        }
        else
        {

            joinButton.interactable = true;
            leaveButton.interactable = false;
            appIdText.interactable = true;
            tokenText.interactable = true;
            channelNameText.interactable = true;

            azimuthSlider.interactable = false;
            elevationSlider.interactable = false;
            distanceSlider.interactable = false;
            orientationSlider.interactable = false;
            attenuationSlider.interactable = false;
            blurToggle.interactable = false;
            airAbsorbToggle.interactable = false;
            userDropdown.interactable = false;
        }

        azimuthText.text = azimuthSlider.value.ToString("F2");
        elevationText.text = elevationSlider.value.ToString("F2");
        distanceText.text = distanceSlider.value.ToString("F2");
        orientationText.text = orientationSlider.value.ToString();
        attenuationText.text = attenuationSlider.value.ToString("F2");
    }

    bool CheckAppId()
    {
        return (appInfo.appID.Length > 10);
    }

    void InitEngine()
    {
        mRtcEngine = IRtcEngine.GetEngine(appInfo.appID);
        mRtcEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        
        mRtcEngine.EnableAudio();
        
        mRtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        mRtcEngine.SetMultiChannelWant(true);

        spatialAudioChannel = mRtcEngine.CreateChannel(CHANNEL_NAME_1);
        spatialAudioChannel.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        spatialAudio = mRtcEngine.GetLocalSpatialAudioEngine();
        spatialAudio.Initialize_MC();

        spatialAudioChannel.ChannelOnJoinChannelSuccess += OnJoinChannelSuccessHandler;
        spatialAudioChannel.ChannelOnUserJoined += OnUserJoinedHandler;
        spatialAudioChannel.ChannelOnUserOffLine += OnUserLeaveHandler;
    }

    void OnJoinChannelSuccessHandler(string channelId, uint uid, int elapsed)
    {

    }

    void OnUserJoinedHandler(string channelId, uint uid, int elapsed)
    {
        remoteClientIDs.Add(uid);
        UpdateDropDown();
    }

    void OnUserLeaveHandler(string channelId, uint uid, USER_OFFLINE_REASON reason)
    {
        remoteClientIDs.Remove(uid);
        UpdateDropDown();
    }

    void UpdateDropDown()
    {
        userDropdown.ClearOptions();
        userDropdown.AddOptions(remoteClientIDs.Select(c => new Dropdown.OptionData(c.ToString())).ToList());
        userDropdown.value = 0;
    }

    public void JoinChannel()
    {
        spatialAudioChannel.JoinChannel(appInfo.token, "", 0, new ChannelMediaOptions(true, false, true, false));
        joinedChannel = true;
    }

    public void LeaveChannel()
    {

        spatialAudioChannel.LeaveChannel();
        joinedChannel = false;

        azimuthSlider.value = 0f;
        elevationSlider.value = 0f;
        distanceSlider.value = 1f;
        orientationSlider.value = 0f;
        attenuationSlider.value = .5f;
        blurToggle.isOn = false;
        airAbsorbToggle.isOn = false;
    }

    int prevDropSelect = 0;
    void OnDropDownSelect(int v)
    {
        Debug.Log(v.ToString());
        if (v != prevDropSelect)
        {
            var uid = remoteClientIDs[prevDropSelect];
            UserParams[uid] = new EffectParams
            {
                attenuation = attenuation,
                azimuth = azimuth,
                distance = distance,
                elevation = elevation,
                orientation = orientation,
                blur = spatialBlur,
                airabsort = spatialAirAbsorb,
            };

            // switch to another user
            uid = remoteClientIDs[v];
            if (!UserParams.ContainsKey(uid))
            {
                UserParams[uid] = new EffectParams();
            }
            ResetEffectOptions(UserParams[uid]);
            prevDropSelect = v;
        }
    }

    void ResetEffectOptions(EffectParams p)
    {
        azimuthSlider.value = (float)p.azimuth;
        elevationSlider.value = (float)p.elevation;
        distanceSlider.value = (float)p.distance;
        orientationSlider.value = p.orientation;
        attenuationSlider.value = (float)p.attenuation;
        blurToggle.isOn = p.blur;
        airAbsorbToggle.isOn = p.airabsort;
    }

    void OnDestroy()
    {
        Debug.Log("OnApplicationQuit");
        if (mRtcEngine != null)
        {
            LeaveChannel();
            mRtcEngine.DisableVideoObserver();
            IRtcEngine.Destroy();
        }
    }

    public void updateAzimuth()
    {
        azimuth = azimuthSlider.value;
        updateSpatialAudio();
    }

    public void updateElevation()
    {
        elevation = elevationSlider.value;
        updateSpatialAudio();
    }

    public void updateDistance()
    {
        distance = distanceSlider.value;
        updateSpatialAudio();
    }

    public void updateOrientation()
    {
        orientation = (int)orientationSlider.value;
        updateSpatialAudio();
    }

    public void updateAttenuation()
    {
        attenuation = attenuationSlider.value;
        updateSpatialAudio();
    }

    public void updateBlur()
    {
        spatialBlur = blurToggle.isOn;
        updateSpatialAudio();
    }

    public void updateAirAbsorb()
    {
        spatialAirAbsorb = airAbsorbToggle.isOn;
        updateSpatialAudio();
    }

    public void updateSpatialAudio()
    {
        Debug.Log("Updating spatial effect for uid:" + remoteClientIDs[userDropdown.value]);
        if (remoteClientIDs.Count > 0 && enableToggle.isOn)
        {
            Debug.Log("Working");
            spatialAudioChannel.SetRemoteUserSpatialAudioParams(remoteClientIDs[userDropdown.value], azimuth, elevation, distance, orientation, attenuation, spatialBlur, spatialAirAbsorb);
        }
    }

    internal class EffectParams
    {
        public double azimuth = 0;
        public double attenuation = .5f;
        public double distance = 0;
        public double elevation = 0;
        public int orientation = 0;
        public bool blur = false;
        public bool airabsort = false;
    }

}