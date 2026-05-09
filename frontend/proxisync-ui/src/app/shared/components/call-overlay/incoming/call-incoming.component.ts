import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CallStateService } from "../../../../core/services/call-state.service";

@Component({
  selector: "app-call-incoming",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./call-incoming.component.html",
})
export class CallIncomingComponent {
  private callState = inject(CallStateService);

  call = this.callState.activeCall;

  accept() {
    this.callState.acceptCall();
  }

  reject() {
    this.callState.rejectCall();
  }
}
