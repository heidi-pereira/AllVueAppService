# Merge across response set's surveys

Internally we often run different surveys per country and wave/month. For internal ops, and the occasional external client we should provide the original information as attached object column of flexible metadata, available to appropriate users only.

We merge questions that have the same var code and data layout (i.e. choice sets occupying the same spots).
We merge (union) choice sets that are in the same position for merged questions.

Where:
* Questions:
  * questions
  * canonical_questions: Pick a unique question to for each (var code, layout) pair
* Choice Sets:
  * questions + survey_canonical_choice_set_locations
  * _canonical_choice_set_initial_mappings: Where questions have been merged, link choice sets occupying same slots
  * canonical_choice_set_mappings: task and stream to resolve transitive links to create mapping from each canonical choice set to all alternatives
  * canonical_choice_sets: Get the name of each canonical choice set

#### Why

Lots of duplication forces users to explore each survey individually, and usually merge things together themselves for analysis.
Best that we have a single coherent version of this merging.

#### Caveats

Choices change in different waves. For single choice questions, we only store which choice was selected. Hence telling the difference between no-one selecting a choice, and it not being asked is impossible. However, this was already the case since dynamic logic in the survey can filter out choices, and is a bit of an edge case - usually at least one person picks every answer.

Questions varcodes are sometimes the same for fundamentally different questions, especially where people scripted as "Q1", "Q2".
If this becomes an issue, we'll just add a table configurable via AllVue that remaps specific question varcodes to have their surveyid appended before merging.
