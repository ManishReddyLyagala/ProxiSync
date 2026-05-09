import { ChangeDetectorRef, Component, Input, OnDestroy, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  Room,
  RoomEvent,
  Track,
  RemoteParticipant,
  LocalParticipant,
  RemoteTrackPublication,
  LocalTrackPublication,
  Participant,
  TrackPublication,
} from "livekit-client";


type Tile = {
  id: string;
  name: string | any;
  isLocal: boolean;
  isVideoOn: boolean;
  isScreenShare: boolean;
  participant: Participant;
  publication?: TrackPublication;
};

@Component({
  selector: "app-call-video-grid",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./call-video-grid.component.html",
  styles:`
 .overflow-y-auto::-webkit-scrollbar {
  width: 4px;
}
.overflow-y-auto::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 10px;
}
.aspect-video {
  aspect-ratio: 16 / 9;
}
.ring-emerald-500 {
  animation: speaking-glow 1.5s infinite alternate ease-in-out;
}

@keyframes speaking-glow {
  from {
    box-shadow: 0 0 5px rgba(16, 185, 129, 0.2);
    border-color: rgba(16, 185, 129, 0.5);
  }
  to {
    box-shadow: 0 0 15px rgba(16, 185, 129, 0.6);
    border-color: rgba(16, 185, 129, 1);
  }
}

.material-icons {
  display: inline-block;
}
`
})
export class CallVideoGridComponent implements OnDestroy {
  @Input({ required: true }) room!: Room;
  // @Input() isSharing: boolean = false;
  localIsSharing: boolean = false;
  tiles: Tile[] = [];
  pinnedTileId = signal<string | null>(null);
  activeSpeakerId = signal<string | null>(null);
  currentPage = 0;
  pageSize = 6;
  constructor(private cdr: ChangeDetectorRef){}

  ngOnInit() {
    this.setupEventListeners();
    this.refresh();
  }

  private setupEventListeners() {
    if (!this.room) return;

    const delayedRefresh = () => {
    setTimeout(() => this.refresh(), 300); 
  };

  this.room.on(RoomEvent.ActiveSpeakersChanged, (speakers) => {
    this.activeSpeakerId.set(speakers.length > 0 ? speakers[0].identity : null);
  });
  this.room.on(RoomEvent.TrackSubscribed, delayedRefresh);
  this.room.on(RoomEvent.LocalTrackPublished, delayedRefresh);

    const events = [
       RoomEvent.TrackUnsubscribed,
       RoomEvent.LocalTrackUnpublished,
      RoomEvent.ParticipantConnected, RoomEvent.ParticipantDisconnected,
      RoomEvent.TrackMuted, RoomEvent.TrackUnmuted
    ];

   events.forEach(event => this.room.on(event, () => setTimeout(() => this.refresh(), 300)));
  }

  ngOnDestroy() {
    this.room.removeAllListeners();
   this.tiles.forEach(t => t.publication?.videoTrack?.detach());
  }

 private refresh() {
    const next: Tile[] = [];

    // 1. Check for Screen Shares first (Local or Remote)
    const remoteScreenPubs = Array.from(this.room.remoteParticipants.values())
    .map(p => ({ p, pub: p.getTrackPublication(Track.Source.ScreenShare) }))
    .filter(item => item.pub?.videoTrack);

  const localScreenPub = this.room.localParticipant.getTrackPublication(Track.Source.ScreenShare);

  // Update localIsSharing state based on actual tracks found
  const hasActiveShare = remoteScreenPubs.length > 0 || !!localScreenPub?.videoTrack;

  // AUTO-RESET: If sharing stopped, clear pins to return to grid view
  if (!hasActiveShare && this.localIsSharing) {
    this.pinnedTileId.set(null);
  }
  this.localIsSharing = hasActiveShare;
  // Add them to tiles
  remoteScreenPubs.forEach(item => next.push(this.createTile(item.p, true, item.pub)));
  if (localScreenPub?.videoTrack) {
    next.push(this.createTile(this.room.localParticipant, true, localScreenPub));
  }

  // 2. Camera Tiles
  next.push(this.createTile(this.room.localParticipant, false));
  this.room.remoteParticipants.forEach(p => next.push(this.createTile(p, false)));

  this.tiles = next;
  // console.log('Grid Tiles updated. IsSharing:', this.localIsSharing, 'Count:', this.tiles.length);
  
  if (this.localIsSharing && this.pinnedTileId()) {
     const isPinnedTileAScreen = this.tiles.find(t => t.id === this.pinnedTileId())?.isScreenShare;
     if (!isPinnedTileAScreen) {
       this.pinnedTileId.set(null); // Force reset if user tried to pin a person during a share
     }
  }
  this.cdr.detectChanges(); 
  setTimeout(() => this.attachAll(), 200);
  }

  private createTile(p: Participant, isScreen: boolean, pub?: TrackPublication): Tile {
    const isLocal = p instanceof LocalParticipant;
    const videoPub = isScreen ? pub : p.getTrackPublication(Track.Source.Camera);
    
    return {
      id: isScreen ? `screen-${p.identity}` : p.identity,
      name: isScreen ? (isLocal ? 'Your Screen' : `${p.name}'s Screen`) : (isLocal ? 'You' : p.name),
      isLocal,
      isScreenShare: isScreen,
      isVideoOn: !!videoPub?.videoTrack && !videoPub.isMuted,
      participant: p,
      publication: videoPub
    };
  }

  private attachAll() {
    this.tiles.forEach(tile => {
      const track = tile.publication?.videoTrack;
      if (!track) return;

      const elementId = this.getVideoId(tile);
      const el = document.getElementById(elementId) as HTMLVideoElement;
      if (el) {
        track.attach(el);
      } else {
        // Fallback retry if Angular was slow to render the *ngIf block
        setTimeout(() => {
          const retryEl = document.getElementById(elementId) as HTMLVideoElement;
          if (retryEl) track.attach(retryEl);
        }, 300);
      }
    });
  }

  getVideoId(tile: Tile): string {
    const prefix = tile.isScreenShare ? 'video-screen-' : 'video-';
    const identity = tile.isLocal ? 'local' : tile.participant.identity;
    return `${prefix}${identity}`;
  }

  togglePin(tileId: string) {
    if (this.localIsSharing) {
      const target = this.tiles.find(t => t.id === tileId);
      if (!target?.isScreenShare) return;
    }
    if (this.pinnedTileId() === tileId) this.pinnedTileId.set(null);
    else this.pinnedTileId.set(tileId);
    this.refresh();
  }

 get paginatedTiles() {
    // If sharing or pinning, we usually show everyone in sidebar, otherwise paginate
    if (this.localIsSharing || this.pinnedTileId()) return this.tiles;
    return this.tiles.slice(this.currentPage * this.pageSize, (this.currentPage + 1) * this.pageSize);
  }
  
  trackById(_: number, tile: Tile) {
    return tile.id;
  }
}
