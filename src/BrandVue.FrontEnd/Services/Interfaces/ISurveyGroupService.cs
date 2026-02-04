namespace BrandVue.Services.Interfaces;

public interface ISurveyGroupService
{
    Task RenameSurveyGroupAsync(string oldName, string newName);
}