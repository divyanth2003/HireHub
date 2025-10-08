public class ForgotPasswordDto
{
    public string Email { get; set; } = "";
    public string? OriginBaseUrl { get; set; } 
}

public class ResetPasswordDto
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
