namespace GalacticExpansion.Services
{
    /// <summary>
    /// Placeholder for integrating third-party cloud save providers such as Steam Cloud.
    /// </summary>
    public interface ICloudSaveHooks
    {
        void UploadSave(string path);
        void DownloadSave(string path);
    }
}
