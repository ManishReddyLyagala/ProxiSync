import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CallStateService } from "../../../core/services/call-state.service";
import { CallIncomingComponent } from "./incoming/call-incoming.component";
import { CallIncallComponent } from "./incall/call-incall.component";
import { CallOutgoingComponent } from "./outgoing/call-outgoing.component";

@Component({
  selector: "app-call-overlay",
  standalone: true,
  imports: [CommonModule, CallIncomingComponent, CallIncallComponent, CallOutgoingComponent],
  template: `
    <ng-container *ngIf="callState.uiState() !== 'idle'">
      <div
        class="fixed inset-0 z-[999] flex items-center justify-center bg-slate-950/80 backdrop-blur-xl animate-in fade-in duration-300"
      >
        <app-call-incoming
          *ngIf="callState.uiState() === 'incoming'"
          class="w-full h-full flex items-center justify-center"
        />

        <app-call-outgoing
          *ngIf="callState.uiState() === 'outgoing'"
          class="w-full h-full"
        />

        <app-call-incall
          *ngIf="callState.uiState() === 'connecting' || callState.uiState() === 'in-call'"
          class="w-full h-full"
        />
<div
  *ngIf="callState.uiState() === 'offline'"
  class="relative group p-8 rounded-[2.5rem] bg-slate-900 border border-white/10 shadow-2xl text-center max-w-xs w-full mx-4 overflow-hidden animate-in zoom-in-95 duration-300"
>
  <div class="absolute -top-12 -right-12 w-24 h-24 bg-amber-500/10 blur-3xl rounded-full"></div>
  <div class="absolute -bottom-12 -left-12 w-24 h-24 bg-red-500/10 blur-3xl rounded-full"></div>

  <div class="relative z-10">
    <div
      class="w-20 h-20 bg-amber-500/10 border border-amber-500/20 rounded-[2rem] flex items-center justify-center mx-auto mb-6 relative"
    >
      <div class="absolute inset-0 rounded-[2rem] bg-amber-500/5 animate-ping"></div>
      <span class="material-icons text-amber-500 text-4xl relative z-20">cloud_off</span>
    </div>

    <h2 class="text-2xl font-black text-white tracking-tight mb-2">
      User is Offline
    </h2>
    
    <p class="text-slate-400 text-sm mb-8 leading-relaxed">
      We couldn't reach them right now. <br>
      <span class="text-slate-500 text-xs">We'll let them know you called.</span>
    </p>

    <button
      (click)="callState.forceIdle()"
      class="w-full py-4 px-6 rounded-2xl bg-white/5 hover:bg-white/10 border border-white/10 text-white text-sm font-bold transition-all active:scale-95 flex items-center justify-center gap-2 group-hover:border-amber-500/30"
    >
      Dismiss
    </button>
  </div>
</div>
        <div
          *ngIf="callState.uiState() === 'ended'"
          class="relative group p-8 rounded-[2.5rem] bg-slate-900 border border-white/10 shadow-2xl text-center max-w-xs w-full mx-4 overflow-hidden animate-in zoom-in-95 duration-300"
        >
          <div class="absolute -top-12 -left-12 w-24 h-24 bg-red-500/20 blur-3xl rounded-full"></div>

          <div class="relative z-10">
            <div
              class="w-16 h-16 bg-red-500/10 border border-red-500/20 rounded-2xl flex items-center justify-center mx-auto mb-6"
            >
              <span class="material-icons text-red-500 text-3xl">call_end</span>
            </div>

            <h2 class="text-2xl font-black text-white tracking-tight mb-2">
              Call Ended
            </h2>
            <p class="text-slate-400 text-sm mb-8">
              The session has been disconnected.
            </p>

            <button
              (click)="callState.forceIdle()"
              class="w-full py-3 px-6 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 text-white text-sm font-bold transition-all active:scale-95"
            >
              Dismiss
            </button>
          </div>
        </div>
      </div>
    </ng-container>
  `,
})
export class CallOverlayComponent {
  callState = inject(CallStateService);
}
