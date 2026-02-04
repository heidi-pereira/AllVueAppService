
# Merge within survey

Dedupe choice sets - e.g. Choice set Q7Answers is BrandsA + BrandsB. Q8Answers is also BrandsA + BrandsB. We pick just one and use it for both.

Why:
* It reduces the number of entities which can simplify exploration of the data.
* Configuration built on top works for both questions - though this would be possible by just maintaining a link between the two rather than totally deduping.

Where:
* Questions:
    * raw_survey.questions: Filter out confidential, calculate question type and layout (TEMP: filter out text for now) Reads from raw_survey.all_questions_including_confidential
* Choice Sets:
    * choice_set_root_ancestors: Task/stream reads from raw_survey.choice_sets and walks tree of parents
    * canonical_choice_set_mappings
    * canonical_choice_set_locations
