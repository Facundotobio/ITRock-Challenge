using ITRockChallenge.Application.Dtos;
using ITRockChallenge.Domain;

namespace ITRockChallenge.Application.Mapping;

public static class TaskMapper
{
    public static TaskResponse ToResponse(TodoTask task) =>
        new(task.Id, task.Title, task.Description, task.Completed, task.CreatedAt);
}
