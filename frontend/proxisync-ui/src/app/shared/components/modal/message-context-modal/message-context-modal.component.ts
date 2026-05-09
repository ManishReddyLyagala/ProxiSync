import { CommonModule } from "@angular/common";
import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { environment } from "../../../../../environments/environment";

export type ContextAction = 'edit' | 'delete' | 'clearChat' | 'seen' | 'cancel' | 'none';

@Component({
  selector: 'app-message-context-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
   <div class="fixed inset-0 z-50 overflow-y-auto bg-gray-900/50 flex items-center justify-center transition-opacity duration-300">
     <div class="
            w-full max-w-sm m-4 rounded-xl shadow-2xl backdrop-blur-sm 
            bg-white/90 dark:bg-gray-800/90 
            border border-gray-200 dark:border-gray-700
          ">

        <div class="p-4">
            <div *ngIf="isLoadingData" class="flex flex-col items-center justify-center h-48">
          <svg class="size-8 text-blue-500 animate-spin" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p class="mt-4 text-gray-700 dark:text-gray-300">Loading details...</p>
        </div>
        <ng-container *ngIf="!isLoadingData">
          <ng-container *ngIf="viewState === 'list'">
            <h3 class="text-lg font-semibold border-b pb-2 mb-2 dark:text-gray-100">Message Options</h3>
            
            <div (click)="selectAction('edit')" class="
              flex items-center p-3 cursor-pointer rounded-lg transition duration-150 ease-in-out 
              text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700
              text-base font-medium
            ">
              <span class="text-blue-500 mr-3">✍️</span> Edit Message
            </div>
            
            <div (click)="selectAction('delete')" class="
              flex items-center p-3 cursor-pointer rounded-lg transition duration-150 ease-in-out 
              text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700
              text-base font-medium
            ">
              <span class="text-red-500 mr-3">🗑️</span> Delete Message
            </div>
            
            <div (click)="selectAction('seen')" class="
              flex items-center p-3 cursor-pointer rounded-lg transition duration-150 ease-in-out 
              text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700
              text-base font-medium
            ">
              <span class="text-green-500 mr-3">👀</span> Seen By {{seenByUsers.length}}
            </div>

            <button (click)="close.emit()" class="mt-4 w-full py-2 cursor-pointer bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition">
              Cancel
            </button>
          </ng-container>

          <ng-container *ngIf="viewState === 'delete'">
            <h3 class="text-xl font-bold text-red-600 dark:text-red-400 border-b pb-2 mb-4">Confirm Delete</h3>
            <p class="text-gray-700 dark:text-gray-300 mb-6">Are you sure you want to delete this message? This action cannot be undone.</p>
            
            <div class="flex justify-end space-x-3">
              <button (click)="viewState = 'list'" class="py-2 px-4 rounded-lg bg-gray-300 dark:bg-gray-600 text-gray-800 dark:text-gray-300 hover:bg-gray-400 dark:hover:bg-gray-500 transition">
                No, Cancel
              </button>
              <button (click)="confirmAction('delete')" class="py-2 px-4 rounded-lg bg-red-500 text-white hover:bg-red-600 transition">
                Yes, Delete
              </button>
            </div>
          </ng-container>

          <ng-container *ngIf="viewState === 'clearChat'">
            <h3 class="text-xl font-bold text-red-600 dark:text-red-400 border-b pb-2 mb-4">Confirm Clear All Chat?</h3>
            <p class="text-gray-700 dark:text-gray-300 mb-6">This will delete the conversation for <strong>everyone</strong>. This action is permanent.</p>
            <div class="flex justify-end space-x-3">
              <button (click)="close.emit()" class="py-2 px-4 rounded-lg bg-gray-300 dark:bg-gray-600 text-gray-800 dark:text-gray-300 hover:bg-gray-400 dark:hover:bg-gray-500 transition">
                No, Cancel
              </button>
              <button (click)="confirmAction('clearChat')" class="py-2 px-4 rounded-lg bg-red-500 text-white hover:bg-red-600 transition">
                Yes, Clear All
              </button>
            </div>
          </ng-container>
          
          <ng-container *ngIf="viewState === 'seen'">
  <div class="flex items-center justify-between border-b pb-3 mb-4 dark:border-gray-700">
    <h3 class="text-lg font-bold text-gray-800 dark:text-white flex items-center gap-2">
      <span class="text-green-500">👀</span> 
      Read Receipts
    </h3>
    <span class="bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-xs font-bold px-2.5 py-1 rounded-full">
      {{ seenByUsers.length }} Seen
    </span>
  </div>

  <div class="max-h-[350px] overflow-y-auto pr-1 custom-scrollbar">
    <div *ngIf="seenByUsers.length === 0" class="flex flex-col items-center justify-center py-10 text-gray-400">
      <span class="text-3xl mb-2">✉️</span>
      <p class="text-sm italic">No one has read this yet</p>
    </div>

    <ul class="space-y-3">
      <li *ngFor="let user of seenByUsers" 
          class="flex items-center gap-3 p-2 rounded-xl transition-all duration-200 hover:bg-gray-200 dark:hover:bg-gray-700/40">
        
        <div class="relative shrink-0">
          <img [src]="getProfileUrl(user)" 
               class="w-10 h-10 rounded-full object-cover ring-2 ring-transparent group-hover:ring-blue-400" 
               [alt]="user.displayName" />
          
          <span *ngIf="user.isOnline" 
                class="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white dark:border-gray-800 rounded-full">
          </span>
        </div>

        <div class="flex-1 min-w-0">
          <div class="flex items-center justify-between">
            <h4 class="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate">
              {{ user.displayName || user.userName }}
            </h4>
            <span class="text-[10px] text-gray-400 font-medium">
              {{ user.readAt | date:'shortTime' }}
            </span>
          </div>
          
          <div class="flex items-center gap-2">
            <span class="text-xs" [ngClass]="user.isOnline ? 'text-green-500' : 'text-gray-400'">
              {{ user.isOnline ? 'Active' : 'Offline' }}
            </span>
            <span class="text-[10px] text-gray-300 dark:text-gray-600">•</span>
            <span class="text-[10px] text-gray-400 truncate">
              Read {{ user.readAt | date:'MMM d' }}
            </span>
          </div>
        </div>
      </li>
    </ul>
  </div>

  <div class="mt-6">
    <button (click)="viewState = 'list'" 
            class="mt-4 w-full py-2 cursor-pointer bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition">
      Back
    </button>
  </div>
</ng-container>
        </ng-container>
          </div>
      </div>
    </div>
    `
})
export class MessageContextModalComponent implements OnInit {
  // @Input() seenCount: number = 0;
  @Input() seenByUsers: any = [];
  @Input() isLoadingData: boolean = false;
  @Input() isClearAllClicked: boolean = false;
  @Output() action = new EventEmitter<ContextAction>();
  @Output() close = new EventEmitter<void>();

  
  apiBaseUrl = environment.gatewayUrl;
  viewState: 'list' | 'delete' | 'seen' | 'clearChat' = 'list';

  ngOnInit(): void {
      if(this.isClearAllClicked){
        this.viewState = 'clearChat'
      }
  }
  selectAction(action: 'edit' | 'delete' | 'seen') {
    if (action === 'edit') {
      this.confirmAction(action);
    } else {
      this.viewState = action;
    }
  }

  confirmAction(action: 'edit' | 'delete' | 'clearChat') {
    console.log("in conf", action)
    this.action.emit(action);
    this.close.emit();
  }

   getProfileUrl(user: any) {
    if (user.profilePictureUrl) {
      return this.apiBaseUrl + user.profilePictureUrl;
    }
    const initialChar = (user.userName?.[0] || 'U').toUpperCase();
    return `https://placehold.co/100x100/1F2937/ffffff?text=${initialChar}`;
  }
}