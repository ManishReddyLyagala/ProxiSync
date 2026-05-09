import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { API } from '../constants/api-endpoints';
import { StorageService } from './storage.service';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private chatHub?: signalR.HubConnection;
  private callHub?: signalR.HubConnection;
  // chat
  public onMessage = new Subject<any>();
  public onAck = new Subject<any>();
  public onTyping = new Subject<any>();
  public onMarkedRead = new Subject<any>();
  public onMessageEdited = new Subject<any>();

  // calls
  public onIncomingCall = new Subject<any>();
  public onCallEnded = new Subject<any>();
  public onStopRinging = new Subject<any>();
  public onParticipantStatusChanged = new Subject<any>();
  public onCallStatusChanged = new Subject<any>();
  public onParticipantMediaUpdated = new Subject<any>();
  public onCallAccepted = new Subject<any>();
  public onCallRejected = new Subject<any>();
  public onCallCancelled = new Subject<any>();
  public onCallMissed = new Subject<any>();
  public onUserIsOnline = new Subject<{userId: string, isOnline: boolean, lastSeen?: Date}>();
  private presenceMap = new Map<string, { isOnline: boolean, lastSeen?: Date | null }>();
  public presenceUpdates$ = new BehaviorSubject<Map<string, any>>(this.presenceMap);

  constructor(private storage: StorageService) {}

  async start(){
    await this.startChatHub();
    await this.startCallHub();
  }

  private async startChatHub() {
    if (this.chatHub?.state === signalR.HubConnectionState.Connected) return;
    // if (this.chatHub) return;
    const token = this.storage.getToken();
    if (!token) return;
    this.chatHub = new signalR.HubConnectionBuilder()
      .withUrl(API.HUBS.CHAT, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    // 2. Real-time updates: Someone toggles between Online/Offline
    this.chatHub?.on('UserPresenceStatusChanged', (data: { userId: string, isOnline: boolean, lastSeen?: Date | null }) => {
      this.presenceMap.set(data.userId, { 
        isOnline: data.isOnline, 
        lastSeen: data.isOnline ? null : (data.lastSeen || new Date())
      });
      this.broadcastPresence();
    });

   this.chatHub?.on('GetOnlineUsers', (userIds: string[]) => {
      userIds.forEach(id => {
        this.presenceMap.set(id, { isOnline: true, lastSeen: null });
      });
      this.broadcastPresence();
    });

    this.chatHub.on('ReceiveMessage', (msg: any) =>{
      console.log("in recive: ", msg);
      return this.onMessage.next(msg)
    });
    this.chatHub.on('MessageSentAck', ack => this.onAck.next(ack));
    this.chatHub.on('UserTyping', (convId, userId) => this.onTyping.next({ convId, userId }));
    this.chatHub.on('MessagesMarkedRead', (payload: any) => this.onMarkedRead.next({ conversationId: payload.conversationId, seenArray: payload.updates }));
    this.chatHub.on('MessageEdited', msg => this.onMessageEdited.next(msg));

    await this.chatHub.start();
  }

  async joinConversation(conversationId: string) {
    if (!this.chatHub) await this.start();
    await this.chatHub?.invoke('JoinConversation', conversationId);
  }

  async sendToConversation(payload: any) {
    await this.chatHub?.invoke('SendToConversation', payload);
  }

  async markRead(dto: any) {
   var result =  await this.chatHub?.invoke('MarkRead', dto);
   console.log("latest ",result);
  }

  async typing(conversationId: string) {
    await this.chatHub?.invoke('Typing', conversationId);
  }

  async editMessage(dto: any) {
  await this.chatHub?.invoke('EditMessage', dto);
  }

  private async startCallHub(){
    if(this.callHub) return;

    const token = this.storage.getToken();
    if(!token) return;

    this.callHub = new signalR.HubConnectionBuilder()
                    .withUrl(API.HUBS.CALL, {accessTokenFactory: ()=> token})
                    .withAutomaticReconnect()
                    .build();

    this.callHub.on('IncomingCall', (payload: any)=> {
      console.log('[call] incoming call: ', payload);
      this.onIncomingCall.next(payload);
    });

    this.callHub.on('CallAccepted', (payload: any) => this.onCallAccepted.next(payload));
    this.callHub.on('CallRejected', (payload: any) => this.onCallRejected.next(payload));
    this.callHub.on('CallCancelled', (payload: any) => this.onCallCancelled.next(payload));
    this.callHub.on('CallMissed', (payload: any) => this.onCallMissed.next(payload));

    this.callHub.on('StopRinging', (payload: any)=>{
      this.onStopRinging.next(payload);
    });

     this.callHub.on('ParticipantStatusChanged', (payload: any) => {
      console.log('[CALL] ParticipantStatusChanged:', payload);
      this.onParticipantStatusChanged.next(payload);
    });

    this.callHub.on('CallEnded', (payload: any) => {
      console.log('[CALL] CallEnded:', payload);
      this.onCallEnded.next(payload);
    });

     this.callHub.on('CallStatusChanged', (payload: any) => {
      console.log('[CALL] CallStatusChanged:', payload);
      this.onCallStatusChanged.next(payload);
    });

    this.callHub.on('ParticipantMediaUpdated', (payload: any) => {
      console.log('[CALL] ParticipantMediaUpdated:', payload);
      this.onParticipantMediaUpdated.next(payload);
    });
     await this.callHub.start();

  }

   // =========================
  // CALL HUB METHODS (OPTIONAL)
  // =========================
  async joinCallRoom(callId: string) {
    if (!this.callHub) await this.startCallHub();
    await this.callHub?.invoke('JoinCallGroup', callId);
  }

  async leaveCallRoom(callId: string) {
    await this.callHub?.invoke('LeaveCallGroup', callId);
  }

  private broadcastPresence() {
    // Push a fresh copy so subscribers detect a change
    this.presenceUpdates$.next(new Map(this.presenceMap));
  }
  getPresence(userId: string) {
    return this.presenceMap.get(userId) || { isOnline: false };
}

async stopAll() {
    try {
    if (this.chatHub) {
      await this.chatHub.stop();
      this.chatHub = undefined;
    }
    if (this.callHub) {
      await this.callHub.stop();
      this.callHub = undefined;
    }
    this.presenceMap.clear();
    this.presenceUpdates$.next(new Map());
  }catch(err){
    console.log('Error while stopping SignalR connection:', err);
  }finally{
      this.chatHub = undefined;
      this.callHub = undefined;
  }
  }
}