namespace ITRockChallenge.Application.Dtos
{
    public record TaskResponse(Guid Id, string Title, string Description, bool Completed, DateTime CreatedAt);
}
