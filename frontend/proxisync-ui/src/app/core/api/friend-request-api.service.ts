import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API } from '../constants/api-endpoints';
import { Observable } from 'rxjs';
@Injectable({ providedIn: 'root' })
export class FriendRequestAPIService {
    constructor(private http: HttpClient){}

    getAllUsersList(){
        return this.http.get(API.USER.GET_ALL_USERS);
    }

    getRecivedRequets(){
        return this.http.get(API.FRIENDS.GET_RECIVED_REQUESTS);
    }

    getSentRequsets(){
        return this.http.get(API.FRIENDS.GET_SENT_REQUESTS);
    }

    getMyContactList(){
        return this.http.get(API.FRIENDS.GET_MY_CONTACTS);
    }

    getMutualContactList(){
        return this.http.get(API.FRIENDS.GET_MUTUAL_CONTACT_LIST);
    }

    SendFriendRequest(toUserId: string, messageToSend: string){
        return this.http.post(API.FRIENDS.SEND_REQUEST(toUserId), {Message: messageToSend});
    }

    cancelSentRequest(requestId: string){
        return this.http.delete(API.FRIENDS.CANCEL_SENT_REQUEST(requestId));
    }

    respondToRecivedRequest(requestId: string, accept: boolean){
        return this.http.post(API.FRIENDS.RESPOND_TO_REQUEST(requestId), {Accept: accept});
    }

    toggleUnFollowRequest(requestId: string){
        return this.http.delete(API.FRIENDS.Toggle_Follow_REQUEST(requestId));
    }
}