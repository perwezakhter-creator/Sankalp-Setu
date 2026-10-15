using Microsoft.AspNetCore.Mvc;
using SankalpAPI.Model;
using SankalpAPI.Services;

namespace SankalpAPI.Controllers
{
    /// <summary>
    /// Admin group management. Unlike public portal endpoints, these routes require
    /// the X-Client-Token checked by ClientTokenMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class GroupController : ControllerBase
    {
        private readonly GroupService _groupService;

        public GroupController(GroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpGet("List")]
        [ProducesResponseType(typeof(GroupManagementResponse<GroupManagementListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GroupManagementResponse<GroupManagementListDto>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List()
        {
            var result = await _groupService.GetGroupManagementAsync();
            return result.Status ? Ok(result) : StatusCode(StatusCodes.Status500InternalServerError, result);
        }

        [HttpPost("Create")]
        [ProducesResponseType(typeof(GroupManagementResponse<GroupDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(GroupManagementResponse<GroupDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(new GroupManagementResponse<GroupDto>
                {
                    Status = false,
                    Message = "Please provide valid group details."
                });
            }

            var result = await _groupService.CreateGroupAsync(request);
            return result.Status
                ? StatusCode(StatusCodes.Status201Created, result)
                : BadRequest(result);
        }

        [HttpPost("CreateSubGroup")]
        [ProducesResponseType(typeof(GroupManagementResponse<SubGroupDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(GroupManagementResponse<SubGroupDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSubGroup([FromBody] CreateSubGroupRequest request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(new GroupManagementResponse<SubGroupDto>
                {
                    Status = false,
                    Message = "Please provide valid subgroup details."
                });
            }

            var result = await _groupService.CreateSubGroupAsync(request);
            return result.Status
                ? StatusCode(StatusCodes.Status201Created, result)
                : BadRequest(result);
        }
    }
}
