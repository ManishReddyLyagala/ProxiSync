import { Injectable, inject } from "@angular/core";
import { BehaviorSubject, firstValueFrom, Subscription } from "rxjs";
import { CallApi } from "../api/call-api.service";
import { CallSessionDto, CallType, StartCallDto } from "../models/call.model";
import { SignalRService } from "./signalr.service";
// import icomingCallSound from "../../../../public/sounds/proxisync_incoming_call.mp3";
import { Room, RoomEvent, createLocalVideoTrack, createLocalAudioTrack, Track, ConnectionState } from "livekit-client";
import { toSignal } from "@angular/core/rxjs-interop";
import { RingtoneService } from "./ringtone.service";

export type CallUiState = | 'idle'
  | 'incoming'
  | 'outgoing'
  | 'offline'
  | 'connecting'
  | 'in-call'
  | 'ended';

  @Injectable({ providedIn: 'root' })
export class CallStateService {
  private callApi = inject(CallApi);
  private signalr = inject(SignalRService);
  public uiState$ = new BehaviorSubject<CallUiState>('idle');
  public activeCall$ = new BehaviorSubject<CallSessionDto | null | any>(null);
  public room$ = new BehaviorSubject<Room | null>(null);

  public uiState = toSignal(this.uiState$, { initialValue: 'idle' });
  public activeCall = toSignal(this.activeCall$, { initialValue: null });
  public room = toSignal(this.room$, { initialValue: null });

  public micEnabled$ = new BehaviorSubject<boolean>(true);
  public videoEnabled$ = new BehaviorSubject<boolean>(false);
  public micEnabled = toSignal(this.micEnabled$, { initialValue: true });
  public videoEnabled = toSignal(this.videoEnabled$, { initialValue: false });

  private subs = new Subscription();

  //  private ringtone = new Audio("proxisync-ui/public/sounds/proxisync_incoming_call.mp3");
  // private incomingAudio = new Audio('proxisync-ui/public/sounds/proxisync_incoming_call.mp3');
  // private outgoingAudio = new Audio('proxisync-ui/public/sounds/proxisync_incoming_call.mp3');
  constructor(private ringtone: RingtoneService) {
    // this.incomingAudio.loop = true;
    // this.outgoingAudio.loop = true;
    // Make sure hubs are started
    this.signalr.start();

    this.subs.add(
      this.signalr.onIncomingCall.subscribe((payload) => {
        this.handleIncoming(payload);
      })
    );

    this.subs.add(
      this.signalr.onStopRinging.subscribe((payload) => {
        const current = this.activeCall$.value;
        if (!current) return;
        // if (payload?.callId && payload.callId !== current.callId) return;
        this.onStopRingingEvt(payload);
      })
    );

    this.subs.add(this.signalr.onCallAccepted.subscribe((evt) => {
  this.handleCallAccepted(evt);
}));

this.subs.add(this.signalr.onCallRejected.subscribe((evt) => {
  this.handleCallRejected(evt);
}));

this.subs.add(this.signalr.onCallCancelled.subscribe((evt) => {
  this.handleCallCancelled(evt);
}));

this.subs.add(this.signalr.onCallMissed.subscribe((evt) => {
  this.handleCallMissed(evt);
}));

this.subs.add(this.signalr.onCallEnded.subscribe((evt) => {
  this.handleCallEnded(evt);
}));
     // call ended
    // this.subs.add(
    //   this.signalr.onCallEnded.subscribe((payload) => {
    //     const current = this.activeCall$.value;
    //     if (!current) return;
    //     if (payload?.callId && payload.callId !== current.callId) return;
    //     this.uiState$.next('ended');

    //     setTimeout(() => {
    //       this.cleanupCall();
    //     }, 1200);
    //   })
    // );

    this.subs.add(
      this.signalr.onParticipantStatusChanged.subscribe((evt) => {
        this.handleParticipantStatusChanged(evt);
      })
    );

    // Optional: if backend sends CallStatusChanged
    this.subs.add(
      this.signalr.onCallStatusChanged.subscribe((evt) => {
        this.handleCallStatusChanged(evt);
      })
    );
}

// OUTGOING
async startCall(conversationId: string, conversationName: string, participantUserIds: string[], type: CallType){
    const dto: StartCallDto = {
        conversationId,
        type,
        participantUserIds,
        callerMicEnabled: true,
        callerVideoEnabled: type == 'Video'
    };
    console.log("in call service ", dto)
    await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
    this.micEnabled$.next(true);
    this.videoEnabled$.next(type == 'Video');
    this.uiState$.next('outgoing');
    this.playOutgoingRingtone();
    this.callApi.start(dto).subscribe({
        next: async (resp)=>{
          if(resp.receiverOffline){
            this.stopOutgoingRingtone();
            this.uiState$.next('offline');
            setTimeout(() => this.uiState$.next('idle'), 5000);
            return;
          }
            await this.signalr.joinCallRoom(resp.callId);
            this.activeCall$.next({
                callId: resp.callId,
                conversationId: resp.conversationId,
                roomName: resp.roomName,
                conversationName: conversationName,
                type: resp.type,
                status: resp.status,
                isGroupCall: resp.isGroupCall,
                startedByUserId: '',
                startedAt: new Date().toISOString(),
                participants:  [],
            });

            // await this.connectToLiveKit(resp.callId);
        },
        error: ()=>{
            this.uiState$.next('idle');
        }
    });
}

private async handleCallAccepted(evt: any) {
  const call = this.activeCall$.value;
  console.log("handle call accept ",call);
  if (!call) return;
  if (evt.callId !== call.callId) return;

  // caller should connect exactly once
  if (this.uiState$.value !== 'outgoing') return;

  // already connected? ignore
  const room = this.room$.value;
  if (room && room.state === ConnectionState.Connected) return;
  // const res = await this.callApi.getCall(call.callId);
  // console.log("res ", res);
  this.stopOutgoingRingtone();
  this.uiState$.next('connecting');

  await this.connectToLiveKit(call.callId);
}

private async handleCallRejected(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  // caller should connect exactly once
  if (this.uiState$.value !== 'outgoing') return;

  this.stopIncomingRingtone();
  this.uiState$.next('ended');
   setTimeout(() => this.cleanupCall(), 1500);
}

private handleCallCancelled(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  this.stopIncomingRingtone();
  this.uiState$.next('ended');

  setTimeout(() => this.cleanupCall(), 1500);
}

private handleCallMissed(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  this.stopIncomingRingtone();
  this.stopOutgoingRingtone();

  this.uiState$.next('ended');
  setTimeout(() => this.cleanupCall(), 1500);
}

private handleCallEnded(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  this.stopIncomingRingtone();
  this.stopOutgoingRingtone();

  this.uiState$.next('ended');
  setTimeout(() => this.cleanupCall(), 1500);
}

private onStopRingingEvt(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  this.stopIncomingRingtone();
  this.stopOutgoingRingtone();
}

// incoming 
private async handleIncoming(payload: any){
    const call: CallSessionDto = {
      callId: payload.callId,
      conversationId: payload.conversationId,
      roomName: payload.roomName,
      callerName: payload?.callerName,
      type: payload.type,
      status: "Ringing",
      isGroupCall: payload.isGroupCall,
      startedByUserId: payload.startedByUserId,
      startedAt: payload.startedAt,
      endedAt: null,
      participants: [],
    };
    this.activeCall$.next(call);
    try {
  const full = await this.callApi.getCall(call.callId);
  if (full) {
    this.activeCall$.next(full);
  }
} catch {}
    this.micEnabled$.next(true);
    this.videoEnabled$.next(call.type === 'Video');

    // Join call hub group for this call
    await this.signalr.joinCallRoom(call.callId);

    this.uiState$.next('incoming');
    this.playIncomingRingtone();
}

//accept
async acceptCall(){
    const call = this.activeCall$.value;
    if(!call) return;

    this.stopIncomingRingtone();
    await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
    this.uiState$.next('connecting');
    this.callApi.join(call.callId, {
        micEnabled: this.micEnabled$.value,
        videoEnabled: this.videoEnabled$.value
    })
    .subscribe({
        next: async ()=> {
            await this.connectToLiveKit(call.callId);
                    try {
        const full = await this.callApi.getCall(call.callId);
        if (full) {
          this.activeCall$.next(full);
        }
        } catch {}
        },
        error: ()=> {
            this.uiState$.next('idle');
        }
    });
}

// reject
rejectCall(){
    const call = this.activeCall$.value;
  if (call) {
    // Notify backend so caller doesn't wait forever
    try{
      this.callApi.reject(call.callId).subscribe(); 
    }catch(e){console.log("error in reject call ", e)}
  }
  this.stopIncomingRingtone();
  this.cleanupCall(); 
}

 // ---------------- LEAVE (IN-CALL) ----------------
 leaveCall() {
    const call = this.activeCall$.value;
    if (call) {
      try{
      this.callApi.leave(call.callId).subscribe();
      }catch(e){console.log("error in leave call ", e)}
    }
    this.cleanupCall();
  }

   // ---------------- CANCEL (CALLER WHILE RINGING) ----------------
  cancelCall() {
    const call = this.activeCall$.value;
    if (call) {
      try{
        this.callApi.cancelCall(call.callId).subscribe();
      }catch(e){console.log("error in cancel call ", e)}
    }
    this.stopOutgoingRingtone();
    this.cleanupCall();
  }

   // end call
  async endCall() {
    const call = this.activeCall$.value;
    if (!call) return;
    try{
    this.callApi.end(call.callId).subscribe();
    }catch(e){console.log("error in end call ", e)}
    this.stopIncomingRingtone();
    this.cleanupCall();
  }
// LiveKit connect
private async connectToLiveKit(callId: string){
    const tokenresp = await firstValueFrom(this.callApi.token(callId));
    if(!tokenresp){
        this.cleanupCall()
        return;
    }

    const room = new Room({
        adaptiveStream: true,
        dynacast: true,
    });

    this.room$.next(room);

      room.on(RoomEvent.Reconnecting, () => {
    console.log('[LIVEKIT] reconnecting...');
    if(this.uiState$.value !== 'ended')
      this.uiState$.next('connecting');
  });

  room.on(RoomEvent.Reconnected, () => {
    console.log('[LIVEKIT] reconnected');
    if(this.uiState$.value !== 'ended')
      this.uiState$.next('in-call');
  });

    room.on(RoomEvent.Disconnected, ()=> {
        this.cleanupCall();
    })
    .on(RoomEvent.TrackSubscribed, (track, pub, participant)=> {
        console.log('[LIVEKIT] TrackSubscribed', track.kind, participant.identity);
         if (track.kind === Track.Kind.Audio) {
          const el = track.attach();
          el.autoplay = true;
          document.body.appendChild(el); // simplest working method
    }
    });

    room.on(RoomEvent.TrackUnsubscribed, (track, pub, participant) => {
    try {
      track.detach().forEach((el) => el.remove());
    } catch (e) {}
});
    await room.connect(tokenresp.liveKitUrl, tokenresp.token);
    await room.localParticipant.setMicrophoneEnabled(this.micEnabled$.value);
    await room.localParticipant.setCameraEnabled(this.videoEnabled$.value);
    this.uiState$.next('in-call');
}

 private async publishLocalTracks(room: Room) {
    if (this.micEnabled$.value) {
      const audioTrack = await createLocalAudioTrack();
      await room?.localParticipant.publishTrack(audioTrack);
    }

    if (this.videoEnabled$.value) {
      const videoTrack = await createLocalVideoTrack();
      await room?.localParticipant.publishTrack(videoTrack);
    }
  }

   private async handleParticipantStatusChanged(evt: any) {
  const call = this.activeCall$.value;
  if (!call) return;
  if (evt.callId !== call.callId) return;

  // if someone rejected while we are outgoing
  if (this.uiState$.value === 'outgoing' && evt.status === 'Rejected') {
    // this.stopOutgoingRingtone();
    this.uiState$.next('ended');
    setTimeout(() => this.cleanupCall(), 1500);
    return;
  }

  // if someone joined while we are outgoing (fallback)
  // but CallAccepted is better for caller
  if (this.uiState$.value === 'outgoing' && evt.status === 'Joined') {
    // do nothing here, CallAccepted handles it
    return;
  }
}

  private async handleCallStatusChanged(evt: any) {
    // Example evt:
    // { callId, status: "Ongoing" | "Ended" | "Missed" }

    const call = this.activeCall$.value;
    if (!call) return;
    if (evt.callId !== call.callId) return;

    if (evt.status === 'Ended') {
      this.cleanupCall();
      return;
    }
    // this.stopIncomingRingtone();
  }
  // toggles

    async toggleMic() {
    const room = this.room$.value;
    if (!room) return;

    const enabled = !this.micEnabled$.value;
    this.micEnabled$.next(enabled);

      try {
    await room?.localParticipant?.setMicrophoneEnabled(enabled);
    } catch (err) {
    console.error('[LIVEKIT] toggleMic failed', err);
    this.micEnabled$.next(!enabled);
    return;
  }

     const call = this.activeCall$.value;
    if (call) {
      this.callApi
        .updateMedia(call.callId, {
          isMicEnabled: enabled,
          isVideoEnabled: this.videoEnabled$.value,
        })
        .subscribe();
    }
}

 async toggleVideo() {
    const room = this.room$.value;
    if (!room) return;

    const enabled = !this.videoEnabled$.value;
    this.videoEnabled$.next(enabled);

    try {
    await room.localParticipant.setCameraEnabled(enabled);
    } catch (err) {
    console.error('[LIVEKIT] toggleMic failed', err);
    this.videoEnabled$.next(!enabled);
    return;
  }

    const call = this.activeCall$.value;
    if (call) {
      this.callApi
        .updateMedia(call.callId, {
          isMicEnabled: this.micEnabled$.value,
          isVideoEnabled: enabled,
        })
        .subscribe();
    }
  }

  // cleanup
    private async cleanupCall() {
    this.stopIncomingRingtone();
    this.stopOutgoingRingtone();

    const call = this.activeCall$.value;
    if (call) {
      await this.signalr.leaveCallRoom(call.callId);
    }

    const room = this.room$.value;
    if (room) {
       try {
      room.localParticipant.trackPublications.forEach((pub) => {
        if (pub.track) {
           try {
            room.localParticipant.unpublishTrack(pub.track);
          } catch {}

          try {
            pub.track.stop();
          } catch {}
        }
      });
      room.disconnect();
       } catch {}
    }

    this.room$.next(null);
    this.activeCall$.next(null);
    this.uiState$.next('idle');
  }

  forceIdle() {
  this.uiState$.next('idle');
}

   private async playIncomingRingtone() {
  try {
    await this.ringtone.play('incoming');
  } catch {
    console.warn('Incoming ringtone blocked by browser autoplay policy');
  }
}

private async stopIncomingRingtone() {
  try { await this.ringtone.stop() } catch {}
}

private async playOutgoingRingtone() {
  try {
    await this.ringtone.play('outgoing');
  } catch {
    console.warn('Outgoing ringtone blocked by browser autoplay policy');
  }
}

private async stopOutgoingRingtone() {
  try { await this.ringtone.stop() } catch {}
}
}