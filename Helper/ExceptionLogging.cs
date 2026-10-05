namespace SankalpAPI.Helper
{
    public class ExceptionLogging
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;

        public ExceptionLogging(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment environment)
        {
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }

        public void SendErrorToText(Exception ex)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                string errorLineNo = ex.StackTrace != null && ex.StackTrace.Length > 7
                    ? ex.StackTrace.Substring(ex.StackTrace.Length - 7)
                    : "N/A";

                string errorMsg = ex.GetType().Name;
                string errorType = ex.GetType().ToString();
                string errorLocation = ex.Message;
                string errorUrl = httpContext?.Request?.Path.Value ?? "N/A";
                string hostIp = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";

                // Log folder path
                string logFolder = Path.Combine(_environment.ContentRootPath, "ExceptionDetailsFile");

                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string fileName = $"{DateTime.Now:yyyyMMdd_HHmm}.txt";
                string filePath = Path.Combine(logFolder, fileName);

                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    sw.WriteLine("----------- Exception Details -----------");
                    sw.WriteLine($"Log Date       : {DateTime.Now}");
                    sw.WriteLine($"Error Line No  : {errorLineNo}");
                    sw.WriteLine($"Error Message  : {errorMsg}");
                    sw.WriteLine($"Exception Type : {errorType}");
                    sw.WriteLine($"Error Location : {errorLocation}");
                    sw.WriteLine($"Request URL    : {errorUrl}");
                    sw.WriteLine($"User Host IP   : {hostIp}");
                    sw.WriteLine("----------------------------------------");
                    sw.WriteLine();
                }
            }
            catch
            {
                // Do not throw from logger
            }
        }
    }
}
