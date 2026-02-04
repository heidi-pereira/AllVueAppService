using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenEnds.BackEnd.Library;
using System.IO.Compression;

namespace OpenEnds.BackEnd.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class OpenEndsController(ILogger<OpenEndsController> logger, 
    SavantaTextThemeAnalyzer savantaTextThemeAnalyzer, OpenEndsService openEndsService) : ControllerBase
{

    [HttpPost("surveys/{surveyId}/questions/{questionId}/initialise")]
    public async Task<IActionResult> InitialiseQuestionForAnalysis(string surveyId, int questionId, [FromBody] AdditionalContextPayload additionalContext)
    {
        if (additionalContext.Instructions != null && additionalContext.Instructions.Length > 200)
        {
            return BadRequest("Additional instructions must be 200 characters or fewer.");
        }

        await savantaTextThemeAnalyzer.InitialiseProject(surveyId, questionId, additionalContext.Instructions);
        return Ok();
    }
    
    [HttpGet("surveys/{surveyId}/questions/{questionId}/status")]
    public async Task<IActionResult> GetQuestionStatus(string surveyId, int questionId)
    {
        var statusResult = await openEndsService.RetrieveStatus(surveyId, questionId, false);
        return Ok(statusResult);
    }

    [HttpPost("surveys/{surveyId}/questions/{questionId}/recalculate")]
    public async Task<IActionResult> RecalculateQuestion(string surveyId, int questionId)
    {
        await savantaTextThemeAnalyzer.RecalculateQuestion(surveyId, questionId);
        return Ok();
    }

    [HttpDelete("surveys/{surveyId}/questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(string surveyId, int questionId)
    {
        await savantaTextThemeAnalyzer.DeleteQuestion(surveyId, questionId);
        return Ok();
    }


    [HttpGet("surveys/{surveyId}/questions")]
    public async Task<IActionResult> GetQuestions(string surveyId)
    {
        var questions = await openEndsService.GetQuestions(surveyId);
        return Ok(questions);
    }
    
    [HttpGet("surveys/{surveyId}/questions/{questionId}/summary")]
    public async Task<IActionResult> GetQuestionSummary(string surveyId, int questionId)
    {
        var questionSummary = await openEndsService.GetQuestionSummary(surveyId, questionId);
        return Ok(questionSummary);
    }

    [HttpGet("surveys/{surveyId}/questions/{questionId}")]
    public async Task<dynamic> GetQuestion(string surveyId, int questionId)
    {
        var question = await openEndsService.GetQuestion(surveyId, questionId);
        return Ok(question);
    }

    [HttpGet("surveys/{surveyId}/questions/{questionId}/summary/codedtextcount")]
    public async Task<dynamic> GetCodedTextCount(string surveyId, int questionId)
    {
        var questionCount = await openEndsService.GetCodedTextCount(surveyId, questionId);
        return Ok(questionCount);
    }

    [HttpGet("surveys/{surveyId}/questions/{questionId}/summary/export")]
    public async Task<IActionResult> Export(string surveyId, int questionId, [FromQuery] ExportService.ExportFormat format)
    {
        var export = await openEndsService.GetExport(surveyId, questionId, format);
        var fileName = $"OpenEndsThemeSummary_{surveyId}_{questionId}";

        var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var zipEntry = archive.CreateEntry($"{fileName}.{ExportService.GetExportFileExtension(format)}");
            using var entryStream = zipEntry.Open();
            await entryStream.WriteAsync(export, 0, export.Length);
        }
        zipStream.Position = 0;

        return File(zipStream, "application/zip", $"{fileName}.zip");
    }

    public class AdditionalContextPayload
    {
        public string Instructions { get; set; }
    }
}