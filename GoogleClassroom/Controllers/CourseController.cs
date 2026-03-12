using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.DTOs.Course.Application.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers
{
    [ApiController]
    [Route("api")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Создать новый курс
        /// </summary>
        [HttpPost("course")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<CreateUpdateCourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateUpdateCourseRequestDto request)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.CreateCourseAsync(userId, request);
            return Ok(new ApiResponse<CreateUpdateCourseResponseDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Обновить информацию о курсе (только преподаватель)
        /// </summary>
        [HttpPut("course/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<CreateUpdateCourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] CreateUpdateCourseRequestDto request)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.UpdateCourseAsync(userId, id, request);
            return Ok(new ApiResponse<CreateUpdateCourseResponseDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Получить информацию о курсе
        /// </summary>
        [HttpGet("course/{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<CourseDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCourseDetails(Guid id)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.GetCourseDetailsAsync(userId, id);
            return Ok(new ApiResponse<CourseDetailsDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Получить список участников курса (только преподаватель)
        /// </summary>
        [HttpGet("course/{id}/members")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<CourseMemberDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMembers(Guid id, [FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? query = null)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.GetMembersAsync(userId, id, skip, take, query);
            return Ok(new ApiResponse<PagedResponse<CourseMemberDto>>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Изменить роль участника (только преподаватель)
        /// </summary>
        [HttpPut("course/{id}/members/{userId}/role")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<ChangeRoleResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeRole(Guid id, Guid userId, [FromBody] ChangeRoleRequestDto request)
        {
            var currentUserId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.ChangeRoleAsync(currentUserId, id, userId, request);
            return Ok(new ApiResponse<ChangeRoleResponseDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Удалить участника из курса (только преподаватель)
        /// </summary>
        [HttpDelete("course/{id}/members/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            var currentUserId = HttpContext.GetUserId()!.Value;
            await _courseService.RemoveMemberAsync(currentUserId, id, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = new { id }
            });
        }



        /// <summary>
        /// Выйти из курса
        /// </summary>
        [HttpDelete("course/{id}/leave")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LeaveCourse(Guid id)
        {
            var currentUserId = HttpContext.GetUserId()!.Value;
            await _courseService.RemoveMemberAsync(currentUserId, id, currentUserId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = new { id }
            });
        }

        /// <summary>
        /// Присоединиться к курсу по инвайт-коду
        /// </summary>
        [HttpPost("course/join")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<JoinCourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> JoinCourse([FromBody] JoinCourseRequestDto request)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.JoinCourseAsync(userId, request);
            return Ok(new ApiResponse<JoinCourseResponseDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }

        /// <summary>
        /// Получить список курсов текущего пользователя с указанием роли
        /// </summary>
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("user/courses")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserCourseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyCourses([FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var result = await _courseService.GetUserCoursesAsync(userId, skip, take);
            return Ok(new ApiResponse<PagedResponse<UserCourseDto>>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = result
            });
        }
    }
}
