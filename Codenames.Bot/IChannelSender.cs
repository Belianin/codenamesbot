using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot;

public interface IChannelSender
{
    Task SendGameSummaryAsync(Game game);
    Task SendVoteStartedAsync(Game game);
}