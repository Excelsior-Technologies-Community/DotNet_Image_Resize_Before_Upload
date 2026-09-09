using DotNet_Image_Resize_Before_Upload.Models;
using Microsoft.Data.SqlClient;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Data;

namespace DotNet_Image_Resize_Before_Upload.Services
{
    public class ImageResizeService : IImageResizeService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public ImageResizeService(
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<ImageUpload?> ResizeAndSaveAsync(
            IFormFile imageFile,
            int width,
            int height,
            bool maintainAspectRatio)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            ValidateImage(imageFile);

            string uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

            Directory.CreateDirectory(uploadFolder);

            string extension =
                Path.GetExtension(
                    imageFile.FileName)
                .ToLowerInvariant();

            string savedFileName =
                $"{Guid.NewGuid()}{extension}";

            string filePath =
                Path.Combine(
                    uploadFolder,
                    savedFileName);

            int originalWidth;
            int originalHeight;

            using (Image image =
                   await Image.LoadAsync(
                       imageFile.OpenReadStream()))
            {
                originalWidth = image.Width;
                originalHeight = image.Height;

                ResizeOptions options;

                if (maintainAspectRatio)
                {
                    options = new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Max
                    };
                }
                else
                {
                    options = new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Stretch
                    };
                }

                image.Mutate(x => x.Resize(options));

                await image.SaveAsync(filePath);
            }

            int resizedWidth;
            int resizedHeight;

            using (Image resizedImage =
                   await Image.LoadAsync(filePath))
            {
                resizedWidth = resizedImage.Width;
                resizedHeight = resizedImage.Height;
            }

            FileInfo fileInfo =
                new FileInfo(filePath);

            string imagePath =
                "/uploads/" + savedFileName;

            int imageId =
                await InsertIntoDatabase(
                    imageFile.FileName,
                    savedFileName,
                    imagePath,
                    originalWidth,
                    originalHeight,
                    resizedWidth,
                    resizedHeight,
                    fileInfo.Length);

            return new ImageUpload
            {
                ImageId = imageId,
                OriginalFileName = imageFile.FileName,
                SavedFileName = savedFileName,
                ImagePath = imagePath,
                OriginalWidth = originalWidth,
                OriginalHeight = originalHeight,
                ResizedWidth = resizedWidth,
                ResizedHeight = resizedHeight,
                FileSize = fileInfo.Length,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<bool> UpdateImageAsync(
            int imageId,
            IFormFile? imageFile,
            int width,
            int height,
            bool maintainAspectRatio)
        {
            ImageUpload? existing =
                await GetImageById(imageId);

            if (existing == null)
                return false;

            string savedFileName =
                existing.SavedFileName;

            string imagePath =
                existing.ImagePath;

            int originalWidth =
                existing.OriginalWidth;

            int originalHeight =
                existing.OriginalHeight;

            int resizedWidth =
                existing.ResizedWidth;

            int resizedHeight =
                existing.ResizedHeight;

            long fileSize =
                existing.FileSize;

            string uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads");

            Directory.CreateDirectory(uploadFolder);

            string filePath =
                Path.Combine(
                    uploadFolder,
                    savedFileName);

            if (imageFile != null &&
                imageFile.Length > 0)
            {
                ValidateImage(imageFile);

                string extension =
                    Path.GetExtension(
                        imageFile.FileName)
                    .ToLowerInvariant();

                string newSavedFileName =
                    $"{Guid.NewGuid()}{extension}";

                string newFilePath =
                    Path.Combine(
                        uploadFolder,
                        newSavedFileName);

                using (Image image =
                       await Image.LoadAsync(
                           imageFile.OpenReadStream()))
                {
                    originalWidth = image.Width;
                    originalHeight = image.Height;

                    ResizeOptions options;

                    if (maintainAspectRatio)
                    {
                        options = new ResizeOptions
                        {
                            Size =
                                new Size(width, height),
                            Mode =
                                ResizeMode.Max
                        };
                    }
                    else
                    {
                        options = new ResizeOptions
                        {
                            Size =
                                new Size(width, height),
                            Mode =
                                ResizeMode.Stretch
                        };
                    }

                    image.Mutate(x =>
                        x.Resize(options));

                    await image.SaveAsync(
                        newFilePath);
                }

                using (Image resizedImage =
                       await Image.LoadAsync(
                           newFilePath))
                {
                    resizedWidth =
                        resizedImage.Width;

                    resizedHeight =
                        resizedImage.Height;
                }

                FileInfo newFileInfo =
                    new FileInfo(newFilePath);

                fileSize =
                    newFileInfo.Length;

                savedFileName =
                    newSavedFileName;

                imagePath =
                    "/uploads/" +
                    newSavedFileName;

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            else
            {
                // Existing image ને ફરી resize કરવી
                using (Image image =
                       await Image.LoadAsync(filePath))
                {
                    ResizeOptions options;

                    if (maintainAspectRatio)
                    {
                        options = new ResizeOptions
                        {
                            Size =
                                new Size(width, height),
                            Mode =
                                ResizeMode.Max
                        };
                    }
                    else
                    {
                        options = new ResizeOptions
                        {
                            Size =
                                new Size(width, height),
                            Mode =
                                ResizeMode.Stretch
                        };
                    }

                    image.Mutate(x =>
                        x.Resize(options));

                    await image.SaveAsync(filePath);
                }

                using (Image resizedImage =
                       await Image.LoadAsync(filePath))
                {
                    resizedWidth =
                        resizedImage.Width;

                    resizedHeight =
                        resizedImage.Height;
                }

                FileInfo fileInfo =
                    new FileInfo(filePath);

                fileSize =
                    fileInfo.Length;
            }

            return await UpdateDatabase(
                imageId,
                savedFileName,
                imagePath,
                originalWidth,
                originalHeight,
                resizedWidth,
                resizedHeight,
                fileSize);
        }

        private async Task<int> InsertIntoDatabase(
            string originalFileName,
            string savedFileName,
            string imagePath,
            int originalWidth,
            int originalHeight,
            int resizedWidth,
            int resizedHeight,
            long fileSize)
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_Insert",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@OriginalFileName",
                originalFileName);

            command.Parameters.AddWithValue(
                "@SavedFileName",
                savedFileName);

            command.Parameters.AddWithValue(
                "@ImagePath",
                imagePath);

            command.Parameters.AddWithValue(
                "@OriginalWidth",
                originalWidth);

            command.Parameters.AddWithValue(
                "@OriginalHeight",
                originalHeight);

            command.Parameters.AddWithValue(
                "@ResizedWidth",
                resizedWidth);

            command.Parameters.AddWithValue(
                "@ResizedHeight",
                resizedHeight);

            command.Parameters.AddWithValue(
                "@FileSize",
                fileSize);

            await connection.OpenAsync();

            object? result =
                await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        private async Task<bool> UpdateDatabase(
            int imageId,
            string savedFileName,
            string imagePath,
            int originalWidth,
            int originalHeight,
            int resizedWidth,
            int resizedHeight,
            long fileSize)
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_Update",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@ImageId",
                imageId);

            command.Parameters.AddWithValue(
                "@SavedFileName",
                savedFileName);

            command.Parameters.AddWithValue(
                "@ImagePath",
                imagePath);

            command.Parameters.AddWithValue(
                "@OriginalWidth",
                originalWidth);

            command.Parameters.AddWithValue(
                "@OriginalHeight",
                originalHeight);

            command.Parameters.AddWithValue(
                "@ResizedWidth",
                resizedWidth);

            command.Parameters.AddWithValue(
                "@ResizedHeight",
                resizedHeight);

            command.Parameters.AddWithValue(
                "@FileSize",
                fileSize);

            await connection.OpenAsync();

            int result =
                Convert.ToInt32(
                    await command.ExecuteScalarAsync());

            return result > 0;
        }

        private async Task<ImageUpload?> GetImageById(
            int imageId)
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "DBConnection");

            using SqlConnection connection =
                new SqlConnection(connectionString);

            using SqlCommand command =
                new SqlCommand(
                    "SP_ImageUpload_GetById",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.AddWithValue(
                "@ImageId",
                imageId);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ImageUpload
                {
                    ImageId =
                        Convert.ToInt32(
                            reader["ImageId"]),

                    OriginalFileName =
                        reader["OriginalFileName"]
                        .ToString() ?? "",

                    SavedFileName =
                        reader["SavedFileName"]
                        .ToString() ?? "",

                    ImagePath =
                        reader["ImagePath"]
                        .ToString() ?? "",

                    OriginalWidth =
                        Convert.ToInt32(
                            reader["OriginalWidth"]),

                    OriginalHeight =
                        Convert.ToInt32(
                            reader["OriginalHeight"]),

                    ResizedWidth =
                        Convert.ToInt32(
                            reader["ResizedWidth"]),

                    ResizedHeight =
                        Convert.ToInt32(
                            reader["ResizedHeight"]),

                    FileSize =
                        Convert.ToInt64(
                            reader["FileSize"]),

                    CreatedDate =
                        Convert.ToDateTime(
                            reader["CreatedDate"])
                };
            }

            return null;
        }

        private void ValidateImage(IFormFile imageFile)
        {
            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            string extension =
                Path.GetExtension(
                    imageFile.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new Exception(
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            const long maxFileSize =
                10 * 1024 * 1024;

            if (imageFile.Length > maxFileSize)
            {
                throw new Exception(
                    "Maximum image size is 10 MB.");
            }
        }
    }
}