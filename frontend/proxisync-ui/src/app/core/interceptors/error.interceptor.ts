import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { inject } from "@angular/core";
import { BehaviorSubject, catchError, filter, Observable, switchMap, take, throwError } from "rxjs";
import { StorageService } from "../services/storage.service";
import { AuthApiService } from "../api/auth-api.service";
import { Router } from "@angular/router";

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const errorInterceptor: HttpInterceptorFn =(req, next)=>{
    const storage = inject(StorageService);
    const authService = inject(AuthApiService);
    const router = inject(Router);
    try{
        return next(req).pipe(
            catchError((error)=>{
                if(error instanceof HttpErrorResponse && error.status === 401 && !req.url.includes('login') && !req.url.includes('refreshtoken')){
                    return handle401Error(req, next, storage, authService, router);
                }
                return throwError(()=> error);
            })
        );
    }catch(err){
        console.error("API Error: ", err);
        throw err;
    }
}

function handle401Error(req: HttpRequest<any>, next: HttpHandlerFn, storage: StorageService, auth: AuthApiService, router: Router): Observable<HttpEvent<any>>{
    if(!isRefreshing){
        isRefreshing = true;
        refreshTokenSubject.next(null);
        const token = storage.getToken();
        const refreshToken = storage.getRefreshToken();

       return auth.refreshTokenAsync(token, refreshToken).pipe(
      switchMap((res: any) => {
        isRefreshing = false;
        storage.saveTokens(res.data.token, res.data.refreshToken); // Save new pair
        refreshTokenSubject.next(res.data.token);
        
        // Retry the original request with the new token
        return next(req.clone({ setHeaders: { Authorization: `Bearer ${res.data.token}` } })) as Observable<HttpEvent<any>>;
      }),
      catchError((err) => {
        isRefreshing = false;
       auth.performLocalCleanup();
        return throwError(() => err);
      })
    );
  } else {
    // If a refresh is already in progress, wait for the new token and then retry
    return refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap((token) => next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })) as Observable<HttpEvent<any>>)
    );
  }
}