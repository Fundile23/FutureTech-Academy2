using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FutureTechAcademy.Models;
using FutureTechAcademy.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace FutureTechAcademy.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;
        private readonly ILogger<CosmosDbService> _logger;

        public CosmosDbService(IConfiguration config, ILogger<CosmosDbService> logger)
        {
            _logger = logger;

            var endpointUri = config["Azure:CosmosDb:EndpointUri"];
            var primaryKey = config["Azure:CosmosDb:PrimaryKey"];
            var databaseName = config["Azure:CosmosDb:DatabaseName"];
            var containerName = config["Azure:CosmosDb:ContainerName"];

            var cosmosClient = new CosmosClient(endpointUri, primaryKey, new CosmosClientOptions
            {
                ApplicationName = "FutureTechStudentSystem",
                ConnectionMode = ConnectionMode.Direct
            });

            // Create database and container if they don't exist
            CreateDatabaseIfNotExistsAsync(cosmosClient, databaseName, containerName).GetAwaiter().GetResult();

            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        private async Task CreateDatabaseIfNotExistsAsync(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            try
            {
                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
                await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
                _logger.LogInformation($"Database {databaseName} and container {containerName} initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database or container");
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetStudentsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _container.GetItemQueryIterator<Student>(
                    new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false OFFSET @offset LIMIT @limit")
                        .WithParameter("@offset", (page - 1) * pageSize)
                        .WithParameter("@limit", pageSize)
                );

                var results = new List<Student>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students");
                throw;
            }
        }

        public async Task<Student> GetStudentAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Student with ID {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting student {id}");
                throw;
            }
        }

        public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetStudentsAsync();

                searchTerm = searchTerm.ToLowerInvariant();

                var query = _container.GetItemQueryIterator<Student>(
                    new QueryDefinition(
                        "SELECT * FROM c WHERE c.IsDeleted = false AND (" +
                        "CONTAINS(LOWER(c.FirstName), @term) OR " +
                        "CONTAINS(LOWER(c.LastName), @term) OR " +
                        "CONTAINS(LOWER(c.Email), @term) OR " +
                        "CONTAINS(LOWER(c.Id), @term))")
                        .WithParameter("@term", searchTerm)
                );

                var results = new List<Student>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    results.AddRange(response);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching students with term '{searchTerm}'");
                throw;
            }
        }

        public async Task AddStudentAsync(Student student)
        {
            try
            {
                student.CreatedAt = DateTime.UtcNow;
                student.IsDeleted = false;

                await _container.CreateItemAsync(student, new PartitionKey(student.Id));
                _logger.LogInformation($"Student {student.Id} created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating student {student.Id}");
                throw;
            }
        }

        public async Task UpdateStudentAsync(string id, Student student)
        {
            try
            {
                student.UpdatedAt = DateTime.UtcNow;

                await _container.UpsertItemAsync(student, new PartitionKey(id));
                _logger.LogInformation($"Student {id} updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating student {id}");
                throw;
            }
        }

        public async Task SoftDeleteStudentAsync(string id)
        {
            try
            {
                var student = await GetStudentAsync(id);
                if (student != null)
                {
                    student.IsDeleted = true;
                    student.UpdatedAt = DateTime.UtcNow;
                    await UpdateStudentAsync(id, student);
                    _logger.LogInformation($"Student {id} soft deleted successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting student {id}");
                throw;
            }
        }

        public async Task HardDeleteStudentAsync(string id)
        {
            try
            {
                await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));
                _logger.LogInformation($"Student {id} permanently deleted");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Student {id} not found for deletion");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting student {id}");
                throw;
            }


        }

        public async Task<IEnumerable<AdminUser>> GetAdminUsersAsync()
        {
            var query = _container.GetItemQueryIterator<AdminUser>(
                new QueryDefinition("SELECT * FROM c WHERE c.email != null")
            );

            var results = new List<AdminUser>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task AddAdminUserAsync(AdminUser admin)
        {
            await _container.CreateItemAsync(admin, new PartitionKey(admin.Id));
        }

        public async Task RemoveAdminUserAsync(string email)
        {
            var admins = await GetAdminUsersAsync();
            var admin = admins.FirstOrDefault(a => a.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (admin != null)
            {
                await _container.DeleteItemAsync<AdminUser>(admin.Id, new PartitionKey(admin.Id));
            }
        }
    }
}