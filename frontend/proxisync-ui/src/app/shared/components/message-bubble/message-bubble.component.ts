import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-message-bubble',
  standalone: true,
  imports: [CommonModule],
  template: `
  <div class="w-full flex flex-col px-2 my-1.5 group"
       [ngClass]="mine ? 'items-end' : 'items-start'">

    <div *ngIf="!mine"
         class="flex items-start gap-2 max-w-[85%] sm:max-w-[70%]">

      <img *ngIf="avatar"
           [src]="avatar"
           class="w-6 h-6 rounded-full object-cover mt-1"/>

      <div class="flex flex-col">

        <span *ngIf="from" [ngClass]="['text-[12px] font-medium mb-0.5', senderColor]">
          {{ from }}
        </span>

        <div class="relative overflow-hidden rounded-2xl rounded-bl-md bg-white text-gray-800 border border-gray-200/60 shadow-sm transition-all duration-200 active:scale-[0.98]"
          (contextmenu)="disableNativeMenu($event)">
          
          <div *ngIf="messageType === 2" class="p-1 pb-0 relative group/img">
            <img [src]="attachmentUrl" (click)="openFullScreen()"
                 class="w-full max-w-[280px] max-h-[320px] object-cover rounded-xl cursor-pointer hover:brightness-95 transition-all"/>
            <button (click)="downloadFile($event)" 
                    class="absolute bottom-3 right-3 p-1.5 bg-black/40 backdrop-blur-md text-white rounded-full lg:opacity-0 lg:group-hover/img:opacity-100 transition-opacity active:scale-90">
              <span class="material-icons text-[16px]">download</span>
            </button>
          </div>

          <div *ngIf="messageType === 3" class="p-1 pb-0">
            <video controls class="w-full max-w-[280px] rounded-xl outline-none bg-black">
              <source [src]="attachmentUrl" [type]="attachmentType">
            </video>
          </div>

          <div *ngIf="messageType === 4" class="p-3 pb-2 min-w-[280px] sm:min-w-[320px]">
            <div class="flex items-center gap-2">
              <div class="h-9 w-9 flex items-center justify-center rounded-full bg-blue-500/10">
                <span class="material-icons text-blue-500">volume_up</span>
              </div>
              <div class="flex-1">
                <audio controls class="w-full h-8 custom-audio-player">
                  <source [src]="attachmentUrl" [type]="attachmentType">
                </audio>
              </div>
              <button (click)="downloadFile($event)" class="p-1 rounded-full hover:bg-gray-100 transition-colors">
                <span class="material-icons text-[18px] text-gray-500">download</span>
              </button>
            </div>
          </div>

          <div *ngIf="messageType === 5" class="flex items-center gap-2 p-2 min-w-[200px]">
            <div class="h-9 w-9 flex items-center justify-center rounded-md bg-gray-200">
              <span class="material-icons text-gray-600 text-lg">description</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium truncate">{{ text }}</p>
              <p class="text-[10px] opacity-60">{{ fileExtension }}</p>
            </div>
            <button (click)="downloadFile($event)" class="p-1.5 rounded-full hover:bg-gray-200">
              <span class="material-icons text-sm">download</span>
            </button>
          </div>

          <div *ngIf="messageType === 1 || (text && messageType !== 5 && messageType !== 4)"
               class="px-4 py-2 text-[14px] leading-relaxed whitespace-pre-wrap break-words max-w-[260px]">
            {{ text }}
          </div>
        </div>

        <div class="flex items-center gap-1 mt-0.5 text-[10px] text-gray-400 pl-1">
          <span *ngIf="isEdited" class="italic opacity-70">edited</span>
          <span>{{ time }}</span>
        </div>

      </div>
    </div>

    <div *ngIf="mine"
         class="relative flex flex-col items-end max-w-[85%] sm:max-w-[70%]">

      <div class="relative overflow-hidden rounded-2xl rounded-br-md bg-blue-600 text-white shadow-sm transition-all duration-200 active:scale-[0.98]"
        (pointerdown)="onPointerDown()" (pointerup)="clearTimer()" (pointerleave)="clearTimer()" (contextmenu)="disableNativeMenu($event)">

        <button (click)="emitMenu()" class="absolute -top-1 -right-1 opacity-0 group-hover:opacity-80 transition-all duration-200 bg-white shadow-sm rounded-full p-1 border border-gray-200 hover:scale-105 active:scale-95 z-10">
          <span class="material-icons text-[14px] text-gray-600">more_vert</span>
        </button>

        <div *ngIf="messageType === 2" class="p-1 pb-0 relative group/img">
          <img [src]="attachmentUrl" (click)="openFullScreen()"
               class="w-full max-w-[280px] max-h-[320px] object-cover rounded-xl cursor-pointer hover:brightness-95 transition-all"/>
          <button (click)="downloadFile($event)" 
                  class="absolute bottom-3 right-3 p-1.5 bg-black/40 backdrop-blur-md text-white rounded-full lg:opacity-0 lg:group-hover/img:opacity-100 transition-opacity active:scale-90">
            <span class="material-icons text-[16px]">download</span>
          </button>
        </div>

        <div *ngIf="messageType === 3" class="p-1 pb-0">
          <video controls class="w-full max-w-[280px] rounded-xl outline-none bg-black">
            <source [src]="attachmentUrl" [type]="attachmentType">
          </video>
        </div>

        <div *ngIf="messageType === 4" class="p-3 pb-2 min-w-[280px] sm:min-w-[320px]">
          <div class="flex items-center gap-2">
            <div class="h-9 w-9 flex items-center justify-center rounded-full bg-white/20">
              <span class="material-icons text-white">volume_up</span>
            </div>
            <div class="flex-1">
              <audio controls class="w-full h-8 custom-audio-player">
                <source [src]="attachmentUrl" [type]="attachmentType">
              </audio>
            </div>
            <button (click)="downloadFile($event)" class="p-1 rounded-full hover:bg-white/10 active:bg-white/20 transition-colors">
              <span class="material-icons text-[18px] text-white/80">download</span>
            </button>
          </div>
        </div>

        <div *ngIf="messageType === 5" class="flex items-center gap-2 p-2 min-w-[200px]">
          <div class="h-9 w-9 flex items-center justify-center rounded-md bg-white/20">
            <span class="material-icons text-white text-lg">description</span>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium truncate">{{ text }}</p>
            <p class="text-[10px] opacity-70">{{ fileExtension }}</p>
          </div>
          <button (click)="downloadFile($event)" class="p-1.5 rounded-full hover:bg-white/20">
            <span class="material-icons text-sm">download</span>
          </button>
        </div>

        <div *ngIf="messageType === 1 || (text && messageType !== 5 && messageType !== 4)"
             class="px-4 py-2 text-[14px] leading-relaxed whitespace-pre-wrap break-words max-w-[260px]">
          {{ text }}
        </div>
      </div>

      <div class="flex items-center gap-1 mt-0.5 text-[10px] text-gray-400 pr-1">
        <span *ngIf="isEdited" class="italic opacity-70">edited</span>
        <span>{{ time }}</span>
        <span class="ml-1 flex items-center">
          <span class="material-icons" [ngClass]="isSeen ? 'text-[#53bdeb] text-[13px]' : 'text-gray-400 text-[12px] opacity-70'">
            {{ isSeen ? 'done_all' : 'check' }}
          </span>
        </span>
      </div>

    </div>

  </div>
  `,
  styles: [`
    .custom-audio-player { filter: sepia(20%) saturate(70%) grayscale(100%) brightness(120%); }
    :host-context(.mine) .custom-audio-player { filter: invert(100%) brightness(1.5) hue-rotate(180deg); opacity: 0.9; }
  `]
})
export class MessageBubbleComponent {
  @Input() mine = false;
  @Input() text = '';
  @Input() from = '';
  @Input() time: string | null = '';
  @Input() isEdited = false;
  @Input() isSeen = false;
  @Input() messageType: number = 1;
  @Input() attachmentUrl: string | null = null;
  @Input() attachmentType: string | null = null;
  @Input() avatar: string | null = null;
  @Output() openMenu = new EventEmitter<void>();

  private longPressTimer: any;
  private readonly LONG_PRESS_DURATION = 500;

  get fileExtension(): string {
    if (!this.attachmentType) return 'FILE';
    return this.attachmentType.split('/')[1]?.toUpperCase() || 'FILE';
  }

  get senderColor(): string {
    const colors = ['text-purple-500', 'text-pink-500', 'text-blue-500', 'text-green-500', 'text-yellow-500', 'text-indigo-500'];
    if (!this.from) return 'text-gray-500';
    let hash = 0;
    for (let i = 0; i < this.from.length; i++) hash = this.from.charCodeAt(i) + ((hash << 5) - hash);
    return colors[Math.abs(hash) % colors.length];
  }

  async downloadFile(event: MouseEvent) {
  event.stopPropagation();
  if (!this.attachmentUrl) return;

  try {
    const response = await fetch(this.attachmentUrl, { 
      method: 'GET',
      mode: 'cors' // Ensure this is set
    });

    if (!response.ok) throw new Error('File not found');

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    
    // Use the content/text as filename, or fallback to the URL name
    const fileName = this.text || this.attachmentUrl.split('/').pop() || 'download';
    link.download = fileName;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  } catch (err) {
    console.error("CORS still blocking or file missing:", err);
    // If it still fails, the new tab is the only remaining option
    window.open(this.attachmentUrl, '_blank');
  }
}

  openFullScreen() {
    if (this.messageType !== 2 || !this.attachmentUrl) return;
    const overlay = document.createElement('div');
    overlay.style.cssText = `position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.9); z-index: 10000; display: flex; align-items: center; justify-content: center; cursor: zoom-out;`;
    const img = document.createElement('img');
    img.src = this.attachmentUrl;
    img.style.cssText = 'max-width: 95%; max-height: 95%; object-fit: contain;';
    overlay.appendChild(img);
    overlay.onclick = () => document.body.removeChild(overlay);
    document.body.appendChild(overlay);
  }

  emitMenu() { if (this.mine) this.openMenu.emit(); }
  onPointerDown() { if (this.mine) this.longPressTimer = setTimeout(() => this.openMenu.emit(), this.LONG_PRESS_DURATION); }
  clearTimer() { if (this.longPressTimer) { clearTimeout(this.longPressTimer); this.longPressTimer = null; } }
  disableNativeMenu(event: MouseEvent) { event.preventDefault(); }
}