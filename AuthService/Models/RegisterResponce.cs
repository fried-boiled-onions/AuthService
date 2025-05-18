namespace AuthService.Models
{
    public class RegisterResponse
    {
        public string Message { get; set; }
        public string Username { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        

    }
}
