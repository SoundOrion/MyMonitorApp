using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMonitorApp.Services;

using System.Threading.Tasks;

public interface INotificationService
{
    Task SendAsync(string subject, string message);
}

