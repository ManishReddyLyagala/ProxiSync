namespace CallService.Interfaces
{
    public interface ILiveKitTokenService
    {
        string CreateToken(string roomName, string userId, string displayName);
        string GetLiveKitUrl();
    }
}
