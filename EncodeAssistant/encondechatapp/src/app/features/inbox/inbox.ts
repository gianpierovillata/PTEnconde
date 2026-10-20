import { Component } from '@angular/core';
import { Sidebar } from './sidebar/sidebar';
import { Chat } from './chat/chat';

@Component({
  imports: [Sidebar, Chat],
  selector: 'app-inbox',
  styleUrl: './inbox.css',
  templateUrl: './inbox.html',
})
export class Inbox {}
