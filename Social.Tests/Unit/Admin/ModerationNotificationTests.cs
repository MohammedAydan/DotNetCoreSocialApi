using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Social.Application.Features.Admin.Moderation.Commands;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Unit.Admin
{
    public class ModerationNotificationTests
    {
        private readonly IAdminRepository _adminRepo = Substitute.For<IAdminRepository>();
        private readonly IAuditLogRepository _auditRepo = Substitute.For<IAuditLogRepository>();
        private readonly INotificationRepository _notifRepo = Substitute.For<INotificationRepository>();

        [Fact]
        public async Task HidePost_WhenPostExists_HidesPost_RecordsAudit_AndDispatchesNotificationToAuthor()
        {
            // Arrange
            var post = new Post { Id = "post-100", UserId = "author-123", Content = "Test content", IsDeleted = false };
            _adminRepo.GetPostByIdAsync("post-100", Arg.Any<CancellationToken>()).Returns(post);
            _adminRepo.HidePostAsync("post-100", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new HidePostCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new HidePostCommand("admin-1", "admin@social.com", "post-100", "Inappropriate content");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).HidePostAsync("post-100", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.AdminId == "admin-1" &&
                a.ActionType == "PostHidden" &&
                a.TargetId == "post-100" &&
                a.Reason == "Inappropriate content"), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-123" &&
                n.UserId == "admin-1" &&
                n.Type == "ModerationNotice" &&
                n.PostId == "post-100" &&
                n.Message != null && n.Message.Contains("Inappropriate content")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HidePost_WhenNotificationThrows_StillReturnsTrueAndRecordsAudit()
        {
            // Arrange
            var post = new Post { Id = "post-101", UserId = "author-123", Content = "Test content", IsDeleted = false };
            _adminRepo.GetPostByIdAsync("post-101", Arg.Any<CancellationToken>()).Returns(post);
            _adminRepo.HidePostAsync("post-101", Arg.Any<CancellationToken>()).Returns(true);
            _notifRepo.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
                      .ThrowsAsync(new InvalidOperationException("Notification service temporarily down"));

            var handler = new HidePostCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new HidePostCommand("admin-1", "admin@social.com", "post-101", "Spam");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert - Should not throw and successfully complete
            result.Should().BeTrue();
            await _adminRepo.Received(1).HidePostAsync("post-101", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.TargetId == "post-101"), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestorePost_WhenPostExists_RestoresPost_RecordsAudit_AndDispatchesNotificationToAuthor()
        {
            // Arrange
            var post = new Post { Id = "post-200", UserId = "author-456", Content = "Test content", IsDeleted = true };
            _adminRepo.GetPostByIdAsync("post-200", Arg.Any<CancellationToken>()).Returns(post);
            _adminRepo.RestorePostAsync("post-200", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new RestorePostCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new RestorePostCommand("admin-1", "admin@social.com", "post-200", "Appeal approved");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).RestorePostAsync("post-200", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.AdminId == "admin-1" &&
                a.ActionType == "PostRestored" &&
                a.TargetId == "post-200" &&
                a.Reason == "Appeal approved"), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-456" &&
                n.Type == "ModerationNotice" &&
                n.PostId == "post-200" &&
                n.Message != null && n.Message.Contains("Appeal approved")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HideComment_WhenCommentExists_HidesComment_RecordsAudit_AndDispatchesNotificationToAuthor()
        {
            // Arrange
            var comment = new Comment { Id = "comment-100", UserId = "author-789", Content = "Harsh comment", IsDeleted = false };
            _adminRepo.GetCommentByIdAsync("comment-100", Arg.Any<CancellationToken>()).Returns(comment);
            _adminRepo.HideCommentAsync("comment-100", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new HideCommentCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new HideCommentCommand("admin-1", "admin@social.com", "comment-100", "Harassment");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).HideCommentAsync("comment-100", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.ActionType == "CommentHidden" &&
                a.TargetId == "comment-100"), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-789" &&
                n.Type == "ModerationNotice" &&
                n.CommentId == "comment-100" &&
                n.Message != null && n.Message.Contains("Harassment")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RestoreComment_WhenCommentExists_RestoresComment_RecordsAudit_AndDispatchesNotificationToAuthor()
        {
            // Arrange
            var comment = new Comment { Id = "comment-200", UserId = "author-789", Content = "Restored comment", IsDeleted = true };
            _adminRepo.GetCommentByIdAsync("comment-200", Arg.Any<CancellationToken>()).Returns(comment);
            _adminRepo.RestoreCommentAsync("comment-200", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new RestoreCommentCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new RestoreCommentCommand("admin-1", "admin@social.com", "comment-200", "Restored after review");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).RestoreCommentAsync("comment-200", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.ActionType == "CommentRestored" &&
                a.TargetId == "comment-200"), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-789" &&
                n.Type == "ModerationNotice" &&
                n.CommentId == "comment-200" &&
                n.Message != null && n.Message.Contains("Restored after review")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostVisibility_WhenValid_UpdatesVisibility_RecordsAudit_AndNotifiesAuthor()
        {
            // Arrange
            var post = new Post { Id = "post-300", UserId = "author-300", Content = "Sensitive content", Visibility = "public" };
            _adminRepo.GetPostByIdAsync("post-300", Arg.Any<CancellationToken>()).Returns(post);
            _adminRepo.UpdatePostVisibilityAsync("post-300", "followers_only", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new UpdatePostVisibilityCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new UpdatePostVisibilityCommand("admin-1", "admin@social.com", "post-300", "followers_only", "Restricted to followers");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).UpdatePostVisibilityAsync("post-300", "followers_only", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.ActionType == "PostVisibilityUpdated" &&
                a.TargetId == "post-300" &&
                a.Reason != null && a.Reason.Contains("followers_only")), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-300" &&
                n.Type == "ModerationNotice" &&
                n.PostId == "post-300" &&
                n.Message != null && n.Message.Contains("followers_only")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdatePostVisibility_WhenPostNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _adminRepo.GetPostByIdAsync("non-existent-post", Arg.Any<CancellationToken>()).Returns((Post?)null);
            var handler = new UpdatePostVisibilityCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new UpdatePostVisibilityCommand("admin-1", "admin@social.com", "non-existent-post", "private", "Reason");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeletePostPermanently_WhenPostExists_DeletesPermanently_RecordsAudit_AndNotifiesAuthor()
        {
            // Arrange
            var post = new Post { Id = "post-400", UserId = "author-400", Content = "Severe violation", IsDeleted = true };
            _adminRepo.GetPostByIdAsync("post-400", Arg.Any<CancellationToken>()).Returns(post);
            _adminRepo.DeletePostPermanentlyAsync("post-400", Arg.Any<CancellationToken>()).Returns(true);

            var handler = new DeletePostPermanentlyCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new DeletePostPermanentlyCommand("admin-1", "admin@social.com", "post-400", "Critical legal takedown");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _adminRepo.Received(1).DeletePostPermanentlyAsync("post-400", Arg.Any<CancellationToken>());
            await _auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a =>
                a.ActionType == "PostPermanentlyDeleted" &&
                a.TargetId == "post-400" &&
                a.Reason == "Critical legal takedown"), Arg.Any<CancellationToken>());

            await _notifRepo.Received(1).AddAsync(Arg.Is<Notification>(n =>
                n.RecipientId == "author-400" &&
                n.Type == "ModerationNotice" &&
                n.PostId == "post-400" &&
                n.Message != null && n.Message.Contains("Critical legal takedown")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeletePostPermanently_WhenPostNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _adminRepo.GetPostByIdAsync("unknown-post", Arg.Any<CancellationToken>()).Returns((Post?)null);
            var handler = new DeletePostPermanentlyCommandHandler(_adminRepo, _auditRepo, _notifRepo);
            var command = new DeletePostPermanentlyCommand("admin-1", "admin@social.com", "unknown-post", "Reason");

            // Act
            Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
