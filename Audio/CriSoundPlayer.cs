using System;
using CriWare;
using CriWare.Assets;

public class CriSoundPlayer : IDisposable
{
    public struct SimplePlayback
    {
        private readonly CriAtomExPlayer _player;
        private CriAtomExPlayback _playback;
        
        public void Pause() => _playback.Pause();
        public void Resume() => _playback.Resume(CriAtomEx.ResumeMode.PausedPlayback);
        public bool IsPaused() => _playback.IsPaused();
        public void Stop() => this._playback.Stop();
        public bool IsPlaying() => this._playback.GetStatus() == CriAtomExPlayback.Status.Playing;

        internal SimplePlayback(CriAtomExPlayer player, CriAtomExPlayback pb)
        {
            this._player = player;
            this._playback = pb;
        }
        
        public void SetAisacControl(string aisacControlName, float value)
        {
            this._player.SetAisacControl(aisacControlName, value);
            this._player.Update(_playback);
        }
        
        public void SetVolumeAndPitch(float vol, float pitch)
        {
            this._player.SetVolume(vol);
            this._player.SetPitch(pitch);
            this._player.Update(_playback);
        }
    }
    
    public SimplePlayback StartPlayback(CriAtomExAcb acb, string cueName, float vol = 1.0f, float pitch = 1.0f)
    {
        var player = new CriAtomExPlayer();
        player.SetCue(acb, cueName);
        player.SetVolume(vol);
        player.SetPitch(pitch);
        var pb = new SimplePlayback(player, player.Start());
        return pb;
    }
    
    public SimplePlayback StartPlayback(CriAtomCueReference r, float vol = 1.0f, float pitch = 1.0f)
    {
        var player = new CriAtomExPlayer();
        player.SetCue(r.AcbAsset.Handle, r.CueId);
        player.SetVolume(vol);
        player.SetPitch(pitch);
        var pb = new SimplePlayback(player, player.Start());
        return pb;
    }

    ~CriSoundPlayer()
    { 
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
