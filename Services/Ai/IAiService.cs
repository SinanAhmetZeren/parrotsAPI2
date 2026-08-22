using ParrotsAPI2.Dtos.AiDtos;

namespace ParrotsAPI2.Services.Ai
{
    public interface IAiService
    {
        Task<string?> AskAsync(AskParrotsQueryDto dto, string userId);
    }
}
