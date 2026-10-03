FutureTech Academy
A simple ASP.NET Core MVC app for managing student records with Google login and admin-only access.

Features
Google sign-in

Admin-only student management (Create, Read, Update, Delete)

Profile image uploads via Azure Blob Storage

Student data stored in Azure Cosmos DB

Search and pagination

Tech Stack
ASP.NET Core MVC (.NET 8)

Azure Cosmos DB

Azure Blob Storage

Google OAuth 2.0

Setup
Clone and restore

bash
git clone <repo-url>
cd FutureTechAcademy
dotnet restore
Configure appsettings.json

json
{
  "Azure": {
    "CosmosDb": {
      "EndpointUri": "<your-endpoint>",
      "PrimaryKey": "<your-key>",
      "DatabaseName": "StudentDB",
      "ContainerName": "Students"
    },
    "BlobStorage": {
      "ConnectionString": "<your-connection-string>",
      "ContainerName": "student-images"
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "<your-client-id>",
      "ClientSecret": "<your-client-secret>"
    },
    "AdminSettings": {
      "MasterAdminEmail": "admin@example.com"
    }
  }
}
Set up Azure resources

Cosmos DB database: StudentDB

Containers: Students and Admins (partition key: /id)

Blob container: student-images

Set up Google OAuth

Create OAuth 2.0 credentials in Google Cloud Console

Add redirect URI: https://localhost:<port>/signin-google

Add scopes: email, profile

Run

bash
dotnet run
How Admin Access Works
The MasterAdminEmail in appsettings.json is always an admin.

Other admins are stored in the Admins container in Cosmos DB.

If a signed-in user isn't an admin, they see an Access Denied page.

Config path note
AdminSettings is nested inside Authentication, so access it like this:

csharp
config["Authentication:AdminSettings:MasterAdminEmail"]
Project Structure
text
Controllers/    → Home, Auth, Students
Models/         → Student, AdminUser
Services/       → CosmosDbService, BlobStorageService, AdminService
Views/          → Razor views for Students
Troubleshooting
Issue	Fix
Always "Access Denied"	Check config path is Authentication:AdminSettings:MasterAdminEmail
Email is null	Ensure email scope is added in Google options
No admins found	Verify Admins container exists and has email fields
Blob upload fails	Check connection string and container name
License
Educational use only.

