namespace AuthAppFrontend.Models
{
    public class User
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string passwordHash { get; set; }
        public string ProfileImageUrl { get; set; }
    }
}
