using System;
using System.Collections.Generic;

namespace ClubmanHard.Logic.TrackData
{
    public struct Segment
    {
        public double StartX, StartZ, EndX, EndZ;
        public double TargetOrientation;
        public double TargetSpeed; // mph in original, we might convert to km/h or keep as is

        public Segment(double startX, double startZ, double endX, double endZ, double targetOrientation, double targetSpeed)
        {
            StartX = startX;
            StartZ = startZ;
            EndX = endX;
            EndZ = endZ;
            TargetOrientation = targetOrientation;
            TargetSpeed = targetSpeed;
        }

        public bool IsInSegment(double x, double z)
        {
            // Simple bounding box check
            double minX = Math.Min(StartX, EndX);
            double maxX = Math.Max(StartX, EndX);
            double minZ = Math.Min(StartZ, EndZ);
            double maxZ = Math.Max(StartZ, EndZ);

            // Add some buffer
            double buffer = 5.0;
            return (x >= minX - buffer && x <= maxX + buffer && z >= minZ - buffer && z <= maxZ + buffer);
        }
    }

    public abstract class TrackDataBase
    {
        public abstract TimeSpan eventTime { get; }
        public abstract short numCars { get; }
        public abstract Segment[] initialsegments { get; }
        public abstract Segment[] segments { get; }
        public abstract Segment pitbox { get; }

        public (double TargetSpeed, double TargetOrientation) GetTargets(double x, double z, int lapCount)
        {
            // Check segments
            // Note: Original code likely iterates through segments.
            // We will do a simple linear search for now.
            
            Segment[] currentSegments = (lapCount <= 1) ? initialsegments : segments;

            foreach (var seg in currentSegments)
            {
                if (seg.IsInSegment(x, z))
                {
                    return (seg.TargetSpeed, seg.TargetOrientation);
                }
            }

            // Fallback: Check pitbox
            if (pitbox.IsInSegment(x, z))
            {
                return (-1, -1);
            }

            return (0, 360); // Default/Lost
        }
    }
}
