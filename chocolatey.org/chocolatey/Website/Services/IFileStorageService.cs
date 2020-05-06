using System.IO;
using System.Web.Mvc;

namespace NuGetGallery
{
    public interface IFileStorageService
    {
        ActionResult CreateDownloadFileActionResult(
            string folderName,
            string fileName,
            bool useCache);
        
        void DeleteFile(
            string folderName, 
            string fileName);

        bool FileExists(
            string folderName,
            string fileName);      
        
        Stream GetFile(
            string folderName,
            string fileName,
            bool useCache);

        void SaveFile(
            string folderName, 
            string fileName,
            Stream packageFile);
    }
}