// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.EntityFrameworkCore
{
    public class SimpleLoggerTests
    {
        private const string ContextInitialized =
            @"info: <Local Date> HH:mm:ss.fff CoreEventId.ContextInitialized[10403] (Microsoft.EntityFrameworkCore.Infrastructure) 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ";

        private const string SaveChangesStarting =
            @"dbug: <Local Date> HH:mm:ss.fff CoreEventId.SaveChangesStarting[10004] (Microsoft.EntityFrameworkCore.Update) 
      SaveChanges starting for 'LoggingContext'.";

        private const string SaveChangesCompleted =
            @"dbug: <Local Date> HH:mm:ss.fff CoreEventId.SaveChangesCompleted[10005] (Microsoft.EntityFrameworkCore.Update) 
      SaveChanges completed for 'LoggingContext' with 0 entities written to the database.";

        private const string ContextDisposed =
            @"dbug: <Local Date> HH:mm:ss.fff CoreEventId.ContextDisposed[10407] (Microsoft.EntityFrameworkCore.Infrastructure) 
      'LoggingContext' disposed.";

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_with_default_options(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(async, stream, b => b.LogTo(stream.WriteLine));

            AssertLog(actual, ContextInitialized, SaveChangesStarting, SaveChangesCompleted, ContextDisposed);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_with_minimum_level(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information));

            AssertLog(actual, ContextInitialized);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_multiple_categories(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { DbLoggerCategory.Infrastructure.Name, DbLoggerCategory.Update.Name }));

            AssertLog(actual, ContextInitialized, SaveChangesStarting, SaveChangesCompleted, ContextDisposed);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_single_category(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { DbLoggerCategory.Infrastructure.Name }));

            AssertLog(actual, ContextInitialized, ContextDisposed);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_single_category_and_minimum_level(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { DbLoggerCategory.Infrastructure.Name },
                    LogLevel.Information));

            AssertLog(actual, ContextInitialized);

            stream = new StringWriter();
            actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { DbLoggerCategory.Update.Name },
                    LogLevel.Information));

            Assert.Equal("", actual);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_single_event(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { CoreEventId.ContextInitialized }));

            AssertLog(actual, ContextInitialized);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_multiple_events(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { CoreEventId.ContextInitialized, CoreEventId.ContextDisposed }));

            AssertLog(actual, ContextInitialized, ContextDisposed);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_many_events(bool async) // Hits HashCode usage
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[]
                    {
                        CoreEventId.ContextInitialized,
                        CoreEventId.ContextDisposed,
                        CoreEventId.StartedTracking,
                        CoreEventId.StateChanged,
                        CoreEventId.ValueGenerated,
                        CoreEventId.CascadeDelete
                    }));

            AssertLog(actual, ContextInitialized, ContextDisposed);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_for_single_event_and_minimum_level(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { CoreEventId.ContextInitialized },
                    LogLevel.Information));

            AssertLog(actual, ContextInitialized);

            stream = new StringWriter();
            actual = await LogTest(
                async,
                stream,
                b => b.LogTo(
                    stream.WriteLine,
                    new[] { CoreEventId.ContextDisposed },
                    LogLevel.Information));

            Assert.Equal("", actual);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_with_custom_filter(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, (e, l) => e == CoreEventId.SaveChangesCompleted));

            AssertLog(actual, SaveChangesCompleted);
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_with_custom_logger(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(async, stream, b => b.LogTo(new CustomSimpleLogger(stream.Write)));

            Assert.Equal(@"Initialized LoggingContext" + Environment.NewLine, actual);
        }

        private class CustomSimpleLogger : ISimpleLogger
        {
            public CustomSimpleLogger([NotNull] Action<string> sink)
            {
                Sink = sink;
            }

            public virtual Action<string> Sink { get; }

            public void Log(EventData eventData)
                => Sink("Initialized " + ((ContextInitializedEventData)eventData).Context.GetType().Name);

            public bool ShouldLog(EventId eventId, LogLevel logLevel)
                => eventId == CoreEventId.ContextInitialized;
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_with_raw_message(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.None));

            AssertLog(
                actual,
                @"Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_raw_single_line(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.SingleLine));

            AssertLog(
                actual,
                @"Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_default_single_line(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(
                    stream.WriteLine,
                    LogLevel.Information,
                    SimpleLoggerFormatOptions.SingleLine | SimpleLoggerFormatOptions.DefaultWithLocalTime));

            AssertLog(
                actual,
                @"info: <Local Date> HH:mm:ss.fff CoreEventId.ContextInitialized[10403] (Microsoft.EntityFrameworkCore.Infrastructure) -> Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_only_level(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.Level));

            AssertLog(
                actual,
                @"info: 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_only_local_time(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.LocalTime), 0);

            AssertLog(
                actual,
                @"<Local Date> HH:mm:ss.fff 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_only_UTC_time(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.UtcTime), 0, true);

            AssertLog(
                actual,
                @"YYYY-MM-DDTHH:MM:SS.MMMMMMTZ 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_only_ID(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.Id));

            AssertLog(
                actual,
                @"CoreEventId.ContextInitialized[10403] 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_only_category(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.Category));

            AssertLog(
                actual,
                @"(Microsoft.EntityFrameworkCore.Infrastructure) 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_level_and_ID(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(
                    stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.Id | SimpleLoggerFormatOptions.Level));

            AssertLog(
                actual,
                @"info: CoreEventId.ContextInitialized[10403] 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_level_and_UTC(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(
                    stream.WriteLine,
                    LogLevel.Information,
                    SimpleLoggerFormatOptions.UtcTime | SimpleLoggerFormatOptions.Level),
                6, true);

            AssertLog(
                actual,
                @"info: YYYY-MM-DDTHH:MM:SS.MMMMMMTZ 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Log_default_UTC(bool async)
        {
            var stream = new StringWriter();
            var actual = await LogTest(
                async, stream, b => b.LogTo(
                    stream.WriteLine, LogLevel.Information, SimpleLoggerFormatOptions.DefaultWithUtcTime),6, true);

            AssertLog(
                actual,
                @"info: YYYY-MM-DDTHH:MM:SS.MMMMMMTZ CoreEventId.ContextInitialized[10403] (Microsoft.EntityFrameworkCore.Infrastructure) 
      Entity Framework Core X.X.X-any initialized 'LoggingContext' using provider 'Microsoft.EntityFrameworkCore.InMemory' with options: StoreName=SimpleLoggerTests ");
        }

        private static void AssertLog(string actual, params string[] lines)
            => Assert.Equal(
                string.Join(Environment.NewLine, lines) + Environment.NewLine,
                actual,
                ignoreLineEndingDifferences: true,
                ignoreWhiteSpaceDifferences: true);

        private static async Task<string> LogTest(
            bool async,
            TextWriter writer,
            Func<DbContextOptionsBuilder<LoggingContext>, DbContextOptionsBuilder<LoggingContext>> configureLogging,
            int dateAt = 6,
            bool utc = false)
        {
            var options = configureLogging(
                    new DbContextOptionsBuilder<LoggingContext>()
                        .UseInMemoryDatabase("SimpleLoggerTests"))
                .Options;

            string productVersion;

            using (var context = new LoggingContext(options))
            {
                Assert.Equal(0, async ? await context.SaveChangesAsync() : context.SaveChanges());

                productVersion = context.Model.GetProductVersion();
            }

            var lines = writer.ToString()
                .Replace(productVersion, "X.X.X-any")
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                var normalized = line;

                if (!normalized.StartsWith("Init", StringComparison.Ordinal))
                {
                    if (normalized.Contains("20", StringComparison.Ordinal))
                    {
                        // May fail if test happens to span midnight on a change in length; seems unlikely!
                        var end = (utc ? 28 : DateTime.Now.ToShortDateString().Length + 13) + dateAt;
                        normalized = normalized.Substring(0, dateAt)
                            + (utc ? "YYYY-MM-DDTHH:MM:SS.MMMMMMTZ" : "<Local Date> HH:mm:ss.fff") + normalized.Substring(end);
                    }
                }

                builder.AppendLine(normalized);
            }

            return builder.ToString();
        }

        private class LoggingContext : DbContext
        {
            public LoggingContext([NotNull] DbContextOptions options)
                : base(options)
            {
            }
        }
    }
}
