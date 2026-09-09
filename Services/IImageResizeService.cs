using DotNet_Image_Resize_Before_Upload.Models;

namespace DotNet_Image_Resize_Before_Upload.Services
{
    public interface IImageResizeService
    {
        Task<ImageUpload?> ResizeAndSaveAsync(
            IFormFile imageFile,
            int width,
            int height,
            bool maintainAspectRatio);

        Task<bool> UpdateImageAsync(
            int imageId,
            IFormFile? imageFile,
            int width,
            int height,
            bool maintainAspectRatio);
    }
}