﻿using System;

namespace Jimu.Logger
{
    public interface ILogger
    {
        /// <summary>
        ///     general debug info
        /// </summary>
        /// <param name="info"></param>
        void Debug(string info);
        /// <summary>
        ///     general info
        /// </summary>
        /// <param name="info"></param>
        void Info(string info);

        /// <summary>
        ///   warning info
        /// </summary>
        /// <param name="info"></param>
        void Warn(string info);

        /// <summary>
        ///     exception or error log
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ex"></param>
        void Error(string info, Exception ex);
    }
}