namespace ARPG
{
    /// <summary>
    /// Rolling frame-time statistics over the last <see cref="Capacity"/> frames, for the performance overlay.
    /// A fixed ring buffer, so recording a frame never allocates.
    /// </summary>
    public class FrameStats
    {
        readonly float[] samples;
        int next;

        public FrameStats(int capacity)
        {
            samples = new float[capacity < 1 ? 1 : capacity];
        }

        public int Capacity => samples.Length;
        public int Count { get; private set; }

        public void Record(float frameSeconds)
        {
            samples[next] = frameSeconds;
            next = (next + 1) % samples.Length;
            if (Count < samples.Length)
                Count++;
        }

        public float AverageSeconds
        {
            get
            {
                if (Count == 0)
                    return 0f;
                var sum = 0f;
                for (var i = 0; i < Count; i++)
                    sum += samples[i];
                return sum / Count;
            }
        }

        /// <summary>Frames per second from the average frame time, not the average of per-frame fps.</summary>
        public float AverageFps
        {
            get
            {
                var average = AverageSeconds;
                return average > 0f ? 1f / average : 0f;
            }
        }

        public float WorstSeconds
        {
            get
            {
                var worst = 0f;
                for (var i = 0; i < Count; i++)
                    if (samples[i] > worst)
                        worst = samples[i];
                return worst;
            }
        }

        /// <summary>How many of the recorded frames took longer than <paramref name="thresholdSeconds"/>.</summary>
        public int CountOver(float thresholdSeconds)
        {
            var over = 0;
            for (var i = 0; i < Count; i++)
                if (samples[i] > thresholdSeconds)
                    over++;
            return over;
        }
    }
}
