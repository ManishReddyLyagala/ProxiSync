import { Component, computed, effect, ElementRef, HostListener, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CallStateService } from '../../../../core/services/call-state.service';
import { Room, RoomEvent, Track } from 'livekit-client';
import { CallVideoGridComponent } from './call-video-grid.component';

@Component({
  selector: 'app-call-incall',
  standalone: true,
  imports: [CommonModule, CallVideoGridComponent],
  templateUrl: './call-incall.component.html',
})
export class CallIncallComponent {
  private callState = inject(CallStateService);
  @ViewChild('videoGrid') videoGrid!: CallVideoGridComponent;
  @ViewChild('miniControlPanel') miniPanel!: ElementRef;
 
  uiState = this.callState.uiState;
  call = this.callState.activeCall;
  room = this.callState.room;
  
  micEnabled = this.callState.micEnabled;
  videoEnabled = this.callState.videoEnabled;
  showParticipantsModal = signal<boolean>(false);
  remoteParticipants = signal<string[]>([]);
  showControls = signal(true);
  isFloating = signal(false);
  private hideTimer: any;
  private pipWindow: any = null;
  activeSpeakerName = signal('Connecting...');

  constructor() {
    effect(() => {
      const room = this.callState.room();
      if (!room) return;

      this.updateParticipants(room);
      const syncSharing = () => {
        // This dummy update triggers the signal refresh if needed
        this.remoteParticipants.update(ids => [...ids]); 
      };

      const speakerId = this.videoGrid?.activeSpeakerId();
      if (this.isFloating() && this.pipWindow) {
        this.updatePipVideo(speakerId);
      }

      room.on(RoomEvent.ParticipantConnected, () => this.updateParticipants(room));
      room.on(RoomEvent.ParticipantDisconnected, () => this.updateParticipants(room));
      room.on(RoomEvent.TrackSubscribed, () => syncSharing);
      room.on(RoomEvent.TrackUnsubscribed, () => syncSharing);
      room.on(RoomEvent.LocalTrackPublished, () => syncSharing);
      room.on(RoomEvent.LocalTrackUnpublished, () => syncSharing);
    });
  }

  private updateParticipants(room: Room) {
    const ids = Array.from(room.remoteParticipants.values()).map((p) => p.identity);
    this.remoteParticipants.set(ids);
  }

  async toggleScreenShare() {
    const room = this.room();
    if (!room) return;
    try {
      const enabled = room.localParticipant.isScreenShareEnabled;
      await room.localParticipant.setScreenShareEnabled(!enabled);
    } catch (e) {
      console.error("Screen share failed", e);
    }
  }

  @HostListener('window:mousemove')
  @HostListener('window:mousedown')
  @HostListener('window:touchstart')
  resetTimer() {
    this.showControls.set(true);
    // Clear existing timer
    if (this.hideTimer) clearTimeout(this.hideTimer);
    // Set new timer: Hide after 3 seconds of inactivity
    if (window.innerWidth > 1024) {
    this.hideTimer = setTimeout(() => {
      // Only auto-hide if someone is screen sharing or pinning (Main content is active)
      if (this.room()?.localParticipant.isScreenShareEnabled || !!this.videoGrid?.pinnedTileId() ) {
         this.showControls.set(false);
      }
    }, 3000);
  }
  }

  async toggleFloatingPanel() {
    // Check if browser supports Document PiP
    if (!('documentPictureInPicture' in window)) {
      alert("Browser doesn't support floating panels. Please use Chrome or Edge.");
      return;
    }

    if (this.isFloating()) {
      this.pipWindow?.close();
      return;
    }

    try {
      // 1. Open the floating window
      const pipWindow = await (window as any).documentPictureInPicture.requestWindow({
        width: 320,
        height: 450,
      });

      // 2. Move your styles to the new window so it looks correct
      [...document.styleSheets].forEach((styleSheet) => {
        try {
          const cssRules = [...styleSheet.cssRules].map((rule) => rule.cssText).join('');
          const style = document.createElement('style');
          style.textContent = cssRules;
          pipWindow.document.head.appendChild(style);
        } catch (e) {
          const link = document.createElement('link');
          link.rel = 'stylesheet';
          link.href = (styleSheet as any).href;
          pipWindow.document.head.appendChild(link);
        }
      });

      // 3. Move your control panel into the PiP window
      const container = this.miniPanel.nativeElement;
      container.classList.remove('hidden');
      pipWindow.document.body.appendChild(container);
      this.isFloating.set(true);
// 3. Initial Video Attach
    setTimeout(() => this.updatePipVideo(this.videoGrid?.activeSpeakerId()), 300);
      // 4. Handle closing
      pipWindow.addEventListener("pagehide", () => {
        this.isFloating.set(false);
        container.classList.add('hidden');
        // Move the controls back to the main page
        document.body.appendChild(container);
        this.pipWindow = null
      });

    } catch (err) {
      console.error("Failed to open floating panel:", err);
    }
  }

private updatePipVideo(speakerId: string | null) {
    if (!this.pipWindow) return;

    const pipVideo = this.pipWindow.document.getElementById('pip-active-video') as HTMLVideoElement;
    if (!pipVideo) return;

    // Find the track for the active speaker
    const participant = speakerId ? this.room()?.getParticipantByIdentity(speakerId) : this.room()?.localParticipant;
    const track = participant?.getTrackPublication(Track.Source.Camera)?.videoTrack;

    if (track) {
      track.attach(pipVideo);
      this.activeSpeakerName.set(participant?.name || 'You');
    }
  }  

  toggleMic() {
    this.room()?.localParticipant.setMicrophoneEnabled(!this.micEnabled());
    this.callState.toggleMic();
  }

  toggleVideo() {
    this.callState.toggleVideo();
  }

  leaveCall() {
    this.callState.leaveCall();
  }

  cancelCall() {
    this.callState.cancelCall();
  }

  endCall() {
    this.callState.endCall();
  }

}
