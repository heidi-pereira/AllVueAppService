using System;
using System.Collections.Generic;
using System.Linq;

namespace DashboardMetadataBuilder.MapProcessing
{
    public class MapValidationResult
    {
        public bool HasErrors => Errors.Any();
        public IReadOnlyCollection<string> Errors { get; }
        public IReadOnlyCollection<string> Warnings { get; }

        public MapValidationResult(IReadOnlyCollection<string> errors, IReadOnlyCollection<string> warnings)
        {
            Errors = errors;
            Warnings = warnings;
        }

        public static MapValidationResult ThrewException(string taskBeingPerformed, Exception e)
        {
            return new MapValidationResult(new[] {$"Error while {taskBeingPerformed}: {e}"}, new string[0]);
        }
    }
}