using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.DTOs
{
    public class UpdateNotificationDto : CreateNotificationDto
    {
        public string Id { get; set; }
    }
}
