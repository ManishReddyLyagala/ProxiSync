import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Output, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthApiService } from '../../../core/api/auth-api.service';
import { StorageService } from '../../../core/services/storage.service';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {

  expanded = signal(false);
  mobileOpen = signal(false);
  isLoggingOut = signal<boolean>(false);
  unreadChats = signal(3); // dynamic later from API
  @Output() toggle = new EventEmitter<boolean>();

  constructor(public router: Router, private authService: AuthApiService, public storage: StorageService) {}

  toggleExpand() {
    this.expanded.update(v => !v);
    this.toggle.emit(this.expanded());
  }

  toggleMobile() {
    this.mobileOpen.update(v => !v);
  }

  isDesktop() {
    return window.innerWidth >= 1024; // 'md' breakpoint
  }

  @HostListener('window:resize')
  onResize() {
    if (this.isDesktop()) this.mobileOpen.set(false);
  }

  navigate(path: string) {
    this.router.navigate([path]);
    
    // Close mobile drawer on click
    this.mobileOpen.set(false);
    
    // Logic: Collapse the sidebar labels after clicking on desktop (optional)
    if (this.expanded()) {
      this.expanded.set(false);
      this.toggle.emit(false);
    }
  }

  async logout() {
    if (this.isLoggingOut()) return;
    this.isLoggingOut.set(true);

    this.authService.logout().subscribe({
      next: ()=>{
        console.log("backend logout success"),
        this.isLoggingOut.set(false);
      } ,
      error: (err)=>{ 
        this.isLoggingOut.set(false);
         console.log("Backend Logout failed", err);
        }
    })
  }

  isActive(path: string): boolean {
    return this.router.url.startsWith(path);
  }
}

