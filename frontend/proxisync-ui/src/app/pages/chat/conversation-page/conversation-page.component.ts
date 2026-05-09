import { Component, OnInit, OnDestroy, ViewChild, ElementRef, signal, computed, AfterViewInit, HostListener } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ChatApiService } from '../../../core/api/chat-api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { StorageService } from '../../../core/services/storage.service';
import { Subscription } from 'rxjs';
import { MessageBubbleComponent } from '../../../shared/components/message-bubble/message-bubble.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { ContextAction, MessageContextModalComponent } from '../../../shared/components/modal/message-context-modal/message-context-modal.component';
import { UserDetailModalComponent } from '../../../shared/components/modal/user-details-modal/user-detail.component';
import { UserApiService } from '../../../core/api/user-api.service';
import { FriendRequestAPIService } from '../../../core/api/friend-request-api.service';
import { CallStateService } from '../../../core/services/call-state.service';
import { RingtoneService } from '../../../core/services/ringtone.service';
import { PickerComponent } from '@ctrl/ngx-emoji-mart';
import imageCompression from 'browser-image-compression';

@Component({
  selector: 'app-conversation-page',
  standalone: true,
  templateUrl: './conversation-page.component.html',
  styleUrl: './conversation-page.component.css',
  imports: [MessageBubbleComponent, CommonModule, FormsModule, MessageContextModalComponent, UserDetailModalComponent, PickerComponent]
})
export class ConversationPageComponent implements OnInit, OnDestroy, AfterViewInit {
  convId?: string;
  messages: any[] = [];
  currentMessageContent = '';
  conversationName: string = '';
  BlockedUserName: string = '';
  BlockedUserId: string = '';
  headerProfilePic: string = '';
  currentPage = 1;
  pageSize = 15;
  private subs: Subscription[] = [];
  apiBaseUrl = environment.gatewayUrl;
  selectedMessage: any | null = null;
  editingMessage: any | null = null;
  seenByUsers: any[] = [];
  ReciptentUserDetails: any = null;
  selectedUser: any = null;
  selectedConversationType: 'Direct' | 'Group' = 'Direct';
  CurrentUserId: string = '';
  groupParticipantDetails: any[] = [];
  
  public userPresenceStatus: { isOnline: boolean, lastSeen?: Date | null } | null = null;
  private presenceSub?: Subscription;

  @ViewChild('scrollContainer') scrollContainer!: ElementRef;
  constructor(private route: ActivatedRoute, public router: Router, private api: ChatApiService, 
    private userApi: UserApiService, private signalr: SignalRService, public storage: StorageService,
     public callState: CallStateService, private ringtone: RingtoneService) { }

  isLoadingMore = signal<boolean>(false);
  hasMoreMessages = signal<boolean>(true);
  isContextDataLoading = signal(false);
  isThreeDotOpen: boolean = false;
  isUserDetailModalOpen: boolean = false;
  isClearChatClicked = signal<boolean>(false);
  isConversationBlocked = signal<boolean>(false);
  friendshipStatus = signal<string | null>("");
  isEmojiPickerVisible: boolean = false;

  // --- Scroll State for UI ---
  isScrolledToBottom = signal<boolean>(true);
  isScrolledToTop = signal<boolean>(false);
  // Computed state for UI visibility
  showLoadMoreButton = computed(() => this.isScrolledToTop() && this.hasMoreMessages() && !this.isLoadingMore());
  showScrollDownButton = computed(() => !this.isScrolledToBottom());

  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  isUploading = false;


  ngOnInit() {
    this.subs.push(
      this.route.paramMap.subscribe(params => {
        this.convId = params.get('id') || undefined;
        if (!this.convId) return;

        this.resetConversationState();
        this.load();
        this.signalr.joinConversation(this.convId);
        this.markRead();
      })
    );
    // this.convId = this.route.snapshot.paramMap.get('id') || undefined;
    // if (!this.convId) return;
    // this.load();
    // this.markRead();
    // console.log("clear all", this.isClearChatClicked)
    this.signalr.start().then(() => this.signalr.joinConversation(this.convId!)).catch(console.error);

//     this.subs.push(
//   this.signalr.onUserIsOnline.subscribe((status) => {
//     if (this.selectedConversationType === 'Direct' && status.userId === this.selectedUser?.userId) {
//       this.userPresenceStatus = {
//         isOnline: status.isOnline,
//         lastSeen: status.lastSeen
//       };
//     }
//   })
// );
this.subs.push(this.signalr.presenceUpdates$.subscribe(map => {
  if (this.selectedConversationType === 'Direct' && this.selectedUser) {
  const liveData = map.get(this.selectedUser.userId);
      if (liveData) {
        this.userPresenceStatus = {
          isOnline: liveData.isOnline,
          lastSeen: liveData.lastSeen
        };
      }
}
}));

    this.subs.push(this.signalr.onMessage.subscribe((m: any) => {
      if (m.conversationId === this.convId) {
        if (this.messages.some(msg => msg.messageId === m.messageId)) return;
        const newMsgTime = new Date(m.sentAt).getTime();
        const firstMsgTime = this.messages.length > 0 ? new Date(this.messages[0].sentAt).getTime() : null;
        const lastMsgTime = this.messages.length > 0 ? new Date(this.messages[this.messages.length - 1].sentAt).getTime() : null;

        // Case A: List is empty OR message is newer than the current last message (99% of cases)
        if (lastMsgTime === null || newMsgTime >= lastMsgTime) {
          this.messages.push(m);
        }
        // Case B: Message is older than the very first message we have
        else if (firstMsgTime !== null && newMsgTime <= firstMsgTime) {
          this.messages.unshift(m);
        }
        // Case C: Message belongs somewhere in the middle (Out-of-order arrival)
        else {
          const insertIndex = this.messages.findIndex(msg =>
            new Date(msg.sentAt).getTime() > newMsgTime
          );

          if (insertIndex !== -1) {
            this.messages.splice(insertIndex, 0, m);
          }
        }
        
        setTimeout(() => this.scrollToBottom(true), 50);
      }
    }));

    this.signalr.onMarkedRead.subscribe(({ conversationId, seenArray }) => {
      if (conversationId !== this.convId) return;
      console.log("in onmarked broadcast ", seenArray);
      seenArray.forEach((u: any) => {
        const msg = this.messages.find(m => m.messageId === u.messageId);
        if (msg) {
          msg.isSeen = u.isSeen;
          console.log(msg.content + " is seen  = " + msg.isSeen);
        }
      });
      console.log("after mark msg ", this.messages);
    });


    this.subs.push(
      this.signalr.onMessageEdited.subscribe((updated) => {
        const index = this.messages.findIndex(
          m => m.messageId === updated.messageId
        );

        if (index !== -1) {
          this.messages[index] = {
            ...this.messages[index],
            content: updated.content,
            isEdited: updated.isEdited
          };
        }
      })
    );
  }

  ngAfterViewInit(): void {
    // Initial scroll check (after content is rendered)
    this.scrollToBottom(false);
  }

  private resetConversationState() {
    this.messages = [];
    this.currentPage = 1;
    this.hasMoreMessages.set(true);
    this.isScrolledToBottom.set(true);
  }

  loadMoreMessages() {
    try {
      if (this.isLoadingMore() || !this.hasMoreMessages() || !this.isAuthReady()) return;
      this.isLoadingMore.set(true);
      const container = this.scrollContainer.nativeElement;
      const oldScrollHeight = container.scrollHeight;
      this.currentPage++;
      this.api.getMessages(this.convId!, this.currentPage, this.pageSize).subscribe({
        next: (res: any) => {
          let newMessages = res?.data.messages || [];

          if (newMessages.length === 0) {
            this.hasMoreMessages.set(false); // Stop fetching if no documents found
            this.isLoadingMore.set(false);
            return;
          }
          newMessages = this.sortMessages(newMessages);
          this.messages = [...newMessages, ...this.messages];
          // CRITICAL: Restore scroll position
          requestAnimationFrame(() => {
            const newScrollHeight = container.scrollHeight;
            const scrollAdjustment = newScrollHeight - oldScrollHeight;
            // Adjust scrollTop to maintain the view anchored at the old top message
            container.scrollTop = scrollAdjustment;
            this.isLoadingMore.set(false);
          });
          // this.markRead();
        },
        error: (error: any) => {
          console.error('Error loading older messages:', error);
          this.isLoadingMore.set(false);
          // Decrement page number on error so the next attempt tries the same page
          this.currentPage--;
        }
      });
    } catch (error) {
      console.error('Error loading older messages:', error);
      this.isLoadingMore.set(false);
    }
  }

  load() {
    this.api.getMessages(this.convId!, 1, this.pageSize).subscribe((res: any) => {

      this.messages = this.sortMessages(res?.data.messages || []) || [];
      if (this.messages.length <= this.pageSize && this.messages.length >= res?.data.totalCount) {
        this.hasMoreMessages.set(false);
      }
      this.scrollToBottom(false);
      // setTimeout(() => this.scrollToBottom(true), 100);
      this.loadConversationName();
    });
  }

  isAuthReady() {
    return this.storage.getUser() != null;
  }

  sortMessages(messages: Array<any>): Array<any> | undefined {
    const sortedMessages = messages.sort((m1: any, m2: any) => {
      const date1 = new Date(m1.sentAt);
      const date2 = new Date(m2.sentAt);
      return date1.getTime() - date2.getTime();
      // or simply: return date1.valueOf() - date2.valueOf();
      // or return date1 - date2; 
    });
    return sortedMessages;
  }

  async sendMessage() {
    if (!this.currentMessageContent.trim()) return;

    if (this.editingMessage) {
      console.log(`[ACTION] Editing message ID: ${this.editingMessage.id} with new content: ${this.currentMessageContent}`);
      let payload = {
        messageId: this.editingMessage.messageId,
        newContent: this.currentMessageContent
      }

      this.signalr.editMessage(payload);
      this.editingMessage = null;
      this.cancelEdit(); // Exit edit mode
    }
    else {
      const payload = { ConversationId: this.convId, Content: this.currentMessageContent, MessageType: 1 };
      var val = await this.signalr.sendToConversation(payload);
      console.log("resp ", val);
    }
    this.currentMessageContent = '';
    console.log("after send msg ", this.messages);
  }

  async markRead() {
    const last = this.messages[this.messages.length - 1];
    if (!last) return;
    console.log("in markread payload ", { ConversationId: this.convId, UpToTimestamp: last.sentAt });
    await this.signalr.markRead({ ConversationId: this.convId, UpToTimestamp: last.sentAt });
  }

  // getSeenCount(message: any): number {
  //   // NOTE: Replace with actual logic to count receipts/read status
  //   return message.senderId === this.storage.getUser()?.id ? 2 : 0;
  // }

  // getSeenByUsers(message: any) {
  //   // NOTE: Replace with actual logic to fetch users who read the message
  //   if (message.senderId !== this.storage.getUser()?.id) return [];
  //   // this.isContextDataLoading.set(true);
  //   console.log("convid "+ this.convId + " msg id "+ message.messageId);
  //   this.api.getMessageReadUsers(this.convId!, message.messageId).subscribe(data=>{
  //     console.log("in data ", data);
  //     // data && this.isContextDataLoading.set(false);
  //     return data;
  //   })
  //   // this.isContextDataLoading.set(false);
  //   return [];

  // }

  openMessageMenu(message: any) {
    // Only allow context menu for own messages
    this.isUserDetailModalOpen = false;
    this.isThreeDotOpen = false;
    if (message.senderId === this.storage.getUser()?.id) {
      this.selectedMessage = message;
      this.seenByUsers = []; // Reset
      this.isLoadingMore.set(true);
      this.api.getMessageReadUsers(this.convId!, message.messageId)
        .subscribe({
          next: (res) => {
            if (res.success) {
              this.seenByUsers = res.data.seenUsers;
            } else {
              console.log(res.message)
            }
            this.isLoadingMore.set(false);
          },
          error: () => {
            this.isLoadingMore.set(false);
          }
        });
    }
  }

  closeMessageMenu() {
    this.selectedMessage = null;
    this.isClearChatClicked.set(false);
    this.isThreeDotOpen = false;
  }

  openUserDeatilsModal() {
   if (this.selectedConversationType === 'Direct' && this.selectedUser) {
      this.userApi.getUserDetailsById(this.selectedUser?.userId).subscribe({
        next: (res) => {
          this.ReciptentUserDetails = res;
          // this.isUserDetailModalOpen = true;
        },
        error: (err) => {
          console.error("Error fetching user details", err);
          this.isUserDetailModalOpen = true;
        }
      })

    }
    else if (this.selectedConversationType === 'Group') {
      this.ReciptentUserDetails = null;
      // this.isUserDetailModalOpen = true;
    }

    // Close UI popovers
    this.isUserDetailModalOpen = true;
    this.isThreeDotOpen = false;
    this.selectedMessage = null;
  }

  closeUserDetailModal() {
    this.isUserDetailModalOpen = false;
    this.isThreeDotOpen = false;
    this.selectedMessage = null;
  }

  onConversationDeleted() {
    this.isUserDetailModalOpen = false;
    this.convId = ''; // Clear active conversation
    
    // Logic to refresh your sidebar/conversation list
    this.refreshConversationList();
  }

  private refreshConversationList() {
    // Trigger your chat list component to update
    this.load();
  }
  // @HostListener('document:click', ['$event'])
  // onDocumentClick(event: MouseEvent) {
  //   const target = event.target as HTMLElement;
  //   if (this.isThreeDotOpen && !target.closest('.menu-container')) {
  //     this.isThreeDotOpen = false;
  //   }
  // }

  toggleThreeDotMenu() {
    // event.stopPropagation();
    this.isUserDetailModalOpen = false;
    this.selectedMessage = null;
    this.isThreeDotOpen = !this.isThreeDotOpen;
  }

  toggleEmojiPicker() {
    this.isEmojiPickerVisible = !this.isEmojiPickerVisible;
  }

 addEmoji(event: any) {
  // Try native first, then fallback to the calculated emoji if native is missing
  const emoji = event.emoji.native || event.emoji.unified;
  
  if (emoji) {
    // If it's a colon-style emoji (like :smile:), we keep it as text 
    // but usually, native works best for input boxes.
    this.currentMessageContent += event.emoji.native;
  }
}

// Ensure the picker closes on mobile when clicking input
onInputFocus() {
  if (this.isMobileView()) {
    this.isEmojiPickerVisible = false;
  }
}

@HostListener('document:click', ['$event'])
onClickedOutside(event: Event) {
  const target = event.target as HTMLElement;
  if (this.isEmojiPickerVisible && !target.closest('emoji-mart') && !target.closest('.material-icons')) {
    this.isEmojiPickerVisible = false;
  }
}

async onFileSelected(event: any) {
  const file: File = event.target.files[0];
  if (!file) return;

  this.selectedFile = file;

  const reader = new FileReader();
  reader.onload = () => (this.filePreviewUrl = reader.result as string);
  reader.readAsDataURL(file);
}

async uploadAndSendMessage() {
  if (!this.selectedFile) return;
  this.isUploading = true;

  const originalName = this.selectedFile.name; 
  let fileToUpload: File | Blob = this.selectedFile;

  // 1. Compress Images
  if (this.selectedFile.type.startsWith('image/')) {
    const options = { maxSizeMB: 1, maxWidthOrHeight: 1920, useWebWorker: true };
    try {
      fileToUpload = await imageCompression(this.selectedFile, options);
    } catch (e) { console.error("Compression failed", e); }
  }

  // 2. Upload to Gateway
  this.api.uploadAttachment(fileToUpload, originalName).subscribe({
    next: async (res: any) => {
      // Mapping to your Backend Enum: Image=2, Video=3, Audio=4, File=5
      let msgType = 5; // Default to File (5)
      if (this.selectedFile?.type.startsWith('image/')) msgType = 2;
      else if (this.selectedFile?.type.startsWith('video/')) msgType = 3;
      else if (this.selectedFile?.type.startsWith('audio/')) msgType = 4;

      const payload = {
        ConversationId: this.convId,
        Content: originalName,
        MessageType: msgType,
        AttachmentUrl: res.url, // Full URL from Gateway (e.g., .jpg)
        AttachmentType: this.selectedFile?.type
      };

      // 3. Send via SignalR (Matches your manual send pattern)
      try {
        const val = await this.signalr.sendToConversation(payload);
        console.log("Attachment sent via SignalR: ", val);
        this.clearPreview();
      } catch (err) {
        console.error("SignalR failed", err);
      } finally {
        this.isUploading = false;
      }
    },
    error: () => { this.isUploading = false; }
  });
}

clearPreview() {
  this.selectedFile = null;
  this.filePreviewUrl = null;
  this.isUploading = false;
}

  async handleCall(type: 'Audio' | 'Video') {
    await this.ringtone.unlockAudio();
    if(!this.convId) return;
    console.log(`Starting ${type} call...`);
    // Add your signaling logic here
    if(this.selectedConversationType == "Direct")
    this.callState.startCall(this.convId, this.conversationName, [this.selectedUser?.userId], type);
  else
    {
      const otherParticipantsIds = this.groupParticipantDetails.map((p: any) => p.userId);
      this.callState.startCall(this.convId, this.conversationName, otherParticipantsIds, type);
    }
  }

  handleContextAction(action: ContextAction) {
    if (!this.selectedMessage && !this.isClearChatClicked()) return;

    switch (action) {
      case 'edit':
        this.startEdit(this.selectedMessage);
        break;
      case 'delete':
        this.deleteMessage(this.selectedMessage.messageId);
        break;
      case 'clearChat':
        this.clearAllMessagesUntilNow();
        break;
      case 'seen':
      case 'cancel':
      case 'none':
        break;
    }
    this.selectedMessage = null;
  }

  startEdit(message: any) {
    this.editingMessage = message;
    this.currentMessageContent = message.content;
  }

  cancelEdit() {
    this.editingMessage = null;
    this.currentMessageContent = '';
  }

  deleteMessage(messageId: string) {
    this.api.deleteMessage(messageId).subscribe((res: any) => {
      if (res?.success) {
        // this.messages = this.messages.filter(m => m.messageId !== messageId);
        this.messages = this.messages.filter(m => m.messageId !== messageId);
        //  this.loadMoreMessages() 
      } else {
        console.error('Failed to delete message:', res);
        // OPTIONAL: Add an error notification
        // this.notificationService.showError('Could not delete message. Please try again.');
      }
    },
      (error) => {
        console.error('API Error during message deletion:', error);
        // this.notificationService.showError('An unexpected error occurred.');
      });
  }

  clearAllMessagesUntilNow() {
    if (this.convId == null) return;

    this.api.deleteMessagesUntilNow(this.convId).subscribe((res: any) => {
      if (res && res?.success) {
        this.load()
      }
    })
    this.isClearChatClicked.set(false);
    this.isThreeDotOpen = false;
  }

  // New method to truncate text for the edit bar preview
  truncateText(text: string, limit: number = 30): string {
    return text.length > limit ? text.substring(0, limit) + '...' : text;
  }

  @HostListener('window:resize')
  onResize() {
    // Re-check scroll position on resize
    this.handleScroll();
  }

  handleScroll() {
    if (!this.scrollContainer) return;
    const container = this.scrollContainer.nativeElement;

    // Check if scrolled to bottom
    const isAtBottom = container.scrollHeight - container.scrollTop <= container.clientHeight + 5; // +5 tolerance
    this.isScrolledToBottom.set(isAtBottom);

    // Check if scrolled to top
    const isAtTop = container.scrollTop < 10 && container.scrollHeight > container.clientHeight; // <10 tolerance
    this.isScrolledToTop.set(isAtTop);

    // If scrolled to top and not currently loading, trigger load
    if (isAtTop && this.hasMoreMessages() && !this.isLoadingMore()) {
      // Small debounce before actually calling loadMore, this is handled by the button click
      // The button will only show if isScrolledToTop is true.
    }
    if (isAtBottom) this.markRead();
  }

  scrollToBottom(animate: boolean) {
    if (!this.scrollContainer) return;

    const container = this.scrollContainer.nativeElement;
    // Use requestAnimationFrame for next tick rendering stability
    requestAnimationFrame(() => {
      container.scrollTo({
        top: container.scrollHeight,
        behavior: animate ? 'smooth' : 'auto',
      });
    });
    this.markRead();
  }

  getProfileUrl(m: any) {
    if (m.senderProfileUrl) {
      return this.apiBaseUrl + m.senderProfileUrl;
    }
    const initialChar = (m.senderDisplayName?.[0] || 'U').toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${initialChar}`;
  }

  loadConversationName() {
    if (!this.convId) return;

    this.api.getConversationById(this.convId).subscribe((res: any) => {
      if (!res) return;

      this.CurrentUserId = this.storage.getUser()?.id;
      this.selectedConversationType = res?.type;
      this.isConversationBlocked.set(res?.isBlocked ?? false);
      this.friendshipStatus.set(res.friendStatus);
      this.BlockedUserId = res.blockedByUserId;
      if (res?.isBlocked || res?.blockedByUserId) {
        this.BlockedUserName = res.participants?.find((p: any) => p.userId == res?.blockedByUserId).displayName;
      }
      if (res.type === "Direct") {
        // 1. Find the other participant
        const other = res.participants?.find((p: any) => p.userId !== this.CurrentUserId);

        // 2. Store in your central variable for future use (Modal, etc.)
        this.selectedUser = other;
         
        const live = this.signalr.getPresence(other.userId);
        this.userPresenceStatus = {
          isOnline : live.isOnline,
          lastSeen : live.lastSeen || other.lastSeen
        }
        // 3. Set Header Details
        this.conversationName = other?.displayName ?? "Unknown";

        if (other?.profilePictureUrl) {
          this.headerProfilePic = this.apiBaseUrl + other.profilePictureUrl;
        } else {
          // Fallback to placeholder if no URL exists
          const initial = (other?.displayName?.[0] || 'U').toUpperCase();
          this.headerProfilePic = `https://placehold.co/100x100/1F2937/ffffff?text=${initial}`;
        }
      }
      else {
        // Group Conversation Logic
        this.selectedUser = null; // Clear or set to group metadata
        this.userPresenceStatus = null;
        this.conversationName = res?.name ?? "Group Chat";
        this.groupParticipantDetails = res.participants?.filter((p: any) => p.userId !== this.CurrentUserId) || [];
        const groupInitial = (res?.name?.[0] || 'G').toUpperCase();
        this.headerProfilePic = `https://placehold.co/100x100/1F2937/ffffff?text=${groupInitial}`;
      }
    });
  }

  toggleConversationBlock() {
    if (!this.convId) return;

    this.api.toggleConversationBlock(this.convId, this.isConversationBlocked() ? false : true).subscribe((res: any) => {
      this.loadConversationName();
      console.log("conv blocked ", this.isConversationBlocked())
    })
  }

handleFollowBack() {
  this.router.navigate(['/people']);
}

getPresenceText(): string {
  if (!this.userPresenceStatus) return '';
  if (this.userPresenceStatus.isOnline) return 'Online';
  if (!this.userPresenceStatus.lastSeen) return 'Offline';

  const lastSeenDate = new Date(this.userPresenceStatus.lastSeen);
  const now = new Date();
  
  // Format time (e.g., 4:30 PM)
  const timeStr = new Intl.DateTimeFormat('en-US', { 
    hour: 'numeric', 
    minute: 'numeric', 
    hour12: true 
  }).format(lastSeenDate);

  // Check if it was today
  if (lastSeenDate.toDateString() === now.toDateString()) {
    return `last seen today at ${timeStr}`;
  }

  // Check if it was yesterday
  const yesterday = new Date();
  yesterday.setDate(now.getDate() - 1);
  if (lastSeenDate.toDateString() === yesterday.toDateString()) {
    return `last seen yesterday at ${timeStr}`;
  }

  // Fallback to date
  return `last seen ${lastSeenDate.toLocaleDateString()} at ${timeStr}`;
}

goBackToList() {
  // Navigate to the base chat route to trigger the [class.hidden] logic
  this.router.navigate(['/chats']);
}

isMobileView(): boolean {
    return window.innerWidth < 1024;
  }
  
  ngOnDestroy() { this.subs.forEach(s => s.unsubscribe()); }
}
