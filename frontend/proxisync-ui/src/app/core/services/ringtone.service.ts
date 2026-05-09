import { Injectable } from '@angular/core';

type RingtoneType = 'incoming' | 'outgoing';

@Injectable({ providedIn: 'root' })
export class RingtoneService {
  private unlocked = false;

  private incomingAudio = new Audio('/sounds/proxisync_incoming_call.mp3');
  private outgoingAudio = new Audio('/sounds/proxisync_outgoing_call.mp3');

  constructor() {
    this.setupAudio(this.incomingAudio);
    this.setupAudio(this.outgoingAudio);
  }

  private setupAudio(audio: HTMLAudioElement) {
    audio.loop = true;
    audio.preload = 'auto';
    audio.volume = 1.0;
  }

  /** MUST be called once after user interaction (click/tap) */
  async unlockAudio() {
    if (this.unlocked) return;

    try {
      // tiny silent play to unlock autoplay policy
      const a = new Audio();
      a.src = '/sounds/proxisync_incoming_call.mp3';
      a.volume = 0;
      await a.play();
      a.pause();
      a.currentTime = 0;

      this.unlocked = true;
      console.log('[RINGTONE] Audio unlocked');
    } catch (err) {
      console.warn('[RINGTONE] Unlock failed (user interaction needed)', err);
      this.unlocked = false;
    }
  }

  async play(type: RingtoneType) {
    if (!this.unlocked) {
      console.warn('[RINGTONE] Not unlocked yet');
      return;
    }

    this.stop(); // stop any previous

    try {
      const audio = type === 'incoming' ? this.incomingAudio : this.outgoingAudio;
      if (audio.readyState < 2) { 
    audio.load(); 
  }
      audio.currentTime = 0;
      await audio.play();
      console.log('[RINGTONE] playing', type);
    } catch (err) {
      console.warn('[RINGTONE] play failed', err);
    }
  }

  stop() {
    [this.incomingAudio, this.outgoingAudio].forEach((a) => {
      try {
        a.pause();
        a.currentTime = 0;
      } catch {}
    });
  }
}