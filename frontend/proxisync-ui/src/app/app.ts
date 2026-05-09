import { Component, OnChanges, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { CallOverlayComponent } from './shared/components/call-overlay/call-overlay.component';
import { SignalRService } from './core/services/signalr.service';
import { StorageService } from './core/services/storage.service';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SidebarComponent, CallOverlayComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit{
  protected readonly title = signal('proxisync-ui');

  constructor(private signalr: SignalRService, public router: Router, public storage: StorageService){}
  ngOnInit(): void {
      this.signalr.start();
      
  }
  dark = signal(false);
   sidebarExpanded = false;

  onSidebarToggle(expanded: boolean) {
    this.sidebarExpanded = expanded;
  }

  isMobileView(): boolean {
  return window.innerWidth < 1024; // matches 'lg' breakpoint in Tailwind
}

getProfilePicUrl(profileUrl: string): string{
    return environment.gatewayUrl + profileUrl;
  }

isChatSelectedOnMobile(): boolean {
  // Returns true if we are on a conversation route
  return this.isMobileView() && this.router.url.includes('/chats/conv/');
}
}
