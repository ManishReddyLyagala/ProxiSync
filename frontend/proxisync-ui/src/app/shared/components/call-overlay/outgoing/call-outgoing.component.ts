import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CallStateService } from "../../../../core/services/call-state.service";

@Component({
  selector: "app-call-outgoing",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./call-outgoing.component.html",
})
export class CallOutgoingComponent {
  private callState = inject(CallStateService);

  call = this.callState.activeCall;
  micEnabled = this.callState.micEnabled;
  videoEnabled = this.callState.videoEnabled;

  toggleMic() {
    this.callState.toggleMic();
  }

  toggleVideo() {
    this.callState.toggleVideo();
  }

  cancel() {
    this.callState.cancelCall();
  }
}
