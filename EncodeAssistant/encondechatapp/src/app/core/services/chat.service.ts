import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Message } from '../models/message.model';

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  // Stub: sin implementación
  getMessages(contactId: string | number): Observable<Message[]> {
    return of([]);
  }

  // Stub: sin implementación
  sendMessage(contactId: string | number, text: string): Observable<Message | null> {
    return of(null);
  }

  // Stub: sin implementación
  onNewMessage(): Observable<Message> {
    return of(null as any);
  }
}
