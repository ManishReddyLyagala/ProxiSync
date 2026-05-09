import { inject } from "@angular/core";
import { Router } from "@angular/router";
import { StorageService } from "../services/storage.service";

export function AuthGuard(){
    const router = inject(Router);
    const storage = new StorageService();
    const token = storage.getToken();
    const refreshToken = storage.getRefreshToken();
   if (!token && !refreshToken) {
    router.navigate(['/login']);
    return false;
  }
    return true;
}