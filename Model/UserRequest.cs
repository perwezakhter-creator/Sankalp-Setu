namespace SankalpAPI.Model
{
    public class UserRequest
    {
        public string ClientKey { get; set; } = string.Empty;

        //public string UserID { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

       // public string UserRole { get; set; } = string.Empty;

        public string Mobile { get; set; } = string.Empty;

        //public string DistCode { get; set; } = string.Empty;

        //public string BlockCode { get; set; } = string.Empty;

        //public string DeptId { get; set; } = string.Empty;
    }

    public class BlockListRequest
    {
        public string ClientKey { get; set; } = string.Empty;

        public string District_Code { get; set; } = string.Empty;
    }

    public class PanchayatListRequest
    {
        public string ClientKey { get; set; } = string.Empty;

        public string BlockCode { get; set; } = string.Empty;
    }

    public class VillageListRequest
    {
        public string ClientKey { get; set; } = string.Empty;

        public string PanchayatCode { get; set; } = string.Empty;
    }
    public class WardListRequest
    {
        public string ClientKey { get; set; } = string.Empty;

        public string PanchayatCode { get; set; } = string.Empty;
    }
}
