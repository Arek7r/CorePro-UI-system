// Drop this component anywhere in your Settings screen.
// Wire the public methods directly to UnityEngine.UI.Slider.onValueChanged
// or call them from your own settings panel script.
//
// Quick setup:
//   1. Add AudioVolumeSettings to your Settings Canvas / Panel GameObject.
//   2. In the Inspector, assign the four Slider references (or leave null
//      if you don't need that channel).
//   3. The sliders will be initialized from PlayerPrefs on Start automatically.
//   4. Wire Slider.onValueChanged -> AudioVolumeSettings.SetMasterVolume etc.,
//      OR call the Set* methods from your own code.
//
// All values are persisted to PlayerPrefs via CoreAudio immediately on change.

using UnityEngine;
using UnityEngine.UI;

namespace CorePro.AudioSystem
{
    /// <summary>
    /// Ready-to-use settings component that bridges UI Sliders with
    /// <see cref="CoreAudio"/> volume channels.
    /// Attach to your Settings screen and wire the sliders in the Inspector.
    /// </summary>
    [AddComponentMenu("CorePro/Audio Volume Settings")]
    public sealed class AudioVolumeSettings : MonoBehaviour
    {
        //  Optional Slider references 
        // Assign these in the Inspector. Each slider is initialized from
        // PlayerPrefs on Start and drives its volume channel on change.

        [Header("Sliders (assign in Inspector, all optional)")]
        [Tooltip("Slider that controls Master volume.")]
        [SerializeField] private Slider _masterSlider;

        [Tooltip("Slider that controls Music volume.")]
        [SerializeField] private Slider _musicSlider;

        [Tooltip("Slider that controls SFX volume.")]
        [SerializeField] private Slider _sfxSlider;

        [Tooltip("Slider that controls Voice / Dialogue volume.")]
        [SerializeField] private Slider _voiceSlider;

        //  Lifecycle 

        private void Start()
        {
            // Initialize sliders from saved PlayerPrefs values.
            // We unsubscribe first to avoid triggering Set* callbacks during init.
            InitSlider(_masterSlider, CoreAudio.MasterVolume, SetMasterVolume);
            InitSlider(_musicSlider,  CoreAudio.MusicVolume,  SetMusicVolume);
            InitSlider(_sfxSlider,    CoreAudio.SFXVolume,    SetSFXVolume);
            InitSlider(_voiceSlider,  CoreAudio.VoiceVolume,  SetVoiceVolume);
        }

        private void OnDestroy()
        {
            // Clean up listeners so there are no dangling references
            _masterSlider?.onValueChanged.RemoveListener(SetMasterVolume);
            _musicSlider ?.onValueChanged.RemoveListener(SetMusicVolume);
            _sfxSlider   ?.onValueChanged.RemoveListener(SetSFXVolume);
            _voiceSlider ?.onValueChanged.RemoveListener(SetVoiceVolume);
        }

        //  Public Set* methods - wire to Slider.onValueChanged 

        /// <summary>
        /// Sets Master volume (0–1) and persists to PlayerPrefs.
        /// Wire to <c>Slider.onValueChanged</c> or call from code.
        /// </summary>
        public void SetMasterVolume(float value) => CoreAudio.MasterVolume = value;

        /// <summary>
        /// Sets Music volume (0–1) and persists to PlayerPrefs.
        /// </summary>
        public void SetMusicVolume(float value)  => CoreAudio.MusicVolume  = value;

        /// <summary>
        /// Sets SFX volume (0–1) and persists to PlayerPrefs.
        /// </summary>
        public void SetSFXVolume(float value)    => CoreAudio.SFXVolume    = value;

        /// <summary>
        /// Sets Voice/Dialogue volume (0–1) and persists to PlayerPrefs.
        /// </summary>
        public void SetVoiceVolume(float value)  => CoreAudio.VoiceVolume  = value;

        //  Public Get* - useful for initializing custom UI from code 

        /// <summary>Returns the current Master volume (0–1).</summary>
        public float GetMasterVolume() => CoreAudio.MasterVolume;

        /// <summary>Returns the current Music volume (0–1).</summary>
        public float GetMusicVolume()  => CoreAudio.MusicVolume;

        /// <summary>Returns the current SFX volume (0–1).</summary>
        public float GetSFXVolume()    => CoreAudio.SFXVolume;

        /// <summary>Returns the current Voice volume (0–1).</summary>
        public float GetVoiceVolume()  => CoreAudio.VoiceVolume;

        //  Reset all to 1 

        /// <summary>
        /// Resets all volume channels to 1 and updates the sliders.
        /// Wire to a "Reset to defaults" button.
        /// </summary>
        public void ResetToDefaults()
        {
            CoreAudio.MasterVolume = 1f;
            CoreAudio.MusicVolume  = 1f;
            CoreAudio.SFXVolume    = 1f;
            CoreAudio.VoiceVolume  = 1f;

            SetSliderValue(_masterSlider, 1f);
            SetSliderValue(_musicSlider,  1f);
            SetSliderValue(_sfxSlider,    1f);
            SetSliderValue(_voiceSlider,  1f);
        }

        //  Helpers 

        private static void InitSlider(
            Slider slider,
            float  savedValue,
            UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null) return;

            // Set value without firing onValueChanged during init
            slider.onValueChanged.RemoveListener(callback);
            slider.SetValueWithoutNotify(Mathf.Clamp01(savedValue));
            slider.onValueChanged.AddListener(callback);
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider == null) return;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }
}