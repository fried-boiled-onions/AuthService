namespace AuthService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string RefreshToken { get; set; }
        public string Username { get; set; } // Добавлено
    }
}
