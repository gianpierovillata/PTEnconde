export interface Contact {
  id: string | number;
  name?: string;
  username?: string;
  lastMessage?: string;
  /** Fecha del último mensaje (ISO en UTC); se usa para ordenar y mostrar la hora. */
  lastMessageAt?: Date | string;
  unreadCount: number;
  avatarUrl?: string;
  isActive?: boolean;
}
