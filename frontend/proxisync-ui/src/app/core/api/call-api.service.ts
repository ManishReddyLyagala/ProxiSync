import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { CallLogDto, GenerateTokenResponseDto, JoinCallDto, ParticipantMediaDto, StartCallDto, StartCallResponseDto } from "../models/call.model";
import { API } from "../constants/api-endpoints";
import { firstValueFrom, Observable } from "rxjs";

@Injectable({providedIn: 'root'})
export class CallApi{
    private http = inject(HttpClient);
    start(dto: StartCallDto){
        return this.http.post<StartCallResponseDto>(API.CALLS.START, dto);
    }

    token(callId: string){
        return this.http.post<GenerateTokenResponseDto>(API.CALLS.TOKEN(callId), {});
    }

    join(callId: string, dto: JoinCallDto){
        return this.http.post(API.CALLS.JOIN(callId), dto);
    }

    updateMedia(callId: string, dto: ParticipantMediaDto){
        return this.http.patch(API.CALLS.UPDATE_MEDIA(callId), dto);
    }

    end(callId: string){
        return this.http.post(API.CALLS.END(callId), {});
    }

    reject(callId: string){
        return this.http.post(API.CALLS.REJECT(callId), {});
    }

    leave(callId: string){
        return this.http.post(API.CALLS.LEAVE(callId), {});
    }

  cancelCall(callId: string) {
    return this.http.post(API.CALLS.CANCEL(callId), {});
  }

  getCall(callId: string) {
    return firstValueFrom(this.http.get(API.CALLS.GET(callId)));
  }
  getUserCallHistory():Observable<CallLogDto[]>{
    return this.http.get<CallLogDto[]>(API.CALLS.GETUSERCALLHISTORY);
  }
}