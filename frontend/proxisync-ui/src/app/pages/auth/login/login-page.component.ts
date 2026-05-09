import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { StorageService } from '../../../core/services/storage.service';
import { AuthApiService } from '../../../core/api/auth-api.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './login-page.component.html',
})
export class LoginPageComponent {
  username = '';
  password = '';
  loading = false;
  error = '';
  showRestoreUI = false;
  tempUserData: any = null;

  constructor(
    private api: AuthApiService,
    private storage: StorageService,
    private router: Router
  ) {}

  submit() {
    this.error = '';

    // Simple Client-side Validation
    if (!this.username || !this.password) {
      this.error = 'Please enter both username and password';
      return;
    }

    this.loading = true;
    this.api.login({ usernameOrEmail: this.username, password: this.password }).subscribe({
      next: (res: any) => {

        const u = res?.data;
        if (u?.isDeleted) {
          // 1. Show the Restore UI instead of logging in
          this.tempUserData = u;
          this.storage.saveAccessToken(u.token);
          this.showRestoreUI = true;
          this.loading = false;
        }else if (u?.token && u?.refreshToken) {
          // 2. Normal Login
          this.proceedWithLogin(u);
        }else {
          this.error = 'Invalid response from server';
          this.loading = false;
        }
      },
      error: (e) => {
        // Checking for specific backend error structures
        this.error = e?.error?.message || e?.error?.title || 'Login failed. Please try again.';
        this.loading = false;
      },
    });
  }

  confirmRestore() {
    this.loading = true;
    // Call your new restore endpoint
    this.api.restoreAccount().subscribe({
      next: (res: any) => {
        console.log(res);
        this.proceedWithLogin(this.tempUserData);
      },
      error: () => {
        this.error = 'Failed to restore account.';
        this.loading = false;
      }
    });
  }

  private proceedWithLogin(u: any) {
    this.storage.saveTokens(u.token, u.refreshToken);
    this.storage.saveUser(u);
    this.router.navigate(['/chats']);
    this.loading = false;
  }
}