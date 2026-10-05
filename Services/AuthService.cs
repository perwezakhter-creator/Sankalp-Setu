using SankalpAPI.DTO;
using SankalpAPI.Interface;
using SankalpAPI.Model;
using SankalpAPI.Repository;
using SankalpAPI.Security;
using SankalpAPI.Helper;

namespace SankalpAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IClientTokenRepository _clientTokenRepository;
        //private readonly JwtTokenService _jwtTokenService;
        private readonly ClientTokenService _clientTokenService;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepository, IClientTokenRepository clientTokenRepository, ClientTokenService clientTokenService, IConfiguration configuration)
        {
            _authRepository = authRepository;
            _clientTokenRepository = clientTokenRepository;
            //_jwtTokenService = jwtTokenService;
            _clientTokenService = clientTokenService;
            _configuration = configuration;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (request == null)
            {
                return new LoginResponse
                {
                    Status = false,
                    Message = "Invalid request."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ClientKey))
            {
                return new LoginResponse
                {
                    Status = false,
                    Message = "ClientKey is required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.UserID))
            {
                return new LoginResponse
                {
                    Status = false,
                    Message = "UserID is required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResponse
                {
                    Status = false,
                    Message = "Password is required."
                };
            }

            try
            {
                // --------------------------------------------------
                // STEP 1
                // RSA decrypt ClientKey
                // --------------------------------------------------

                string clientKey = RSA_Enc_Dec.Decryption(request.ClientKey);

                if (string.IsNullOrWhiteSpace(clientKey))
                {
                    return new LoginResponse
                    {
                        Status = false,
                        Message = "Invalid ClientKey."
                    };
                }

                // --------------------------------------------------
                // STEP 2
                // AES decrypt UserID
                // --------------------------------------------------

                string userId = Aes256CbcEncrypterApp.Decrypt(request.UserID, clientKey);

                // --------------------------------------------------
                // STEP 3
                // AES decrypt Password
                // --------------------------------------------------

                string password = Aes256CbcEncrypterApp.Decrypt(request.Password, clientKey);

                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
                {
                    return new LoginResponse
                    {
                        Status = false,
                        Message = "Invalid login credentials."
                    };
                }

                // --------------------------------------------------
                // STEP 4
                // Validate user against SQL Server
                // --------------------------------------------------

                UserDto? user = await _authRepository.LoginAsync(userId, password);

                if (user == null)
                {
                    return new LoginResponse
                    {
                        Status = false,
                        Message = "Invalid UserID or Password."
                    };
                }

                // --------------------------------------------------
                // STEP 5
                // Generate Client Token
                // --------------------------------------------------

                string clientToken = _clientTokenService.GenerateToken();

                if (string.IsNullOrWhiteSpace(clientToken))
                {
                    return new LoginResponse
                    {
                        Status = false,
                        Message = "Unable to generate ClientToken."
                    };
                }

                // ======================================================
                // 6. Hash Client Token
                // ======================================================

                string tokenHash = ClientTokenHasher.Hash(clientToken);


                // ======================================================
                // 7. Token Expiry
                // ======================================================

                DateTime expiryDate = DateTime.UtcNow.AddHours(8);


                // ======================================================
                // 8. Save Token Hash in Database
                // ======================================================

                await _clientTokenRepository.SaveTokenAsync(user.UserID!, tokenHash);

                // --------------------------------------------------
                // STEP 9
                // Return response
                // --------------------------------------------------

                return new LoginResponse
                {
                    Status = true,
                    Message = "Success",
                    ClientToken = clientToken,

                    Data = new UsrResponse
                    {
                        UserID = Aes256CbcEncrypterApp.Encrypt(user.UserID ?? "", clientKey),
                        Username = Aes256CbcEncrypterApp.Encrypt(user.Username ?? "", clientKey),
                        UserRole = Aes256CbcEncrypterApp.Encrypt(user.UserRole ?? "", clientKey),
                        Mobile = Aes256CbcEncrypterApp.Encrypt(user.Mobile ?? "", clientKey)
                    }
                };
            }
            catch (Exception)
            {
                return new LoginResponse
                {
                    Status = false,
                    Message = "Unable to process login."
                };
            }
        }
    }
}
