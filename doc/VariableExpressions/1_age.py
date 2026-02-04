from allvue_simulator import Response, Result, QuestionVariable, evaluate

# Set up a single respondent's answers

response = Response()
response.age = QuestionVariable(
    ["Value"], # No entity types here, just the value
    [
        [25], # One row of data with age
    ]
)

# Set up "result" to represent a particular data point on a chart we're calculating for
result = Result(Q604Answers=20) # AllVue loops over each result data point on a chart - we'll just pick one to test with here.

# You can put any expression in here, surround it with `evaluate(f"{    =}")` to see the result printed out
evaluate(f"{ response.age()  =}")     # [25] isn't a valid expression in vue because it's a list
evaluate(f"{ max(response.age())  =}")     # 25 is valid