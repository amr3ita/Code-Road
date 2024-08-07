﻿using Code_Road.Dto.Account;
using Code_Road.Dto.Comment;
using Code_Road.Dto.Post;
using Code_Road.Dto.User;
using Code_Road.Models;
using Code_Road.Services.UserService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Code_Road.Services.PostService
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PostService(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _userManager = userManager;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        public async Task<List<PostAndCommentsDto>> GetAllAsync()
        {

            var postsIds = _context.Posts.OrderByDescending(i => i.Up).Select(p => p.Id).ToList();
            List<PostAndCommentsDto> posts = new List<PostAndCommentsDto>();
            //Posts Not Found
            if (postsIds is null)
            {
                return null;
            }
            foreach (var post in postsIds)
            {
                posts.Add(await GetByIdAsync(post));
            }
            return posts;
        }



        public async Task<PostAndCommentsDto> GetByIdAsync(int post_id)
        {
            StateDto state = new StateDto();
            var post = await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == post_id);

            if (post == null)
            {
                state.Flag = false;
                state.Message = "Post Not Found ! ";
                return new PostAndCommentsDto { State = state };
            }
            state.Flag = true;
            state.Message = "Sucsses";

            var postDto = new PostDto
            {
                Status = state,
                PostId = post.Id,
                UserId = post.UserId,
                UserImage = await _userService.GetUserImage(post.UserId),
                UserName = post.User.FirstName + " " + post.User.LastName,
                Content = post.Content,
                Up = post.Up,
                Down = post.Down,
                Date = post.Date,
                Image_url = post.Images.Where(i => i.UserId == post.User.Id).Select(i => i.ImageUrl).ToList()
            };
            PostAndCommentsDto pcd = new PostAndCommentsDto();
            pcd.State = state;
            pcd.post = postDto;

            pcd.Comments = await _context.Comments.Include(d => d.User).Where(c => c.PostId == post.Id).Select(cs => new CommentDto { Id = cs.Id, UserId = cs.UserId, UserName = cs.User.UserName, UserImage = cs.User.Image.ImageUrl, Content = cs.Content, Up = cs.Up, Down = cs.Down, Date = cs.Date }).ToListAsync();

            return pcd;
        }

        public async Task<PostDto> AddPostAsync(AllUserDataDto currentUser, AddPostDto postModel)
        {
            if (currentUser is not null)
            {
                StateDto state = new StateDto();
                var validationResult = await ValidatePostModel(postModel);

                if (!validationResult.Flag)
                    return new PostDto { Status = validationResult };

                var user = await _userManager.FindByIdAsync(postModel.UserId);
                if (user == null)
                {
                    state.Flag = false;
                    state.Message = "Invalid user 'user not Found!' ";
                    return new PostDto { Status = state };
                }

                var post = new Post
                {
                    Content = postModel.Content,
                    Date = DateTime.Now,
                    UserId = user.Id
                };
                await _context.Posts.AddAsync(post);
                await _context.SaveChangesAsync();
                if (postModel.Images != null)
                {
                    string user_name = user.UserName;
                    post.Images = await GetImagePath(postModel.Images, user_name, post.Id, user.Id);
                }

                var test = await GetByIdAsync(post.Id);
                return test.post;
            }
            return new PostDto { Status = new StateDto { Flag = false, Message = "You don't have permission to Add new post" } };

        }

        public async Task<PostDto> UpdatePostAsync(AllUserDataDto currentUser, int post_id, UpdatePostDto postModel)
        {
            StateDto state = new StateDto();
            var old_post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == post_id);
            if (currentUser.IsAdmin == true || currentUser.userInfo.Id == old_post.UserId)
            {
                //check user access 
                Console.WriteLine($"beFore operation {old_post.Content}");
                if (old_post.UserId != postModel.UserId)
                {
                    state.Flag = false;
                    state.Message = "'Access Denied'or inValid UserId ";
                    return new PostDto { Status = state };
                }
                //Check If post exisest or not
                if (old_post == null)
                {
                    state.Flag = false;
                    state.Message = "Pst Not Found! ";
                    return new PostDto { Status = state };
                }

                var user = await _userManager.FindByIdAsync(postModel.UserId);
                if (user == null)
                {
                    state.Flag = false;
                    state.Message = "Invalid user 'user not Found!' ";
                    return new PostDto { Status = state };
                }

                old_post.Content = postModel.Content;
                if (postModel.Images != null)
                {
                    List<Image> images = await _context.Image.Where(p => p.PostId == post_id).ToListAsync();

                    foreach (var item in postModel.Images)
                    {
                        await DeletImage(post_id);
                    }
                    _context.Image.RemoveRange(images);
                    old_post.Images = await GetImagePath(postModel.Images, user.UserName, old_post.Id, old_post.UserId);
                }
                _context.Posts.Update(old_post);
                await _context.SaveChangesAsync();
                Console.WriteLine($"After operation {old_post.Content}");
                var test = await GetByIdAsync(old_post.Id);
                return test.post;
            }
            return new PostDto { Status = new StateDto { Flag = false, Message = "You don't have permission to update this post" } };
        }

        public async Task<StateDto> DeletePostAsync(AllUserDataDto currentUser, int post_id)
        {
            var del_post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == post_id);
            if (currentUser.IsAdmin == true || del_post.UserId == currentUser.userInfo.Id)
            {
                // delete comments
                var comments = await _context.Comments.Where(c => c.PostId == post_id).ToListAsync();
                foreach (var comment in comments)
                {
                    // delete comments votes
                    var commentVotes = await _context.Comments_Vote.Where(cv => cv.CommentId == comment.Id).ToListAsync();
                    _context.Comments_Vote.RemoveRange(commentVotes);
                    await _context.SaveChangesAsync();
                }
                _context.Comments.RemoveRange(comments);
                await _context.SaveChangesAsync();
                // delete post votes
                var postVotes = await _context.Posts_Vote.Where(pv => pv.PostId == post_id).ToListAsync();
                _context.Posts_Vote.RemoveRange(postVotes);
                await _context.SaveChangesAsync();

                List<Image> images = await _context.Image.Where(p => p.PostId == post_id).ToListAsync();
                if (del_post != null)
                {
                    if (images.Count > 0)
                    {
                        foreach (var image in images)
                        {
                            await DeletImage(del_post.Id);
                        }
                        _context.Image.RemoveRange(images);
                    }
                    _context.Posts.Remove(del_post);

                    await _context.SaveChangesAsync();

                    return new StateDto { Flag = true, Message = "deleted Successfully" };
                }
                return new StateDto { Flag = false, Message = "not found" };
            }
            return new StateDto { Flag = false, Message = "You don't have permission to delete this post" };
        }

        public async Task<List<UsersReactDto>> GetUpVotes(int postId)
        {
            Post? post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            List<UsersReactDto> users = new List<UsersReactDto>();
            StateDto state = new StateDto { Flag = false, Message = "there is no post with this id" };
            if (post == null)
            {
                users.Add(new UsersReactDto { State = state });
                return users;
            }
            var votes = _context.Posts_Vote
                .Where(pv => pv.PostId == postId && pv.Vote == 1)
                .Select(u => new { u.UserId, u.UserName, u.ImageUrl })
                .ToList();
            state.Message = "there is no Up Votes yet";
            if (votes.Count == 0)
            {
                users.Add(new UsersReactDto { State = state });
                return users;
            }
            else
            {
                state.Flag = true;
                state.Message = $"There is {votes.Count} Up votes";
                foreach (var vote in votes)
                {
                    users.Add(new UsersReactDto { State = state, UserId = vote.UserId, UserName = vote.UserName, ImageUrl = vote.ImageUrl });
                }
                return users;
            }
        }
        public async Task<List<UsersReactDto>> GetDownVotes(int postId)
        {
            Post? post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            List<UsersReactDto> users = new List<UsersReactDto>();
            StateDto state = new StateDto { Flag = false, Message = "there is no post with this id" };
            if (post == null)
            {
                users.Add(new UsersReactDto { State = state });
                return users;
            }
            var votes = _context.Posts_Vote
                .Where(pv => pv.PostId == postId && pv.Vote == 0)
                .Select(u => new { u.UserId, u.UserName, u.ImageUrl })
                .ToList();
            state.Message = "there is no Down Votes yet";
            if (votes.Count == 0)
            {
                users.Add(new UsersReactDto { State = state });
                return users;
            }
            else
            {
                state.Flag = true;
                state.Message = $"There is {votes.Count} Down votes"; ;
                foreach (var vote in votes)
                {
                    users.Add(new UsersReactDto { State = state, UserId = vote.UserId, UserName = vote.UserName, ImageUrl = vote.ImageUrl });
                }
                return users;
            }
        }

        private async Task<bool> DeletImage(int post_id)
        {
            try
            {
                string file_path = await getFilePath(post_id);
                if (Directory.Exists(file_path))
                {
                    Directory.Delete(file_path, true);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<List<Image>> GetImagePath(IFormFileCollection formFileCollection, string userName, int postId, string user_id)
        {
            int counter = 0;
            var httpContext = _httpContextAccessor.HttpContext;
            string hosturl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            List<Image> imageReturnd = new List<Image>();
            string filepath = await getFilePath(postId);
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }
            foreach (var file in formFileCollection)
            {
                Image img = new Image();
                string imgpath = filepath + "\\" + userName + "_" + counter++ + ".png";
                if (File.Exists(imgpath))
                {
                    File.Delete(imgpath);
                }
                using (FileStream stream = File.Create(imgpath))
                {
                    await file.CopyToAsync(stream);
                }
                counter--;
                string imgUrl = hosturl + "/Upload/Post/" + postId + "/" + userName + "_" + counter++ + ".png";
                img.ImageUrl = imgUrl;
                img.PostId = postId;
                img.UserId = user_id;
                imageReturnd.Add(img);
                await _context.Image.AddAsync(img);
                await _context.SaveChangesAsync();

            }
            return imageReturnd;
        }

        private async Task<string> getFilePath(int post_id)
        {
            return _environment.WebRootPath + "\\Upload\\Post\\" + post_id;
        }

        private Task<StateDto> ValidatePostModel(AddPostDto postModel)
        {
            StateDto state = new StateDto();
            state.Flag = true;
            state.Message = "Success!";

            if (postModel == null)
            {
                state.Flag = false;
                state.Message = "Post Is null Enter Data!";
                return Task.FromResult(state);
            }

            if (string.IsNullOrEmpty(postModel.UserId))
            {
                state.Flag = false;
                state.Message = "User Id Is null Enter User Id!";
                return Task.FromResult(state);
            }

            if (string.IsNullOrEmpty(postModel.Content) || postModel.Content.Length < 5 || postModel.Content.Length > 500)
            {
                state.Flag = false;
                state.Message = "Content must be greater than 5 char and less Than 500 char!";
                return Task.FromResult(state);
            }

            return Task.FromResult(state);
        }



    }
}
