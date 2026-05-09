// import { Component } from '@angular/core';
// import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
// import { filter } from 'rxjs';

// @Component({
//   selector: 'app-chat-shell.component',
//   imports: [RouterOutlet],
//   templateUrl: './chat-shell.component.html',
//   styleUrl: './chat-shell.component.css',
// })
// export class ChatShellComponent {
//    selectedConversationId: string | null = null;

//   constructor(private router: Router, private route: ActivatedRoute) {
//     this.router.events
//       .pipe(filter(e => e instanceof NavigationEnd))
//       .subscribe(() => {
//         const child = this.route.firstChild;
//         this.selectedConversationId = child?.snapshot.paramMap.get('conversationId') ?? null;
//       });
//   }

//   goBackMobile() {
//     this.router.navigate(['/chats']);
//   }
// }
