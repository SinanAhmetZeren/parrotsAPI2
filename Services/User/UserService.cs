using ParrotsAPI2.Dtos.User;

namespace ParrotsAPI2.Services.User
{
    public class UserService : IUserService
    {


        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public UserService(IMapper mapper, DataContext context)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> AddUser(AddUserDto newUser)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();
            var user = _mapper.Map<Models.User>(newUser);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var updatedUsers = await _context.Users.ToListAsync();
            serviceResponse.Data = updatedUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> DeleteUser(int id)
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    throw new Exception($"User with ID `{id}` not found");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                var updatedUsers = await _context.Users.ToListAsync();
                serviceResponse.Data = updatedUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            }
            catch (Exception ex)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = ex.Message;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<GetUserDto>>> GetAllUsers()
        {
            var serviceResponse = new ServiceResponse<List<GetUserDto>>();
            var dbUsers = await _context.Users.ToListAsync();
            serviceResponse.Data = dbUsers.Select(c => _mapper.Map<GetUserDto>(c)).ToList();
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> GetUserById(int id)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            var dbUsers = await _context.Users.ToListAsync();
            var user = dbUsers.FirstOrDefault(c => c.Id == id);
            serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser)
        {
            var serviceResponse = new ServiceResponse<GetUserDto>();
            try
            {
                var user = await _context.Users.FindAsync(updatedUser.Id);
                if (user == null)
                {
                    throw new Exception($"Character with ID `{updatedUser.Id}` not found");
                }
                user.Name = updatedUser.Name;
                user.Title = updatedUser.Title;
                user.Bio = updatedUser.Bio;
                user.Email = updatedUser.Email;
                user.Instagram = updatedUser.Instagram;
                user.Facebook = updatedUser.Facebook;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.ProfileImageUrl = updatedUser.ProfileImageUrl;
                //user.UnseenMessages = updatedUser.UnseenMessages;
                //user.Vehicles = updatedUser.Vehicles;
                //user.Voyages = updatedUser.Voyages;
                //user.Bids = updatedUser.Bids;
                //user.SentMessages = updatedUser.SentMessages;
                //user.ReceivedMessages = updatedUser.ReceivedMessages;

            await _context.SaveChangesAsync();
                    serviceResponse.Data = _mapper.Map<GetUserDto>(user);
                }
                catch (Exception ex)
                {
                    serviceResponse.Success = false;
                    serviceResponse.Message = ex.Message;
                }

                return serviceResponse;
        }
    }
}
