using Asp.Versioning;

using ITRockChallenge.Application.Dtos;

using ITRockChallenge.Application.Interfaces;

using ITRockChallenge.Presentation.Extensions;

using ITRockChallenge.Presentation.Filters;

using Microsoft.AspNetCore.Mvc;



namespace ITRockChallenge.Presentation;



public static class TaskEndpoints

{

    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)

    {

        var apiVersionSet = app.NewApiVersionSet()

            .HasApiVersion(new ApiVersion(1, 0))

            .ReportApiVersions()

            .Build();



        var group = app.MapGroup("/api/v{version:apiVersion}/tasks")

            .WithApiVersionSet(apiVersionSet)

            .RequireAuthorization()

            .AddEndpointFilter<AuthenticatedUserFilter>();



        group.MapGet("", async (ITaskService taskService, HttpContext httpContext, [FromQuery] int? page, [FromQuery] int? pageSize,

            [FromQuery] bool? completed, [FromQuery] string? search, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate) =>

        {

            var result = await taskService.GetTasksByUserIdAsync(

                httpContext.GetUserId(), page ?? 1, pageSize ?? 10, completed, search, fromDate, toDate);



            return Results.Ok(result);

        })

        .WithName("GetPagedAndFilteredTasks")

        .HasApiVersion(1, 0);



        group.MapPost("/", async (CreateTaskRequest request, ITaskService taskService, HttpContext httpContext) =>

        {

            var taskResponse = await taskService.CreateTaskAsync(request, httpContext.GetUserId());

            return Results.Created(ProblemResults.BuildTaskLocation(httpContext, taskResponse.Id), taskResponse);

        })

        .WithName("CreateTask")

        .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>();



        group.MapPatch("/{id:guid}", async (Guid id, UpdateTaskRequest request, ITaskService taskService, HttpContext httpContext) =>

        {

            var updatedTask = await taskService.UpdateTaskAsync(id, request, httpContext.GetUserId());



            if (updatedTask == null)

            {

                return ProblemResults.NotFound(httpContext, ProblemResults.TaskNotFoundDetail);

            }



            return Results.Ok(updatedTask);

        })

        .WithName("UpdateTask")

        .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>();



        group.MapDelete("/{id:guid}", async (Guid id, ITaskService taskService, HttpContext httpContext) =>

        {

            var success = await taskService.DeleteTaskAsync(id, httpContext.GetUserId());



            if (!success)

            {

                return ProblemResults.NotFound(httpContext, ProblemResults.TaskNotFoundDetail);

            }



            return Results.NoContent();

        })

        .WithName("DeleteTask");



        group.MapPost("/import", async (ITaskService taskService, HttpContext httpContext) =>

        {

            var result = await taskService.ImportExternalTasksAsync(httpContext.GetUserId());

            return Results.Ok(result);

        })

        .WithName("ImportTasks");

    }

}


