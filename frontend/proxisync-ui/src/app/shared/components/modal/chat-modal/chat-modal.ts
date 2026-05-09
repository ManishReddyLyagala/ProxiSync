import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FriendRequestAPIService } from '../../../../core/api/friend-request-api.service';
import { ChatApiService } from '../../../../core/api/chat-api.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../../environments/environment';

export interface Contact {
  userId: string;
  userName: string;
  profilePictureUrl: string;
}

export interface CreateConversationDto {
  type: string;
  name?: string;
  participantIds: string[];
}

@Component({
  selector: 'app-chat-modal',
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-modal.html',
  styleUrl: './chat-modal.css',
})
export class ChatModal implements OnInit{

  @Output() conversationCreated = new EventEmitter<void>();

  isOpen = false;
  view: 'menu' | 'select-friends' | 'group-details' = 'menu';
  apiBaseUrl = environment.gatewayUrl;
  // Data State
  friends: Contact[] = [];
  filteredFriends: Contact[] = [];
  searchTerm: string = '';
  selectedUserIds = new Set<string>();
  groupName: string = '';
  
  loading = false;
  error: string | null = null;

  constructor(private friendApi: FriendRequestAPIService, private chatService: ChatApiService){}
  ngOnInit() {
    this.loadFriends();
  }

  // API Calls
  loadFriends() {
    this.friendApi.getMutualContactList().subscribe({
      next: (res: any) => {
        console.log("contacts ", res);
        this.friends = res.data; // Adjusted for your ServiceResponse structure
        this.filteredFriends = res.data;
      }
    });
  }

  // Navigation & Actions
  open() {
    this.isOpen = true;
    this.resetModal();
  }

  close() {
    this.isOpen = false;
  }

  resetModal() {
    this.view = 'menu';
    this.selectedUserIds.clear();
    this.groupName = '';
    this.error = null;
    this.searchTerm = '';
    this.filteredFriends = [...this.friends];
  }

  filterFriends() {
    const term = this.searchTerm.toLowerCase();
    this.filteredFriends = this.friends.filter((f: any) => 
      f?.userName?.toLowerCase()?.includes(term)
    );
  }

  toggleSelection(userId: string) {
    if (this.selectedUserIds.has(userId)) {
      this.selectedUserIds.delete(userId);
    } else {
      this.selectedUserIds.add(userId);
    }
  }

  createGroup() {
    if (!this.isGroupValid()) return;

    this.loading = true;
    const dto: CreateConversationDto = {
      type: 'group',
      name: this.groupName,
      participantIds: Array.from(this.selectedUserIds)
    };

    this.chatService.createNewConversation(dto).subscribe({
      next: () => {
        this.loading = false;
        this.conversationCreated.emit();
        this.close();
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || "Group name already exists or invalid data.";
      }
    });
  }

  // Validation
  isGroupValid(): boolean {
    return this.groupName.trim().length >= 3 && this.selectedUserIds.size > 0;
  }

  getProfilePic(friend: Contact){
     if (friend.profilePictureUrl) {
      return friend.profilePictureUrl.startsWith('/') ? this.apiBaseUrl + friend.profilePictureUrl : friend.profilePictureUrl;
    }
    const initialChar = (friend.userName?.[0]).toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${initialChar}`;
  }
}
