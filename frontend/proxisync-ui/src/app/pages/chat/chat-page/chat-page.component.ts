import { Component, HostListener, OnChanges, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterOutlet, RouterLinkWithHref, RouterLinkActive } from '@angular/router';
import { SignalRService } from '../../../core/services/signalr.service';
import { StorageService } from '../../../core/services/storage.service';
import { ChatApiService } from '../../../core/api/chat-api.service';
import { CommonModule } from '@angular/common';
import { environment } from '../../../../environments/environment';
import { Subscription } from 'rxjs';
import { ChatModal } from "../../../shared/components/modal/chat-modal/chat-modal";
import { RingtoneService } from '../../../core/services/ringtone.service';

@Component({
  selector: 'app-chat-page',
  standalone: true,
  templateUrl: './chat-page.component.html',
  imports: [CommonModule, RouterOutlet, RouterLinkWithHref, RouterLinkActive, ChatModal]
})
export class ChatPageComponent implements OnInit, OnDestroy {
  convs: any[] = [];
  currentPage = 1;
  pageSize = 15;
  loading = false;
  hasMore = true;
  isMuted: boolean = true;
  apiBaseUrl = environment.gatewayUrl;
  private messageSub?: Subscription;
  private presenceSub?: Subscription;
  private presenceMapSub?: Subscription;
  
  constructor(private api: ChatApiService, public router: Router, private s: SignalRService, private storage: StorageService, private ringtone: RingtoneService) {}

  ngOnInit(){
    this.load(false);
    this.s.start().catch(console.error);
    // Presence Subscription
    this.presenceMapSub = this.s.presenceUpdates$.subscribe((map) => {
      if (!this.convs || this.convs.length === 0) return;
        this.convs.forEach(c => {
          if(c.type === 'Direct' || c.type === 'direct'){
            const otherUser = c.participants?.find((p: any) => p.userId !== this.storage.getUser()?.id);
            
            if (otherUser && map.has(otherUser.userId)) {
                // Update the participant object directly
                const status = map.get(otherUser.userId);
                if (otherUser.isOnline !== status.isOnline) {
                otherUser.isOnline = status.isOnline;
                otherUser.lastSeen = status.lastSeen;
                }
            }
          }
          return c;
        });
    });

    this.messageSub = this.s.onMessage.subscribe((msg: any) =>{
      const index = this.convs.findIndex(c => c.conversationId === msg.conversationId);
      if(index !== -1){
        const updatedConv = { ...this.convs[index] };
        const isSameMessage = updatedConv.LastMessageContent === msg.content && 
                          new Date(updatedConv.LastMessageSentAt).getTime() === new Date(msg.sentAt).getTime();

        if (isSameMessage) return;
        updatedConv.lastMessageContent = msg.content;
        updatedConv.lastMessageSentAt = msg.sentAt;
        updatedConv.lastMessageSenderName = msg.senderDisplayName;
        const currentRouteId = this.router.url.split('/').pop();
        if(currentRouteId !== msg.conversationId){
          updatedConv.unreadCount = (updatedConv.unreadCount || 0) + 1;
        }else{
          updatedConv.unreadCount = 0;
        // Optional: tell server we read it
        this.s.markRead({ conversationId: msg.conversationId, upToTimestamp: new Date().toISOString() });
        }

        // 4. Move to top: Remove from old position and add to start
      this.convs.splice(index, 1);
      this.convs.unshift(updatedConv);

      }else{
        this.load(false);
      } 
    });
  }

  ngOnDestroy(): void {
      this.messageSub?.unsubscribe();
      this.presenceSub?.unsubscribe();
      this.presenceMapSub?.unsubscribe();
  }

  // ngOnChanges(): void {
  //      this.s.onMessage.subscribe(() => this.load(false));
  // }
  load(isLoadingMore: boolean = false){
    this.loading = true;
    this.api.getConversations(this.currentPage, this.pageSize).subscribe({
      next: (res:any) =>{
        console.log("in group conv ", res);
        const data = res || [];
        const syncedData = data.map((c: any) => {
        if (c.type === 'Direct' || c.type === 'direct') {
          const other = c.participants?.find((p: any) => p.userId !== this.storage.getUser()?.id);
          if (other) {
            // Check if our SignalR map already knows this person is online
            const liveStatus = this.s.getPresence(other.userId);
            other.isOnline = liveStatus.isOnline;
          }
        }
        return c;
      });
        this.hasMore = syncedData.length === this.pageSize;
        if(isLoadingMore){
          this.convs = [...this.convs, ...syncedData];
        }else{
          this.convs = syncedData;
        }
        this.loading = false;
    },
    error: ()=> this.loading = false
  });
  }

  loadMore() {
    if (!this.loading && this.hasMore) {
      this.currentPage++;
      this.load(true);
    }
  }

  clearBadge(conv: any){ 
       conv.unreadCount = 0;
      this.s.markRead({ conversationId: conv.conversationId, upToTimestamp: new Date().toISOString() });
  }

  getProfileUrl(c: any) {
  // Check if it's a Direct conversation
  if (c.type === "Direct" || c.type === "direct") {
    const otherUser = c.participants.find(
      (u: any) => u.userId !== this.storage.getUser()?.id
    );
    const profilePictureUrl = otherUser?.profilePictureUrl;
    if (profilePictureUrl) {
      // Check if the URL is relative (starts with '/')
      return profilePictureUrl.startsWith('/') ? this.apiBaseUrl + profilePictureUrl : profilePictureUrl;
    }else{
      const initialChar = (otherUser.displayName?.[0] || 'U').toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${initialChar}`;
    }
  } 
  const groupChar = (c.name?.[0]).toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${groupChar}G`;
  // return '/assets/avatar.png';
}

formatChatDate(date: any): string {
  if (!date) return '';
  const d = new Date(date);
  const now = new Date();
  
  // Is it today?
  if (d.toDateString() === now.toDateString()) {
    return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: 'numeric', hour12: true }).format(d);
  }
  
  // Is it yesterday?
  const yesterday = new Date();
  yesterday.setDate(now.getDate() - 1);
 return d.toDateString() === yesterday.toDateString() ? 'Yesterday' : d.toLocaleDateString();
}

async enableAudio() {
   this.isMuted = !this.isMuted;
  
  if (!this.isMuted) {
    // Unlock and play ringtone
    await this.ringtone.unlockAudio();
  } else {
    // Mute logic
    this.ringtone.stop();
  }
  }

  isConversationOnline(c: any): boolean {
    if (c.type === 'Direct' || c.type === 'direct') {
        const otherUser = c.participants.find((u: any) => u.userId !== this.storage.getUser()?.id);
        return otherUser?.isOnline || false;
    }
    return false; // Groups usually don't have a single "online" dot
}

trackByConvId(index: number, item: any) {
  return item.conversationId; 
}

isMobileView(): boolean {
    return window.innerWidth < 1024;
  }

  isChatSelectedOnMobile(): boolean {
    // This matches your route: [routerLink]="['/chats/conv', c.conversationId]"
    return this.isMobileView() && this.router.url.includes('/chats/conv/');
  }
  @HostListener('window:resize')
  onResize() {
    // The getter methods above will naturally re-evaluate 
    // during the next change detection cycle.
  }
}
