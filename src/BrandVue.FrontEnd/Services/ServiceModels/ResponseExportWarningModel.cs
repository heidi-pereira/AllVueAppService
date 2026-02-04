using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.Services.ServiceModels
{
    public class ResponseExportWarningModel
    {
        public readonly Question Question;
        public readonly string WarningMessage;

        public ResponseExportWarningModel(Question question, string message)
        {
            Question = question;
            WarningMessage = message;
        }
    }
}
