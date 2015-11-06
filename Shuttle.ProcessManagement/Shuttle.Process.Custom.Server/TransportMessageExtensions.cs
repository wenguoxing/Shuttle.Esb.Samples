﻿using Shuttle.Core.Infrastructure;
using Shuttle.ESB.Core;

namespace Shuttle.Process.Custom.Server
{
    public static class TransportMessageExtensions
    {
        public static bool IsHandledHere(this TransportMessage transportMessage)
        {
            Guard.AgainstNull(transportMessage, "transportMessage");

            foreach (var header in transportMessage.Headers)
            {
                if (header.Key.Equals("TargetSystem"))
                {
                    return header.Value.ToLower().Equals("custom");
                }
            }

            return false;
        }
    }
}