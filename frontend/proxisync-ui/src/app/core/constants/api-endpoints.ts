import { environment } from "../../../environments/environment";
// export const environment = {
//   gatewayUrl: 'http://localhost:5181',
//   chatUrl: 'http://localhost:5226',
//   callUrl: 'https://localhost:7211'
// };

export const API = {
  AUTH: {
    REGISTER: `${environment.gatewayUrl}/api/auth/register`,
    LOGIN: `${environment.gatewayUrl}/api/auth/login`,
    PROFILE: `${environment.gatewayUrl}/api/user/profile`,
    GET_REFRESH_TOKEN: `${environment.gatewayUrl}/api/auth/refreshtoken`,
    LOGOUT: `${environment.gatewayUrl}/api/auth/logout`
  },
   FRIENDS: {
    SEND_REQUEST: (toUserId: string) => `${environment.gatewayUrl}/api/friend/request/${toUserId}`,
    RESPOND_TO_REQUEST: (requestId: string) => `${environment.gatewayUrl}/api/friend/respond/${requestId}`,
    CANCEL_SENT_REQUEST: (requestId: string) => `${environment.gatewayUrl}/api/friend/cancel/${requestId}`,
    Toggle_Follow_REQUEST: (requestId: string) => `${environment.gatewayUrl}/api/friend/toggleUnFollow/${requestId}`,
    GET_RECIVED_REQUESTS: `${environment.gatewayUrl}/api/friend/respond/recived`,
    GET_SENT_REQUESTS: `${environment.gatewayUrl}/api/friend/request/sent`,
    GET_MY_CONTACTS: `${environment.gatewayUrl}/api/friend/contacts`,
    GET_MUTUAL_CONTACT_LIST: `${environment.gatewayUrl}/api/friend/mutualContactList`,

  },
  CHAT: {
    CONVERSATIONS: `${environment.chatUrl}/api/Conversations`,
    CONVERSATIONById: (conversationId: string) => `${environment.chatUrl}/api/conversations/${conversationId}`,
    CONVERSATIONByUSERId: (otherUserId: string) => `${environment.chatUrl}/api/conversations/userConversation/${otherUserId}`,
    CREATE_CONVERSATION: `${environment.chatUrl}/api/conversations`,
    BLOCK_UNBLOCK_USER: (conversationId: string) => `${environment.chatUrl}/api/conversations/${conversationId}/toggleBlock`,
    MESSAGES_HISTORY: (conversationId: string) => `${environment.chatUrl}/api/messages/history/${conversationId}`,
    SEND: `${environment.chatUrl}/api/messages/send`,
    DELETE_MESSAGE: (messageId: string) => `${environment.chatUrl}/api/messages/delete/${messageId}`,
    DELETE_MESSAGES_UNTIL: (conversationId: string) => `${environment.chatUrl}/api/messages/deleteUntil/${conversationId}`,
    // EDIT_MESSAGE: `${environment.chatUrl}/api/messages/update`,
    MARK_READ: `${environment.chatUrl}/api/messages/mark-read`,
    GET_MESSAGE_SEEN_LIST: `${environment.chatUrl}/api/messages/message-read-list`,
    ADD_PARTICIPANT: (convId: string) => `${environment.chatUrl}/api/conversations/${convId}/participants`,
    REMOVE_PARTICIPANT: (convId: string, userId: string) => `${environment.chatUrl}/api/conversations/${convId}/participants/${userId}`,
    DELETE_GROUP: (convId: string) => `${environment.chatUrl}/api/conversations/delete/${convId}`,
    LEAVE_FROM_GROUP: (convId: string) => `${environment.chatUrl}/api/conversations/${convId}/leave`,
  },
  HUBS: {
    CHAT: `${environment.chatUrl}/chathub`,
    CALL: `${environment.callUrl}/callhub`,
  },
  USER: {
    GET_USER_DETAILS_BY_ID: (userId: string) => `${environment.gatewayUrl}/api/User/${userId}`,
    GET_ALL_USERS: `${environment.gatewayUrl}/api/User/all`,
    GET_MY_PROFILE: `${environment.gatewayUrl}/api/User/me`,
    UPDATE_MY_PROFILE: `${environment.gatewayUrl}/api/User/update`,
    DELETE_MY_PROFILE: `${environment.gatewayUrl}/api/User/delete`,
    RESTORE_ACCOUNT: `${environment.gatewayUrl}/api/User/restoreaccount`,
  },
  ATTACHMENTS: {
    UPLOAD_ATTACHMENT: `${environment.gatewayUrl}/api/attachments/upload`,
  },
  CALLS: {
    START: `${environment.callUrl}/api/calls/start`,
    TOKEN: (callId: string) => `${environment.callUrl}/api/calls/${callId}/token`,
    JOIN: (callId: string) => `${environment.callUrl}/api/calls/${callId}/join`,
    END: (callId: string) => `${environment.callUrl}/api/calls/${callId}/end`,
    REJECT: (callId: string) => `${environment.callUrl}/api/calls/${callId}/reject`,
    LEAVE: (callId: string) => `${environment.callUrl}/api/calls/${callId}/leave`,
    CANCEL: (callId: string) => `${environment.callUrl}/api/calls/${callId}/cancel`,
    UPDATE_MEDIA: (callId: string) => `${environment.callUrl}/api/calls/${callId}/participant/media`,
    GET: (callId: string) => `/api/calls/${callId}`,
    GETUSERCALLHISTORY:  `${environment.callUrl}/api/calls/history`
  },
};