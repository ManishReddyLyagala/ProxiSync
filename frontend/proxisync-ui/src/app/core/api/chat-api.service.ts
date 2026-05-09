import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API } from '../constants/api-endpoints';
import { Observable } from 'rxjs';
import { CreateConversationDto } from '../../shared/components/modal/chat-modal/chat-modal';
@Injectable({ providedIn: 'root' })
export class ChatApiService {
  constructor(private http: HttpClient) { }
  getConversations(page: number = 1, size: number = 10) {
    return this.http.get(`${API.CHAT.CONVERSATIONS}?pageNumber=${page}&pageSize=${size}`);
  }
  getConversationById(convId: string) { return this.http.get(API.CHAT.CONVERSATIONById(convId)) }
  getConversationByUserId(otherUserId: string) { return this.http.get(API.CHAT.CONVERSATIONByUSERId(otherUserId)) }
  createNewConversation(createConversation: CreateConversationDto) {
    return this.http.post(API.CHAT.CREATE_CONVERSATION, createConversation);
  }
  toggleConversationBlock(conversationId: string, block: boolean) {
    return this.http.post(API.CHAT.BLOCK_UNBLOCK_USER(conversationId) + `?block=${block}`, null);
  }
  getMessages(convId: string, page = 1, pageSize = 50) {
    return this.http.get(API.CHAT.MESSAGES_HISTORY(convId) + `?page=${page}&pageSize=${pageSize}`);
  }
  sendMessage(payload: any) { return this.http.post(API.CHAT.SEND, payload); }
  deleteMessage(messageId: string) {
    return this.http.delete(API.CHAT.DELETE_MESSAGE(messageId))
  }
  deleteMessagesUntilNow(conversationId: string) {
    return this.http.delete(API.CHAT.DELETE_MESSAGES_UNTIL(conversationId));
  }
  // editMessage(payload: { messageId: string; newContent: string }){
  //   return this.http.put(API.CHAT.EDIT_MESSAGE,payload);
  // }
  markRead(payload: any) { return this.http.post(API.CHAT.MARK_READ, payload); }
  getMessageReadUsers(conversationId: string, messageId: string): Observable<any> {
    // const params = new HttpParams()
    //   .set('conversationId', conversationId)
    //   .set('messageId', messageId);

    return this.http.get(`${API.CHAT.GET_MESSAGE_SEEN_LIST}?conversationId=${conversationId}&messageId=${messageId}`,);
  }
  addParticipant(convId: string, dtos: any) { return this.http.post(API.CHAT.ADD_PARTICIPANT(convId), dtos); }
  removeParticipant(convId: string, userId: string) {
    return this.http.delete(API.CHAT.REMOVE_PARTICIPANT(convId, userId));
  }
  deleteGroup(convId: string) {
    return this.http.delete(API.CHAT.DELETE_GROUP(convId));
  }

  leaveGroup(convId: string) {
    return this.http.delete(API.CHAT.LEAVE_FROM_GROUP(convId));
  }

  // Add this to your existing ChatService
uploadAttachment(file: File | Blob , originalName: string) {
  const formData = new FormData();
 formData.append('file', file, originalName);

  return this.http.post(API.ATTACHMENTS.UPLOAD_ATTACHMENT, formData);
}
}
