export interface UserDto {
  id: string;
  username: string;
  displayName?: string;
  profilePictureUrl?: string | null;
  token?: string;
  isOnline?: boolean;
}
