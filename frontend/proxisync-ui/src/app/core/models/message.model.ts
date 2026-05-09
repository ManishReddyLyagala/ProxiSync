export interface Message {
  messageId: string;
  conversationId: string;
  senderId: string;
  senderDisplayName?: string;
  senderProfileUrl?: string | null;
  content?: string;
  attachmentUrl?: string | null;
  messageType?: number;
  sentAt: string;
  isSeen: boolean;
}
