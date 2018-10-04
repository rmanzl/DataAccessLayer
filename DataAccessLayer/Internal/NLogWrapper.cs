﻿using NLog;
using System;

namespace RobinManzl.DataAccessLayer.Internal
{

    internal class NLogWrapper : ILogger
    {

        private readonly Logger _logger;

        public NLogWrapper(Logger logger)
        {
            _logger = logger;
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Error(string message, Exception exception)
        {
            _logger.Error(exception, message);
        }

        public void Fatal(string message, Exception exception)
        {
            _logger.Fatal(exception, message);
        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warning(string message, Exception exception)
        {
            _logger.Warn(exception, message);
        }

    }

}
