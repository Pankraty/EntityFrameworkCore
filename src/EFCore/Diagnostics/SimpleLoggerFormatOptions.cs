// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Diagnostics
{
    /// <summary>
    ///     Formatting options for use with <see cref="SimpleLogger" />
    ///     and <see cref="DbContextOptionsBuilder.LogTo(Action{string},LogLevel,SimpleLoggerFormatOptions?)" />.
    /// </summary>
    [Flags]
    public enum SimpleLoggerFormatOptions
    {
        /// <summary>
        ///     The raw log message with no additional metadata or formatting.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Each event will only occupy a single line in the log. Multiple lines are used by default.
        /// </summary>
        SingleLine = 1,

        /// <summary>
        ///     Include the event <see cref="LogLevel" /> in each log message. The level is included by default.
        /// </summary>
        Level = 1 << 1,

        /// <summary>
        ///     Includes the event <see cref="DbLoggerCategory" /> in each message. The category is included by default.
        /// </summary>
        Category = 1 << 2,

        /// <summary>
        ///     Includes the <see cref="EventId" /> in each message. The event ID is included by default.
        /// </summary>
        Id = 1 << 3,

        /// <summary>
        ///     Includes a UTC timestamp in each message. The local time is included by default. Use <see cref="DefaultWithUtcTime" />
        ///     to include all default options but change timestamps to UTC.
        /// </summary>
        UtcTime = 1 << 4,

        /// <summary>
        ///     Includes a local time timestamp in each message. The local time is included by default.
        /// </summary>
        LocalTime = 1 << 5,

        /// <summary>
        ///     <para>
        ///         The default used by <see cref="DbContextOptionsBuilder.LogTo(Action{string},LogLevel,SimpleLoggerFormatOptions?)" />.
        ///     </para>
        ///     <para>
        ///         Includes <see cref="Level" />, <see cref="Category" />, <see cref="Id" />, <see cref="LocalTime" />.
        ///     </para>
        /// </summary>
        DefaultWithLocalTime = Level | Category | Id | LocalTime,

        /// <summary>
        ///     <para>
        ///         The same defaults as used by <see cref="DbContextOptionsBuilder.LogTo(Action{string},LogLevel,SimpleLoggerFormatOptions?)" />,
        ///         but with UTC timestamps.
        ///     </para>
        ///     <para>
        ///         Includes <see cref="Level" />, <see cref="Category" />, <see cref="Id" />, <see cref="UtcTime" />.
        ///     </para>
        /// </summary>
        DefaultWithUtcTime = Level | Category | Id | UtcTime
    }
}
