import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConfigService } from '../config/app-config.service';
import { Contact } from '../models/contact.model';

/** Contactos de la bandeja (usuarios de Telegram que escribieron al bot). */
@Injectable({
  providedIn: 'root',
})
export class ContactService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  private get baseUrl(): string {
    return `${this.config.apiBaseUrl}/api/v1/contacts`;
  }

  /** Contactos ordenados por último mensaje (el más reciente primero). */
  getContacts(): Observable<Contact[]> {
    return this.http.get<Contact[]>(this.baseUrl);
  }

  /** Marca la conversación como leída (pone los no leídos en 0). */
  markAsRead(id: string | number): Observable<Contact> {
    return this.http.post<Contact>(`${this.baseUrl}/${id}/read`, {});
  }

  /** Elimina una conversación completa (contacto + mensajes). */
  deleteContact(id: string | number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Vacía la bandeja: borra todas las conversaciones y sus mensajes. */
  deleteAllContacts(): Observable<{ eliminadas: number }> {
    return this.http.delete<{ eliminadas: number }>(this.baseUrl);
  }
}
