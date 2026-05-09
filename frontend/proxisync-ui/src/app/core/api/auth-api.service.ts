import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API } from '../constants/api-endpoints';
import { StorageService } from '../services/storage.service';
import { ChatApiService } from './chat-api.service';
import { CallApi } from './call-api.service';
import { SignalRService } from '../services/signalr.service';
import { Router } from '@angular/router';
import { finalize, Observable, of } from 'rxjs';
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  constructor(private http: HttpClient, private storage: StorageService, private signalrService: SignalRService, private router: Router) {}
  login(payload: any) { return this.http.post(API.AUTH.LOGIN, payload); }
  register(form: FormData) { return this.http.post(API.AUTH.REGISTER, form); }
  profile() { return this.http.get(API.AUTH.PROFILE); }
  refreshTokenAsync(token: string | null, refreshToken: string | null) {
    return this.http.post(API.AUTH.GET_REFRESH_TOKEN, { 
      token, 
      refreshToken 
    });
  }
  logout(): Observable<any>{
    const user = this.storage.getUser();
    if (!user) {
    this.performLocalCleanup();
    return of(null);
  }
   return this.http.post(API.AUTH.LOGOUT, {}).pipe(
    finalize(async () => await this.performLocalCleanup())
  );
  }
  restoreAccount(){
    return this.http.post(API.USER.RESTORE_ACCOUNT, {});
  }

  public async performLocalCleanup() {
    try {
    await this.signalrService.stopAll();
  } catch (e) {
      console.warn("SignalR stop failed", e);
    } finally {
      this.storage.clear();
      this.router.navigate(['/login']);
    }
}
}
