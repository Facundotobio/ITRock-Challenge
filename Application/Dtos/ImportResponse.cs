namespace ITRockChallenge.Application.Dtos
{
    public record ImportResponse(int ImportedCount, IEnumerable<TaskResponse> Tasks);
}
