using System;

namespace SankalpAPI.Model
{
    public class SmsApiResponse
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? Error { get; set; }
    }

    public class SendOtpRequest
    {
        public string? ClientKey { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
    }

    public class SmsVerifyOtpRequest
    {
        public string? ClientKey { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class OtpSendResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public OtpSendData? Data { get; set; }
    }

    public class OtpSendData
    {
        public string Mobile { get; set; } = string.Empty;
        public string MaskedMobile { get; set; } = string.Empty;
        public int OtpExpiryMinutes { get; set; }
        public string? DevOtp { get; set; }
    }

    public class OtpVerifyResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public OtpVerifyData? Data { get; set; }
    }

    public class OtpVerifyData
    {
        public string Mobile { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    // Aliases for compatibility
    public class SendOtpResponse : OtpSendResponse { }
    public class VerifyOtpResultResponse : OtpVerifyResponse { }
}
