namespace ChatService.Dtos
{
    public class MessagePageDto
    {
        public List<MessageDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
