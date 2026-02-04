from allvue_simulator import ExpressionVariable, Response, Result, QuestionVariable, evaluate

# Set up a single respondent's answers

response = Response()
response.frequently_platform_week_QP604 = QuestionVariable(
    ["Q603Answers", "Q604Answers", "Value"],
    [ # This respondent has 3 answers to the question
     # This is a radio button question, so the value is equal to the answer they clicked from Q604Answers (frequency of use)
        [8,  2, 2], #Q603Answers=8, Q604Answers=2. e.g. "I use facebook three times a day"
        [9,  2, 2],
        [10, 3, 3]
    ]
)

# Set up "result" to represent a particular data point on a chart we're calculating for
for result in [Result(Q604Answers=x) for x in [2, 3]]:
    print (f"\n\nFor result with Q604Answers={result.Q604Answers}")
    
    # Get the value for their most used platform
    evaluate(f"{ min(response.frequently_platform_week_QP604(), default=None) =}")
    
    # But we only to know if this respondent should be included in the current result, i.e. is their minimum equal to the result's entity
    evaluate(f"{ min(response.frequently_platform_week_QP604(), default=None) == result.Q604Answers =}")
    
    # The above could be a base expression, but we actually want to output the value so it can be used for things like filter value mapping
    evaluate(f"{ result.Q604Answers if min(response.frequently_platform_week_QP604(), default=None) == result.Q604Answers else None =}")

# Advanced cases: Now we can add our new variable expression as a function which takes the result data point as context
response.social_media_use = ExpressionVariable(lambda result:
    result.Q604Answers if min(response.frequently_platform_week_QP604(), default=None) == result.Q604Answers else None
)

# Now we can use it in other expressions
evaluate(f"{ min(response.social_media_use(Result(Q604Answers=[2, 3])), default=None) =}")