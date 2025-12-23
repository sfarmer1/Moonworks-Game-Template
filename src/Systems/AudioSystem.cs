using System;
using MoonTools.ECS;
using MoonWorks.Audio;
using Tactician.Content;
using Tactician.Utility;
using Tactician.Data;
using Tactician.Messages;

namespace Tactician.Systems;

public class AudioSystem : MoonTools.ECS.System {
    private readonly AudioDevice _audioDevice;
    private readonly StreamingSoundID[] _gameplaySongs;
    private readonly PersistentVoice _musicVoice;
    private AudioDataQoa _music;

    public AudioSystem(World world, AudioDevice audioDevice) : base(world) {
        _audioDevice = audioDevice;

        _gameplaySongs = [
            StreamingAudio.attentiontwerkers,
            StreamingAudio.attention_shoppers_v1,
            StreamingAudio.attention_shoppers_v2
        ];

        var streamingAudioData = StreamingAudio.Lookup(StreamingAudio.attention_shoppers_v1);
        _musicVoice = _audioDevice.Obtain<PersistentVoice>(streamingAudioData.Format);
#if DEBUG
        _musicVoice.SetVolume(0.0f);
#else
        _musicVoice.SetVolume(0.5f);
#endif
    }

    public override void Update(TimeSpan delta) {
        foreach (var staticSoundMessage in ReadMessages<PlayStaticSoundMessage>())
            PlayStaticSound(
                staticSoundMessage.Sound,
                staticSoundMessage.Volume,
                staticSoundMessage.Pitch,
                staticSoundMessage.Pan,
                staticSoundMessage.Category
            );

        if (SomeMessage<PlaySongMessage>()) {
            _music = StreamingAudio.Lookup(_gameplaySongs.GetRandomItem());
            _music.Seek(0);
            _music.SendTo(_musicVoice);
            _musicVoice.Play();
        }
    }

    public void Cleanup() {
        _music.Disconnect();
        _musicVoice.Dispose();
    }

    private void PlayStaticSound(
        AudioBuffer sound,
        float volume,
        float pitch,
        float pan,
        SoundCategory soundCategory
    ) {
        var voice = _audioDevice.Obtain<TransientVoice>(sound.Format);

#if DEBUG
        voice.SetVolume(0.0f);
#else
        voice.SetVolume(0.5f);
#endif
        voice.SetPitch(pitch);
        voice.SetPan(pan);
        voice.Submit(sound);
        voice.Play();
    }
}