export interface Participant {
  userId: string;
  displayName?: string;
  profilePictureUrl?: string | null;
  role?: string;
}
export interface Conversation {
  id: string;
  name?: string | null;
  type: 'direct' | 'group';
  participants: Participant[];
  lastMessage?: any;
  unreadCount?: number;
}
