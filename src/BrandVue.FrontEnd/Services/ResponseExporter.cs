using System.IO;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Services.ServiceModels;
using BrandVue.SourceData.Respondents;
using OfficeOpenXml;

namespace BrandVue.Services
{
    public class ResponseExporter
    {
        private IEnumerable<ResponseFieldModel> _fieldModels;
        private IEnumerable<ResponseExportWarningModel> _warnings;
        private ExcelWorksheet _fieldsSheet;
        private ExcelWorksheet _dataSheet;
        private int _rowId;
        private int _colId;
        private readonly ExcelPackage _excelPackage;
        private readonly SurveyResponse[] _allResponses;
        private List<(string, string)> _additionalWarnings = new List<(string, string)>();
        private int[] _allResponseIds => _allResponses.Select(r => r.ResponseId).ToArray();

        public ResponseExporter(IEnumerable<ResponseFieldModel> fieldModels, SurveyResponse[] allResponses,
            IEnumerable<ResponseExportWarningModel> warnings)
        {
            _fieldModels = fieldModels;
            _allResponses = allResponses;
            _warnings = warnings;
            _excelPackage = new ExcelPackage();
        }

        public void CreateAndPopulateSheets()
        {
            CreateFieldsSheet();
            CreateDataSheet();
            CreateWarningSheet();
        }

        private void CreateWarningSheet()
        {
            if(!_warnings.Any() && !_additionalWarnings.Any())
            {
                return;
            }
            _dataSheet = _excelPackage.Workbook.Worksheets.Add("Warnings");
            _rowId = 1;
            _colId = 1;

            PopulateCellData(_rowId, "Question Varcode", _colId++);
            PopulateCellData(_rowId, "Message", _colId++);

            foreach (var error in _warnings)
            {
                _rowId++;
                _colId = 1;
                PopulateCellData(_rowId, error.Question.VarCode, _colId++);
                PopulateCellData(_rowId, error.WarningMessage, _colId++);
            }

            foreach ((string, string) additionalWarning in _additionalWarnings)
            {
                _rowId++;
                _colId = 1;
                PopulateCellData(_rowId, additionalWarning.Item1, _colId++);
                PopulateCellData(_rowId, additionalWarning.Item2, _colId++);
            }
        }

        private void CreateDataSheet()
        {
            _dataSheet = _excelPackage.Workbook.Worksheets.Add("Data export");
            _rowId = 1;
            _colId = 1;

            PopulateResponseIdColumn();
            PopulateResponseData();
        }

        private void PopulateResponseData()
        {
            PopulateAnswers();
            PopulateSurveyResponseData();
        }

        private void PopulateSurveyResponseData()
        {
            var timeElapsedColumn = _colId + 1;
            var completeTimeColumn = _colId + 2;

            PopulateCellData(1, "Time elapsed (hh:mm:ss)", timeElapsedColumn);
            PopulateCellData(1, "Complete time", completeTimeColumn);

            foreach (var response in _allResponses)
            {
                var row = Array.IndexOf(_allResponses, response) + 2;
                var timeDifference = response.LastChangeTime - response.Timestamp;
                string formattedTimeDifference = string.Format("{0}:{1}:{2}",
                                                               timeDifference.Hours.ToString("00"),
                                                               timeDifference.Minutes.ToString("00"),
                                                               timeDifference.Seconds.ToString("00"));

                PopulateCellData(row, formattedTimeDifference, timeElapsedColumn);
                PopulateCellData(row, response.LastChangeTime, completeTimeColumn);
            }
        }

        private void PopulateAnswers()
        {
            foreach (var model in _fieldModels)
            {
                _colId++;
                PopulateCellData(1, model.VarCode);

                switch (model.MasterType)
                {
                    case "RADIO":
                        PopulateRadioFieldModel(model);
                        break;
                    case "CHECKBOX":
                        PopulateCheckBoxFieldModel(model);
                        break;
                    case "DROPZONE":
                    case "COMBO":
                        PopulateDropZoneOrComboFieldModel(model);
                        break;
                    case "SLIDER":
                        PopulateSliderFieldModel(model);
                        break;
                    case "TEXT":
                    case "TEXTENTRY":
                        PopulateTextFieldModel(model);
                        break;
                }
            }
        }

        private void PopulateCheckBoxFieldModel(ResponseFieldModel model)
        {
            var split = model.VarCode.Split('_');
            int? filterByChoiceId = null;
            int splitByChoiceId;

            if (split.Length > 2)
            {
                filterByChoiceId = int.Parse(split.Last());
                if (!int.TryParse(split[split.Length - 2], out splitByChoiceId))
                {
                    filterByChoiceId = null;
                    if (int.TryParse(split.Last(), out splitByChoiceId))
                    {
                        _additionalWarnings.Add(new(model.VarCode, $"Non-standard CHECKBOX varCode, Assuming '{model.VarCode}' split by {splitByChoiceId}"));
                    }
                    else
                    {
                        _additionalWarnings.Add(new(model.VarCode, $"Error: Non-standard CHECKBOX varCode, Incorrectly assumed '{model.VarCode}' split by {splitByChoiceId}"));
                        return;
                    }
                }
            }
            else
            {
                splitByChoiceId = int.Parse(split.Last());
            }


            foreach (var group in model.GroupedData)
            {
                int insertionRow = GetInsertionRow(group);
                if (insertionRow == 1) { continue; }

                int? answerValue = null;
                var answers = group.Where(a => a.QuestionId == model.QuestionId);
                Answer answer = null;

                //because we do not use the answerChoiceSet for checkbox questions, we assume we can disregard any answerChoiceSetIds
                switch (model.SplitByType)
                {
                    case ChoiceSetType.SectionChoiceSet:
                        {
                            if (model.FilterByType == null)
                            {
                                answerValue = answers.SingleOrDefault(a => a.SectionChoiceId == splitByChoiceId)?.AnswerValue;
                            }
                            else
                            {
                                answer = model.FilterByType == ChoiceSetType.QuestionChoiceSet
                                    ? answers.SingleOrDefault(a => a.SectionChoiceId == splitByChoiceId && a.QuestionChoiceId == filterByChoiceId)
                                    : answers.SingleOrDefault(a => a.SectionChoiceId == splitByChoiceId && a.PageChoiceId == filterByChoiceId);
                            }
                            break;
                        }

                    case ChoiceSetType.QuestionChoiceSet:
                        {
                            if (model.FilterByType == null)
                            {
                                answerValue = answers.SingleOrDefault(a => a.QuestionChoiceId == splitByChoiceId)?.AnswerValue;
                            }
                            else
                            {
                                answer = model.FilterByType == ChoiceSetType.SectionChoiceSet
                                    ? answers.SingleOrDefault(a => a.QuestionChoiceId == splitByChoiceId && a.SectionChoiceId == filterByChoiceId)
                                    : answers.SingleOrDefault(a => a.QuestionChoiceId == splitByChoiceId && a.PageChoiceId == filterByChoiceId);
                            }
                            break;
                        }

                    case ChoiceSetType.PageChoiceSet:
                        {
                            if (model.FilterByType == null)
                            {
                                answerValue = answers.SingleOrDefault(a => a.PageChoiceId == splitByChoiceId)?.AnswerValue;
                            }
                            else
                            {
                                answer = model.FilterByType == ChoiceSetType.SectionChoiceSet
                                    ? answers.SingleOrDefault(a => a.PageChoiceId == splitByChoiceId && a.SectionChoiceId == filterByChoiceId)
                                    : answers.SingleOrDefault(a => a.PageChoiceId == splitByChoiceId && a.QuestionChoiceId == filterByChoiceId);
                            }
                            break;
                        }
                }

                if(answer != null)
                {
                    answerValue = answer.AnswerChoiceId.HasValue ? answer.AnswerChoiceId : answer.AnswerValue;
                }

                if (answerValue != null)
                {
                    PopulateCellData(insertionRow, answerValue);
                }
            }
        }

        private void PopulateRadioFieldModel(ResponseFieldModel model)
        {
            foreach (var group in model.GroupedData)
            {
                int insertionRow = GetInsertionRow(group);
                if (insertionRow == 1) { continue; }

                if(model.SplitByType == null)
                {
                    var answer = group.Single().AnswerChoiceId;
                    PopulateCellData(insertionRow, answer);
                }
                else
                {
                    var answers = group.Where(a => a.QuestionId == model.QuestionId);
                    int? answerValue = null;
                    var entityInstance = model.FilterByChoiceId;

                    switch (model.FilterByType)
                    {
                        case ChoiceSetType.SectionChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.SectionChoiceId == entityInstance)?.AnswerChoiceId;
                                break;
                            }

                        case ChoiceSetType.QuestionChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.QuestionChoiceId == entityInstance)?.AnswerChoiceId;
                                break;
                            }

                        case ChoiceSetType.AnswerChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.AnswerChoiceId == entityInstance)?.AnswerChoiceId;
                                break;
                            }


                        case ChoiceSetType.PageChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.PageChoiceId == entityInstance)?.AnswerChoiceId;
                                break;
                            }
                    }

                    if (answerValue != null)
                    {
                        PopulateCellData(insertionRow, answerValue);
                    }
                }
            }
        }

        private int GetInsertionRow(IGrouping<int, Answer> group)
        {
            var responseId = group.Key;
            return Array.IndexOf(_allResponseIds, responseId) + 2;
        }

        private void PopulateTextFieldModel(ResponseFieldModel model)
        {
            foreach (var group in model.GroupedData)
            {
                int insertionRow = GetInsertionRow(group);
                if (insertionRow == 1) { continue; }

                if (group.Count() == 1)
                {
                    PopulateCellDataText(insertionRow, group.Single());
                }
                else
                {
                    var entityInstance = int.Parse(model.VarCode.Split('_').Last());
                    Answer answer = null;
                    if (model.SplitByType == ChoiceSetType.QuestionChoiceSet)
                    {
                        answer = group.Single(g => g.QuestionChoiceId == entityInstance);
                    }
                    else if (model.SplitByType == ChoiceSetType.AnswerChoiceSet)
                    {
                        answer = group.Single(g => g.AnswerChoiceId == entityInstance);
                    }
                    else if (model.SplitByType == ChoiceSetType.SectionChoiceSet)
                    {
                        answer = group.Single(g => g.SectionChoiceId == entityInstance);
                    }
                    else if (model.SplitByType == ChoiceSetType.PageChoiceSet)
                    {
                        answer = group.Single(g => g.PageChoiceId == entityInstance);
                    }
                    PopulateCellDataText(insertionRow, answer);
                }
            }
        }

        private void PopulateCellDataText(int row, Answer answer)
        {
            if (answer.AnswerValue.HasValue && answer.AnswerValue.Value != SpecialResponseFieldValues.TextFieldIsNotANumber)
            {
                PopulateCellData(row, answer.AnswerValue.Value);
            }
            else
            {
                PopulateCellData(row, answer?.AnswerText??string.Empty);
            }

        }
        private void PopulateDropZoneOrComboFieldModel(ResponseFieldModel model)
        {
            var entityInstance = model.FilterByChoiceId;

            foreach (var group in model.GroupedData)
            {
                int insertionRow = GetInsertionRow(group);
                if (insertionRow == 1) { continue; }
                var answers = group.Where(a => a.QuestionId == model.QuestionId);
                int? answerValue = null;

                switch (model.FilterByType)
                {
                    //we assume that splitby is the answerChoiceSet
                    case ChoiceSetType.SectionChoiceSet:
                        {
                            answerValue = answers.SingleOrDefault(answer => answer.SectionChoiceId == entityInstance)?.AnswerChoiceId;
                            break;
                        }

                    case ChoiceSetType.QuestionChoiceSet:
                        {
                            answerValue = answers.SingleOrDefault(answer => answer.QuestionChoiceId == entityInstance)?.AnswerChoiceId;
                            break;
                        }

                    case ChoiceSetType.PageChoiceSet:
                        {
                            answerValue = answers.SingleOrDefault(answer => answer.PageChoiceId == entityInstance)?.AnswerChoiceId;
                            break;
                        }
                }

                if (answerValue != null)
                {
                    PopulateCellData(insertionRow, answerValue);
                }
            }
        }

        private void PopulateSliderFieldModel(ResponseFieldModel model)
        {
            foreach (var group in model.GroupedData)
            {
                int insertionRow = GetInsertionRow(group);
                if(insertionRow == 1) { continue; }

                var answers = group.Where(a => a.QuestionId == model.QuestionId);

                //if there is more than one answer we need to see which choice set it belongs to
                if (answers.Count() > 1)
                {
                    var entityInstance = model.FilterByChoiceId;
                    int? answerValue = null;
                    switch (model.FilterByType)
                    {
                        case ChoiceSetType.SectionChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.SectionChoiceId == entityInstance)?.AnswerValue;
                                break;
                            }

                        case ChoiceSetType.QuestionChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.QuestionChoiceId == entityInstance)?.AnswerValue;
                                break;
                            }

                        case ChoiceSetType.PageChoiceSet:
                            {
                                answerValue = answers.SingleOrDefault(answer => answer.PageChoiceId == entityInstance)?.AnswerValue;
                                break;
                            }
                    }

                    if (answerValue != null)
                    {
                        PopulateCellData(insertionRow, answerValue);
                    }
                }
                else
                {
                    PopulateCellData(insertionRow, answers.Single().AnswerValue);
                }
            }
        }

        private void PopulateResponseIdColumn()
        {
            PopulateCellData(_rowId++, "Response Id");
            foreach (var response in _allResponses)
            {
                PopulateCellData(_rowId++, response.ResponseId);
            }
        }

        private void PopulateCellData<T>(int insertRow, T value, int? insertCol = null)
        {
            var cell = _dataSheet.Cells[insertRow, insertCol ?? _colId];
            if (value != null && value.GetType() == typeof(string))
            {
                var valueAsString = value.ToString();
                if (int.TryParse(valueAsString, out var number))
                {
                    PopulateCellData(insertRow, number);
                }
                else if (double.TryParse(valueAsString, out var _double))
                {
                    PopulateCellData(insertRow, _double);
                }
                else
                {
                    cell.Value = value.ToString();
                }
            }
            else
            if (value != null && value.GetType() == typeof(int))
            {
                cell.Style.Numberformat.Format = "0";
                cell.Value = value;
            }
            else {
                cell.Value = $"{value}";
            }
        }

        private static Stream GetExcelStream(ExcelPackage excelPackage)
        {
            var ms = new MemoryStream();
            excelPackage.SaveAs(ms);
            ms.Flush();
            ms.Position = 0;
            return ms;
        }

        private void CreateFieldsSheet()
        {
            _fieldsSheet = _excelPackage.Workbook.Worksheets.Add("Fields");
            _rowId = 1;
            _colId = 1;

            _fieldsSheet.Cells[_rowId, _colId++].Value = "Variable name";
            _fieldsSheet.Cells[_rowId, _colId++].Value = "Label";
            _fieldsSheet.Cells[_rowId, _colId++].Value = "Value Labels";
            _fieldsSheet.Cells[_rowId, _colId++].Value = "Question type";
            _fieldsSheet.Cells[_rowId, _colId++].Value = "Full question";
            _rowId++;

            foreach (var field in _fieldModels)
            {
                _colId = 1;
                var valueLabels = field.MasterType == "CHECKBOX" ? "1:Yes|-99:No" : field.ValueLabels;

                _fieldsSheet.Cells[_rowId, _colId++].Value = $"{field.VarCode}";
                _fieldsSheet.Cells[_rowId, _colId++].Value = $"{field.Label}";
                _fieldsSheet.Cells[_rowId, _colId++].Value = $"{valueLabels}";
                _fieldsSheet.Cells[_rowId, _colId++].Value = $"{field.MasterType}";
                _fieldsSheet.Cells[_rowId, _colId++].Value = $"{field.QuestionText}";
                _rowId++;
            }
        }

        public void FinalizeExport()
        {
            _fieldsSheet.Cells.AutoFitColumns();
        }

        public Stream ToStream()
        {
            return GetExcelStream(_excelPackage);
        }
    }
}