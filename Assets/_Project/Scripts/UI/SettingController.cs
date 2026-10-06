using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Toggle _windowedToggle;

    private Resolution[] _resolutions;

    private void Start()
    {
        SetUpResolutionDropdown();
        SetUpScreenModeToggles();
    }

    private void SetUpResolutionDropdown()
    {
        _resolutions = Screen.resolutions;

        _resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option =
                $"{_resolutions[i].width} X {_resolutions[i].height}";

            options.Add(option);

            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        _resolutionDropdown.AddOptions(options);
        _resolutionDropdown.value = currentResolutionIndex;
        _resolutionDropdown.RefreshShownValue();
    }

    private void SetUpScreenModeToggles()
    {
        bool isFullscreen = Screen.fullScreen;

        _fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        _windowedToggle.SetIsOnWithoutNotify(!isFullscreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = _resolutions[resolutionIndex];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode
        );
    }

    public void SetFullscreen(bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        _windowedToggle.SetIsOnWithoutNotify(false);
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }

    public void SetWindowed(bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        _fullscreenToggle.SetIsOnWithoutNotify(false);
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }
}