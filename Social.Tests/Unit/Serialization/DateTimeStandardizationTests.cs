using FluentAssertions;
using Social.API.Serialization;
using System;
using System.Text.Json;
using Xunit;

namespace Social.Tests.Unit.Serialization
{
    public class DateTimeStandardizationTests
    {
        private readonly JsonSerializerOptions _options;

        public DateTimeStandardizationTests()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(new UtcDateTimeJsonConverter());
            _options.Converters.Add(new NullableUtcDateTimeJsonConverter());
        }

        private class TestModel
        {
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        [Fact]
        public void Serialize_UnspecifiedDateTime_OutputsStrictUtcIso8601WithZ()
        {
            // Arrange - simulates DateTime read from MySQL without timezone
            var date = new DateTime(2026, 9, 15, 4, 30, 0, DateTimeKind.Unspecified);
            var model = new TestModel { CreatedAt = date };

            // Act
            var json = JsonSerializer.Serialize(model, _options);

            // Assert
            json.Should().Contain("\"CreatedAt\":\"2026-09-15T04:30:00.000Z\"");
        }

        [Fact]
        public void Serialize_UtcDateTime_OutputsStrictUtcIso8601WithZ()
        {
            // Arrange
            var date = new DateTime(2026, 9, 15, 12, 45, 30, 123, DateTimeKind.Utc);
            var model = new TestModel { CreatedAt = date };

            // Act
            var json = JsonSerializer.Serialize(model, _options);

            // Assert
            json.Should().Contain("\"CreatedAt\":\"2026-09-15T12:45:30.123Z\"");
        }

        [Fact]
        public void Serialize_NullableDateTime_WhenNull_OutputsNull()
        {
            // Arrange
            var model = new TestModel { CreatedAt = DateTime.UtcNow, UpdatedAt = null };

            // Act
            var json = JsonSerializer.Serialize(model, _options);

            // Assert
            json.Should().Contain("\"UpdatedAt\":null");
        }

        [Fact]
        public void Serialize_NullableDateTime_WhenValuePresent_OutputsStrictUtcWithZ()
        {
            // Arrange
            var date = new DateTime(2026, 9, 15, 8, 15, 0, DateTimeKind.Utc);
            var model = new TestModel { CreatedAt = DateTime.UtcNow, UpdatedAt = date };

            // Act
            var json = JsonSerializer.Serialize(model, _options);

            // Assert
            json.Should().Contain("\"UpdatedAt\":\"2026-09-15T08:15:00.000Z\"");
        }

        [Fact]
        public void Deserialize_Iso8601StringWithZ_ProducesDateTimeWithUtcKind()
        {
            // Arrange
            var json = "{\"CreatedAt\":\"2026-09-15T10:00:00.000Z\",\"UpdatedAt\":\"2026-09-15T11:00:00.000Z\"}";

            // Act
            var model = JsonSerializer.Deserialize<TestModel>(json, _options);

            // Assert
            model.Should().NotBeNull();
            model!.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
            model.CreatedAt.Hour.Should().Be(10);
            model.UpdatedAt.Should().NotBeNull();
            model.UpdatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
            model.UpdatedAt!.Value.Hour.Should().Be(11);
        }

        [Fact]
        public void Deserialize_Iso8601StringWithoutZ_ProducesDateTimeWithUtcKind()
        {
            // Arrange - handles clients sending timestamp without explicit 'Z'
            var json = "{\"CreatedAt\":\"2026-09-15T14:30:00\",\"UpdatedAt\":null}";

            // Act
            var model = JsonSerializer.Deserialize<TestModel>(json, _options);

            // Assert
            model.Should().NotBeNull();
            model!.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
            model.CreatedAt.Hour.Should().Be(14);
            model.UpdatedAt.Should().BeNull();
        }
    }
}
