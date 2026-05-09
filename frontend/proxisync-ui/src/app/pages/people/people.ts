import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, HostListener, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UserApiService } from '../../core/api/user-api.service';
import { FriendRequestAPIService } from '../../core/api/friend-request-api.service';
import { environment } from '../../../environments/environment';
import { StorageService } from '../../core/services/storage.service';
import { ChatApiService } from '../../core/api/chat-api.service';
import { Router } from '@angular/router';

// 1. Data Interfaces
// export interface AppUser {
//   id: string;
//   displayName: string;
//   profilePictureUrl: string;
//   isOnline: boolean;
//   bio: string;
// }

// export interface FriendRequest {
//   id: string;
//   fromUserId: string;
//   toUserId: string;
//   status: number;
//   message?: string;
//   // These objects should be populated by your .NET backend
//   sender?: AppUser; 
//   receiver?: AppUser;
// }

// 2. UI-Specific Interface to prevent Template Errors
export interface PersonUI {
  id: string;          // Action ID (Request ID or User ID)
  userId: string;      // The actual User's ID
  displayName: string;
  profilePictureUrl: string;
  isOnline: boolean;
  status: number;
  bio: string;
  mutualCount: number;
  message?: string;    // Added for pending/sent messages
}

@Component({
  selector: 'app-people',
  imports: [CommonModule, FormsModule],
  templateUrl: './people.html',
  styleUrl: './people.css',
  standalone: true
})

export class People implements OnInit{
activeTab = signal<'discover' | 'recived' | 'sent' | 'contacts'>('discover');
  isLoading = signal<boolean>(true);
  searchQuery = signal<string>('');
  tabs = ['discover', 'recived', 'sent', 'contacts'] as const;
  // Dimensions for the sliding pill
  pillWidth = signal<number>(0);
  pillLeft = signal<number>(0);
  readonly defaultAvatar = 'https://placehold.co/100x100/1F2937/ffffff?text=';
   

  constructor(private userApi: UserApiService, private friendsApi: FriendRequestAPIService, private storage: StorageService, private chatApi : ChatApiService, private router: Router){}
  // Data Signals
  users = signal<PersonUI[]>([]);
  pendingRequests = signal<PersonUI[]>([]);
  sentRequests = signal<PersonUI[]>([]);
  contacts = signal<PersonUI[]>([]);

  // Modal State
  requestModalUser = signal<PersonUI | null>(null);
  requestMessage = signal<string>('');

  ngOnInit(): void {
    this.loadData();
    this.updatePillPosition();
  }

 loadData() {
    this.isLoading.set(true);
    // Load all data in parallel
    this.loadUsersList();
    this.loadRecivedRequests();
    this.loadSentRequests();
    this.loadContacts();
  }

  // --- MAPPING HELPER ---
  private mapUserToUI(user: any, requestId?: string, msg?: string): PersonUI {
    return {
      id: requestId || user.id,
      userId: user.id,
      displayName: user.displayName || 'Unknown',
      profilePictureUrl: user.profilePictureUrl 
        ? `${environment.gatewayUrl}${user.profilePictureUrl}` 
        : this.defaultAvatar + (user.displayName?.[0] || 'U').toUpperCase(),
      isOnline: user.isOnline,
      status : user?.status,
      bio: user.bio ?? 'Available to connect',
      mutualCount: Math.floor(Math.random() * 10),
      message: msg
    };
  }

 loadUsersList() {
    this.userApi.getAllUsersList().subscribe({
      next: (res: any) => {
        if (res.success) {
          this.users.set(res.data.map((u: any) => this.mapUserToUI(u)));
        }
        this.isLoading.set(false);
        setTimeout(() => this.updatePillPosition(), 100);
      },
      error: (err) => {
        console.error('Fetch users error:', err);
        this.isLoading.set(false);
      }
    });
    }

   loadRecivedRequests() {
    this.friendsApi.getRecivedRequets().subscribe({
      next: (res: any) => {
        if (res.success) {
          // In "Received", the person we see is the SENDER
          this.pendingRequests.set(res.data.map((req: any) => 
            this.mapUserToUI(req.sender, req.id, req.message)
          ));
        }
      },
    error: (err) => {
      console.error('Fetch received req error:', err);
    }
  });
}

loadSentRequests() {
    this.friendsApi.getSentRequsets().subscribe({
      next: (res: any) => {
        if (res.success && res.data) {
         const mappedData = res.data.map((req: any) => ({
          ...this.mapUserToUI(req.receiver, req.id, req.message),
          status: req.status 
        }));

        this.sentRequests.set(mappedData);
        }
      }
    });
  }

  loadContacts() {
    // TODO : need to get only mutual contact list
    this.friendsApi.getMyContactList().subscribe({
      next: (res: any) => {
        if (res.success) {
          const mappedData = res.data.map((req: any) => ({
          ...this.mapUserToUI(this.storage.getUser()?.id == req.sender?.id ?  req.receiver : req.sender, req.id, req.message),
          status: req.status 
        }));
          this.contacts.set(mappedData);
        }
      }
    });
  }

  // Handle window resize to keep the pill aligned
  @HostListener('window:resize')
  onResize() {
    this.updatePillPosition();
  }

 // --- FILTERED DISPLAY LIST ---
  displayList = computed(() => {
    let list: PersonUI[] = [];
    switch (this.activeTab()) {
      case 'discover': list = this.users(); break;
      case 'recived':  list = this.pendingRequests(); break;
      case 'sent':     list = this.sentRequests(); break;
      case 'contacts': list = this.contacts(); break;
    }

    const query = this.searchQuery().toLowerCase();
    if (!query) return list;

    return list.filter(p => 
      p.displayName.toLowerCase().includes(query) || 
      p.bio.toLowerCase().includes(query)
    );
  });

  // Action Methods
  openRequestModal(person: PersonUI) {
    this.requestModalUser.set(person);
    this.requestMessage.set(`Hi ${person.displayName}, I'd love to connect!`);
  }

  
  // --- ACTIONS ---

  confirmSendRequest() {
    const user = this.requestModalUser();
    if (user) {
      this.friendsApi.SendFriendRequest(user.userId, this.requestMessage()).subscribe((res: any) => {
        if (res.success) {
          this.users.update(list => list.filter(u => u.userId !== user.userId));
          this.loadSentRequests(); // Refresh sent tab
          this.requestModalUser.set(null);
        }
      });
    }
  }

  respondToRequest(requestId: string, accept: boolean) {
    this.friendsApi.respondToRecivedRequest(requestId, accept).subscribe((res: any) => {
      if (res.success) {
        // Remove from UI list immediately
        this.pendingRequests.update(list => list.filter(r => r.id !== requestId));
        if (accept) this.loadContacts(); // Refresh contacts if accepted
      }
    });
  }

  cancelRequest(requestId: string) {
    this.friendsApi.cancelSentRequest(requestId).subscribe((res: any) => {
      if (res && res?.success) {
        this.sentRequests.update(list => list.filter(r => r.id !== requestId));
      }
    });
  }

  removeContact(requestId: string) {
    if (confirm('Remove this connection?')) {
      this.friendsApi.toggleUnFollowRequest(requestId).subscribe((res: any) =>{
        if(res && res?.success){
          this.contacts.update(list => list.filter(c => c.id !== requestId));
          alert(res?.message);
        }else{
          console.error(res?.message);
        }
      })

    }
  }

  openConversation(otherUserId: string){
    if(!otherUserId) return;
    this.chatApi.getConversationByUserId(otherUserId).subscribe({
      next: (res: any)=>{
        console.log(res);
        this.router.navigate(['/chats/conv/', res?.conversationId]);
      }
    })
  }

  updatePillPosition() {
    const activeElement = document.getElementById('tab-' + this.activeTab());
    if (activeElement) {
      this.pillWidth.set(activeElement.offsetWidth);
      this.pillLeft.set(activeElement.offsetLeft);
      
      // Auto-scroll the tab container on mobile
      activeElement.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
    }
  }

  setActiveTab(tab: any) {
    this.activeTab.set(tab);
    this.updatePillPosition();
  }
}
