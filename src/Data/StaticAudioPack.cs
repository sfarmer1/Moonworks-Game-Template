using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonWorks.AsyncIO;
using MoonWorks.Audio;

namespace Tactician.Data;

[JsonSerializable(typeof(Dictionary<string, StaticAudioPackDataEntry>))]
internal partial class StaticAudioPackDictionaryContext : JsonSerializerContext {
}

public class StaticAudioPack : IDisposable {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        IncludeFields = true
    };

    private static readonly StaticAudioPackDictionaryContext SerializerContext = new(SerializerOptions);
    private readonly Dictionary<string, AudioBuffer> _audioBuffers = new();

    private Dictionary<string, StaticAudioPackDataEntry> _entries;
    private bool _isDisposed;
    public FileInfo AudioFile { get; private set; }

    public AudioBuffer MainBuffer { get; private set; }

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Init(AudioDevice audioDevice, string audioFilePath, string jsonFilePath) {
        AudioFile = new FileInfo(audioFilePath);
        _entries = JsonSerializer.Deserialize(
            File.ReadAllText(jsonFilePath),
            typeof(Dictionary<string, StaticAudioPackDataEntry>),
            SerializerContext) as Dictionary<string, StaticAudioPackDataEntry>;
        MainBuffer = AudioBuffer.Create(audioDevice);
    }

    public void LoadAsync(AsyncFileLoader loader) {
        loader.EnqueueWavLoad(AudioFile.FullName, MainBuffer);
    }

    /// <summary>
    ///     Call this after the audio buffer data is loaded.
    /// </summary>
    public void SliceBuffers() {
        foreach (var (name, dataEntry) in _entries)
            _audioBuffers[name] = MainBuffer.Slice(dataEntry.Start, (uint)dataEntry.Length);
    }

    public AudioBuffer GetAudioBuffer(string name) {
        return _audioBuffers[name];
    }

    protected virtual void Dispose(bool disposing) {
        if (!_isDisposed) {
            if (disposing) {
                foreach (var sound in _audioBuffers.Values) sound.Dispose();

                MainBuffer.Dispose();
            }

            _isDisposed = true;
        }
    }

    ~StaticAudioPack() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(false);
    }
}