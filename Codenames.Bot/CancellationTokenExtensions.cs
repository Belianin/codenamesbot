using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codenames.Bot
{
    public static class CancellationTokenExtensions
    {
        public static Task WhenCanceled(this CancellationToken cancellationToken)
        {
            var tsc = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tsc.SetResult(true));
            return tsc.Task;
        }
    }
}
