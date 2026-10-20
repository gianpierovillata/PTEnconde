export interface Contact {
  id: string | number;
  name?: string;
  username?: string;
  lastMessage?: string;
  unreadCount: number;
  avatarUrl?: string;
  isActive?: boolean;
}
