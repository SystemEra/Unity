/*
  Copyright 2015 System Era Softworks
 
 	Licensed under the Apache License, Version 2.0 (the "License");
 	you may not use this file except in compliance with the License.
 	You may obtain a copy of the License at
 
 		http://www.apache.org/licenses/LICENSE-2.0
 
 		Unless required by applicable law or agreed to in writing, software
 		distributed under the License is distributed on an "AS IS" BASIS,
 		WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 		See the License for the specific language governing permissions and
 		limitations under the License.

 
 */

/*-------------------------------------------------------------------------+
Activations that represent looping and one-time playing audio.

Audio in unity is kind of silly, and requires a Component to be added.
This is necessary to do initialization work for any audio that we know
will be played sometime, but it's a pain to have to add these components
to things that don't fit on GameObjects.  This allows us to have serialized
audio activations anywhere.
+-------------------------------------------------------------------------*/

using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Class that 
public class AudioSourceProperties
{
    public AudioClip Clip;
    public bool Loop;

    public override int GetHashCode()
    {
        return (Clip != null ? Clip.GetHashCode() : 0) ^ Loop.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        var aud = (obj as AudioSourceProperties);
        return aud != null && (aud.Clip == Clip && aud.Loop == Loop);
    }
}

public class GlobalAudio
{
	public Dictionary<AudioSourceProperties, AudioSource> AudioSources = new Dictionary<AudioSourceProperties, AudioSource>();
	public Dictionary<AudioSourceProperties, Ref<AudioFadeBehavior>> AudioFades = new Dictionary<AudioSourceProperties, Ref<AudioFadeBehavior>>();
}

// This dummy interface is for the inspector to allow it to be nullable
public interface IThreeDimensionalAudioSettings {}
public class ThreeDimensionalAudioSettings : IThreeDimensionalAudioSettings
{
	public float DopplerLevel = 0.0f;
	public float MaxDistance = 100.0f;
	public float PanLevel = 1.0f;
	public float Spread = 0.0f;
}

public abstract class ActivateAudioClipBase : ActivationCommand, IScope<BaseContextView>
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	// Globals
	[Inject] public MainContextView MainContextView { get; set; }
	[Inject] public GlobalAudio GlobalAudio { get; set; }

	// The owner view we are associated with
	[Inject] public IBaseView View { get; set; }
		
	// Audio behavior parameters
	public bool Loop = false;
	public IAudioFade FadeIn = null;
	public IAudioFade FadeOut = new AudioFade();
	public ParameterRangeReal Volume = new ParameterRangeReal();
	public ParameterRangeReal Pitch = new ParameterRangeReal();
	public bool AutoReplay = false;
	public bool Randomize = false;
	public IThreeDimensionalAudioSettings ThreeDimensional = null;

	// Expose an event to fire when the clip ends
	[HideInNodeEditor("Loop")]
	public IBinding<Action> OnClipEnd;

	// Internal state	
	protected float m_volume = 1.0f;

	// Initialize audio by finding a place to slap an AudioSource component with our clip
	protected void InitializeSource(out AudioSource source, out Ref<AudioFadeBehavior> fade, AudioClip clip)
	{
		// 3D is still TODO, this code is untested
		if (ThreeDimensional != null)
		{
			var threeDimensionalSettings = ThreeDimensional as ThreeDimensionalAudioSettings;
			source = gameObject.AddComponent<AudioSource>();
			fade = new Ref<AudioFadeBehavior>();
			source.clip = clip;
			source.loop = Loop;
			source.volume = 0.0f;
			source.rolloffMode = AudioRolloffMode.Custom;
			source.minDistance = 0.0f;
			source.maxDistance = threeDimensionalSettings.MaxDistance;
			source.dopplerLevel = threeDimensionalSettings.DopplerLevel;
		}
		else
		{
			// TODO: pooling with a max play count option.

			// Here we see if we already have a source and fade for this clip
			var audioProperties = new AudioSourceProperties() { Clip = clip, Loop = Loop };
			GlobalAudio.AudioSources.TryGetValue(audioProperties, out source);
			GlobalAudio.AudioFades.TryGetValue(audioProperties, out fade);

			// If we need to create an AudioSource component
			if (source == null)
			{
				source = MainContextView.gameObject.AddComponent<AudioSource>();
				GlobalAudio.AudioSources[audioProperties] = source;
				fade = GlobalAudio.AudioFades[audioProperties] = new Ref<AudioFadeBehavior>(null);

				// Oddly, the 3D attribute is marked on the clip import settings, which is insanity
				source.minDistance = source.maxDistance = float.MaxValue;
				source.dopplerLevel = 0.0f;
				source.clip = clip;
				source.loop = Loop;
				source.volume = 0.0f;
			}
		}
	}
	
	// Remove our component
	protected void DestroySource(AudioSource source, Ref<AudioFadeBehavior> fade)
	{
		if (ThreeDimensional != null)
		{
			Component.Destroy(source);
		}
		if (fade.Get() != null)
			Component.Destroy(fade.Get());
	}

	protected abstract bool IsPlaying();
	protected abstract AudioClip GetClip();
	protected abstract void DoPlay();

	protected void ActivateSource(AudioSource source, Ref<AudioFadeBehavior> fade)
	{
		if (fade.Get() != null)
		{
			fade.Get().WillManuallyDestroy = true;
			Component.Destroy(fade.Get());
		}

		if (source == null || source.clip == null)
			return;

		source.enabled = true;

		RandomizePitchDelay(source);
		m_volume = Volume.GetRandom();

		if (!IsPlaying() || !Loop)
		{
			source.Play();

			if (!Loop && OnClipEnd != null)
			{
				if (ThreeDimensional != null)
				{
					View.behavior.StartCoroutine(ClipEnd(source));
				}
				else
				{
					MainContextView.StartCoroutine(ClipEnd(source));
				}
			}
		}

		var fadein = FadeIn as AudioFade;
		if (fadein != null && fadein.Seconds > 0.0f)
		{
			fade.Set(MainContextView.gameObject.AddComponent<AudioFadeinBehavior>());
			fade.Get().Source = source;
			fade.Get().FalloffRate = 1.0f / fadein.Seconds;
			fade.Get().VolumeTarget = m_volume;
		}
		else
			source.volume = m_volume;
	}

	// Stop the clip playing and add fades if necessary
	protected void DeactivateSource(AudioSource source, Ref<AudioFadeBehavior> fade)
	{
		if (fade.Get() != null)
			Component.Destroy(fade.Get());

		if (FadeOut != null)
		{
			var fadeout = FadeOut as AudioFade;
			if (fadeout.Seconds > 0.0f)
			{
				fade.Set(MainContextView.gameObject.AddComponent<AudioFadeoutBehavior>());
				fade.Get().Source = source;
				fade.Get().FalloffRate = 1.0f / fadeout.Seconds;
				fade.Get().VolumeTarget = m_volume;
			}
			else if (source != null)
				source.Stop();
		}
		else if (Loop)
			source.loop = false; // Let it play to the end but don't restart
	}

	// Coroutine to fire when the clip is finished
	protected IEnumerator ClipEnd(AudioSource source)
	{
		yield return new WaitForSeconds((source.clip.length - source.time) / source.pitch);
		if (AutoReplay && IsActive())
			DoPlay();
		OnClipEnd.Get()();
	}

	// Randomize our parameters according to ranges
	private void RandomizePitchDelay(AudioSource source)
	{
		source.pitch = Pitch.GetRandom();
		if (Randomize)
		{
			float delay = UnityEngine.Random.Range(0.0f, source.clip.length);
			source.time = delay;
		}
	}

	// A code to allow us to preview this audio activation in-editor
#if UNITY_EDITOR
	private static AudioSource m_tempSource = null;

	[FullInspector.InspectorButton]
	public void Preview()
	{
		if (m_tempSource != null)
			m_tempSource.Stop();

		var tempObject = new GameObject();

		tempObject.hideFlags = HideFlags.HideAndDontSave;

		m_tempSource = tempObject.AddComponent<AudioSource>();
		m_tempSource.clip = GetClip();
		m_tempSource.volume = Volume.GetRandom();
		m_tempSource.minDistance = m_tempSource.maxDistance = float.MaxValue;

		RandomizePitchDelay(m_tempSource);

		m_tempSource.Play();
	}

	[FullInspector.InspectorButton]
	public void Stop()
	{
		if (m_tempSource != null)
			m_tempSource.Stop();
	}
#endif
}

// Activate single audio clip.  Binds to an Activation, which provides a start and a stop
// If looping, the deactivation will play to the end, otherwise it will cut/fade
[Name("Audio/Activate Clip")]
public class ActivateAudioClip : ActivateAudioClipBase
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }

	public AudioClip Clip;
	protected AudioSource m_source = null;
	protected Ref<AudioFadeBehavior> m_fade;

	public override string Title { get { return Loop ? "Loop Audio" : "Play Audio"; } }

	protected override bool IsPlaying()
	{
		return m_source.isPlaying;
	}

	protected override AudioClip GetClip()
	{
		return Clip;
	}

	public override void Create(BaseContext context)
	{
		base.Create(context);
		InitializeSource(out m_source, out m_fade, Clip);
	}

	protected override void DoPlay()
	{
		ActivateSource(m_source, m_fade);
	}

	protected override void OnActivated()
	{
		base.OnActivated();
		ActivateSource(m_source, m_fade);
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		DeactivateSource(m_source, m_fade);
	}

	public override void Destroy()
	{
		base.Destroy();
		DestroySource(m_source, m_fade);
	}
}

// Pick between many audio clips randomly.
[Name("Audio/Activate Randomized Clip")]
public class ActivateMultiAudioClip : ActivateAudioClipBase
{
	public override ScriptInfo ScriptInfo { get { return ScriptInfo.Get(); } }
	
	public List<AudioClip> Clips = new List<AudioClip>();

	protected AudioSource[] m_sources;
	protected Ref<AudioFadeBehavior>[] m_fades;

	public override string Title { get { return Loop ? "Loop Randomized Audio" : "Play Randomized Audio"; } }

	protected override bool IsPlaying()
	{
		return m_sources.Any(s => s.isPlaying);
	}

	protected override AudioClip GetClip()
	{
		if (Clips.Any())
		{
			int randomClip = UnityEngine.Random.Range(0, Clips.Count);
			return Clips[randomClip];
		}
		else
			return null;
	}

	public override void Create(BaseContext context)
	{
		base.Create(context);
		m_sources = new AudioSource[Clips.Count];
		m_fades = new Ref<AudioFadeBehavior>[Clips.Count];

		for (int i = 0; i < m_sources.Length; ++i)
		{
			InitializeSource(out m_sources[i], out m_fades[i], Clips[i]);
		}
	}

	private AudioSource m_currentSource = null;
	private Ref<AudioFadeBehavior> m_currentFade = null;
	protected override void OnActivated()
	{
		base.OnActivated();
		DoPlay();
	}

	protected override void DoPlay()
	{
		if (Clips.Any())
		{
			int randomClip = UnityEngine.Random.Range(0, m_sources.Length);
			m_currentSource = m_sources[randomClip];
			m_currentFade = m_fades[randomClip];
			ActivateSource(m_currentSource, m_currentFade);
		}
	}

	protected override void OnDeactivated()
	{
		base.OnDeactivated();
		if (m_currentSource != null)
		{
			DeactivateSource(m_currentSource, m_currentFade);

			m_currentSource = null;
			m_currentFade = null;
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		for (int i = 0; i < m_sources.Length; ++i)
			DestroySource(m_sources[i], m_fades[i]);
	}
}

// A type that the inspector will pick up and allow us to null out fades if the user doesn't want them
public interface IAudioFade { }

// Audio fade in and out, only one active at a time
public class AudioFade : IAudioFade
{
    public float Seconds = 0.5f;
}

// Components to adjust audio volume to do fade in/out
public abstract class AudioFadeBehavior : MonoBehaviour
{
    public AudioSource Source { get; set; }
    public float FalloffRate = 1.0f;
    public float VolumeTarget = 1.0f;
	public bool WillManuallyDestroy = false;
}

// Decrease volume to zero
public class AudioFadeoutBehavior : AudioFadeBehavior
{
    private void Update()
    {
        Source.volume -= FalloffRate * Time.deltaTime * VolumeTarget;
        if (Source.volume <= 0.0f)
        {
            Destroy(this);            
        }
    }

	private void OnDestroy()
	{
		if (!WillManuallyDestroy && Source != null)
			Source.Stop();
	}
}

// Increase volume to a target
public class AudioFadeinBehavior : AudioFadeBehavior
{
    private void Update()
    {
        Source.volume += FalloffRate * Time.deltaTime * VolumeTarget;
        if (Source.volume >= VolumeTarget)
        {
            Destroy(this);
        }
    }
}
