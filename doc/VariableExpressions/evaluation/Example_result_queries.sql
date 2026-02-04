SELECT
    responseid,
    ARRAY_AGG(ARRAY_CONSTRUCT(questionchoiceid, answerchoiceid, answervalue)) AS arr_of_arrs
FROM vue.answers
WHERE answervalue != 0
  AND questionid = 871901
GROUP BY responseid;

-- Note that to get a nested dictionary, we need another layer of grouping on the query
-- we can easily do that client side for now and add the complexity to snowflake if needed later.
WITH question_agg AS (
    SELECT
        responseid,
        questionid,
        questionchoiceid,
        ARRAY_AGG(answervalue::int) AS answervalues
    FROM vue.answers
    WHERE answervalue != 0
      AND questionid = 871901
    GROUP BY responseid, questionid, questionchoiceid
)
SELECT
    responseid,
    OBJECT_AGG(questionchoiceid::int, answervalues) AS nested_dict
FROM question_agg
GROUP BY responseid;

-- So for now we'll use this. Note we'll actually do this with variableanswers later
-- This will let's us pull the full set of answers for a variable into the python world. We can then turn into a dictionary in the form needed for efficiency in multiple iterations over it
CREATE OR REPLACE DYNAMIC TABLE brandvue.questionanswers
    target_lag = 'DOWNSTREAM' refresh_mode = INCREMENTAL initialize = ON_CREATE warehouse = WAREHOUSE_XSMALL
AS
SELECT
    a.responseid,
    a.questionid,
    ARRAY_AGG(
        ARRAY_CONSTRUCT_COMPACT(
            a.sectionchoiceid,
            a.pagechoiceid,
            a.questionchoiceid,
            a.answerchoiceid,
            a.answervalue
        )
    ) AS arr_of_values
FROM vue.answers a
INNER JOIN vue.questions q ON a.questionid = q.questionid
 -- Took 5m14s with this where clause grabbing 243,208,997 answers. For 6B answers scaling linearly it'd have taken ~2h30m
WHERE surveyid = 11817
GROUP BY a.responseid, a.questionid;

SELECT COUNT(1) from vue.answers a
INNER JOIN vue.questions q ON a.questionid = q.questionid
WHERE surveyid = 11817;


SELECT COUNT(1) from vue.answers a
INNER JOIN vue.questions q ON a.questionid = q.questionid;
