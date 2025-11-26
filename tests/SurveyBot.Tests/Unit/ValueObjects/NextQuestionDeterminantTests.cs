using SurveyBot.Core.Enums;
using SurveyBot.Core.ValueObjects;
using System.Text.Json;
using Xunit;

namespace SurveyBot.Tests.Unit.ValueObjects;

/// <summary>
/// Unit tests for NextQuestionDeterminant value object.
/// Tests factory methods, invariant enforcement, equality semantics, and JSON serialization.
/// </summary>
public class NextQuestionDeterminantTests
{
    #region Factory Method Tests

    [Fact]
    public void ToQuestion_ValidQuestionId_CreatesGoToQuestionType()
    {
        // Arrange
        const int questionId = 42;

        // Act
        var result = NextQuestionDeterminant.ToQuestion(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NextStepType.GoToQuestion, result.Type);
        Assert.Equal(questionId, result.NextQuestionId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public void ToQuestion_VariousValidIds_CreatesCorrectly(int questionId)
    {
        // Act
        var result = NextQuestionDeterminant.ToQuestion(questionId);

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, result.Type);
        Assert.Equal(questionId, result.NextQuestionId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    [InlineData(int.MinValue)]
    public void ToQuestion_InvalidQuestionId_ThrowsArgumentException(int invalidId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            NextQuestionDeterminant.ToQuestion(invalidId));

        Assert.Contains("Question ID must be greater than 0", exception.Message);
        Assert.Equal("questionId", exception.ParamName);
    }

    [Fact]
    public void End_CreatesEndSurveyType()
    {
        // Act
        var result = NextQuestionDeterminant.End();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NextStepType.EndSurvey, result.Type);
        Assert.Null(result.NextQuestionId);
    }

    [Fact]
    public void End_CalledMultipleTimes_CreatesDistinctInstances()
    {
        // Act
        var result1 = NextQuestionDeterminant.End();
        var result2 = NextQuestionDeterminant.End();

        // Assert
        Assert.NotSame(result1, result2); // Different references
        Assert.Equal(result1, result2);    // But equal by value
    }

    #endregion

    #region Invariant Enforcement Tests

    [Fact]
    public void ToQuestion_CreatesInstanceWithCorrectInvariants()
    {
        // Arrange
        const int questionId = 5;

        // Act
        var result = NextQuestionDeterminant.ToQuestion(questionId);

        // Assert - Verify invariants are maintained
        Assert.Equal(NextStepType.GoToQuestion, result.Type);
        Assert.NotNull(result.NextQuestionId);
        Assert.True(result.NextQuestionId > 0);
    }

    [Fact]
    public void End_CreatesInstanceWithCorrectInvariants()
    {
        // Act
        var result = NextQuestionDeterminant.End();

        // Assert - Verify invariants are maintained
        Assert.Equal(NextStepType.EndSurvey, result.Type);
        Assert.Null(result.NextQuestionId);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameGoToQuestionInstances_ReturnsTrue()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(10);

        // Act & Assert
        Assert.Equal(det1, det2);
        Assert.True(det1.Equals(det2));
        Assert.True(det1 == det2);
        Assert.False(det1 != det2);
    }

    [Fact]
    public void Equals_DifferentGoToQuestionInstances_ReturnsFalse()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(20);

        // Act & Assert
        Assert.NotEqual(det1, det2);
        Assert.False(det1.Equals(det2));
        Assert.False(det1 == det2);
        Assert.True(det1 != det2);
    }

    [Fact]
    public void Equals_SameEndSurveyInstances_ReturnsTrue()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.End();
        var det2 = NextQuestionDeterminant.End();

        // Act & Assert
        Assert.Equal(det1, det2);
        Assert.True(det1.Equals(det2));
        Assert.True(det1 == det2);
        Assert.False(det1 != det2);
    }

    [Fact]
    public void Equals_GoToQuestionVsEndSurvey_ReturnsFalse()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.End();

        // Act & Assert
        Assert.NotEqual(det1, det2);
        Assert.False(det1.Equals(det2));
        Assert.False(det1 == det2);
        Assert.True(det1 != det2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(10);

        // Act & Assert
        Assert.False(det.Equals(null));
        Assert.False(det == null);
        Assert.True(det != null);
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        NextQuestionDeterminant? det1 = null;
        NextQuestionDeterminant? det2 = null;

        // Act & Assert
        Assert.True(det1 == det2);
        Assert.False(det1 != det2);
    }

    [Fact]
    public void Equals_WithDifferentObjectType_ReturnsFalse()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(10);
        var obj = new object();

        // Act & Assert
        Assert.False(det.Equals(obj));
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameGoToQuestionValues_ReturnsSameHash()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(10);

        // Act
        var hash1 = det1.GetHashCode();
        var hash2 = det2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentGoToQuestionValues_ReturnsDifferentHash()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(20);

        // Act
        var hash1 = det1.GetHashCode();
        var hash2 = det2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_SameEndSurveyValues_ReturnsSameHash()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.End();
        var det2 = NextQuestionDeterminant.End();

        // Act
        var hash1 = det1.GetHashCode();
        var hash2 = det2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_CanBeUsedInHashSet()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(10); // Same value
        var det3 = NextQuestionDeterminant.ToQuestion(20);
        var det4 = NextQuestionDeterminant.End();

        var hashSet = new HashSet<NextQuestionDeterminant> { det1, det2, det3, det4 };

        // Assert - det1 and det2 are considered equal, so only 3 unique items
        Assert.Equal(3, hashSet.Count);
        Assert.Contains(det1, hashSet);
        Assert.Contains(det3, hashSet);
        Assert.Contains(det4, hashSet);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void JsonSerialization_GoToQuestion_SerializesCorrectly()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(42);

        // Act
        var json = JsonSerializer.Serialize(det);
        var deserialized = JsonSerializer.Deserialize<NextQuestionDeterminant>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(det.Type, deserialized.Type);
        Assert.Equal(det.NextQuestionId, deserialized.NextQuestionId);
        Assert.Equal(det, deserialized);
    }

    [Fact]
    public void JsonSerialization_EndSurvey_SerializesCorrectly()
    {
        // Arrange
        var det = NextQuestionDeterminant.End();

        // Act
        var json = JsonSerializer.Serialize(det);
        var deserialized = JsonSerializer.Deserialize<NextQuestionDeterminant>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(det.Type, deserialized.Type);
        Assert.Null(deserialized.NextQuestionId);
        Assert.Equal(det, deserialized);
    }

    [Fact]
    public void JsonSerialization_GoToQuestion_HasExpectedJsonStructure()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(42);

        // Act
        var json = JsonSerializer.Serialize(det);

        // Assert
        Assert.Contains("\"type\":0", json); // GoToQuestion = 0
        Assert.Contains("\"nextQuestionId\":42", json);
    }

    [Fact]
    public void JsonSerialization_EndSurvey_HasExpectedJsonStructure()
    {
        // Arrange
        var det = NextQuestionDeterminant.End();

        // Act
        var json = JsonSerializer.Serialize(det);

        // Assert
        Assert.Contains("\"type\":1", json); // EndSurvey = 1
        Assert.Contains("\"nextQuestionId\":null", json);
    }

    [Fact]
    public void JsonDeserialization_ValidGoToQuestionJson_DeserializesCorrectly()
    {
        // Arrange
        const string json = "{\"type\":0,\"nextQuestionId\":42}";

        // Act
        var det = JsonSerializer.Deserialize<NextQuestionDeterminant>(json);

        // Assert
        Assert.NotNull(det);
        Assert.Equal(NextStepType.GoToQuestion, det.Type);
        Assert.Equal(42, det.NextQuestionId);
    }

    [Fact]
    public void JsonDeserialization_ValidEndSurveyJson_DeserializesCorrectly()
    {
        // Arrange
        const string json = "{\"type\":1,\"nextQuestionId\":null}";

        // Act
        var det = JsonSerializer.Deserialize<NextQuestionDeterminant>(json);

        // Assert
        Assert.NotNull(det);
        Assert.Equal(NextStepType.EndSurvey, det.Type);
        Assert.Null(det.NextQuestionId);
    }

    [Fact]
    public void JsonDeserialization_InvalidGoToQuestionWithNullId_ThrowsInvalidOperationException()
    {
        // Arrange - GoToQuestion with null NextQuestionId (violates invariant)
        const string json = "{\"type\":0,\"nextQuestionId\":null}";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NextQuestionDeterminant>(json));

        // Inner exception should be InvalidOperationException from ValidateInvariants
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void JsonDeserialization_InvalidEndSurveyWithQuestionId_ThrowsInvalidOperationException()
    {
        // Arrange - EndSurvey with NextQuestionId set (violates invariant)
        const string json = "{\"type\":1,\"nextQuestionId\":42}";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NextQuestionDeterminant>(json));

        // Inner exception should be InvalidOperationException from ValidateInvariants
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_GoToQuestion_ReturnsExpectedFormat()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(42);

        // Act
        var result = det.ToString();

        // Assert
        Assert.Equal("GoToQuestion(Id: 42)", result);
    }

    [Fact]
    public void ToString_EndSurvey_ReturnsExpectedFormat()
    {
        // Arrange
        var det = NextQuestionDeterminant.End();

        // Act
        var result = det.ToString();

        // Assert
        Assert.Equal("EndSurvey", result);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ToQuestion_BoundaryValue_QuestionId1_Works()
    {
        // Arrange & Act
        var det = NextQuestionDeterminant.ToQuestion(1);

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, det.Type);
        Assert.Equal(1, det.NextQuestionId);
    }

    [Fact]
    public void ToQuestion_LargeQuestionId_Works()
    {
        // Arrange
        const int largeId = 1_000_000;

        // Act
        var det = NextQuestionDeterminant.ToQuestion(largeId);

        // Assert
        Assert.Equal(NextStepType.GoToQuestion, det.Type);
        Assert.Equal(largeId, det.NextQuestionId);
    }

    [Fact]
    public void ReferenceEquals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var det = NextQuestionDeterminant.ToQuestion(10);

        // Act & Assert
        Assert.True(det.Equals(det)); // Same reference
        Assert.True(det == det);
    }

    [Fact]
    public void ValueObject_CanBeUsedInDictionary()
    {
        // Arrange
        var det1 = NextQuestionDeterminant.ToQuestion(10);
        var det2 = NextQuestionDeterminant.ToQuestion(10); // Same value
        var det3 = NextQuestionDeterminant.End();

        var dict = new Dictionary<NextQuestionDeterminant, string>
        {
            { det1, "First" },
            { det3, "End" }
        };

        // Act & Assert
        Assert.Equal("First", dict[det1]);
        Assert.Equal("First", dict[det2]); // det2 equals det1, so same key
        Assert.Equal("End", dict[det3]);
        Assert.Equal(2, dict.Count);
    }

    #endregion
}
