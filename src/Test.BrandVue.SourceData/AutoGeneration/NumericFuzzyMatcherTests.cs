using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AutoGeneration;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.AutoGeneration
{
    public class NumericFuzzyMatcherTests
    {
        [TestCase("First, a few simple questions about you: How old are you?", "Nothing", true, Ignore ="There are only 7 surveys with this question text")]
        [TestCase("Hi", "Age", true)]
        [TestCase("Hi", "AAge", false)]
        [TestCase("Hi", "AGE", true)]
        [TestCase("Hi", "PAge", false)]
        [TestCase("Hi", "gAe", false)]
        //Top few questions with varCode=Age
        [TestCase("About you: How old are you?", "Nothing", true)]
        [TestCase("How old are you?", "Nothing", true)]
        [TestCase("About you: And how old are you?", "Nothing", true)]
        [TestCase("About you: What is your age?", "Nothing", true)]
        [TestCase("A little about you: How old are you?", "Nothing", true, Ignore = "There are no examples of this Question without varCode = Age")]
        [TestCase("What is your age?", "Nothing", true)]
        [TestCase("Firstly, how old are you?", "Nothing", true)]
        public void WillMatchAge(string questionText, string varCode, bool matches)
        {
            var question = new Question()
            {
                QuestionText = questionText,
                SurveyId = 1,
                VarCode = varCode,
                MinimumValue = 0,
                MaximumValue = 0,
            };
            Assert.That(new NumericFuzzyMatchers().AgeFuzzyMatcher(question), Is.EqualTo(matches), $"Question {question.QuestionText} failed to {nameof(NumericFuzzyMatchers.AgeFuzzyMatcher)}");
        }


        [TestCase("How many children do you have living with you at home?")]
        [TestCase("How many people under the age of 18 live in your household?")]
        [TestCase("About you: How many children are there in your household")]
        [TestCase("How many children do you have that are school aged (Kindergarten-12th grade)?")]
        [TestCase("How many children (16 and under) do you have in your household?")]
        [TestCase("How many children under the age of 18 do you have living with you at home?  If you don’t have any children living with you, please enter “0”.", Ignore ="Too much extra guff")]
        [TestCase("How many children do you have?")]
        [TestCase("How many children live in your household, at least a few days a week?")]


        //Taken from live databasse where varCode = no_of_kids        The top 2 were also found by other methods

        [TestCase("About you: How many children are there in your household?")]
        [TestCase("How many children are there in your household?")]
        [TestCase("About you: How many children are there in your household")]
        [TestCase("About you: How many children are there in your household? Please move your mouse along the scale until it shows the correct number in the box on the left.If you don't get the right number first time just click again on the scale.", Ignore ="Only a single example in the database of this")]
        public void QuestionTextMatchNumberOfChildren(string questionText)
        {
            var varCode = "NothingSpecial";
            var question = new Question()
            {
                QuestionText = questionText,
                SurveyId = 1,
                VarCode = varCode,
                MinimumValue = 0,
                MaximumValue = 0,
            };

            var matches = new NumericFuzzyMatchers().NoOfChildrenFuzzyMatcher(question);

            Assert.That(matches, Is.True, $"Question {question.QuestionText} failed to {nameof(NumericFuzzyMatchers.NoOfChildrenFuzzyMatcher)}");
        }

        [TestCase("Some Question")]
        [TestCase("Introduction")]
        [TestCase("How many people over the age of 18 live in your household?", Ignore="This currently succeeds")]
        [TestCase("How many people aged 18 and over live in your household?")]
        [TestCase("Please advise how many children you have that fall into the following age bands:")]
        [TestCase("About you: How many people live in your household?")]
        [TestCase("Final questions: How many people live in your household (including you)?")]
        [TestCase("Number of IN-STORE PURCHASES in the last year: How many times have you PURCHASED womenswear from these retailers, IN-STORE in the last year?")]
        [TestCase("When you visited #PAGECHOICEALT# in-store for Womenswear and bought something: How many items did you buy?")]
        [TestCase("Meals out of the home: Approximately how many times every MONTH do you eat in restaurants at each of these times of day?")]
        [TestCase("Eating / drinking habits: How many times have you visited / ordered from the following brands in the last 12 months?")]
        [TestCase("Your recent experience with #PAGECHOICE# (2 of 8): How many people (including you) were in the party?")]
        [TestCase("How many cups of coffee / tea do you drink in a normal day? ")]
        [TestCase("How many times have you #PAGECHOICE# in the last seven days?")]
        [TestCase("In the last month, how many times would you roughly say that you had a takeaway or food to be delivered/collected?")]
        [TestCase("Brands usage frequency: How many times have you used the following brands in the last 12 months?")]
        [TestCase("Food & drink habits in the last month: How many times have you done following in the last month?")]
        [TestCase("Eating habits: How many times have you VISITED / ORDERED from the following brands in the last 12 months?")]
        [TestCase("How many adults including yourself normally live in your household? ")]
        [TestCase("How many people, roughly, were you ordering for?")]
        [TestCase("How many months is it since your diagnosis?")]
        [TestCase("How many years is it since your diagnosis?")]
        [TestCase("Please write in how many (if any) cups of (hot) coffee you drank since your last visit to the survey.")]
        [TestCase("Please write in how many (if any) glasses of milk you drank since your last visit to the survey.")]
        [TestCase("Please write in how many (if any) alcoholic drinks you drank since your last visit to the survey.")]
        [TestCase("Please write in how many (if any) glasses of tap water you drank since your last visit to the survey.")]
        [TestCase("About you: How many people are there in your household?")]
        [TestCase("Please write in how many (if any) cups of (hot) tea you drank since your last visit to the survey.")]
        [TestCase("Financial services: How many cars does your household own?")]
        [TestCase("How many individual pairs of jeans have you bought in the past 7 days?")]
        [TestCase("Number of IN-STORE PURCHASES in the last year: How many times have you PURCHASED menswear from these retailers, IN-STORE in the last year?")]
        [TestCase("When you visited #PAGECHOICEALT# in-store for Menswear and bought something: How many items did you buy?")]
        [TestCase("How many individual t-shirts have you bought in the past 7 days?")]
        [TestCase("Insurance products: How many cars does your household own?")]
        [TestCase("Individual donations: How many individual donations have you made in the last 12 months?")]
        [TestCase("Of the t-shirts you bought in the past week, how many have you returned / do you think you will return?")]
        [TestCase("Of the jeans you bought in the past week, how many have you returned / do you think you will return?")]
        [TestCase("How many times a year do you think you eat chocolate?")]
        [TestCase("How many times a week do you think you eat chocolate?")]
        [TestCase("Clothing and luxury brands: How many items have you bought from the brand(s) shown below (online or in-store) in the last 2 years?")]
        [TestCase("About you: Thinking of the last month, how many minutes a DAY did you typically spend doing the following activities?")]
        [TestCase("Number of online purchases in the last year: How many times have you bought something from the following retailers in the last 12 months online i.e. through their website / an app?")]
        [TestCase("Number of in-store purchases in the last year: How many times have you BOUGHT something from the following retailers in the last 12 months in-store i.e. in-person in a physical store?")]
        [TestCase("Grocery shopping in the last week: How many times have you done the following types of grocery shopping in the last week?")]
        [TestCase("How long have been a customer: How many years have you been a customer of the following providers for #PAGECHOICESUPP#?")]
        [TestCase("Including yourself, how many people in total live in your household?")]
        [TestCase("How many times have you done each of the following activities in the past seven days? ")]
        [TestCase("About you: In your life, how many gyms have you been a member of?")]
        [TestCase("In the last month, how many times would you roughly say that you have ordered Takeaway / Food delivery?")]
        [TestCase("Food & drink habits in the last month: How many times have you done the following in the last month?")]
        [TestCase("Your Fitness First experience: How many classes have you attended in the last MONTH?")]
        [TestCase("Gym or fitness club providers: How many MONTHS have you been a member of your current gym or fitness club?")]
        [TestCase("Your recent experience with #PAGECHOICE#: How many people (including you) were in the party?")]
        [TestCase("Restaurant brands: How many times have you visited each of the following restaurants in the last 12 months?")]
        [TestCase("About you: How many cars does your household own?")]
        [TestCase("Your Fitness First experience: How many sessions with your Personal Trainer do you take per month?")]
        [TestCase("About you: And how many other investment properties do you own?")]
        [TestCase("How many days after developing symptoms did you first leave your home?")]
        [TestCase("Still thinking about the last time you met friends and/or family that you don’t live with, how many people from your household were there (including yourself)?")]
        [TestCase("The last time you met with friends and/ or family that you don’t live with, how many households (not people) did those people come from?")]
        [TestCase("And still thinking about the last time you met friends and/or family that you don’t live with, how many people from outside your household were there?")]
        [TestCase("And how many times after developing symptoms recently, if at all, have you completed each of these activities?    ")]
        [TestCase("A little more about you / your situation (2/2): How many restaurants, pub restaurants, or places where you can order prepared food are located within a 15 minute drive of where you live?")]
        [TestCase("Your organization's printing habits: Typically, how many quotes from other suppliers do you get before deciding where to purchase your printed products?")]
        [TestCase("If yes, how many times in the past 2 months have you visited a fashion store to just browse? ")]
        [TestCase("How many of the following types of financial products do you currently have?")]
        [TestCase("How many of your current accounts do you primarily use online?")]
        [TestCase("About you - extracurricular activities: How many times a MONTH do/does your child(ren) do each of the following activities?")]
        [TestCase("Drinks consumed in the last 7 days: In the last 7 days, roughly how many drinks in total, if any, have you had in the following places?")]
        [TestCase("How many of your savings accounts are online only?")]
        [TestCase("How many of your savings accounts do you primarily use online?")]
        [TestCase("A little more about you / your situation (2/2): How many restaurants, pub restaurants, or places where you can order prepared food are located within a 10 minute drive of where you live?")]
        [TestCase("Brands that offer Takeaway / Food delivery: How many times have you used these brands in the last THREE months?")]
        [TestCase("How many people live in your household, not including yourself?")]
        [TestCase("Locations: How many times have you visited the location(s) shown below, in the last 12 months?")]
        [TestCase("Clothing and luxury brands: How many times have you bought from the brand(s) shown below (online or in-store) in the last 2 years?")]
        [TestCase("Clothing and luxury brands: How many ITEMS  have you bought from the brand(s) shown below (online or in-store) in the last 2 years?")]
        [TestCase("Your food preferences: Again thinking about a typical week, please select how many times you’ll eat a different type of food or dish for each meal.")]
        [TestCase("How many times have you ordered food delivery / takeaway in the last month?")]
        [TestCase("About you: How many credit cards do you have?")]
        [TestCase("Health & Wellbeing (Fitness): Typically how many hours a WEEK do you spend doing the following activities?")]
        [TestCase("How many people are living in your household including yourself?")]
        [TestCase("And how many of these holidays have you already booked (e.g. booked flights, and/or accommodation)?")]
        [TestCase("How many years old is each of your children?")]
        [TestCase("On average, how many times in a day do you worry about your personal finances?")]
        [TestCase("Personal training sessions at #PAGECHOICE#: How many sessions with your Personal Trainer do you take per month?")]
        [TestCase("Of the jeans you bought in the past week, how many have you returned/do you think you will return?")]
        [TestCase("About you: How many people live in your household? Please write in the box - USE NUMBERS ONLY")]
        [TestCase("When you visited #PAGECHOICEALT# in-store for Womenswear and bought something: How many items did you buy? Please write in the box - USE NUMBERS ONLY")]
        [TestCase("And approximately how many books would you say you have read or listen to in the past day?")]
        [TestCase("Number of IN-STORE PURCHASES in the last year: How many times have you PURCHASED womenswear from these retailers, IN-STORE in the last year?  Please NOTE: This is the number of occasions you have made purchases, NOT the number items you have bought   Please select one response per retailer. If you don't know, please select '?'")]
        [TestCase("Please write in how many (if any) glasses of milk you drank since your last visit.")]
        [TestCase("Please write in how many (if any) cups of (hot) tea you drank since your last visit.")]
        [TestCase("Please write in how many (if any) alcoholic drinks you drank since your last visit.")]
        [TestCase("About you and your organization: In total, roughly how many employees are there in your organization globally?")]
        [TestCase("Number of photo products ordered: How many times have you ordered printed photo products from each of these brands in the last 12 months? ")]
        [TestCase("Your wealth management relationships: How many wealth management firms do you work with?")]
        [TestCase("How many days per month would you be willing to give up to help your local council make decisions on local services and democracy?")]
        [TestCase("Of the t-shirts you bought in the past week, how many have you returned/do you think you will return?")]
        [TestCase("Please write in how many (if any) glasses of tap water you drank since your last visit.")]
        [TestCase("Please write in how many (if any) cups of (hot) coffee you drank since your last visit.")]
        [TestCase("How many members, including yourself, are permanently living in your household? And how many of these members are adults (aged 18+)? And how many, if any, are children aged 0-13, 14-17?")]
        [TestCase("Including yourself, how many people live in your household?")]
        [TestCase("About you: How many business and leisure trips abroad have you made in the LAST 12 months?")]
        [TestCase("Your photos: How many photos do you take in a typical month?Please click on the right amount on the scale")]
        [TestCase("Restaurants you visited recently - #PAGECHOICE# (1 of 5): How many people (including you) were in the party?")]
        [TestCase("How many ants are there in the average ants nest?")]
        [TestCase("How many glasses of  have you personally consumed on this occasion?")]
        [TestCase("Last visit to #PAGECHOICE# (3 of 8): How many people (including you) were in the party?")]
        [TestCase("And still thinking about when you met up with friends and/or family you don’t live with over the period Friday 24th to Sunday 26th December, how many people from outside your household did you meet up with in total?  ")]
        [TestCase("How many mortgage advisers do you have working in your business?")]
        [TestCase("Your photos: How many photos do you take in a peak month (e.g. If you are on holiday, or have special occasions)?Please click on the right amount on the scale")]
        [TestCase("Drinks consumed in the last 7 days: In the last 7 days, about how many drinks, if any, in total have you had in the following places? ")]
        public void QuestionTextNotMatchNumberOfChildrenOrAge(string questionText)
        {
            var varCode = "NothingSpecial";
            var question = new Question()
            {
                QuestionText = questionText,
                SurveyId = 1,
                VarCode = varCode,
                MinimumValue = 0,
                MaximumValue = 0,
            };

            var matches = new NumericFuzzyMatchers().NoOfChildrenFuzzyMatcher(question);

            Assert.That(matches, Is.False, $"Question {question.QuestionText} failed to {nameof(NumericFuzzyMatchers.NoOfChildrenFuzzyMatcher)}");
            matches = new NumericFuzzyMatchers().AgeFuzzyMatcher(question);
            Assert.That(matches, Is.False, $"Question {question.QuestionText} failed to {nameof(NumericFuzzyMatchers.AgeFuzzyMatcher)}");
        }

    }
}
