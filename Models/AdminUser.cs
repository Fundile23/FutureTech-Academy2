using Newtonsoft.Json;

namespace FutureTechAcademy.Models
{
    public class AdminUser
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("addedBy")]
        public string AddedBy { get; set; }

        [JsonProperty("addedAt")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}