namespace CarminePeople.Application.Common.Face;

public static class FaceEmbeddingMatcher
{
    // Tuned against MobileFaceNet-style embeddings; revisit once real enrollment data exists.
    public const double DefaultThreshold = 0.6;

    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    // Server recomputes this itself from the two embeddings — a client's claimed match is never trusted.
    public static bool IsMatch(float[] reference, float[] candidate, double threshold = DefaultThreshold) =>
        reference.Length == candidate.Length && CosineSimilarity(reference, candidate) >= threshold;
}
