bcp "[SurveyPortalMorar].[dbo].[panelRespondents]" in .\panelRespondents.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[surveyResponse]" in .\surveyResponse.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[data]" in .\data.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[workingData]" in .\workingData.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[surveys]" in .\surveys.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[surveySegments]" in .\surveySegments.txt -T -c -S ".\SQL2017" -E
bcp "[SurveyPortalMorar].[dbo].[surveyStructures]" in .\surveyStructures.txt -T -c -S ".\SQL2017" -E
