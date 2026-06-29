using ProjekatScada.Models.Enums;

namespace ProjekatScada.Models
{
    public class UserSession
    {
        public UserSession(string username, UserRole role)
        {
            Username = username;
            Role = role;
        }

        public string Username { get; private set; }
        public UserRole Role { get; private set; }

        public bool CanWrite
        {
            get { return Role == UserRole.Admin; }
        }

        public string RoleDisplayName
        {
            get { return Role.ToString(); }
        }
    }
}
