using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Helpdesk.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : BaseController
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationsController(
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork) : base(currentUser)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = CurrentUserId;
            var notifications = await _unitOfWork.InAppNotifications.Query()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<InAppNotification>>.SuccessResponse(notifications));
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<object>.FailResponse("Invalid ID"));
            }

            var userId = CurrentUserId;
            var notification = await _unitOfWork.InAppNotifications.GetByIdAsync(id);

            if (notification == null)
            {
                return NotFound(ApiResponse<object>.FailResponse("Notification not found."));
            }

            if (notification.UserId != userId)
            {
                return Forbid();
            }

            notification.IsRead = true;
            await _unitOfWork.InAppNotifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Notification marked as read."));
        }

        // PUT: api/notifications/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = CurrentUserId;
            var unreadNotifications = await _unitOfWork.InAppNotifications.GetByConditionAsync(n => n.UserId == userId && !n.IsRead);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                await _unitOfWork.InAppNotifications.UpdateAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "All notifications marked as read."));
        }
    }
}
