using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Services.Interfaces;

namespace BrandVue.Services;

public class SurveyGroupService : ISurveyGroupService
{
    private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
    private readonly ILogger<SurveyGroupService> _logger;

    public SurveyGroupService(IDbContextFactory<MetaDataContext> dbContextFactory, ILogger<SurveyGroupService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task RenameSurveyGroupAsync(string oldName, string newName)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(oldName))
        {
            throw new ArgumentException("Old name cannot be null or empty.", nameof(oldName));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("New name cannot be null or empty.", nameof(newName));
        }

        if (oldName.Equals(newName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Old name and new name cannot be the same.", nameof(newName));
        }

        if (dbContext
            .VariableConfigurations
            .Any(x => x.SubProductId == newName))
        {
            throw new ArgumentException($"A survey group with the name '{newName}' already exists.", nameof(newName));
        }

        try
        {   
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC renameSurveyGroup @oldName = {0}, @newName = {1}",
                oldName,
                newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while renaming survey group from '{OldName}' to '{NewName}'", oldName, newName);
            throw;
        }
    }
}
