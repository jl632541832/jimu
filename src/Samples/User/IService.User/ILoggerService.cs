﻿using Jimu;

namespace IService.User
{
    /// <summary>
    /// try logger
    /// </summary>
    [Jimu("/{Service}")]
    public interface ILoggerService : IJimuService
    {
        /// <summary>
        /// post something to log
        /// </summary>
        /// <param name="log">something to log</param>
        [JimuPost(true)]
        void Post(string log);
    }
}
