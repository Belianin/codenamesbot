using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Codenames.Bot.Commands
{
    public class SendMessageCommand : ICommand
    {
        public string Name => throw new NotImplementedException();

        public Task HandleAsync(Message message, GameUpdateHandler handler)
        {
            throw new NotImplementedException();
        }
    }
}
