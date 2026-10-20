import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfigService } from '../config/app-config.service';
import { Message } from '../models/message.model';

/** Mensajes de una conversación y envío de respuestas del operador. */
@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/api/v1/contacts`;
  }

  /** Historial de la conversación, en orden cronológico. */
  getMessages(contactId: string | number): Observable<Message[]> {
    return this.http.get<Message[]>(`${this.baseUrl}/${contactId}/messages`);
  }

  /** Respuesta del operador: se persiste y se emite por SignalR. */
  sendMessage(contactId: string | number, text: string): Observable<Message> {
    return this.http.post<Message>(`${this.baseUrl}/${contactId}/messages`, { text });
  }
}
