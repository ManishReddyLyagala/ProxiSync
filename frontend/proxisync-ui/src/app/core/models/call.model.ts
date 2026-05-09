export type CallType = 'Audio' | 'Video';
export type CallStatus = 'Ringing' | 'Ongoing' | 'Ended' | 'Missed';

export type ParticipantCallStatus =
  | 'Invited'
  | 'Joined'
  | 'Left'
  | 'Missed'
  | 'Rejected';

 export interface CallParticipantDto {
  id: string;
  userId: string;
  status: ParticipantCallStatus;
  joined: boolean;
  joinedAt?: string | null;
  left: boolean;
  leftAt?: string | null;
  isMicEnabled: boolean;
  isVideoEnabled: boolean;
  invitedAt?: string | null;
}

export interface CallSessionDto {
  callId: string;
  conversationId: string;
  roomName: string;
  type: CallType;
  status: CallStatus;
  isGroupCall: boolean;
  startedByUserId: string;
  callerName: string;
  startedAt: string;
  endedAt?: string | null;
  participants: CallParticipantDto[];
}

export interface StartCallDto {
  conversationId: string;
  type: CallType;
  participantUserIds: string[];

  callerMicEnabled: boolean;
  callerVideoEnabled: boolean;
}

export interface StartCallResponseDto {
  callId: string;
  conversationId: string;
  roomName: string;
  callerName: string;
  type: CallType;
  status: CallStatus;
  isGroupCall: boolean;
  receiverOffline: boolean;
}

export interface GenerateTokenResponseDto {
  callId: string;
  roomName: string;
  token: string;
  liveKitUrl: string;
}

export interface JoinCallDto {
  micEnabled: boolean;
  videoEnabled: boolean;
}

export interface ParticipantMediaDto {
  isMicEnabled: boolean;
  isVideoEnabled: boolean;
}


export interface CallHistoryParticipantDto {
  userId: string;
  displayName: string;
  profilePictureUrl: string;
  status: string; // Ringing, Joined, Rejected, Missed, etc.
  isOnline: boolean;
}

export interface CallLogDto {
  id: string;
  conversationId: string;
  displayName: string;
  profilePictureUrl: string;
  type: string;
  status: string;
  startedAt: Date;
  isIncoming: boolean;
  isGroupCall: boolean;
  participants: CallHistoryParticipantDto[];
}
