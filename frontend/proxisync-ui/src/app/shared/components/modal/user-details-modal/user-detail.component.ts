import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, Output, signal } from "@angular/core";
import { environment } from "../../../../../environments/environment";
import { ChatApiService } from "../../../../core/api/chat-api.service";
import { Router } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { FriendRequestAPIService } from "../../../../core/api/friend-request-api.service";

@Component({
    selector: 'app-user-details-modal',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './user-detail.component.html'
})
export class UserDetailModalComponent {
    @Input() convId!: string; 
  @Input() type: 'Direct' | 'Group' = 'Direct';
  @Input() selectedUser: any = null; // Data from getUserDetailsById
  @Input() currentUserId!: string;
  @Output() conversationDeleted = new EventEmitter<void>();
    @Output() closeUserDetails = new EventEmitter<void>();

    readonly ROLES = [
  { label: 'Member', value: 'member' },
  { label: 'Admin', value: 'admin' }
];
    // Signals for Group Logic
  groupDetails = signal<any>(null);
  isLoading = signal(false);
  isAdmin = signal(false);
  isCopied = signal(false);
  showAddParticipantView = false;
  searchTerm = '';
friends: any[] = [];
filteredFriends: any[] = [];
selectedParticipants = new Map<string, { userId: string, role: string }>();

  constructor(private chatService: ChatApiService, private router: Router, private friendApi: FriendRequestAPIService) {}
  ngOnInit() {
    if (this.type === 'Group') {
      this.loadGroupDetails();
    }
  }

  loadGroupDetails() {
    this.isLoading.set(true);
    this.chatService.getConversationById(this.convId).subscribe({
      next: (res : any) => {
        this.groupDetails.set(res);
        const me = res?.participants.find((p: any) => p.userId === this.currentUserId);
        this.isAdmin.set(me?.role === 'Admin' || me?.role === 'Owner');
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadFriends() {
  this.friendApi.getMutualContactList().subscribe({
    next: (res: any) => {
      const currentParticipants = this.groupDetails()?.participants || [];
      const participantIds = new Set(currentParticipants.map((p: any) => p.userId));
      
      // Only show friends who are NOT already in the group
      this.friends = res.data.filter((f: any) => !participantIds.has(f.userId));
      this.filteredFriends = [...this.friends];
    }
  });
}
  // --- Group Actions ---
  removeParticipant(userId: string) {
    
    if (confirm('Remove user?')) {
      this.chatService.removeParticipant(this.convId, userId).subscribe(() => {
        if(userId == this.currentUserId){
          this.chatService.getConversations().subscribe();
        }else{
          this.loadGroupDetails()
        }
    });
    }
  }

  deleteGroup() {
    if (confirm('Delete this group and all messages?')) {
      this.chatService.deleteGroup(this.convId).subscribe(() => {
        this.closeUserDetails.emit();
        this.conversationDeleted.emit();
        this.router.navigate(['/']);
      });
    }
  }

  exitGroup() {
  if (confirm('Are you sure you want to leave this group?')) {
    this.chatService.leaveGroup(this.convId).subscribe({
      next: () => {
        this.closeModal();
        this.conversationDeleted.emit(); // Refresh the sidebar list
      },
      error: (err: any) => console.error('Failed to leave group', err)
    });
  }
}

// Logic for the Add Participant feature
openAddParticipant() {
  this.loadFriends();
  this.showAddParticipantView = true;
  this.selectedParticipants.clear();
}


// --- Helpers ---
filterFriends() {
  const term = this.searchTerm.toLowerCase();
  this.filteredFriends = this.friends.filter((f: any) => 
    f?.userName?.toLowerCase()?.includes(term)
  );
}

toggleSelection(friend: any) {
  if (this.selectedParticipants.has(friend.userId)) {
    this.selectedParticipants.delete(friend.userId);
  } else {
    this.selectedParticipants.set(friend.userId, { userId: friend.userId, role: 'member' });
  }
}

isAllSelected(): boolean {
  return this.filteredFriends.length > 0 && 
         this.filteredFriends.every(f => this.selectedParticipants.has(f.userId));
}

// Select/De-select all visible (filtered) friends
toggleSelectAll() {
  if (this.isAllSelected()) {
    this.filteredFriends.forEach(f => this.selectedParticipants.delete(f.userId));
  } else {
    this.filteredFriends.forEach(f => {
      if (!this.selectedParticipants.has(f.userId)) {
        this.selectedParticipants.set(f.userId, { userId: f.userId, role: 'member' });
      }
    });
  }
}

setRole(userId: string, role: string, event: Event) {
  event.stopPropagation();
  const participant = this.selectedParticipants.get(userId);
  if (participant) {
    this.selectedParticipants.set(userId, { ...participant, role });
  }
}

confirmAddParticipants() {
  const payload = Array.from(this.selectedParticipants.values());
  console.log("Add Participants Payload:", payload);
  // Your API call logic here
  this.chatService.addParticipant(this.convId, payload).subscribe({
    next: ()=> {
       this.loadGroupDetails()
    },
    error: (err)=> console.log(err)
  });
  // alert(payload)
  this.showAddParticipantView = false;
  this.selectedParticipants.clear();
}

    closeModal() {
        this.closeUserDetails.emit();
    }

    copyToClipboard(text: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.isCopied.set(true);
      setTimeout(() => this.isCopied.set(false), 2000);
    });
  }

    getProfileUrl(profile: string | null, name?: string) {
    if (profile) return environment.gatewayUrl + profile;
    const initial = (name?.[0] || 'U').toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${initial}`;
  }
}