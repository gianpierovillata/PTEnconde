import { Component } from '@angular/core';
import { ChatHeader } from './chat-header/chat-header';
import { MessageList } from './message-list/message-list';
import { MessageInput } from './message-input/message-input';

@Component({
  imports: [ChatHeader, MessageList, MessageInput],
  selector: 'app-chat',
  styleUrl: './chat.css',
  templateUrl: './chat.html',
})
export class Chat {}
