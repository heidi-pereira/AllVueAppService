create or alter table staging.answers_staging (
    responseid number(38, 0),
    questionid number(38, 0),
    sectionchoiceid number(38, 0),
    pagechoiceid number(38, 0),
    questionchoiceid float,
    answerchoiceid float,
    answervalue float,
    answertext varchar(16777216)
);
