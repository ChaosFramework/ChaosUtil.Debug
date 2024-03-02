using ChaosUtil.Platform.Paths;

namespace ChaosUtil.Debug
{
    /// <summary> Provides values that are constant throughout the lifetime of the application process. </summary>
    public static class Constants
    {
        /// <summary> The directory to save log files to. </summary>
        public static readonly string LOG_DIRECTORY = Application.GetExecutableDirectory() + "\\_Logs\\";
    }
}
