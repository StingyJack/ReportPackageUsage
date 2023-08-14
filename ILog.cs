namespace ReportPackageUsage
{
    using System.Runtime.CompilerServices;

    /// <summary>
    ///     Interface ILog
    /// </summary>
    public interface ILog
    {
        /// <summary>
        ///     Informations the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="callingMethod">The calling method.</param>
        void Info(string message, [CallerMemberName] string callingMethod = null);

        /// <summary>
        ///     Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="callingMethod">The calling method.</param>
        void Err(string message, Exception ex = null, [CallerMemberName] string callingMethod = null);
    }
}