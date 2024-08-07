﻿using Code_Road.Dto.Account;
using Code_Road.Dto.Comment;
using Code_Road.Dto.Post;
using Code_Road.Dto.User;
using Code_Road.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Code_Road.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _user;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(AppDbContext context, UserManager<ApplicationUser> user, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _user = user;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;

        }
        public async Task<FollowersDto> GetAllFollowers(string id)
        {
            ApplicationUser? user = await _user.FindByIdAsync(id);
            StateDto state = new StateDto() { Flag = false, Message = "invalid Id" };
            FollowersDto fd = new FollowersDto();
            if (user is null)
            {
                fd.Count = 0;
                fd.State = state;
                return fd;

            }
            state.Flag = false;
            state.Message = "there is now followers";

            var followers = await _context.Follow.Where(f => f.FollowingId == id).ToListAsync();
            if (followers is null)
            {
                fd.State = state;
                fd.Count = 0;
                return fd;
            }
            state.Flag = true;
            state.Message = "done";
            fd.State = state;
            foreach (var f in followers)
            {
                var folwee = await _user.FindByIdAsync(f.FollowerId);
                fd.FollowersList.Add(new UserDetailsDto { Id = f.FollowerId, UserName = f.Follower.UserName });

            }
            fd.Count = fd.FollowersList.Count;
            return fd;
        }
        public async Task<FollowingDto> GetAllFollowing(string id)
        {
            ApplicationUser? user = await _user.FindByIdAsync(id);
            StateDto state = new StateDto() { Flag = false, Message = "invalid Id" };
            FollowingDto fd = new FollowingDto();
            if (user is null)
            {
                fd.Count = 0;
                fd.State = state;
                return fd;

            }
            state.Flag = false;
            state.Message = "there is now followings";

            var followers = await _context.Follow.Where(f => f.FollowerId == id).ToListAsync();
            if (followers is null)
            {
                fd.State = state;
                fd.Count = 0;
                return fd;
            }
            state.Flag = true;
            state.Message = "done";
            fd.State = state;
            foreach (var f in followers)
            {
                var folwee = await _user.FindByIdAsync(f.FollowingId);
                fd.FollowingList.Add(new UserDetailsDto { Id = f.FollowerId, UserName = folwee.UserName });

            }
            fd.Count = fd.FollowingList.Count;
            return fd;
        }

        public async Task<StateDto> Follow(string followerId, string followingId)
        {
            ApplicationUser? follower = await _user.FindByIdAsync(followerId);
            ApplicationUser? following = await _user.FindByIdAsync(followingId);
            StateDto state = new StateDto { Flag = false, Message = "Invalid ID" };
            if (follower is null || following is null)
                return state;
            state.Flag = true;
            state.Message = "Following";
            Follow follow = new Follow() { FollowingId = followingId, FollowerId = followerId };
            if (!_context.Follow.Any(f => f.FollowingId == followingId && f.FollowerId == followerId))
            {
                _context.Follow.Add(follow);

                await _context.SaveChangesAsync();
                return state;
            }
            else
            {
                state.Flag = true;
                state.Message = "You Already Follow this Account";
                return state;
            }


        }
        public async Task<StateDto> UnFollow(string followerId, string followingId)
        {
            ApplicationUser? follower = await _user.FindByIdAsync(followerId);
            ApplicationUser? following = await _user.FindByIdAsync(followingId);

            if (follower is null || following is null)
                return new StateDto { Flag = false, Message = "Invalid ID" };
            Follow follow = new Follow() { FollowingId = followingId, FollowerId = followerId };
            if (_context.Follow.Any(f => f.FollowingId == followingId && f.FollowerId == followerId))
            {
                _context.Follow.Remove(follow);

                await _context.SaveChangesAsync();
                return new StateDto { Flag = true, Message = "Following removed" };
            }
            else
            {
                return new StateDto { Flag = true, Message = "You Already un Follow this Account" };
            }


        }

        public async Task<FinishedLessonsDto> GetFinishedLessonsForSpecificUser(string userId)
        {

            StateDto state = await CheckUserId(userId);
            FinishedLessonsDto finishedLessons = new FinishedLessonsDto { State = state, Count = 0 };
            if (!state.Flag)
                return finishedLessons;
            state.Flag = false;
            state.Message = "there is now followings";
            finishedLessons.FinishedLessons = new List<FinishedLessonDetailsDto>();
            var lessons = _context.FinishedLessons.Where(u => u.UserId == userId).ToList();
            if (lessons is null)
            {
                finishedLessons.State = state;
                return finishedLessons;
            }
            state.Flag = true;
            state.Message = "done";
            finishedLessons.Count = lessons.Count;
            foreach (var lesson in lessons)
            {
                if ((await _context.Quizzes.FirstOrDefaultAsync(c => c.LessonId == lesson.LessonId)) == null)
                {
                    string finishedLessonName = await CheckLessonId(lesson.LessonId);
                    finishedLessons.FinishedLessons.Add(new FinishedLessonDetailsDto() { LessonId = lesson.LessonId, LessonName = finishedLessonName, QuizId = 0, Degree = lesson.Degree });

                }
                else
                {
                    int quizid = (await _context.Quizzes.FirstOrDefaultAsync(c => c.LessonId == lesson.LessonId)).Id;
                    string finishedLessonName = await CheckLessonId(lesson.LessonId);
                    finishedLessons.FinishedLessons.Add(new FinishedLessonDetailsDto() { LessonId = lesson.LessonId, LessonName = finishedLessonName, QuizId = quizid, Degree = lesson.Degree });
                }


            }
            return finishedLessons;
        }
        public async Task<StateDto> FinishLesson(string userId, int lessonId, int degree)
        {
            StateDto state = await CheckUserId(userId);
            if (!state.Flag)
                return state;
            state.Flag = false;
            state.Message = "Invalid Lesson";
            string lesson = await CheckLessonId(lessonId);
            if (lesson is null) return state;
            state.Flag = false;
            state.Message = "Invalid Quiz";
            Quiz? quiz = await _context.Quizzes.Where(l => l.LessonId == lessonId).FirstOrDefaultAsync();
            if (quiz is null) return state;
            state = await UpdateDegree(userId, lessonId, degree, quiz);
            state.Flag = true;
            state.Message = "Congratulation,You Successed";
            FinishedLessons finishedLesson = new FinishedLessons { UserId = userId, LessonId = lessonId, Degree = degree };
            _context.FinishedLessons.Add(finishedLesson);
            await _context.SaveChangesAsync();
            return state;

        }
        public async Task<string> GetUserImage(string userId)
        {
            StateDto state = await CheckUserId(userId);
            if (!state.Flag) return "invalid User";
            Image? img = await _context.Image.FirstOrDefaultAsync(u => u.UserId == userId && u.PostId == null && u.LessonId == null);
            if (img is not null)
                return img.ImageUrl;
            var httpContext = _httpContextAccessor.HttpContext;
            string hosturl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            return hosturl + "/Upload/User/Avatar/Avatar.jpg";
        }
        public async Task<StateDto> UpdateUserImage(string userId, IFormFile image)
        {
            StateDto state = await GetImagePath(image, userId);
            if (state.Flag)
            {
                return state;
            }
            return new StateDto { Flag = false, Message = "failed" };

        }
        public async Task<StateDto> DeleteUserImage(string userId)
        {
            StateDto state = await DeleteImage(userId);
            if (state.Flag)
            {

                return state;
            }
            return new StateDto { Flag = false, Message = "failed" };

        }
        public async Task<int> ActiveDays(string userId)
        {
            StateDto state = await CheckUserId(userId);

            if (!state.Flag) return 1;
            var user = await _user.FindByIdAsync(userId);
            if (((DateTime.Now.Day) != (user.ActiceDay.Day)))
            {
                var yasterday = DateTime.Now.AddDays(-1);
                if (((user.ActiceDay.Day).CompareTo(yasterday.Day) == 0) && ((user.ActiceDay.Month).CompareTo(yasterday.Month) == 0) && ((user.ActiceDay.Year).CompareTo(yasterday.Year) == 0))
                {
                    user.OnlineDays++;
                    user.ActiceDay = DateTime.Now;
                    await _user.UpdateAsync(user);
                    return user.OnlineDays;
                }
                user.OnlineDays = 1;
                user.ActiceDay = DateTime.Now;
                await _user.UpdateAsync(user);
                return 1;

            }
            return user.OnlineDays;
        }
        public async Task<UserProfileDto> GetUserById(string id)
        {
            StateDto userState = await CheckUserId(id);
            UserProfileDto profileInfo = new UserProfileDto();
            if (!userState.Flag) return null;
            ApplicationUser? user = await _user.FindByIdAsync(id);

            profileInfo.UserInfo.State = new StateDto { Flag = true, Message = "done" };
            profileInfo.UserInfo.UserId = id;
            profileInfo.UserInfo.UserName = user.UserName;
            profileInfo.UserInfo.ImageUrl = await GetUserImage(id);
            var Posts = await GetAllByUserIdAsync(id);
            foreach (var post in Posts)
            {
                var comment = await _context.Comments.Where(c => c.PostId == post.PostId)
                    .Select(cd => new CommentDto { Id = cd.Id, UserId = cd.UserId, UserName = cd.User.UserName, UserImage = cd.User.Image.ImageUrl, Content = cd.Content, Up = cd.Up, Down = cd.Down, Date = cd.Date }).ToListAsync();
                profileInfo.Posts.Add(new PostAndCommentsDto { post = post, Comments = comment });
            }
            if (profileInfo.Posts.Count == 0)
            {
                profileInfo.UserInfo.State = new StateDto { Flag = false, Message = "User Don't Have Posts" }; ;

            }
            return profileInfo;

        }
        public async Task<List<PostDto>> GetAllByUserIdAsync(string user_id)
        {
            if (await _user.FindByIdAsync(user_id) is null)
                return new List<PostDto> { new PostDto { Status = new StateDto { Flag = false, Message = "user invalid" } } };
            var today = DateTime.Today;
            var posts = await _context.Posts
               .Include(p => p.Images)
               .Include(p => p.User)
               .Where(p => p.UserId == user_id)
               .Select(p => new PostDto
               {
                   Status = new StateDto { Flag = true, Message = "Success" },
                   PostId = p.Id,
                   UserId = p.UserId,
                   UserImage = p.User.Image.ImageUrl,
                   UserName = p.User.FirstName + " " + p.User.LastName,
                   Content = p.Content,
                   Up = p.Up,
                   Down = p.Down,
                   Date = p.Date,
                   Image_url = p.Images.Where(i => i.UserId == p.User.Id && i.PostId == p.Id).Select(i => i.ImageUrl).ToList()
               })
               .OrderByDescending(p => p.Date)
              .ToListAsync();
            //Posts Not Found
            if (posts is null)
            {
                return null;
            }
            if (posts.Count > 0)
            {
                posts[0].UserId = user_id;
                posts[0].UserImage = await GetUserImage(user_id);
            }
            return posts;
        }
        #region Private
        private async Task<StateDto> GetImagePath(IFormFile image, string userId)
        {
            StateDto state = await CheckUserId(userId);
            if (!state.Flag) return new StateDto { Flag = false, Message = "Smoething went wrong" };
            ApplicationUser? user = await _user.FindByIdAsync(userId);
            var httpContext = _httpContextAccessor.HttpContext;
            string hosturl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            string filepath = await getFilePath(user.UserName);
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

            string imgpath = filepath + "\\" + user.UserName + ".png";
            if (File.Exists(imgpath))
            {
                File.Delete(imgpath);
            }
            using (FileStream stream = File.Create(imgpath))
            {
                await image.CopyToAsync(stream);
            }
            string imgUrl = hosturl + "/Upload/User/" + $"{user.UserName}/" + user.UserName + ".png";
            Image? img = await _context.Image.FirstOrDefaultAsync(u => u.UserId == userId && u.PostId == null && u.LessonId == null);
            if (img is not null)
            {
                _context.Image.Remove(img);
                _context.SaveChanges();
            }
            img = new Image { UserId = userId, ImageUrl = imgUrl };

            await _context.Image.AddAsync(img);
            await _context.SaveChangesAsync();


            return new StateDto { Flag = true, Message = "Image Updated Successfully" };
        }
        private async Task<StateDto> DeleteImage(string userId)
        {
            try
            {

                StateDto state = await CheckUserId(userId);
                if (!state.Flag) return new StateDto { Flag = false, Message = "Smoething went wrong" };
                ApplicationUser? user = await _user.FindByIdAsync(userId);
                var httpContext = _httpContextAccessor.HttpContext;
                string hosturl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                string filepath = await getFilePath(user.UserName);
                string imgpath = filepath + "\\" + user.UserName + ".png";
                if (File.Exists(imgpath))
                {
                    File.Delete(imgpath);
                }
                Image? img = await _context.Image.FirstOrDefaultAsync(u => u.UserId == userId && u.PostId == null && u.LessonId == null);
                img.ImageUrl = hosturl + "/Upload/User/Avatar/Avatar.jpg";
                _context.Image.Update(img);
                await _context.SaveChangesAsync();
                state.Message = "deleted";
                return state;
            }
            catch (Exception ex)
            {
                return new StateDto { Flag = false, Message = ex.Message };
            }
        }
        private async Task<string> getFilePath(string userName)
        {
            return _environment.WebRootPath + "\\Upload\\User\\" + userName;
        }
        private async Task<StateDto> UpdateDegree(string userId, int lessonId, int degree, Quiz quiz)
        {
            StateDto state = new StateDto { Flag = false, Message = "new lesson" };
            FinishedLessons? finishedLesson = await _context.FinishedLessons.Where(fl => fl.UserId == userId && fl.LessonId == lessonId).SingleOrDefaultAsync();
            if (finishedLesson is null) return state;
            state.Flag = true;
            state.Message = "oops.You Failed";
            if (degree < (quiz.TotalDegree * .6) || degree < 0 || degree >= quiz.TotalDegree)
            {
                _context.FinishedLessons.Remove(finishedLesson);
                await _context.SaveChangesAsync();
                return state;
            }
            state.Flag = true;
            state.Message = "Congratulation,You Successed";
            finishedLesson.Degree = degree;
            await _context.SaveChangesAsync();
            return state;



        }
        private async Task<StateDto> CheckUserId(string userId)
        {
            ApplicationUser? user = await _user.FindByIdAsync(userId);
            StateDto state = new StateDto() { Flag = false, Message = "invalid Id" };
            if (user is null) return state;
            state.Flag = true;
            state.Message = "user found";
            return state;
        }
        private async Task<string> CheckLessonId(int lessonId)
        {
            Lesson? finishedLesson = await _context.Lessons.Where(l => l.Id == lessonId).FirstOrDefaultAsync();
            if (finishedLesson is null) return null;

            return finishedLesson.Name;
        }
        #endregion
    }
}
