using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;

namespace Test.BrandVue.FrontEnd.Services;

[TestFixture]
public class SurveyGroupServiceTests
{
    private IDbContextFactory<MetaDataContext> _dbContextFactory;
    private ILogger<SurveyGroupService> _logger;
    private MetaDataContext _dbContext;
    private SurveyGroupService _surveyGroupService;

    [SetUp]
    public void Setup()
    {
        _dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
        _logger = Substitute.For<ILogger<SurveyGroupService>>();
        _dbContext = Substitute.For<MetaDataContext>();
        
        _dbContextFactory.CreateDbContextAsync().Returns(_dbContext);
        
        _surveyGroupService = new SurveyGroupService(_dbContextFactory, _logger);
    }

    [Test]
    public void RenameSurveyGroupAsync_OldNameIsNull_ThrowsArgumentException()
    {
        // Arrange
        string oldName = null;
        var newName = "NewGroupName";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("oldName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_OldNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "";
        var newName = "NewGroupName";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("oldName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_OldNameIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "   ";
        var newName = "NewGroupName";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("oldName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_NewNameIsNull_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "OldGroupName";
        string newName = null;

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("newName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_NewNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "OldGroupName";
        var newName = "";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("newName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_NewNameIsWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "OldGroupName";
        var newName = "   ";

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("newName"));
    }

    [Test]
    public void RenameSurveyGroupAsync_OldNameEqualsNewName_ThrowsArgumentException()
    {
        // Arrange
        var oldName = "GroupName";
        var newName = "groupname"; // Case-insensitive match

        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentException>(() => _surveyGroupService.RenameSurveyGroupAsync(oldName, newName));
        Assert.That(exception.ParamName, Is.EqualTo("newName"));
        Assert.That(exception.Message, Does.Contain("Old name and new name cannot be the same"));
    }
}
