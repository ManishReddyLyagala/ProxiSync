import { Routes } from '@angular/router';
import { LoginPageComponent } from './pages/auth/login/login-page.component';
import { RegisterPageComponent } from './pages/auth/register/register-page.component';
import { AuthGuard } from './core/guards/auth.guard';
import { ChatPageComponent } from './pages/chat/chat-page/chat-page.component';
import { ConversationPageComponent } from './pages/chat/conversation-page/conversation-page.component';
import { People } from './pages/people/people';
import { Calls } from './pages/chat/calls/calls';
import { ProfileComponent } from './pages/chat/profile/profile-page.component';

export const routes: Routes = [
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },

  {
    path: 'chats',
    canActivate: [AuthGuard],
    component: ChatPageComponent,   // Shell layout
    children: [
    //   { path: '', component: EmptyChatPlaceholderComponent }, // optional
      { path: 'conv/:id', component: ConversationPageComponent },
    ]
  },
{ path: 'calls', component: Calls, canActivate: [AuthGuard] },
  { path: 'people', component: People, canActivate: [AuthGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: 'chats' }
];
