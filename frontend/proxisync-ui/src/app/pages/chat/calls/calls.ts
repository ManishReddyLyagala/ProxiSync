import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CallApi } from '../../../core/api/call-api.service';
import { Router } from '@angular/router';
import { CallLogDto, StartCallDto } from '../../../core/models/call.model';
import { CallStateService } from '../../../core/services/call-state.service';
import { environment } from '../../../../environments/environment';
import { FriendRequestAPIService } from '../../../core/api/friend-request-api.service';
import { ChatApiService } from '../../../core/api/chat-api.service';


@Component({
  selector: 'app-calls',
  imports: [CommonModule, FormsModule],
  templateUrl: './calls.html',
  styleUrl: './calls.css',
})
export class Calls implements OnInit{
callLogs: CallLogDto[] = [];
  filteredLogs: CallLogDto[] = [];
  callHistorySearchQuery: string = '';
  activeFilter: string = 'All';
  contacts: any[] = []; // Replace 'any' with your User/Contact model
filteredContacts: any[] = [];
contactSearchQuery: string = '';
  
  selectedCallForModal: CallLogDto | null = null;
  showContactPicker: boolean = false;

  constructor(private callApiService: CallApi, private friendApiService: FriendRequestAPIService, private callStateService: CallStateService, private conversationApiService: ChatApiService) {}

  ngOnInit(): void {
    this.loadHistory();
    this.loadContacts();
  }

  loadHistory() {
    this.callApiService.getUserCallHistory().subscribe(data => {
      this.callLogs = data;
      this.filterCalls();
    });
  }

  loadContacts() {
  // Assuming you have a contact service, fetch your list here
  this.friendApiService.getMutualContactList().subscribe((data: any) => {
    this.contacts = data.data as any[];
    this.filteredContacts = data.data as any[];
  });
}

  initiateNewCall(contact: any, mode: 'Audio' | 'Video') {
    this.showContactPicker = false;
    this.conversationApiService.getConversationByUserId(contact?.userId).subscribe((res: any) => {
      const conversationId = res?.conversationId;
      this.callStateService.startCall(
        conversationId,
        contact.displayName,
        [contact.userId],
        mode
      );
    })
  }

filterContacts() {
  if (!this.contactSearchQuery) {
    this.filteredContacts = this.contacts;
    return;
  }
  const query = this.contactSearchQuery.toLowerCase();
  this.filteredContacts = this.contacts.filter((c: any) => 
    c?.userName?.toLowerCase().includes(query)
  );
}

  filterCalls() {
    let filtered = this.callLogs;

    if (this.activeFilter !== 'All') {
      filtered = filtered.filter(log => log.status === this.activeFilter);
    }

    if (this.callHistorySearchQuery) {
      filtered = filtered.filter(log => 
        log.displayName.toLowerCase().includes(this.callHistorySearchQuery.toLowerCase())
      );
    }

    this.filteredLogs = filtered;
  }

  onStartCall(target: CallLogDto, mode: 'Audio' | 'Video') {
    const participantUserIds = target.participants
        .map(p => p.userId);
    this.callStateService.startCall(target.conversationId, target.displayName, participantUserIds, mode)
  }

  openParticipantModal(call: CallLogDto) {
    this.selectedCallForModal = call;
  }

  getProfilePicUrl(profileUrl: string){
    return environment.gatewayUrl + profileUrl;
  }
}
