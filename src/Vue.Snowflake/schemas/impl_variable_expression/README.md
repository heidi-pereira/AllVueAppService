# Variable Expression Evaluation

The intent is to parse and evaluate variable expressions defined in variable configurations.
Having parsed the dependencies from existing variables, we then add metric variable expressions (including its base).
This allows us to use the same machinery to evaluate metric variables as derived variables.
Once we write the adhoc filtering path, it may make more sense to use that, this is just an initial easy path.


## Status

Doesn't handle: 
* Base expressions: could inline expression in future with brackets
* TrueVals: easy enough to generate the python for range/list syntax