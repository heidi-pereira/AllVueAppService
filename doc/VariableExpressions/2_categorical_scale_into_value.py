# Example from https://morar.freshdesk.com/a/tickets/16075
# https://savanta.test.all-vue.com/snapshot/survey/heineken-on-track/ui/crosstabbing?metric=onlineorderingscorecopy

from allvue_simulator import Response, Result, QuestionVariable, evaluate

result = Result() # Result has no entities

for answer in [8,9,97]: # Simulate 3 respondents with answers 8, 9 and 97 respectively
    print(f"\nFor respondent answering {answer}:")
    
    # In the survey Q4NEW is a categorical scale with 10 possible values as well as 97 for "I don't know"
    response = Response()
    response.Q4NEW = QuestionVariable(["Q4NEWAnswers", "Value"], [[answer, answer]])

    # Requirement: The variable's result should have no entities
    #    Therefore, we won't mention "result.someentity"
    # Requirement: The numeric value of the answer, or None if the answer is 97
    evaluate(f"{  max(response.Q4NEW(Q4NEWAnswers=[1,2,3,4,5,6,7,8,9,10]), default=None)  =}")