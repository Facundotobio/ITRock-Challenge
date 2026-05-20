namespace ITRockChallenge.Application.Dtos
{
    public record UpdateTaskRequest(string? Title, string? Description, bool? Completed);
}
