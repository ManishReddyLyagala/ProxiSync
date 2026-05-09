import { Injectable } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class StorageService {
  private tokenKey = 'ps_token';
  private refreshKey = 'ps_refresh_token';
  private userKey = 'ps_user';
  saveTokens(token: string, refresh: string) { 
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.refreshKey, refresh);
   }
   saveAccessToken(token: string) { 
    localStorage.setItem(this.tokenKey, token);
   }
  getToken() { return localStorage.getItem(this.tokenKey); }
  getRefreshToken() { return localStorage.getItem(this.refreshKey); }
  saveUser(u: any) { localStorage.setItem(this.userKey, JSON.stringify(u)); }
  getUser() { const v = localStorage.getItem(this.userKey); return v ? JSON.parse(v) : null; }
  clear() { localStorage.removeItem(this.tokenKey); localStorage.removeItem(this.refreshKey); localStorage.removeItem(this.userKey); }
}
