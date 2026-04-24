using Microsoft.Extensions.Configuration;

using FutureTechAcademy.Services.Interfaces;

namespace FutureTechAcademy.Services
{
    public interface IAdminService
    {
        Task<bool> IsAdmin(string email);
        Task<List<string>> GetAllAdminEmails();
    }

    public class AdminService : IAdminService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly string _masterAdminEmail;

        public AdminService(ICosmosDbService cosmosDbService, IConfiguration config)
        {
            _cosmosDbService = cosmosDbService;
            _masterAdminEmail = config["Authentication:AdminSettings:MasterAdminEmail"] ?? "";
        }

        public async Task<bool> IsAdmin(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            // Master admin always has access
            if (email.Equals(_masterAdminEmail, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if email is in admin list in Cosmos DB
            var admins = await _cosmosDbService.GetAdminUsersAsync();
            return admins.Any(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<string>> GetAllAdminEmails()
        {
            var admins = await _cosmosDbService.GetAdminUsersAsync();
            var emails = admins.Select(a => a.Email).ToList();

            if (!string.IsNullOrEmpty(_masterAdminEmail) && !emails.Contains(_masterAdminEmail))
                emails.Insert(0, _masterAdminEmail);

            return emails;
        }
    }
}