import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Contact } from '../models/contact.model';

@Injectable({
  providedIn: 'root',
})
export class ContactService {
  // Stub: sin implementación
  getContacts(): Observable<Contact[]> {
    return of([]);
  }

  // Stub: sin implementación
  getContact(id: string | number): Observable<Contact | null> {
    return of(null);
  }

  // Stub: sin implementación
  markAsRead(id: string | number): Observable<void> {
    return of(void 0);
  }
}
