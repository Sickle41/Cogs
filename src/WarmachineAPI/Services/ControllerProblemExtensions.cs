using Microsoft.AspNetCore.Mvc;

namespace WarmachineAPI.Services;

public static class ControllerProblemExtensions
{
    public static ObjectResult ProblemBadRequest(this ControllerBase controller, string detail) =>
        controller.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    public static ObjectResult ProblemNotFound(this ControllerBase controller, string detail) =>
        controller.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);
}
