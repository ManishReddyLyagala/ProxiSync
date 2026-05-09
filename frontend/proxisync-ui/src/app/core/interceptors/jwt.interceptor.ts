import { HttpInterceptorFn, HttpRequest } from "@angular/common/http";
import { StorageService } from "../services/storage.service";

export const jwtInterceptor: HttpInterceptorFn = (req, next)=>{
    const token = new StorageService().getToken();
    if(!token) return next(req);
    const authReq = req.clone({
        setHeaders: { Authorization: `Bearer ${token}`}
    })
    return next(authReq);
};