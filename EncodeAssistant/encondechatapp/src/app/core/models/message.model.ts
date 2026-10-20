export interface Message {
  id: string | number;
  contactId: string | number;
  text: string;
  direction: 'incoming' | 'outgoing';
  createdAt: Date | string;
  read?: boolean;
}
