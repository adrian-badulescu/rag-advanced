namespace DefaultNamespace;

public class MathUtils
{
    public static float GetCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("The vectors must have the same dimension");
        }

        float dotProduct = 0.0f;
        float normA = 0.0f;
        float normB = 0.0f;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }
        
        if (normA == 0.0f || normB == 0.0f)
            return 0.0f; 
        
        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}