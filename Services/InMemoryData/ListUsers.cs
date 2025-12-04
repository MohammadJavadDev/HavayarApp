using Entities.Auth;

namespace Services.InMemoryData
{
    public class ListUsers
    {
        public static List<User> SystemUsers { get; set; } = [];
    }
}
