# Api Rest Full

# Estrcutura del proyecto

FileReport.Api
│  Program.cs
│  appsettings.json
│  appsettings.Development.json
│
├─Controllers
│    FilesController.cs
│    UsersController.cs
│    HealthController.cs
│
├─Application
│  ├─DTOs
│  │    FileDto.cs
│  │    PagedResultDto.cs
│  │    DownloadUrlResponseDto.cs
│  │
│  ├─Interfaces
│  │    IFileReportSoapClient.cs
│  │    IMinioPresignService.cs
│  │    IFileService.cs
│  │
│  └─Services
│       FileService.cs
│
├─Infrastructure
│  ├─Soap
│  │    (aquí quedará el proxy generado del WSDL)
│  │
│  ├─Minio
│  │    MinioPresignService.cs
│  │
│  └─Security
│       JwtOptions.cs
│       TokenValidationExtensions.cs
│
└─Common
     ProblemDetailsFactory.cs
     ErrorCodes.cs


# Método	Endpoint
GET	/api/v1/files/{fileId}
GET	/api/v1/users/{userId}/files
GET	/api/v1/files/search?fileName=...
GET	/api/v1/files?page=1
POST	/api/v1/files/{fileId}/download/original
POST	/api/v1/files/{fileId}/download/encrypted
GET	/api/v1/health