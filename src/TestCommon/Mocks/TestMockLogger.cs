using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestCommon.Mocks
{
    public class TestMockLogger<TObject> : ILogger<TObject>
    {
        private List<TestMockLogs> _logs = new();
        
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logs.Add(new TestMockLogs
            {
                LogLevel = logLevel,
                Message = state.ToString()
            });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public List<TestMockLogs> GetLogs()
        {
            return _logs;
        }

        public class TestMockLogs
        {
            public string Message { get; set; }
            public LogLevel LogLevel { get; set; }
        }
    }
}