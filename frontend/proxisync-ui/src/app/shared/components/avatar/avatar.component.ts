import { Component, Input } from "@angular/core";

@Component({
    selector: 'app-avatar',
    standalone: true,
    template: `
    <img [src]="src || '/assets/avatar.png'" class="w-10 h-10 rounded-full object-cover"/>
    `
})
export class AvatarComponent{
    @Input() src?: string | null;
}

