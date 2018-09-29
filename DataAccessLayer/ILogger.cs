using System;

namespace RobinManzl.DataAccessLayer
{

    /// <summary>
    /// Schnittstelle, um einen Logging-Mechanismus dem <code>DbService</code> hinzuzufügen
    /// </summary>
    public interface ILogger
    {

        /// <summary>
        /// Loggt eine Nachricht unter dem Log-Level DEBUG
        /// </summary>
        /// <param name="message">
        /// Die zu loggende Nachricht
        /// </param>
        void Debug(string message);

        /// <summary>
        /// Loggt eine Nachricht unter dem Log-Level INFO
        /// </summary>
        /// <param name="message">
        /// Die zu loggende Nachricht
        /// </param>
        void Info(string message);

        /// <summary>
        /// Loggt eine Nachricht unter dem Log-Level WARN
        /// </summary>
        /// <param name="message">
        /// Die zu loggende Nachricht
        /// </param>
        /// <param name="exception">
        /// Die aufgetretene Ausnahme
        /// </param>
        void Warning(string message, Exception exception);

        /// <summary>
        /// Loggt eine Nachricht unter dem Log-Level ERROR
        /// </summary>
        /// <param name="message">
        /// Die zu loggende Nachricht
        /// </param>
        /// <param name="exception">
        /// Die aufgetretene Ausnahme
        /// </param>
        void Error(string message, Exception exception);

        /// <summary>
        /// Loggt eine Nachricht unter dem Log-Level FATAL
        /// </summary>
        /// <param name="message">
        /// Die zu loggende Nachricht
        /// </param>
        /// <param name="exception">
        /// Die aufgetretene Ausnahme
        /// </param>
        void Fatal(string message, Exception exception);

    }

}
