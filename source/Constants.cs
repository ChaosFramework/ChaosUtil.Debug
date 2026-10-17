namespace ChaosUtil.Debug
{
    /// <summary> Provides values that are constant throughout the lifetime of the application process. </summary>
    public static class Constants
    {
        /// <summary> That's us! </summary>
        const string COMPANY = "ChaosTechnology";

        /// <summary> The format used for time stamps in file names. </summary>
        public const string TIME_FORMAT = "yyyy-MM-dd-HH-mm-ss-fff";

        /// <summary> The startup time of the current process. </summary>
        public static readonly System.DateTime STARTUP_TIME
            = System.Diagnostics.Process.GetCurrentProcess().StartTime;

        /// <summary> A string containing the startup time of the current process. Can be used in file or folder names. </summary>
        public static readonly string STARTUP_TIME_STRING
            = STARTUP_TIME.ToString(TIME_FORMAT);

        /// <summary> The product's folder in the current windows user's temp folder. Ends with a backslash. </summary>
        public static readonly string TEMP_FOLDER =
            $"{System.IO.Path.GetTempPath()}{COMPANY}/{Reflection.AssemblyMeta.productName}/";

        /// <summary> The directory to save log files to. Ends with a backslash. </summary>
        public static readonly string LOG_DIRECTORY = $"{TEMP_FOLDER}Logs/{STARTUP_TIME_STRING}/";
    }
}
