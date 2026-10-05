namespace SankalpAPI.Model
{
    public class InitiateRegistrationRequest
    {
        public long PortalId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? DeviceId { get; set; }
        public string? ClientKey { get; set; }
    }

    public class InitiateRegistrationResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public InitiateRegistrationData? Data { get; set; }
    }

    public class InitiateRegistrationData
    {
        public long PortalId { get; set; }
        public string PortalName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string MaskedMobile { get; set; } = string.Empty;
        public int OtpExpiryMinutes { get; set; }
        public string? DevOtp { get; set; }
    }

    public class ValidateOtpAndGenerateSecretRequest
    {
        public long PortalId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? UserId { get; set; }
        public string? ClientKey { get; set; }
    }

    public class ValidateOtpAndGenerateSecretResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public TotpSecretData? Data { get; set; }
    }

    public class TotpSecretData
    {
        public long PortalId { get; set; }
        public string PortalName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TotpSecret { get; set; } = string.Empty;
        public int Digits { get; set; } = 6;
        public int Period { get; set; } = 30;
        public string Algorithm { get; set; } = "SHA1";
        public string QrCodeUri { get; set; } = string.Empty;
    }

    public class ValidateTotpRequest
    {
        public long PortalId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string TotpCode { get; set; } = string.Empty;
    }

    public class ValidateTotpResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
