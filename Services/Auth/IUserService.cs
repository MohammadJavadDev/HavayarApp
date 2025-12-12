using Data;
using Entities.Auth;
using Entities.ViewModels;

namespace Services.Auth;

public interface IUserService
{
    ApplicationDbContext Context { get; }
    IQueryable<User> Table { get; }
    IQueryable<User> TableNoTracking { get; }
    Task<AuthenticateResponse?> Authenticate(AuthenticateRequest model);
    Task<IEnumerable<User>> GetAll();
    Task<User?> GetById(long id);
     User AddAndUpdateUser(User userObj);
    Task<User?> AddAndUpdateUserAsync(User userObj, CancellationToken cn);

    User CreateUser(CreateUserViewModel userObj);
    Task<User?> CreateUserAsync(CreateUserViewModel userObj, CancellationToken cn);
    Task<User?> UpdateUser(CreateUserViewModel userObj, CancellationToken cn);
 
     Task<IEnumerable<User>> SearchByName(string name);
   
}