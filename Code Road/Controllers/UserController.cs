﻿using Code_Road.Dto.Account;
using Code_Road.Dto.User;
using Code_Road.Services.UserService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Code_Road.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        #region Follow
        [Authorize]
        [HttpGet("GetAllFollowers")]
        public async Task<IActionResult> GetAllFollowers()
        {
            string userId = await getLogginUserId();
            FollowersDto followeres = await _userService.GetAllFollowers(userId);
            if (followeres.State is not null)
            {
                if (!followeres.State.Flag)
                    return Ok(followeres);
            }
            return Ok(followeres);
        }

        [Authorize]
        [HttpGet("GetAllFollowing")]
        public async Task<IActionResult> GetAllFollowing()
        {
            string userId = await getLogginUserId();
            FollowingDto? followeres = await _userService.GetAllFollowing(userId);
            if (followeres is null)
                return Ok("SomeThing went wrong");
            if (followeres.State is not null)
            {
                if (!followeres.State.Flag)
                    return Ok(followeres);
            }
            return Ok(followeres);
        }

        [Authorize]
        [HttpPost("Follow")]
        public async Task<IActionResult> Follow(string followingId)
        {
            string followerId = await getLogginUserId();
            StateDto follower = await _userService.Follow(followerId, followingId);
            if (!follower.Flag)
                return Ok(follower);
            return Ok(follower);
        }

        [Authorize]
        [HttpPost("UnFollow")]
        public async Task<IActionResult> UnFollow(string followingId)
        {
            string followerId = await getLogginUserId();
            StateDto follower = await _userService.UnFollow(followerId, followingId);

            if (!follower.Flag)
                return Ok(follower);
            return Ok(follower);
        }
        #endregion

        #region FinishedLesson
        [Authorize]
        [HttpGet("GetFinishedLessonsForSpecificUser")]
        public async Task<IActionResult> GetFinishedLessonsForSpecificUser()
        {
            string userId = await getLogginUserId();
            FinishedLessonsDto finishedLessons = await _userService.GetFinishedLessonsForSpecificUser(userId);
            if (finishedLessons.State is not null)
            {
                if (!finishedLessons.State.Flag)
                    return Ok(finishedLessons);
            }
            return Ok(finishedLessons);
        }

        [Authorize]
        [HttpPost("FinishNewLesson")]
        public async Task<IActionResult> FinishLesson(int lessonId, int degree)
        {
            string userId = await getLogginUserId();
            StateDto state = await _userService.FinishLesson(userId, lessonId, degree);
            return Ok(state);
        }
        #endregion

        #region User Image
        [HttpGet("GetUserImage")]
        public async Task<IActionResult> GetUserImage(string userId)
        {
            string image = await _userService.GetUserImage(userId);
            if (ModelState.IsValid)
                return Ok(image);

            return BadRequest(ModelState);
        }

        [Authorize]
        [HttpPost("UpdateUserImage")]
        public async Task<IActionResult> UpdateUserImage([FromForm] IFormFile image)
        {
            string userId = await getLogginUserId();
            StateDto state = await _userService.UpdateUserImage(userId, image);
            if (state.Flag)
                return Ok(state);
            return Ok(state);

        }

        [Authorize]
        [HttpDelete("DeleteUserImage")]
        public async Task<IActionResult> DeleteUserImage()
        {
            string userId = await getLogginUserId();
            StateDto state = await _userService.DeleteUserImage(userId);
            if (state.Flag)
                return Ok(state);
            return Ok(state);

        }
        #endregion

        [Authorize]
        [HttpGet("getUSerActiveDays")]
        public async Task<IActionResult> getUSerActiveDays()
        {
            string userId = await getLogginUserId();
            int days = await _userService.ActiveDays(userId);
            return Ok(days);
        }
        [HttpGet("GetUserById")]
        public async Task<IActionResult> GetUserByID(string id)
        {
            var userInfo = await _userService.GetUserById(id);
            if (userInfo == null) return BadRequest("user In Valid");
            if (!userInfo.UserInfo.State.Flag) return Ok(userInfo.UserInfo.State.Message);
            return Ok(userInfo);
        }
        private async Task<string> getLogginUserId()
        {
            string id = HttpContext.User.FindFirstValue("uid") ?? "NA";
            return id;
        }
    }
}
