import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API } from '../constants/api-endpoints';
import { Observable } from 'rxjs';
@Injectable({ providedIn: 'root' })
export class UserApiService {

    constructor(private http: HttpClient){}
    getUserDetailsById(userId: string){
        return this.http.get(API.USER.GET_USER_DETAILS_BY_ID(userId));
    }

    getAllUsersList(){
        return this.http.get(API.USER.GET_ALL_USERS);
    }
    getMyProfileDetails(){
        return this.http.get(API.USER.GET_MY_PROFILE);
    }
    updateMyProfileDetails(data: FormData){
        return this.http.put(API.USER.UPDATE_MY_PROFILE, data);
    }
    deleteMyProfile(){
        return this.http.delete(API.USER.DELETE_MY_PROFILE);
    }
}