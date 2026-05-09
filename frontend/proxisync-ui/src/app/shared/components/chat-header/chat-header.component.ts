import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-chat-header',
  standalone: true,
  template: `
    <div class="flex items-center gap-3 p-3 border-b bg-white dark:bg-[#071028]">
      <img [src]="avatar" class="w-10 h-10 rounded-full" />
      <div>
        <div class="font-semibold">{{title}}</div>
        <div class="text-sm text-gray-500">{{subtitle}}</div>
      </div>
      <div class="ml-auto">
        <button class="px-3 py-1 rounded border">⋯</button>
      </div>
    </div>
  `
})
export class ChatHeaderComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() avatar?: string | null;
}
