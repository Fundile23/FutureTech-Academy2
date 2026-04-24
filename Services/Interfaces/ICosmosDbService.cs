using FutureTechAcademy.Models;

namespace FutureTechAcademy.Services.Interfaces
{
    public interface ICosmosDbService
    {
        // Student methods
        Task<IEnumerable<Student>> GetStudentsAsync(int page = 1, int pageSize = 10);
        Task<Student> GetStudentAsync(string id);
        Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm);
        Task AddStudentAsync(Student student);
        Task UpdateStudentAsync(string id, Student student);
        Task SoftDeleteStudentAsync(string id);
        Task HardDeleteStudentAsync(string id);

        // Admin methods
        Task<IEnumerable<AdminUser>> GetAdminUsersAsync();
        Task AddAdminUserAsync(AdminUser admin);
        Task RemoveAdminUserAsync(string email);
    }
}