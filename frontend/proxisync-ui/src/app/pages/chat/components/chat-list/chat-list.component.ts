import { Component, OnInit } from "@angular/core";
import { ChatApiService } from "../../../../core/api/chat-api.service";
import { ActivatedRoute, Router } from "@angular/router";
import { CommonModule } from "@angular/common";

export interface ConversationDto {
  conversationId: string;
  name: string;
  profilePictureUrl?: string;
  lastMessageContent?: string;
  lastMessageSentAt?: string;
  unreadCount: number;
  isOnline: boolean;
}

@Component({
  selector: 'app-chat-list',
  templateUrl: './chat-list.component.html',
  imports: [CommonModule]
})
export class ChatListComponent implements OnInit {
  conversations: any = [];
  activeId?: string;

  constructor(
    private chatService: ChatApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadChats();

    this.route.params.subscribe(p => {
      this.activeId = p['conversationId'];
    });
  }

  loadChats() {
    this.chatService.getConversations().subscribe(res => {
      this.conversations = res;
    });
  }

  openChat(conv: ConversationDto) {
    this.router.navigate(['/chats', conv.conversationId]);
  }
}
