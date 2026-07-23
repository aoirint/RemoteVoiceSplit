using System;
using System.Collections.Generic;

namespace RemoteVoiceSplit.Core;

internal static class ProcessAncestry
{
    public static bool IsSelfOrDescendant(
        int candidateProcessId,
        int ancestorProcessId,
        IReadOnlyDictionary<int, int> parentByProcessId)
    {
        if (candidateProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateProcessId));
        }

        if (ancestorProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ancestorProcessId));
        }

        if (parentByProcessId is null)
        {
            throw new ArgumentNullException(nameof(parentByProcessId));
        }

        int current = candidateProcessId;
        var visited = new HashSet<int>();
        while (current > 0 && visited.Add(current))
        {
            if (current == ancestorProcessId)
            {
                return true;
            }

            if (!parentByProcessId.TryGetValue(current, out current))
            {
                return false;
            }
        }

        return false;
    }
}
