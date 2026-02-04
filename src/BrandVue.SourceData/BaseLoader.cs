using System.Data;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData
{
    public abstract class BaseLoader<TWidgeroo, TIdentity> where TWidgeroo : class
    {
        private const string NULL_VALUE_STRING = "(null)";
        private const string EMPTY_VALUE_STRING = "(empty)";
        
        protected readonly ILogger _logger;
        
        protected readonly Type _loaderSubclass;
        protected string _fullyQualifiedPathToCsvDataFile;

        protected BaseRepository<TWidgeroo, TIdentity> BaseRepository;

        protected BaseLoader(
            BaseRepository<TWidgeroo, TIdentity> baseRepository,
            Type loaderSubclass,
            ILogger logger)
        {

            if (baseRepository == null)
            {
                throw new ArgumentNullException(
                    nameof(baseRepository),
                    $"Cannot create {loaderSubclass.FullName} for {typeof(TWidgeroo).FullName}s with null BaseIdentifiableRepository<{typeof(TWidgeroo).FullName}>.");
            }

            BaseRepository = baseRepository;
            _loaderSubclass = loaderSubclass;
            _logger = logger;
        }

        public virtual void Load(string fullyQualifiedPathToCsvDataFile)
        {
            _fullyQualifiedPathToCsvDataFile = fullyQualifiedPathToCsvDataFile;
            _logger.LogInformation("{LoaderName} loading data from {Path}", _loaderSubclass.FullName, fullyQualifiedPathToCsvDataFile);
        }

        protected void CreateAndStoreObjectForCsvDataRow(
            string fullyQualifiedPathToCsvDataFile,
            int lineNumber,
            string[] headers,
            string[] data,
            int identityFieldIndex)
        {
            try
            {
                var identity = GetIdentityWithExceptionHandling(
                    fullyQualifiedPathToCsvDataFile,
                    headers,
                    data,
                    identityFieldIndex,
                    lineNumber);

                CreateAndStoreObjectForIdentity(
                    fullyQualifiedPathToCsvDataFile,
                    headers,
                    data,
                    lineNumber,
                    identity);
            }
            catch (DataException ex)
            {
                _logger.LogError(ex,
                    "Error reading identity from line {LineNumber} of {Path}. " +
                    "This might be a single bad row so I'll try to read the rest of the file. Details: {ExceptionMessage}",
                    lineNumber, fullyQualifiedPathToCsvDataFile, ex.Message);
            }
        }

        private void CreateAndStoreObjectForIdentity(
            string fullyQualifiedPathToCsvDataFile,
            string[] headers,
            string[] data,
            int lineNumber,
            TIdentity identity)
        {
            var widgeroo = BaseRepository.GetOrCreate(identity);
            try
            {
                if (!ProcessLoadedRecordFor(widgeroo, data, headers))
                {
                    BaseRepository.Remove(identity);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                _logger.LogError(ex,
                    "Skipping line {LineNumber}. Not enough values found, or header for required value does not exist, in CSV file {Path}. " +
                    "Headers are {CsvHeaders}; values are {CsvData}. Exception message: {ExceptionMessage}",
                    lineNumber, fullyQualifiedPathToCsvDataFile, headers, data, ex.Message);

                BaseRepository.Remove(identity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Skipping line {LineNumber}. Configuration exception in CSV file {Path}. Exception message: {ExceptionMessage}",
                    lineNumber, fullyQualifiedPathToCsvDataFile, ex.Message);

                BaseRepository.Remove(identity);
            }
        }

        protected TIdentity GetIdentityWithExceptionHandling(
            string fullyQualifiedPathToCsvDataFile,
            string[] headers,
            string[] csv,
            int identityFieldIndex,
            int lineNumber)
        {
            try
            {
                return GetIdentity(csv, identityFieldIndex);
            }
            catch (FormatException formatException)
            {
                throw new DataException("Error occurred parsing identity. " + GetCsvInfo(), formatException);
            }
            catch (IndexOutOfRangeException indexOutOfRangeException)
            {
                throw new DataException(
                    $@"Error occurred most likely because the identity field index {identityFieldIndex} for identity field '{IdentityPropertyName}' is outside the range of indices. " + GetCsvInfo(),
                    indexOutOfRangeException);
            }

            string GetCsvInfo()
            {
                return
                    $@"in the current CSV row [{(csv == null ? NULL_VALUE_STRING : (csv.Length == 0 ? EMPTY_VALUE_STRING : string.Join(" ", csv)))}] at line {lineNumber}
                with headers[{(headers == null ? NULL_VALUE_STRING : (headers.Length == 0 ? EMPTY_VALUE_STRING : string.Join(" ", headers)))}] 
                in source file '{fullyQualifiedPathToCsvDataFile}'. This may indicate a problem with the corresponding map file.";
            }
        }

        protected virtual int GetIdentityFieldIndex(string[] fieldHeaders)
        {
            return Array.FindIndex(
                fieldHeaders,
                header => string.Equals(
                    header,
                    IdentityPropertyName,
                    StringComparison.OrdinalIgnoreCase));
        }

        protected abstract string IdentityPropertyName { get; }

        protected abstract TIdentity GetIdentity(
            string[] currentRecord,
            int identityFieldIndex);

        protected abstract bool ProcessLoadedRecordFor(
            TWidgeroo targetThing,
            string[] currentRecord,
            string[] headers);
    }
}
