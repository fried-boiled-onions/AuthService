namespace AuthService.Services
{
    public class CookieService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ��������� ������ �� ����
        public string GetTokenFromCookie(string tokenName)
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies[tokenName];
        }

        // ��������� ������ � ����
        public void SetTokenInCookie(string tokenName, string token, DateTime expiresAt, bool httpOnly = true)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(tokenName, token, new CookieOptions
            {
                HttpOnly = httpOnly,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });
        }

        // �������� ������ �� ����
        public void RemoveTokenFromCookie(string tokenName)
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(tokenName);
        }

        public void SetTokens(
            string accessToken,
            DateTime accessTokenExpires,
            string refreshToken,
            DateTime refreshTokenExpires)
        {
            SetTokenInCookie("accessToken", accessToken, accessTokenExpires, httpOnly: false); // JS-��������
            SetTokenInCookie("refreshToken", refreshToken, refreshTokenExpires, httpOnly: true); // ����������
        }
    }
}
