using System;
using System.Collections.Generic;
using System.Linq;
using Argon;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.SourceData.LlmInsights;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Llm
{
    [TestFixture]
    public class LlmInsightsDocumentTests
    {
        [Test]
        public void TestUpdateFeedbackUserComment_AddNewUserComment()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId = "user1";
            string testComment = "This is a test comment";
            

            // Act
            doc.UpdateFeedbackUserComment(userId, testComment);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(1));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId));
            Assert.That(doc.UserFeedback[0].UserComment.Equals(testComment));
        }

        [Test]
        public void TestUpdateFeedbackUserComment_UpdateUserComment()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId = "user1";
            string testComment1 = "This is test comment 1";
            string testComment2 = "This is test comment 2";

            // Act
            doc.UpdateFeedbackUserComment(userId, testComment1);
            doc.UpdateFeedbackUserComment(userId, testComment2);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(1));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId));
            Assert.That(doc.UserFeedback[0].UserComment.Equals(testComment2));
        }

        [Test]
        public void TestUpdateFeedbackUserComment_ThrowsErrorCorrectly()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userEmail = "user1@someorg.com";
            string reallyLongComment = String.Join("", Enumerable.Range(0, 1024).ToArray());

            // Act
            BadRequestException ex = Assert.Throws<BadRequestException>(() => doc.UpdateFeedbackUserComment(userEmail, reallyLongComment));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("User comment is too long. Max length is 1024."));
            
        }

        [Test]
        public void TestUpdateFeedbackUserComment_AddNewUserCommentValidateSegmentCorrectnessCount()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userEmail = "user1@someorg.com";
            string testComment = "This is a test comment";

            // Act
            doc.UpdateFeedbackUserComment(userEmail, testComment);

            // Assert
            Assert.That(doc.UserFeedback[0].SegmentCorrectness.Count.Equals(doc.AiSummary!.Length));
        }

        [Test]
        public void TestUpdateFeedbackUserUsefulness_Add()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId = "user1";

            // Act
            doc.UpdateFeedbackUserUsefulness(userId, true);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(1));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId));
            Assert.That(doc.UserFeedback[0].IsUseful.Equals(true));

        }

        [Test]
        public void TestUpdateFeedbackUserUsefulness_AddMultiple()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId1 = "user1";
            string userId2 = "user2";
            string userId3 = "user3";


            // Act
            doc.UpdateFeedbackUserUsefulness(userId1, true);
            doc.UpdateFeedbackUserUsefulness(userId2, false);
            doc.UpdateFeedbackUserUsefulness(userId3, null);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(3));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId1));
            Assert.That(doc.UserFeedback[0].IsUseful.Equals(true));
            Assert.That(doc.UserFeedback[1].UserId.Equals(userId2));
            Assert.That(doc.UserFeedback[1].IsUseful.Equals(false));
            Assert.That(doc.UserFeedback[2].UserId.Equals(userId3));
            Assert.That(doc.UserFeedback[2].IsUseful.Equals(null));

        }

        [Test]
        public void TestUpdateFeedbackUserUsefulness_Update()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId1 = "user1";

            // Act
            doc.UpdateFeedbackUserUsefulness(userId1, true);
            doc.UpdateFeedbackUserUsefulness(userId1, false);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(1));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId1));
            Assert.That(doc.UserFeedback[0].IsUseful.Equals(false));
        }

        [Test]
        public void TestUpdateFeedbackUserSegmentCorrectness_AddMultiple()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId1 = "user1";
            string userId2 = "user2";
            string userId3 = "user3";
            int segmentId = doc.AiSummary!.First().SegmentId;


            // Act
            doc.UpdateUserFeedbackSegmentCorrectness(userId1, segmentId, true);
            doc.UpdateUserFeedbackSegmentCorrectness(userId2, segmentId, false);
            doc.UpdateUserFeedbackSegmentCorrectness(userId3, segmentId, null);

            // Assert
            Assert.That(doc.UserFeedback.Count.Equals(3));
            Assert.That(doc.UserFeedback[0].UserId.Equals(userId1));
            Assert.That(doc.UserFeedback[0].SegmentCorrectness[segmentId].Equals(true));
            Assert.That(doc.UserFeedback[1].UserId.Equals(userId2));
            Assert.That(doc.UserFeedback[1].SegmentCorrectness[segmentId].Equals(false));
            Assert.That(doc.UserFeedback[2].UserId.Equals(userId3));
            Assert.That(doc.UserFeedback[2].SegmentCorrectness[segmentId].Equals(null));

        }

        [Test]
        public void TestAddFeedbackUserSegmentCorrectness_ThrowsWhenSegmentIdIsUnknown()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId = "user1";
            int segmentId = -1;


            // Act
            BadRequestException ex = Assert.Throws<BadRequestException>(() => doc.UpdateUserFeedbackSegmentCorrectness(userId, segmentId, true));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo($"SegmentId:{segmentId} is not valid for this AI Summary"));

        }

        [Test]
        public void TestGetUserFeedback()
        {
            // Arrange
            LlmInsightsDocument doc = LlmInsightsDocumentInitialiser();
            string userId = "user1";
 
            // Act
            doc.UpdateFeedbackUserUsefulness(userId, true);
            var userFeedback = doc.GetUserFeedback(userId);

            // Assert
            Assert.That(userFeedback is not null);
            Assert.That(userFeedback!.UserId.Equals(userId));
        }

        private LlmInsightsDocument LlmInsightsDocumentInitialiser()
        {
            return new LlmInsightsDocument(
                new object(),
                new object[0],
                "FAABAF",
                [
                    new LlmInsightsSegment(1,"title","this is interesting because",10, null),
                    new LlmInsightsSegment(2,"title","this is interesting because",10, null),
                    new LlmInsightsSegment(3,"title","this is interesting because",10, null)
                ]);
        }
    }
}
